using Comfort.Common;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace Aero.Movement.Jump
{
    internal static class ReliableJumpPatch
    {
        private static readonly AccessTools.FieldRef<Player, float> VaultingTiming =
            AccessTools.FieldRefAccess<Player, float>("_vaultingTiming");

        private static float _lastJumpCall = -10f;

        [HarmonyPatch(typeof(Player), nameof(Player.Jump))]
        [HarmonyPrefix]
        private static void ResetVaultTimingOnTap(Player __instance)
        {
            try
            {
                if (__instance == null || !__instance.IsYourPlayer) return;

                MovementContext mc = __instance.MovementContext;
                float vaultInputTime = Singleton<GlobalConfiguration>.Instance.VaultingSettings.VaultingInputTime;
                bool grounded = mc.IsGrounded;
                float timing = VaultingTiming(__instance);
                bool freshPress = Time.time - _lastJumpCall > 0.1f;
                _lastJumpCall = Time.time;
                bool reset = Settings.JumpFixes.Value && freshPress && grounded && timing > vaultInputTime;
                if (reset)
                    VaultingTiming(__instance) = 0f;
            }
            catch { }
        }
    }
}
