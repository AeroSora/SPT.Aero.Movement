using EFT;
using EFT.Interactive;
using UnityEngine;

namespace Aero.Movement.Doors
{
    internal static class Kick
    {
        private static float _cooldownUntil = -1f;
        private static Door _pendingDoor;
        private static Vector3 _pendingPosition;
        private static float _impactAt;
        private static bool _impactPending;
        private static bool _impactFail;
        private static Animator _speedBody;
        private static float _speedRestoreAt;

        internal static float CooldownUntil => _cooldownUntil;
        internal static bool HoldsAnimatorSpeed => _speedBody != null;

        internal static void TryFire(Player player, MovementContext mc)
        {
            if (!Settings.Kick.Value || Time.time < _cooldownUntil) return;

            if (!mc.IsGrounded) return;

            Animator body = BodyAnimator.Get(mc.PlayerAnimator);
            if (body == null)
            {
                Diag.Warn("[Kick] body animator unavailable");
                return;
            }

            Door door = DoorBreacher.FindBreachableDoor(player);
            bool fail = door == null && DoorBreacher.IsFailedKick(player);
            mc.PlayerAnimatorSetKickSucceed(!fail);

            mc.InteractionInfo = default(InteractionInfo);
            bool useShort = body.HasState(0, BodyAnimator.KickStateShortHash);
            body.CrossFade(useShort ? BodyAnimator.KickStateShortHash : BodyAnimator.KickStateFullHash, 0.1f, 0);

            float speed = Mathf.Max(0.1f, Settings.KickAnimationSpeed.Value);
            body.speed = speed;
            _speedBody = body;
            _speedRestoreAt = Time.time + 1.4f / speed;
            _cooldownUntil = Time.time + 1.5f / speed;

            _pendingDoor = door;
            _pendingPosition = player.Position;
            _impactFail = fail;
            _impactAt = Time.time + Tuning.KickImpactDelay / speed;
            _impactPending = door != null || fail;
            Diag.Log(DiagChannel.Jump, $"[Kick] fired fail={fail} contact={_impactPending} door={(door != null ? door.Id : "none")} state={mc.CurrentState?.GetType().Name} pose={mc.PoseLevel:F2}");

            Plugin.BroadcastKick();
        }

        internal static void Update(Player player, MovementContext mc)
        {
            if (Settings.Kick.Value && Input.GetKeyDown(Settings.KickKey.Value.MainKey))
                TryFire(player, mc);

            if (_impactPending && Time.time >= _impactAt)
            {
                _impactPending = false;
                if (_pendingDoor != null)
                {
                    DoorBreacher.KickOpenAwayFromPlayer(_pendingDoor, _pendingPosition);
                    _pendingDoor = null;
                }
                float recoil = _impactFail ? Tuning.KickFailRecoilMultiplier : 1f;
                player.ProceduralWeaponAnimation?.ForceReact?.AddForce(1f, Tuning.KickRecoilHands * recoil, Tuning.KickRecoilCamera * recoil);
            }

            if (_speedBody != null && Time.time >= _speedRestoreAt)
            {
                _speedBody.speed = 1f;
                _speedBody = null;
            }
        }

        internal static void PlayOnObserved(Player player)
        {
            try
            {
                Animator body = BodyAnimator.Get(player.MovementContext?.PlayerAnimator);
                if (body != null && body.HasState(0, BodyAnimator.KickStateShortHash))
                    body.CrossFade(BodyAnimator.KickStateShortHash, 0.1f, 0);
            }
            catch { }
        }
    }
}
