using EFT;
using HarmonyLib;

namespace Aero.Movement.Body
{
    internal static class LeanTuning
    {
        private static float _origMaxTiltStep = float.NaN;

        internal static void Apply()
        {
            try
            {
                float multiplier = Settings.LeanSpeed.Value;
                MovementContext mc = Plugin.LocalMovementContext;
                if (mc != null)
                {
                    if (float.IsNaN(_origMaxTiltStep)) _origMaxTiltStep = mc._maxTiltStep;
                    mc._maxTiltStep = _origMaxTiltStep * multiplier;
                    Diag.Log($"[Lean] local player speed x{multiplier:F2} (tiltStep={mc._maxTiltStep:F1}) maxAngle={Settings.MaxLeanAngle.Value:F0}");
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(MovementContext), nameof(MovementContext.LegacySmoothTilt))]
        [HarmonyPrefix]
        private static void ScaleLocalLegacyTilt(MovementContext __instance, ref float deltaTime)
        {
            if (ReferenceEquals(__instance, Plugin.LocalMovementContext))
                deltaTime *= Settings.LeanSpeed.Value;
        }
    }
}
