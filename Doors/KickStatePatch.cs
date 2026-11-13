using EFT;
using EFT.Interactive;
using HarmonyLib;
using UnityEngine;

namespace Aero.Movement.Doors
{
    internal static class KickStatePatch
    {
        [HarmonyPatch(typeof(MovementContext), nameof(MovementContext.ProcessStateEnter))]
        [HarmonyPrefix]
        private static bool BlockDoorlessBreachState(MovementContext __instance, BaseMovementState enteredState)
        {
            if (!(enteredState is BreachDoorState))
                return true;
            if (__instance.InteractionInfo.WorldInteractiveObject is Door)
                return true;

            Diag.Log(DiagChannel.Jump, "[Kick] suppressed door-less BreachDoorState entry");
            return false;
        }
    }
}
