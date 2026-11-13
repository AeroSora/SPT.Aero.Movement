using Comfort.Common;
using EFT;
using UnityEngine;

namespace Aero.Movement.Climb
{
    internal static class ClimbDetection
    {
        private static GlobalConfiguration _source;
        private static float _origMinLength;
        private static float _origAutoMinLength;
        private static float _origReach;
        private static float _origAutoReach;

        internal static void Apply()
        {
            if (!Singleton<GlobalConfiguration>.Instantiated) return;
            GlobalConfiguration settings = Singleton<GlobalConfiguration>.Instance;
            GlobalConfiguration.ClimbSettings climb = settings?.VaultingSettings?.MovesSettings?.ClimbSettings;
            if (climb?.MoveRestrictions == null || climb.AutoMoveRestrictions == null) return;

            if (!ReferenceEquals(_source, settings))
            {
                _source = settings;
                _origMinLength = climb.MoveRestrictions.MinLength;
                _origAutoMinLength = climb.AutoMoveRestrictions.MinLength;
                _origReach = climb.MoveRestrictions.MinDistantToInteract;
                _origAutoReach = climb.AutoMoveRestrictions.MinDistantToInteract;
            }

            float depth = Settings.ThinLedges.Value ? Tuning.MinObstacleDepth : float.MaxValue;
            climb.MoveRestrictions.MinLength = Mathf.Min(_origMinLength, depth);
            climb.AutoMoveRestrictions.MinLength = Mathf.Min(_origAutoMinLength, depth);

            float reach = Settings.ClimbReach.Value;
            climb.MoveRestrictions.MinDistantToInteract = Mathf.Max(_origReach, reach);
            climb.AutoMoveRestrictions.MinDistantToInteract = Mathf.Max(_origAutoReach, reach);

            Diag.Log($"[ClimbDetect] obstacle depth: manual {_origMinLength:F2}->{climb.MoveRestrictions.MinLength:F2}, auto {_origAutoMinLength:F2}->{climb.AutoMoveRestrictions.MinLength:F2} " +
                     $"| reach: manual {_origReach:F2}->{climb.MoveRestrictions.MinDistantToInteract:F2}, auto {_origAutoReach:F2}->{climb.AutoMoveRestrictions.MinDistantToInteract:F2}");
        }
    }
}
