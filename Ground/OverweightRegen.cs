using EFT;
using UnityEngine;

namespace Aero.Movement.Ground
{
    internal static class OverweightRegen
    {
        private static PhysicalBase _patched;

        internal static void Apply(Player player)
        {
            Physical physical = player?.Physical as Physical;
            if (physical == null || ReferenceEquals(_patched, physical)) return;
            if (!physical.Consumptions.TryGetValue(Physical.EConsumptionType.OverweightIdle, out Physical.Consumption idle)
                || idle?.Delta?.Update == null) return;

            System.Func<float> vanilla = idle.Delta.Update;
            idle.Delta.Update = () =>
            {
                try
                {
                    float percent = Settings.OverweightRegenPercent.Value;
                    if (percent <= 0f) return vanilla();
                    return physical.Stamina.SelfRestoration * (1f - Mathf.Clamp01(percent / 100f));
                }
                catch { return vanilla(); }
            };
            _patched = physical;
            Diag.Log($"[Encumbrance] overweight drain rewritten: severely overweight regenerates at {Settings.OverweightRegenPercent.Value:F0}% of normal");
        }
    }
}
