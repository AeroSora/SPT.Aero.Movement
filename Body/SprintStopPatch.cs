using EFT;
using HarmonyLib;

namespace Aero.Movement.Body
{
    internal static class SprintStopPatch
    {
        [HarmonyPatch(typeof(MovementContext), nameof(MovementContext.PlayerAnimatorEnableInert))]
        [HarmonyPrefix]
        private static bool SmoothSprintStop(MovementContext __instance, bool enabled)
        {
            if (enabled || !Settings.SmoothSprintStop.Value) return true;
            if (!ReferenceEquals(__instance, Plugin.LocalMovementContext)) return true;
            if (!(__instance.CurrentState is SprintPlayerState)) return true;
            return false;
        }
    }
}
