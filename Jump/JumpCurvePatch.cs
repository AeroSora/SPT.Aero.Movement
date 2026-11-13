using EFT;
using HarmonyLib;
using UnityEngine;

namespace Aero.Movement.Jump
{
    internal static class JumpCurvePatch
    {
        [HarmonyPatch(typeof(JumpPlayerState), nameof(JumpPlayerState.Enter))]
        [HarmonyPostfix]
        private static void ConsistentMovingJump(JumpPlayerState __instance)
        {
            if (!Settings.SnappierJump.Value || !Settings.JumpFixes.Value) return;

            if (!ReferenceEquals(__instance.MovementContext, Plugin.LocalMovementContext)) return;
            float floor = Tuning.MovingJumpFloorSpeed;
            float old = __instance._initialMagnitude;
            if (old <= 0.1f || old >= floor) return;

            AnimationCurve liftCurve = EFTHardSettings.Instance.LIFT_VELOCITY_BY_SPEED;
            float oldEval = Mathf.Max(0.01f, liftCurve.Evaluate(old));
            float newEval = liftCurve.Evaluate(floor);

            __instance._initialMagnitude = floor;
            __instance._currentMagnitude = floor;
            Vector3 lift = __instance._liftForce;
            lift.y *= newEval / oldEval;
            __instance._liftForce = lift;
            __instance._liftDelay = EFTHardSettings.Instance.JUMP_DELAY_BY_SPEED.Evaluate(floor);

            Diag.Log(DiagChannel.Jump, $"[JumpDiag] CONSISTENT moving-jump floor: speed {old:F2}->{floor:F2}, lift {oldEval:F2}->{newEval:F2}");
        }

        [HarmonyPatch(typeof(JumpPlayerState), nameof(JumpPlayerState.ProcessJumpMovement))]
        [HarmonyPrefix]
        private static bool CurvedJumpVelocity(JumpPlayerState __instance, float deltaTime)
        {
            if (!Settings.SnappierJump.Value)
                return true;

            if (!ReferenceEquals(__instance.MovementContext, Plugin.LocalMovementContext))
                return true;

            if (__instance._initialMagnitude <= 0.5f)
                return true;

            var mc = __instance.MovementContext;
            float num = __instance._stateTime - __instance._liftDelay;

            if (__instance._stateTime < __instance._liftDelay)
            {
                __instance._currentLiftforce = Vector3.zero;
            }
            else
            {
                float g = -Physics.gravity.y;
                float asc = Tuning.AscentGravity;
                float desc = Tuning.DescentGravity;
                float v0 = __instance._liftForce.y * __instance._skilledBoost * Mathf.Sqrt(asc);
                float tApex = v0 / (g * asc);
                float vy = (num < tApex)
                    ? v0 - g * asc * num
                    : -g * desc * (num - tApex);
                __instance._currentLiftforce = new Vector3(0f, vy, 0f);
            }

            Vector3 rhs = (Utils.IsZero(__instance._moveDirection)
                ? Vector3.zero
                : mc.TransformVector(new Vector3(__instance._moveDirection.x, 0f, __instance._moveDirection.y).normalized));
            float num2 = Vector3.Dot(__instance._initialNormalizedDirection, rhs);
            if (mc.TransformPosition.y < __instance._potentialH)
            {
                __instance._currentLiftforce.y += (__instance._potentialH - mc.TransformPosition.y) / deltaTime * 0.9f;
            }
            float b = ((num2 < -0.966f)
                ? (__instance._initialMagnitude * EFTHardSettings.Instance.AIR_CONTROL_BACK_DIR)
                : ((!(num2 > 0.966f))
                    ? (__instance._initialMagnitude * EFTHardSettings.Instance.AIR_CONTROL_NONE_OR_ORT_DIR)
                    : Mathf.Max(EFTHardSettings.Instance.AIR_MIN_SPEED, __instance._initialMagnitude * EFTHardSettings.Instance.AIR_CONTROL_SAME_DIR)));
            if (__instance._jumpState == JumpPlayerState.EJumpState.Jump)
            {
                __instance._currentMagnitude = Mathf.Lerp(__instance._currentMagnitude, b, deltaTime * EFTHardSettings.Instance.AIR_LERP);
            }
            Vector3 motion = (__instance._initialNormalizedDirection * __instance._currentMagnitude + __instance._currentLiftforce) * deltaTime;
            if (__instance._jumpState == JumpPlayerState.EJumpState.Bumbped)
            {
                motion.y = Mathf.Min(motion.y, 0f);
            }
            float y = mc.TransformPosition.y;
            __instance._potentialH = mc.TransformPosition.y + __instance._currentLiftforce.y * deltaTime;
            __instance.LimitMotion(ref motion, deltaTime);
            mc.ApplyMotion(motion, deltaTime);
            if (mc.TransformPosition.y - y < 0.001f || __instance._currentLiftforce.y < 0f)
            {
                __instance._timeDescending += deltaTime;
            }
            return false;
        }
    }
}
