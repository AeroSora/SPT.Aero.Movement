using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace Aero.Movement.Jump
{
    internal static class AirStrafingCompat
    {
        internal const string Guid = "com.dark.airstrafing";

        private static bool _resolved;
        private static ConfigEntry<bool> _enabled;
        private static bool? _lastOwned;

        internal static bool OwnsAirStep
        {
            get
            {
                Resolve();
                bool owned = _enabled != null && _enabled.Value;
                if (_lastOwned != owned)
                {
                    _lastOwned = owned;
                    if (_enabled != null)
                        Plugin.Log.LogInfo(owned
                            ? "[AirStrafing] Air Strafing's air control is on: it moves the player in the air and Aero.Movement's jump curve stands aside"
                            : "[AirStrafing] Air Strafing's air control is off: Aero.Movement's jump curve moves the player in the air");
                }
                return owned;
            }
        }

        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;
            if (!Chainloader.PluginInfos.TryGetValue(Guid, out var info) || info.Instance == null) return;
            if (info.Instance.Config.TryGetEntry(new ConfigDefinition("1. Air Strafing", "Enable air strafing"), out ConfigEntry<bool> entry))
            {
                _enabled = entry;
                Plugin.Log.LogInfo($"[AirStrafing] Air Strafing {info.Metadata.Version} found");
            }
            else
            {
                Plugin.Log.LogWarning($"[AirStrafing] Air Strafing {info.Metadata.Version} found, but its \"Enable air strafing\" setting was not; Aero.Movement's jump curve stays on and both will move the player in the air");
            }
        }
    }
}
