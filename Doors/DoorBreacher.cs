using Comfort.Common;
using EFT;
using EFT.GlobalEvents;
using EFT.Interactive;
using HarmonyLib;
using UnityEngine;

namespace Aero.Movement.Doors
{
    internal static class DoorBreacher
    {
        public static Door FindBreachableDoor(Player player, bool logRejects = true)
        {
            return Eligible(AimCast(player), logRejects);
        }

        private static Door Eligible(Door door, bool logRejects)
        {
            if (door == null) return null;
            bool stateOk = door.DoorState == EDoorState.Shut
                || (door.DoorState == EDoorState.Locked && Settings.KickLocked.Value);
            bool breachOk = door.CanBeBreached || Settings.KickReinforced.Value;
            if (!stateOk || !door.Operatable || !breachOk)
            {
                if (logRejects)
                    Diag.Log(DiagChannel.Jump, $"[DoorKick] door '{door.Id}' rejected: state={door.DoorState} operatable={door.Operatable} canBeBreached={door.CanBeBreached}");
                return null;
            }
            return door;
        }

        private static Door AimCast(Player player)
        {
            Vector3 origin, dir;
            Camera cam = Camera.main;
            if (cam != null)
            {
                origin = cam.transform.position;
                dir = cam.transform.forward;
            }
            else
            {
                Transform t = player.gameObject.transform;
                origin = t.position + new Vector3(0f, 1.4f, 0f);
                dir = t.forward;
            }

            if (Physics.Raycast(origin, dir, out RaycastHit hit, Tuning.KickDoorRange, LayersMaskController.PlayerStaticDoorMask))
            {
                Door door = hit.collider.GetComponentInParent<Door>();
                if (door != null) return door;
            }
            if (Physics.SphereCast(origin, 0.25f, dir, out hit, Tuning.KickDoorRange, LayersMaskController.PlayerStaticDoorMask))
                return hit.collider.GetComponentInParent<Door>();
            return null;
        }

        private static void AimRay(Player player, out Vector3 origin, out Vector3 dir)
        {
            Camera cam = Camera.main;
            if (cam != null) { origin = cam.transform.position; dir = cam.transform.forward; }
            else { Transform t = player.gameObject.transform; origin = t.position + new Vector3(0f, 1.4f, 0f); dir = t.forward; }
        }

        public static bool IsFailedKick(Player player)
        {
            AimRay(player, out Vector3 origin, out Vector3 dir);
            float range = Tuning.KickDoorRange;

            if (Physics.Raycast(origin, dir, out RaycastHit dhit, range, LayersMaskController.PlayerStaticDoorMask))
            {
                Door d = dhit.collider.GetComponentInParent<Door>();
                if (d != null)
                {
                    bool breachable = Eligible(d, logRejects: false) != null;
                    Diag.Log(DiagChannel.Jump, $"[KickFail] door '{d.Id}' in front, breachable={breachable} → {(breachable ? "success" : "FAIL")}");
                    return !breachable;
                }
            }

            bool wall = Physics.Raycast(origin, dir, out RaycastHit whit, range, LayersMaskController.PlayerStaticCollisionsMask);
            Diag.Log(DiagChannel.Jump, $"[KickFail] no door; wallInFront={wall}" + (wall ? $" ('{whit.collider.name}' @ {whit.distance:F2}m)" : "") + $" → {(wall ? "FAIL" : "success/whiff")}");
            return wall;
        }

        public static void KickOpenAwayFromPlayer(Door door, Vector3 playerPosition)
        {
            bool reverse = SwingsTowardsPlayer(door, playerPosition);
            if (reverse)
                ReverseKickOpen(door, playerPosition);
            else
                door.KickOpen(playerPosition);
            Plugin.BroadcastDoor(door.Id, playerPosition, reverse);
        }

        public static bool SwingsTowardsPlayer(Door door, Vector3 playerPosition)
        {
            Vector3 shutNormal = door.GetDoorRotation(door.GetAngle(EDoorState.Shut)) * WorldInteractiveObject.GetRotationAxis(door.DoorForward, door.transform);
            Vector3 openNormal = door.GetDoorRotation(door.GetAngle(EDoorState.Open)) * WorldInteractiveObject.GetRotationAxis(door.DoorForward, door.transform);
            Vector3 swing = shutNormal + openNormal;
            Vector3 toPlayer = playerPosition - door.transform.position;
            return Vector2.Dot(new Vector2(toPlayer.x, toPlayer.z).normalized, new Vector2(swing.x, swing.z).normalized) > 0f;
        }

        internal static readonly System.Collections.Generic.HashSet<string> ReverseOpened =
            new System.Collections.Generic.HashSet<string>();

        internal static void ReverseKickOpen(Door door, Vector3 playerPosition)
        {
            var interaction = (WorldInteractiveObject.InteractionState)AccessTools.Field(typeof(WorldInteractiveObject), "_interaction").GetValue(door);
            if (interaction.ResultState == EDoorState.Breaching && !interaction.Break)
                return;

            float swingSign = Mathf.Sign(Mathf.DeltaAngle(door.CloseAngle, door.OpenAngle));
            float mirroredAngle = door.CloseAngle - swingSign * Tuning.KickReverseAngle;
            interaction.InitSmoothOpen(EDoorState.Breaching, door.CloseAngle, mirroredAngle, door.KickCurve);
            door.StartCoroutine(door.SmoothDoorOpenCoroutine(EDoorState.Open, isLocalInteraction: true));
            door.IsBroken = true;

            var brokenDoor = Traverse.Create(door).Field("brokenDoor_0").GetValue<BrokenDoor>();
            if (brokenDoor != null)
                brokenDoor.Breach(playerPosition);
            if (door.BreachSound != null)
                door.PlaySoundAtPoint(door.GetSoundPos(), door.BreachSound, BetterAudio.AudioSourceGroupType.InteractiveObjects, 60, 1f);
            if (Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance.IsLocalGame())
                GlobalEventsController.CreateEvent<InteractiveObjectInteractionResultEvent>().Invoke(door, EDoorState.Open);

            ReverseOpened.Add(door.Id);
            Diag.Log($"[DoorKick] reverse-breached '{door.Id}' to {mirroredAngle:F0} deg (vanilla open={door.OpenAngle:F0} deg)");
        }

        internal static void ReverseClose(Door door)
        {
            var interaction = (WorldInteractiveObject.InteractionState)AccessTools.Field(typeof(WorldInteractiveObject), "_interaction").GetValue(door);
            interaction.InitSmoothOpen(EDoorState.Shut, door.CurrentAngle, door.CloseAngle);
            door.StartCoroutine(door.SmoothDoorOpenCoroutine(EDoorState.Shut, isLocalInteraction: true));
            ReverseOpened.Remove(door.Id);
            if (Singleton<GameWorld>.Instantiated && Singleton<GameWorld>.Instance.IsLocalGame())
                GlobalEventsController.CreateEvent<InteractiveObjectInteractionResultEvent>().Invoke(door, EDoorState.Shut);
            Diag.Log($"[DoorKick] reverse-closed '{door.Id}' from {door.CurrentAngle:F0} deg back to {door.CloseAngle:F0} deg");
        }
    }
}
