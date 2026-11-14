using System;
using Comfort.Common;
using EFT;
using EFT.Vaulting;
using HarmonyLib;
using UnityEngine;

namespace Aero.Movement.Climb
{
    internal static class ClimbDetection
    {
        private static GlobalConfiguration _source;
        private static object _manual;
        private static object _auto;
        private static float _minLength = float.NaN;
        private static float _autoMinLength = float.NaN;
        private static float _reach = float.NaN;
        private static float _autoReach = float.NaN;

        private static bool _localScope;

        internal static void Apply()
        {
            if (!Singleton<GlobalConfiguration>.Instantiated) return;
            GlobalConfiguration settings = Singleton<GlobalConfiguration>.Instance;
            GlobalConfiguration.ClimbSettings climb = settings?.VaultingSettings?.MovesSettings?.ClimbSettings;
            if (climb?.MoveRestrictions == null || climb.AutoMoveRestrictions == null) return;

            _source = settings;
            _manual = climb.MoveRestrictions;
            _auto = climb.AutoMoveRestrictions;

            float depth = Settings.ThinLedges.Value ? Tuning.MinObstacleDepth : float.MaxValue;
            _minLength = Mathf.Min(climb.MoveRestrictions.MinLength, depth);
            _autoMinLength = Mathf.Min(climb.AutoMoveRestrictions.MinLength, depth);

            float reach = Settings.ClimbReach.Value;
            _reach = Mathf.Max(climb.MoveRestrictions.MinDistantToInteract, reach);
            _autoReach = Mathf.Max(climb.AutoMoveRestrictions.MinDistantToInteract, reach);

            Diag.Log($"[ClimbDetect] local player only: obstacle depth manual {climb.MoveRestrictions.MinLength:F2}->{_minLength:F2}, auto {climb.AutoMoveRestrictions.MinLength:F2}->{_autoMinLength:F2} " +
                     $"| reach manual {climb.MoveRestrictions.MinDistantToInteract:F2}->{_reach:F2}, auto {climb.AutoMoveRestrictions.MinDistantToInteract:F2}->{_autoReach:F2}");
        }

        internal static float LocalMinLength(float shared) => float.IsNaN(_minLength) ? shared : _minLength;

        internal static float LocalReach(float shared) => float.IsNaN(_reach) ? shared : _reach;

        internal static bool IsLocal(VaultingComponent component)
            => component != null && Plugin.LocalMovementContext != null
               && ReferenceEquals(component._vaultingContext, Plugin.LocalMovementContext);

        private static bool Active => _localScope && _source != null && ReferenceEquals(_source, Singleton<GlobalConfiguration>.Instance);

        [HarmonyPatch(typeof(VaultingComponent), nameof(VaultingComponent.Tick))]
        [HarmonyPrefix]
        private static void TickPrefix(VaultingComponent __instance) => _localScope = IsLocal(__instance);

        [HarmonyPatch(typeof(VaultingComponent), nameof(VaultingComponent.Tick))]
        [HarmonyFinalizer]
        private static Exception TickFinalizer(Exception __exception)
        {
            _localScope = false;
            return __exception;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.InitVaultingComponent))]
        [HarmonyPrefix]
        private static void InitPrefix(Player __instance) => _localScope = __instance != null && __instance.IsYourPlayer;

        [HarmonyPatch(typeof(Player), nameof(Player.InitVaultingComponent))]
        [HarmonyFinalizer]
        private static Exception InitFinalizer(Exception __exception)
        {
            _localScope = false;
            return __exception;
        }

        [HarmonyPatch(typeof(GlobalConfiguration.MoveRestrictions), "EFT.Vaulting.IVaultingRestrictions.get_MinLength")]
        [HarmonyPostfix]
        private static void ManualMinLength(object __instance, ref float __result)
        {
            if (Active && ReferenceEquals(__instance, _manual)) __result = _minLength;
        }

        [HarmonyPatch(typeof(GlobalConfiguration.MoveRestrictions), "EFT.Vaulting.IVaultingRestrictions.get_MinDistantToInteract")]
        [HarmonyPostfix]
        private static void ManualReach(object __instance, ref float __result)
        {
            if (Active && ReferenceEquals(__instance, _manual)) __result = _reach;
        }

        [HarmonyPatch(typeof(GlobalConfiguration.AutoMoveRestrictions), "EFT.Vaulting.IVaultingRestrictions.get_MinLength")]
        [HarmonyPostfix]
        private static void AutoMinLength(object __instance, ref float __result)
        {
            if (Active && ReferenceEquals(__instance, _auto)) __result = _autoMinLength;
        }

        [HarmonyPatch(typeof(GlobalConfiguration.AutoMoveRestrictions), "EFT.Vaulting.IVaultingRestrictions.get_MinDistantToInteract")]
        [HarmonyPostfix]
        private static void AutoReach(object __instance, ref float __result)
        {
            if (Active && ReferenceEquals(__instance, _auto)) __result = _autoReach;
        }
    }
}
