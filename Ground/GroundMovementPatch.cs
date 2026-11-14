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
            float metresPerUnit = MetresPerUnit(mc.PoseLevel, TargetSpeed) * Mathf.Max(_floorRatio, 1f);
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
                                   $"metresPerUnit={MetresPerUnit(mc.PoseLevel, TargetSpeed):F2}");
            }
        }

        private static Vector2 _lastVelocity;
        private static float _lastWishSpeed = 0.63f;
        private static int _modelFrame = -1;
        private static float _steadyTime;

        private const float SpeedStep = 0.025f;
        private const int SpeedBuckets = 49;
        private static readonly float[,] _metresPerUnit = new float[11, SpeedBuckets];

        private static readonly float[] SeedSpeeds = { 0.05f, 0.10f, 0.15f, 0.20f, 0.25f, 0.30f, 0.35f, 0.40f };
        private static readonly float[] SeedStandingMetresPerSecond = { 0.86f, 0.95f, 1.045f, 1.17f, 1.30f, 1.455f, 1.62f, 1.80f };
        private const float StandingMetresPerUnit = 4.49f;

        private static float SeedMetresPerUnit(float pose, float speed)
        {
            float poseScale = Mathf.Lerp(2.38f, StandingMetresPerUnit, Mathf.Clamp01(pose)) / StandingMetresPerUnit;
            speed = Mathf.Max(speed, 0.01f);
            float metresPerSecond;
            if (speed >= SeedSpeeds[SeedSpeeds.Length - 1]) return StandingMetresPerUnit * poseScale;
            if (speed <= SeedSpeeds[0]) metresPerSecond = SeedStandingMetresPerSecond[0];
            else
            {
                int i = 1;
                while (SeedSpeeds[i] < speed) i++;
                float f = (speed - SeedSpeeds[i - 1]) / (SeedSpeeds[i] - SeedSpeeds[i - 1]);
                metresPerSecond = Mathf.Lerp(SeedStandingMetresPerSecond[i - 1], SeedStandingMetresPerSecond[i], f);
            }
            return metresPerSecond / speed * poseScale;
        }

        private static int PoseBucket(float pose) => Mathf.Clamp(Mathf.RoundToInt(pose * 10f), 0, 10);
        private static int SpeedBucket(float speed) => Mathf.Clamp(Mathf.RoundToInt(speed / SpeedStep), 0, SpeedBuckets - 1);

        private static float MetresPerUnit(float pose, float targetSpeed)
        {
            int p = PoseBucket(pose), s = SpeedBucket(targetSpeed);
            if (_metresPerUnit[p, s] <= 0f) _metresPerUnit[p, s] = SeedMetresPerUnit(p / 10f, Mathf.Max(s, 1) * SpeedStep);
            return _metresPerUnit[p, s];
        }

        private static float TargetSpeed => _lastWishSpeed * Mathf.Max(_floorRatio, 1f);

        private static float _lastCalibrationLog = -10f;

        private static void Calibrate(MovementContext mc, Vector3 animatorDelta, float dt)
        {
            float modelSpeed = _lastVelocity.magnitude * _floorRatio;
            float target = _lastWishSpeed * _floorRatio;
            bool steady = modelSpeed > 0.01f && target > 0f && Mathf.Abs(modelSpeed - target) < 0.02f * target;
            _steadyTime = steady ? _steadyTime + dt : 0f;
            if (_steadyTime < 0.4f) return;
            float animatorSpeed = new Vector2(animatorDelta.x, animatorDelta.z).magnitude / dt;
            if (animatorSpeed < 0.05f) return;
            int p = PoseBucket(mc.PoseLevel), s = SpeedBucket(target);
            float current = MetresPerUnit(mc.PoseLevel, target);
            float seed = SeedMetresPerUnit(p / 10f, Mathf.Max(s, 1) * SpeedStep);
            _metresPerUnit[p, s] = Mathf.Clamp(Mathf.Lerp(current, animatorSpeed / modelSpeed, 0.05f), seed * 0.7f, seed * 1.3f);
            if (Time.time - _lastCalibrationLog > 2f)
            {
                _lastCalibrationLog = Time.time;
                Diag.Log(DiagChannel.Ground, $"[GroundModel] scale pose={p / 10f:F1} speed={target:F3}: {_metresPerUnit[p, s]:F2} m/unit " +
                                             $"(seed {seed:F2}, animator {animatorSpeed:F2} m/s)");
            }
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
                Vector3 motion = new Vector3(_lastVelocity.x, 0f, _lastVelocity.y) * (_floorRatio * MetresPerUnit(mc.PoseLevel, TargetSpeed) * deltaTime);
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
