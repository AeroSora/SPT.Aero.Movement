using Comfort.Common;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using UnityEngine;

namespace Aero.Movement.Doors
{
    internal static class ContextualJumpPatch
    {
        private static float _lastBlockLog = -1f;

        [HarmonyPatch(typeof(MovementContext), nameof(MovementContext.TryJump))]
        [HarmonyPrefix]
        private static bool KickInsteadOfJump(MovementContext __instance)
        {
            if (!ReferenceEquals(__instance, Plugin.LocalMovementContext)) return true;

            Player player = Singleton<GameWorld>.Instance?.MainPlayer;

            if (player != null && Time.time - _lastBlockLog > 0.25f)
            {
                bool physCanJump = player.Physical?.CanJump ?? true;
                bool mcCanJump = __instance.CanJump;
                if (!physCanJump || !mcCanJump)
                {
                    _lastBlockLog = Time.time;
                    Diag.Log(DiagChannel.Jump, $"[JumpBlock] denied: physCanJump(stamina)={physCanJump} mcCanJump={mcCanJump} pose={__instance.PoseLevel:F2} grounded={__instance.IsGrounded} state={__instance.CurrentState?.GetType().Name}");
                }
            }

            if (!Settings.JumpKick.Value || !Settings.Kick.Value) return true;
            if (Time.time < Kick.CooldownUntil) return true;
            if (player == null) return true;

            Door door = DoorBreacher.FindBreachableDoor(player, logRejects: false);
            if (door == null) return true;

            Diag.Log(DiagChannel.Jump, $"[JumpBlock] jump->kick: contextual hit door '{door.Id}'");
            Kick.TryFire(player, __instance);
            return false;
        }
    }
}
