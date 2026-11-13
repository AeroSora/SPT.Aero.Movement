using EFT;
using HarmonyLib;
using UnityEngine;

namespace Aero.Movement
{
    internal static class BodyAnimator
    {
        internal const int KickStateShortHash = 1839983112;
        internal const int KickStateFullHash = -1550166229;
        internal const int VaultStateHash = 1056007717;
        internal const int ClimbUpStateHash = 2128592042;

        private static Animator _cached;
        private static object _cachedSource;

        internal static Animator Get(object playerAnimator)
        {
            if (playerAnimator == null) return null;
            try
            {
                object wrapper = Traverse.Create(playerAnimator).Property("Animator").GetValue();
                if (wrapper == null) return null;
                if (wrapper is Animator direct) return direct;
                var t = Traverse.Create(wrapper);
                return t.Property("Animator").GetValue<Animator>() ?? t.Field("Animator_0").GetValue<Animator>();
            }
            catch { return null; }
        }

        internal static Animator GetCached(MovementContext mc)
        {
            object source = mc?.PlayerAnimator;
            if (source == null) return null;
            if (!ReferenceEquals(source, _cachedSource) || _cached == null)
            {
                _cached = Get(source);
                _cachedSource = source;
            }
            return _cached;
        }
    }
}
