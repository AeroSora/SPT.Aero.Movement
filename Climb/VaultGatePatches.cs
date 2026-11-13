using EFT.Vaulting;
using HarmonyLib;

namespace Aero.Movement.Climb
{
    internal static class VaultGatePatches
    {
        [HarmonyPatch(typeof(VaultingComponent), nameof(VaultingComponent.CanVaulting))]
        [HarmonyPrefix]
        private static bool AvailabilityGate(VaultingComponent __instance, ref bool __result)
        {
            if (!Settings.MidairGrab.Value) return true;

            __result = __instance._vaultingSettings.IsActive
                && !__instance.IsVaulting()
                && __instance._vaultingContext.MovementDirection.y >= 0f
                && !__instance._vaultingModelDebug.ObstacleCalculatorModel.IsTerrain;
            return false;
        }

        [HarmonyPatch(typeof(VaultingComponent), nameof(VaultingComponent.GetVaultingStrategy))]
        [HarmonyPrefix]
        private static bool StrategyGate(VaultingComponent __instance, ref EVaultingStrategy __result)
        {
            if (!Settings.MidairGrab.Value) return true;

            __result = EVaultingStrategy.None;
            if (!__instance.CanVaulting()) return false;

            bool stairs = __instance._vaultingModelDebug.StairsCalculatorModel.IsStairsCondition();
            bool sprintBehind = __instance._vaultingContext.IsSprintEnabled && __instance.BehindObstacleRatio > -0.5f;
            if (__instance._vaultingContext.IsAnimatorInTransitionState(0))
                return false;

            if (__instance._canVaultByStaminaGetter() && __instance._vaultingModelDebug.VaultingStatesModel.VaultState.CanMove() && !sprintBehind && !stairs)
                __result = EVaultingStrategy.Vault;
            else if (__instance._canClimbByStaminaGetter() && __instance._vaultingModelDebug.VaultingStatesModel.ClimbState.CanMove() && !__instance._vaultingContext.IsSprintEnabled && !stairs)
                __result = EVaultingStrategy.Climb;
            return false;
        }
    }
}
