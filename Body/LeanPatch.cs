using EFT;
using HarmonyLib;

namespace Aero.Movement.Body
{
    internal static class LeanPatch
    {
        [HarmonyPatch(typeof(MovementContext), nameof(MovementContext.SetTilt))]
        [HarmonyPrefix]
        private static void ScaleTilt(MovementContext __instance, ref float tilt)
        {
            if (!ReferenceEquals(__instance, Plugin.LocalMovementContext)) return;
            tilt *= Settings.MaxLeanAngle.Value / 30f;
        }
    }
}
