using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using HarmonyLib;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Aero.Movement.Fika")]

namespace Aero.Movement
{
    [BepInPlugin("aero.movement", "Aero.Movement", "0.1.0")]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static Plugin Instance;

        internal static MovementContext LocalMovementContext;

        internal static float LastGroundedAt = -10f;

        internal static bool FikaPresent;

        internal static System.Action FikaKick;
        internal static System.Action<bool> FikaVault;
        internal static System.Action<string, Vector3, bool> FikaDoor;

        internal bool ClimbAssisting => Climb.ClimbAssist.Active;

        private Player _raidPlayer;

        private void Awake()
        {
            Log = Logger;
            Instance = this;

            Settings.Bind(Config);
            ConfigMigration.Run(Config);

            Harmony.CreateAndPatchAll(typeof(Ground.GroundMovementPatch));
            Harmony.CreateAndPatchAll(typeof(Ground.InertiaCurvePatch));
            Harmony.CreateAndPatchAll(typeof(Jump.JumpCurvePatch));
            Harmony.CreateAndPatchAll(typeof(Jump.CoyoteJumpPatch));
            Harmony.CreateAndPatchAll(typeof(Jump.ReliableJumpPatch));
            Harmony.CreateAndPatchAll(typeof(Climb.VaultGatePatches));
            Harmony.CreateAndPatchAll(typeof(Body.LeanPatch));
            Harmony.CreateAndPatchAll(typeof(Body.SprintStopPatch));
            Harmony.CreateAndPatchAll(typeof(Doors.KickStatePatch));
            Harmony.CreateAndPatchAll(typeof(Doors.ContextualJumpPatch));
            Harmony.CreateAndPatchAll(typeof(Doors.ReverseDoorClosePatch));

            Settings.ThinLedges.SettingChanged += (_, __) => Climb.ClimbDetection.Apply();
            Settings.ClimbReach.SettingChanged += (_, __) => Climb.ClimbDetection.Apply();
            Settings.LeanSpeed.SettingChanged += (_, __) => Body.LeanTuning.Apply();
            Settings.MaxLeanAngle.SettingChanged += (_, __) => Body.LeanTuning.Apply();

            Settings.InertiaCurve.SettingChanged += (_, __) => Ground.InertiaCurvePatch.Refresh();

            FikaPresent = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.fika.core")
                && Shared.FikaLoader.Load(System.IO.Path.GetDirectoryName(Info.Location), "Aero.Movement.Fika.dll", "Aero.Movement.Fika.MovementSync", Log);
            Log.LogInfo($"Aero.Movement {Info.Metadata.Version} loaded" + (FikaPresent ? " with Fika sync" : ""));
        }

        private void Update()
        {
            Player player = Singleton<GameWorld>.Instance?.MainPlayer;
            MovementContext mc = player?.MovementContext;
            LocalMovementContext = mc;
            if (mc == null) return;

            if (mc.IsGrounded)
                LastGroundedAt = Time.time;

            if (!ReferenceEquals(player, _raidPlayer))
            {
                _raidPlayer = player;
                Body.LeanTuning.Apply();
                Ground.InertiaCurvePatch.Refresh();
                Climb.ClimbDetection.Apply();
                Ground.OverweightRegen.Apply(player);
                Doors.DoorBreacher.ReverseOpened.Clear();
            }

            Doors.Kick.Update(player, mc);
            Climb.MidairVault.Update(player, mc);
            Body.CrawlSpeed.Update(mc);
            Climb.ClimbAssist.Update(player, mc);
        }

        internal static void BroadcastDoor(string doorId, Vector3 kickerPosition, bool reverse)
        {
            FikaDoor?.Invoke(doorId, kickerPosition, reverse);
        }

        internal static void BroadcastKick()
        {
            FikaKick?.Invoke();
        }

        internal static void BroadcastVault(bool isVault)
        {
            FikaVault?.Invoke(isVault);
        }
    }
}
