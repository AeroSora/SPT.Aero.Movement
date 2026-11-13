using System.Diagnostics;

namespace Aero.Movement
{
    internal enum DiagChannel
    {
        General,
        Ground,
        Jump,
        Climb
    }

    internal static class Diag
    {
        [Conditional("AERO_DEV")]
        internal static void Log(string message) => Log(DiagChannel.General, message);

        [Conditional("AERO_DEV")]
        internal static void Log(DiagChannel channel, string message)
        {
        }

        [Conditional("AERO_DEV")]
        internal static void Warn(string message)
        {
        }
    }
}
