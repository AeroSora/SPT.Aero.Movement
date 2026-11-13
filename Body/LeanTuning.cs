using EFT;

namespace Aero.Movement.Body
{
    internal static class LeanTuning
    {
        private static float _origTiltSpeed = float.NaN;
        private static float _origMaxTiltStep = float.NaN;

        internal static void Apply()
        {
            try
            {
                float multiplier = Settings.LeanSpeed.Value;
                EFTHardSettings hard = EFTHardSettings.Instance;
                if (hard != null)
                {
                    if (float.IsNaN(_origTiltSpeed)) _origTiltSpeed = hard.TILT_CHANGING_SPEED;
                    hard.TILT_CHANGING_SPEED = _origTiltSpeed * multiplier;
                }
                MovementContext mc = Plugin.LocalMovementContext;
                if (mc != null)
                {
                    if (float.IsNaN(_origMaxTiltStep)) _origMaxTiltStep = mc._maxTiltStep;
                    mc._maxTiltStep = _origMaxTiltStep * multiplier;
                    Diag.Log($"[Lean] speed x{multiplier:F2} (tiltStep={mc._maxTiltStep:F1}) maxAngle={Settings.MaxLeanAngle.Value:F0}");
                }
            }
            catch { }
        }
    }
}
