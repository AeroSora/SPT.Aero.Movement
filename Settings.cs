using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Aero.Movement
{
    internal static class Settings
    {
        internal const string GroundSection = "1. Ground Movement";
        internal const string WeightSection = "2. Weight";
        internal const string JumpSection = "3. Jumping";
        internal const string ClimbSection = "4. Climbing";
        internal const string LeanSection = "5. Lean";
        internal const string ProneSection = "6. Prone";
        internal const string SprintSection = "7. Sprint";
        internal const string KickSection = "8. Door Kick";

        internal static ConfigEntry<bool> GroundModel;
        internal static ConfigEntry<float> Acceleration;
        internal static ConfigEntry<float> StoppingFriction;

        internal static ConfigEntry<bool> InertiaCurve;
        internal static ConfigEntry<float> OverweightRegenPercent;

        internal static ConfigEntry<bool> SnappierJump;
        internal static ConfigEntry<bool> JumpFixes;

        internal static ConfigEntry<bool> ThinLedges;
        internal static ConfigEntry<float> ClimbReach;
        internal static ConfigEntry<bool> ClimbAssist;
        internal static ConfigEntry<bool> MidairGrab;
        internal static ConfigEntry<KeyboardShortcut> GrabKey;

        internal static ConfigEntry<float> LeanSpeed;
        internal static ConfigEntry<float> MaxLeanAngle;

        internal static ConfigEntry<float> CrawlSpeed;

        internal static ConfigEntry<bool> SmoothSprintStop;

        internal static ConfigEntry<bool> Kick;
        internal static ConfigEntry<KeyboardShortcut> KickKey;
        internal static ConfigEntry<bool> JumpKick;
        internal static ConfigEntry<bool> KickLocked;
        internal static ConfigEntry<bool> KickReinforced;
        internal static ConfigEntry<float> KickAnimationSpeed;

        internal static void Bind(ConfigFile config)
        {
            GroundModel = config.Bind(GroundSection, "Responsive ground movement", true, Desc(
                "Replaces the game's eased walking acceleration with a responsive model: you reach full speed sooner and stop shorter, and heavy loadouts carry more momentum. Walk and run speeds are unchanged.", 3));
            Acceleration = config.Bind(GroundSection, "Acceleration", 4f, Desc(
                "How quickly you reach full walking speed. Higher is snappier. (1 to 30)", 2, new AcceptableValueRange<float>(1f, 30f)));
            StoppingFriction = config.Bind(GroundSection, "Stopping friction", 6f, Desc(
                "How quickly you come to a stop after releasing the movement keys. Higher stops shorter. (0.5 to 20)", 1, new AcceptableValueRange<float>(0.5f, 20f)));

            InertiaCurve = config.Bind(WeightSection, "Weight inertia curve", true, Desc(
                "Inertia no longer grows with every kilogram: it stays at the base level up to 60 kg and rises quickly beyond that.", 2));
            OverweightRegenPercent = config.Bind(WeightSection, "Stamina regeneration while overweight (%)", 10f, Desc(
                "Severely overweight, the game stops stamina regeneration completely; this keeps the given percentage of your normal regeneration. 0 keeps the game's behaviour. (0 to 50)", 1, new AcceptableValueRange<float>(0f, 50f)));

            SnappierJump = config.Bind(JumpSection, "Snappier jump arc", true, Desc(
                "Moving jumps rise and fall faster while reaching the same height. Jumps in place are unchanged.", 2));
            JumpFixes = config.Bind(JumpSection, "Jump fixes", true, Desc(
                "A jump while holding a movement key always gets full height and distance, a quick tap of the jump key always jumps instead of vaulting, and a jump pressed just after stepping off an edge still jumps.", 1));

            ThinLedges = config.Bind(ClimbSection, "Climb thin ledges and railings", true, Desc(
                "Thin walls, railings and narrow ledges can be climbed head-on, not only from an angle.", 5));
            ClimbReach = config.Bind(ClimbSection, "Climb reach (m)", 0.5f, Desc(
                "How far from an obstacle you can be and still climb it, in metres. 0.5 is the game's own reach. (0.3 to 2.5)", 4, new AcceptableValueRange<float>(0.3f, 2.5f)));
            ClimbAssist = config.Bind(ClimbSection, "Climb height assist", true, Desc(
                "When a climb ends below the surface you were climbing onto, you are lifted onto it, so every climb the game starts also finishes.", 3));
            MidairGrab = config.Bind(ClimbSection, "Midair ledge grab", true, Desc(
                "Grab and climb a ledge in the middle of a jump by holding the grab key with forward movement.", 2));
            GrabKey = config.Bind(ClimbSection, "Ledge grab key", new KeyboardShortcut(KeyCode.Space), Desc(
                "Hold while in the air and moving forward to grab a ledge. Space lets you keep holding the jump key.", 1));

            LeanSpeed = config.Bind(LeanSection, "Lean speed", 2f, Desc(
                "How fast you lean, as a multiple of the game's speed. (0.25 to 4)", 2, new AcceptableValueRange<float>(0.25f, 4f)));
            MaxLeanAngle = config.Bind(LeanSection, "Maximum lean angle", 30f, Desc(
                "Largest lean angle in degrees; 30 is the game's full lean. (10 to 30)", 1, new AcceptableValueRange<float>(10f, 30f)));

            CrawlSpeed = config.Bind(ProneSection, "Crawl speed", 1.3f, Desc(
                "Prone crawl speed, as a multiple of the game's; the animation speeds up with it. (0.5 to 3)", 1, new AcceptableValueRange<float>(0.5f, 3f)));

            SmoothSprintStop = config.Bind(SprintSection, "Smooth sprint stop", true, Desc(
                "Releasing forward during a straight sprint glides to a stop, as a diagonal release does, instead of halting abruptly.", 1));

            Kick = config.Bind(KickSection, "Kick", true, Desc(
                "Kick with the kick key anywhere; a door in front of you is kicked open.", 6));
            KickKey = config.Bind(KickSection, "Kick key", new KeyboardShortcut(KeyCode.V), Desc(
                "Press to kick.", 5));
            JumpKick = config.Bind(KickSection, "Jump kicks a door in front", true, Desc(
                "Pressing jump while facing a door you can kick open kicks it instead of jumping; everywhere else jump works as usual.", 4));
            KickLocked = config.Bind(KickSection, "Kick open locked doors", false, Desc(
                "Kicks also open locked doors. Off keeps keys meaningful.", 3));
            KickReinforced = config.Bind(KickSection, "Kick open reinforced doors", true, Desc(
                "Kicks also open the metal and bunker doors the game does not let you breach.", 2));
            KickAnimationSpeed = config.Bind(KickSection, "Kick animation speed", 1.25f, Desc(
                "Speed of the kick animation; the door reacts in step with it. 1 is the game's speed. (0.5 to 2)", 1, new AcceptableValueRange<float>(0.5f, 2f)));
        }

        private static ConfigDescription Desc(string text, int order, AcceptableValueBase range = null)
        {
            return new ConfigDescription(text, range, new ConfigurationManagerAttributes { Order = order });
        }
    }

    internal static class Tuning
    {
        internal const float StopSpeed = 0.3f;
        internal const float HeavyAccelerationScale = 0.4f;
        internal const float HeavyFrictionScale = 0.6f;

        internal const float InertiaReduction = 0f;
        internal const float InertiaWeightThreshold = 60f;
        internal const float InertiaRampStrength = 0.1f;

        internal const float AscentGravity = 1.2f;
        internal const float DescentGravity = 1.4f;
        internal const float MovingJumpFloorSpeed = 2.5f;
        internal const float CoyoteTime = 0.15f;

        internal const float MinObstacleDepth = 0.05f;
        internal const float ClimbSeatTolerance = 0.05f;
        internal const float ClimbAssistMax = 3f;
        internal const float ClimbAssistSpeed = 6f;
        internal const float LedgeGrabArmStaminaCost = 20f;
        internal const float LedgeGrabCheckRate = 10f;

        internal const float KickDoorRange = 2.1f;
        internal const float KickImpactDelay = 0.58f;
        internal const float KickReverseAngle = 90f;
        internal const float KickRecoilHands = 1f;
        internal const float KickRecoilCamera = 2f;
        internal const float KickFailRecoilMultiplier = 0.4f;
    }

    internal static class ConfigMigration
    {
        private static readonly string[] OldSections =
        {
            "Body", "Climb Assist", "Climb Detection", "Encumbrance", "Ground Movement", "Inertia", "Lean", "Prone",
            "Spike - Jump Curve", "Spike - Jump Feel", "Spike - Kick", "Spike - Midair Vault", "Sprint", "Sprint Ram"
        };

        internal static void Run(ConfigFile config)
        {
            var orphans = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries")?.GetValue(config) as Dictionary<ConfigDefinition, string>;
            if (orphans == null || orphans.Count == 0) return;

            int carried = 0;
            void Carry(string section, string key, ConfigEntryBase target)
            {
                if (orphans.TryGetValue(new ConfigDefinition(section, key), out string value))
                {
                    target.SetSerializedValue(value);
                    carried++;
                }
            }

            Carry("Ground Movement", "Enable Source Ground Model", Settings.GroundModel);
            Carry("Ground Movement", "Acceleration", Settings.Acceleration);
            Carry("Ground Movement", "Friction", Settings.StoppingFriction);
            Carry("Inertia", "Enable Inertia Curve", Settings.InertiaCurve);
            Carry("Spike - Jump Curve", "Enable Jump Curve", Settings.SnappierJump);
            Carry("Spike - Jump Curve", "Consistent Moving Jump", Settings.JumpFixes);
            Carry("Climb Detection", "Thin Ledge Climbing", Settings.ThinLedges);
            Carry("Climb Detection", "Climb Reach", Settings.ClimbReach);
            Carry("Climb Assist", "Climb Height Assist", Settings.ClimbAssist);
            Carry("Spike - Midair Vault", "Enable Midair Vault", Settings.MidairGrab);
            Carry("Spike - Midair Vault", "Midair Vault Key", Settings.GrabKey);
            Carry("Lean", "Lean Speed Multiplier", Settings.LeanSpeed);
            Carry("Lean", "Maximum Lean Angle", Settings.MaxLeanAngle);
            Carry("Prone", "Crawl Speed Multiplier", Settings.CrawlSpeed);
            Carry("Sprint", "Smooth Sprint Stop", Settings.SmoothSprintStop);
            Carry("Spike - Kick", "Enable Kick", Settings.Kick);
            Carry("Spike - Kick", "Kick Key", Settings.KickKey);
            Carry("Spike - Kick", "Contextual Jump Kick", Settings.JumpKick);
            Carry("Spike - Kick", "Breach Locked Doors", Settings.KickLocked);
            Carry("Spike - Kick", "Kick Non-Breachable Doors", Settings.KickReinforced);
            Carry("Spike - Kick", "Kick Animation Speed", Settings.KickAnimationSpeed);

            if (orphans.TryGetValue(new ConfigDefinition("Encumbrance", "Regenerate While Overweight"), out string regenOn)
                && orphans.TryGetValue(new ConfigDefinition("Encumbrance", "Overweight Regeneration Rate"), out string regenRate)
                && float.TryParse(regenRate, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float rate))
            {
                Settings.OverweightRegenPercent.Value = string.Equals(regenOn, "true", System.StringComparison.OrdinalIgnoreCase)
                    ? Mathf.Clamp(rate * 100f, 0f, 50f) : 0f;
                carried++;
            }

            int removed = 0;
            foreach (ConfigDefinition definition in new List<ConfigDefinition>(orphans.Keys))
            {
                if (System.Array.IndexOf(OldSections, definition.Section) >= 0)
                {
                    orphans.Remove(definition);
                    removed++;
                }
            }
            if (carried == 0 && removed == 0) return;
            config.Save();
            Plugin.Log.LogInfo($"Settings migrated from Aero.Movement 1.14: {carried} value(s) carried over, {removed} old entr{(removed == 1 ? "y" : "ies")} removed");
        }
    }
}
