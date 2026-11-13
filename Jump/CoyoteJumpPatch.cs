using Comfort.Common;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace Aero.Movement.Jump
{
    internal static class CoyoteJumpPatch
    {
        [HarmonyPatch(typeof(IdlePlayerState), nameof(IdlePlayerState.Jump))]
        [HarmonyPrefix]
        private static bool CoyoteIdle(IdlePlayerState __instance) => Coyote(__instance.MovementContext);

        [HarmonyPatch(typeof(MovePlayerState), nameof(MovePlayerState.Jump))]
        [HarmonyPrefix]
        private static bool CoyoteRun(MovePlayerState __instance) => Coyote(__instance.MovementContext);

        private static bool Coyote(MovementContext mc)
        {
            if (!Settings.JumpFixes.Value) return true;
            if (!ReferenceEquals(mc, Plugin.LocalMovementContext)) return true;
            if (mc.IsGrounded) return true;
            if (mc.PoseLevel <= 0.6f) return true;
            if (Time.time - Plugin.LastGroundedAt > Tuning.CoyoteTime) return true;

            Player player = Singleton<GameWorld>.Instance?.MainPlayer;
            if (player == null || player.Velocity.y < -3f) return true;

            Diag.Log(DiagChannel.Jump, $"[JumpPress] coyote jump: airborne {Time.time - Plugin.LastGroundedAt:F2}s, vy={player.Velocity.y:F2}");
            mc.TryJump();
            return false;
        }
    }
}
