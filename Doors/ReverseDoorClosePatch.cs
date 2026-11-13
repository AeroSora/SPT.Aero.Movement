using EFT.Interactive;
using HarmonyLib;

namespace Aero.Movement.Doors
{
    internal static class ReverseDoorClosePatch
    {
        [HarmonyPatch(typeof(WorldInteractiveObject), nameof(WorldInteractiveObject.SetDoorState))]
        [HarmonyPrefix]
        private static bool GuardReverseDoor(WorldInteractiveObject __instance, EDoorState state, bool force)
        {
            if (force) return true;
            if (!(__instance is Door door)) return true;
            if (!DoorBreacher.ReverseOpened.Contains(door.Id)) return true;

            Diag.Log($"[DoorKick] SetDoorState({state}) on reverse door '{door.Id}' (DoorState={door.DoorState} angle={door.CurrentAngle:F0}) — taking over");

            if (state == EDoorState.Shut)
            {
                if (!door.CanStartInteraction(EDoorState.Shut))
                    return false;
                DoorBreacher.ReverseClose(door);
                return false;
            }
            if (state == EDoorState.Open)
            {
                door.DoorState = EDoorState.Open;
                return false;
            }
            return true;
        }
    }
}
