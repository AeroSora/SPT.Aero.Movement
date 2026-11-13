using System;
using System.IO;
using System.Reflection;
using BepInEx.Logging;

namespace Aero.Movement.Shared
{
    internal static class FikaLoader
    {
        internal static bool Load(string pluginDirectory, string fileName, string entryType, ManualLogSource log)
        {
            string path = Path.Combine(pluginDirectory ?? "", fileName);
            if (!File.Exists(path))
            {
                log.LogWarning($"Fika is installed but {fileName} is missing next to the plugin; Fika sync is off.");
                return false;
            }
            try
            {
                Type type = Assembly.LoadFrom(path).GetType(entryType, true);
                type.GetMethod("Init", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                return true;
            }
            catch (Exception ex)
            {
                Exception cause = ex is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : ex;
                log.LogError($"Fika sync failed to start: {cause.GetType().Name}: {cause.Message}");
                return false;
            }
        }
    }
}
