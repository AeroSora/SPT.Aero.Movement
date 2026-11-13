using Comfort.Common;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace Aero.Movement.Ground
{
    internal static class GroundMovementPatch
    {
        private static float _lastLog = -10f;

        private static float _floorRatio = 1f;

        [HarmonyPatch(typeof(MovePlayerState), nameof(MovePlayerState.UpdateMovementDirection))]
        [HarmonyPrefix]
        private static bool SourceGroundStep(MovePlayerState __instance, float deltaTime)
        {
            if (!Settings.GroundModel.Value) return true;
            MovementContext mc = __instance.MovementContext;
            if (mc == null || !ReferenceEquals(mc, Plugin.LocalMovementContext) || mc.IsAI) return true;

            if (__instance is SprintPlayerState || mc.IsSprintEnabled) return true;

            try
            {
                Step(__instance, mc, deltaTime);
                return false;
            }
            catch (System.Exception ex)
            {
                Diag.Warn("[GroundModel] falling back to vanilla this frame: " + ex.Message);
                return true;
            }
        }

        [HarmonyPatch(typeof(MovePlayerState), nameof(MovePlayerState.Enter))]
        [HarmonyPostfix]
        private static void SeedVelocityAfterSprint(MovePlayerState __instance)
        {
            if (!Settings.GroundModel.Value || __instance is SprintPlayerState) return;
            MovementContext mc = __instance.MovementContext;
            if (mc == null || !ReferenceEquals(mc, Plugin.LocalMovementContext) || mc.IsAI || mc.IsInPronePose) return;
            if (mc.PreviousState == null || mc.PreviousState.Name != EPlayerState.Sprint) return;
            Vector3 world = mc.Velocity;
            float metresPerUnit = MetresPerUnit(mc.PoseLevel) * Mathf.Max(_floorRatio, 1f);
            Vector2 seeded = Vector2.ClampMagnitude(new Vector2(world.x, world.z) / metresPerUnit, 2f);
            __instance._speed = seeded;
            __instance._velocity = Vector2.zero;
            Diag.Log(DiagChannel.Ground, $"[GroundModel] sprint exit: seeded velocity {seeded.x:F2},{seeded.y:F2} ({seeded.magnitude:F2}) from {new Vector2(world.x, world.z).magnitude:F2} m/s");
        }

        private static void Step(MovePlayerState s, MovementContext mc, float dt)
        {
            GlobalConfiguration.InertiaSettings inertia = Singleton<GlobalConfiguration>.Instance.Inertia;

            s.moveTime = 0f;

            s.prevDirection = s.Direction.IsZero() ? s.prevDirection : s.Direction;
            s.TransformDirection();
            s._targetSpeed = Mathf.Clamp(mc.ClampedSpeed, 0.01f, 1f);
            float crouch = Mathf.Lerp(1f, inertia.CrouchSpeedAccelerationRange.Evaluate(mc.ClampedSpeed),
                EFTHardSettings.Instance.CrouchAccelerationWeightRange.Evaluate(1f - mc.PoseLevel));
            Vector2 wish = s.Direction.normalized * s._targetSpeed * s._speedAcceleration * crouch;

            if (s.FirstZeroDirection) s.TransitionCoef = mc.SmoothedCharacterMovementSpeed;
            else if (s.FirstNonZeroDirection) s.TransitionCoef = s._targetSpeed;
            else if (!s._previousSpeed.Equals(s._targetSpeed))
            {
                s._lowerRange = Mathf.Max(0f, mc.ClampedSpeed - EFTHardSettings.Instance.MovementAccelerationRange.Evaluate(mc.WalkInertia));
                s.TransitionCoef = Mathf.Abs(s._targetSpeed - s._speed.magnitude);
            }
            s._previousSpeed = s._targetSpeed;

            float heaviness = Mathf.Clamp01(mc.Inertia);
            float accel = Settings.Acceleration.Value * Mathf.Lerp(1f, Tuning.HeavyAccelerationScale, heaviness);
            float friction = Settings.StoppingFriction.Value * Mathf.Lerp(1f, Tuning.HeavyFrictionScale, heaviness);

            friction = Mathf.Min(friction, accel);
            Vector2 v = s._speed;

            float wishSpeed = wish.magnitude;
            float speed = v.magnitude;
            if (speed > 0f)
            {
                float control = wishSpeed > 0f ? speed : Mathf.Max(speed, Tuning.StopSpeed);
                float newSpeed = Mathf.Max(0f, speed - control * friction * dt);
                v *= newSpeed / speed;
            }

            if (wishSpeed > 0f)
            {
                Vector2 wishDir = wish / wishSpeed;
                float add = wishSpeed - Vector2.Dot(v, wishDir);
                if (add > 0f) v += wishDir * Mathf.Min(accel * wishSpeed * dt, add);
            }

            if (wishSpeed > 0f) _floorRatio = s._lowerRange > wishSpeed ? s._lowerRange / wishSpeed : 1f;
            float magnitude = v.magnitude * _floorRatio;
            s._speed = v;
            s._velocity = Vector2.zero;
            if (!mc.IsSprintEnabled)
            {
                mc.SmoothedCharacterMovementSpeed = magnitude;
            }

            if (wishSpeed > 0f) _lastWishSpeed = wishSpeed;
            if (!mc.IsInPronePose)
            {
                float reference = Mathf.Max(_lastWishSpeed, 0.01f);
                s.Direction = Vector2.ClampMagnitude(v / reference, 1f);
            }
            _lastVelocity = v;
            _modelFrame = Time.frameCount;

            s.ProcessDirection(dt, 0f);

            if (s.Direction.IsZero()) s._timeWithoutInput += dt;
            else s._timeWithoutInput = 0f;
            if (s._moveCalled) s._timeWithOutExternalMoveCall = 0f;
            else s._timeWithOutExternalMoveCall += dt;
            s._moveCalled = false;
            if (s._timeWithOutExternalMoveCall > MovePlayerState.MAX_WITHOUT_MOVE_CALL) s.Direction = Vector2.zero;

            if (Time.time - _lastLog > 1f && (wishSpeed > 0f || magnitude > 0.01f))
            {
                _lastLog = Time.time;
                Diag.Log(DiagChannel.Ground, $"[GroundModel] state={mc.CurrentState?.Name} inertia={heaviness:F2} accel={accel:F1} friction={friction:F1} " +
                                   $"wish={wishSpeed:F2} speed={magnitude:F2} target={s._targetSpeed:F2} pose={mc.PoseLevel:F2} " +
                                   $"metresPerUnit={MetresPerUnit(mc.PoseLevel):F2}");
            }
        }

        private static Vector2 _lastVelocity;
        private static float _lastWishSpeed = 0.63f;
        private static int _modelFrame = -1;
        private static float _steadyTime;
        private static readonly float[] _metresPerUnit = new float[11];

        private const float MinCalibrationSpeed = 0.3f;

        private static float SeedMetresPerUnit(float pose) => Mathf.Lerp(2.38f, 4.49f, Mathf.Clamp01(pose));

        private static float MetresPerUnit(float pose)
        {
            int bucket = Mathf.Clamp(Mathf.RoundToInt(pose * 10f), 0, 10);
            if (_metresPerUnit[bucket] <= 0f) _metresPerUnit[bucket] = SeedMetresPerUnit(bucket / 10f);
            return _metresPerUnit[bucket];
        }

        private static void Calibrate(MovementContext mc, Vector3 animatorDelta, float dt)
        {
            float modelSpeed = _lastVelocity.magnitude * _floorRatio;
            float target = _lastWishSpeed * _floorRatio;
            bool steady = modelSpeed > 0.05f && target > 0f && Mathf.Abs(modelSpeed - target) < 0.02f * target;
            _steadyTime = steady ? _steadyTime + dt : 0f;
            if (_steadyTime < 0.4f) return;

            if (target < MinCalibrationSpeed) return;
            float animatorSpeed = new Vector2(animatorDelta.x, animatorDelta.z).magnitude / dt;
            if (animatorSpeed < 0.2f) return;
            int bucket = Mathf.Clamp(Mathf.RoundToInt(mc.PoseLevel * 10f), 0, 10);
            float current = MetresPerUnit(mc.PoseLevel);
            float seed = SeedMetresPerUnit(bucket / 10f);
            _metresPerUnit[bucket] = Mathf.Clamp(Mathf.Lerp(current, animatorSpeed / modelSpeed, 0.05f), seed * 0.7f, seed * 1.3f);
        }

        [HarmonyPatch(typeof(MovePlayerState), nameof(MovePlayerState.UpdatePosition))]
        [HarmonyPrefix]
        private static bool VelocityDrivenPosition(MovePlayerState __instance, float deltaTime)
        {
            if (!Settings.GroundModel.Value) return true;
            MovementContext mc = __instance.MovementContext;

            if (_modelFrame != Time.frameCount || mc == null || !ReferenceEquals(mc, Plugin.LocalMovementContext) || deltaTime <= 0f) return true;

            if (mc.IsInPronePose) return true;
            try
            {
                Vector3 animatorDelta = mc.PlayerAnimatorDeltaPosition;
                Calibrate(mc, animatorDelta, deltaTime);
                Vector3 motion = new Vector3(_lastVelocity.x, 0f, _lastVelocity.y) * (_floorRatio * MetresPerUnit(mc.PoseLevel) * deltaTime);
                motion.y = animatorDelta.y;
                __instance.ApplyMotion(ref motion, deltaTime);
                if (!mc.IsGrounded) mc.PlayerAnimatorEnableFallingDown(true);
                return false;
            }
            catch (System.Exception ex)
            {
                Diag.Warn("[GroundModel] velocity-driven motion fell back to root motion this frame: " + ex.Message);
                return true;
            }
        }
    }
}
