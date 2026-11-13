using EFT;
using Comfort.Common;
using HarmonyLib;
using UnityEngine;

namespace Aero.Movement.Ground
{
    internal static class InertiaCurvePatch
    {
        [HarmonyPatch(typeof(Physical), nameof(Physical.OnWeightUpdated))]
        [HarmonyPostfix]
        private static void CurveInertia(Physical __instance)
        {
            if (!Settings.InertiaCurve.Value) return;
            if (__instance._player == null || !__instance._player.IsYourPlayer) return;

            float weight = __instance._player.InventoryController.Inventory.TotalWeight;
            float multiplier = 1f - Tuning.InertiaReduction;
            float over = weight - Tuning.InertiaWeightThreshold;
            if (over > 0f)
                multiplier += Tuning.InertiaRampStrength * (over / 10f) * (over / 10f);

            float curved = Mathf.Clamp01(__instance.Inertia * multiplier);
            if (Mathf.Approximately(curved, __instance.Inertia)) return;
            __instance.Inertia = curved;

            GlobalConfiguration.InertiaSettings inertia = Singleton<GlobalConfiguration>.Instance.Inertia;
            __instance.SprintAcceleration = VectorTools.InverseLerp(inertia.SprintAccelerationLimits, curved);
            __instance.PreSprintAcceleration = VectorTools.Evaluate(inertia.PreSprintAccelerationLimits, curved);
            float time = Mathf.Lerp(inertia.MinMovementAccelerationRangeRight.x, inertia.MaxMovementAccelerationRangeRight.x, curved);
            float value = Mathf.Lerp(inertia.MinMovementAccelerationRangeRight.y, inertia.MaxMovementAccelerationRangeRight.y, curved);
            EFTHardSettings.Instance.MovementAccelerationRange.MoveKey(1, new Keyframe(time, value));
            __instance.MoveSideInertia = VectorTools.Evaluate(inertia.SideTime, curved);
            __instance.MoveDiagonalInertia = VectorTools.Evaluate(inertia.DiagonalTime, curved);

            Diag.Log($"[Inertia] weight={weight:F1}kg multiplier={multiplier:F2} inertia={curved:F3}");
        }

        internal static void Refresh()
        {
            try
            {
                Singleton<GameWorld>.Instance?.MainPlayer?.Physical?.OnWeightUpdated();
            }
            catch { }
        }
    }
}
