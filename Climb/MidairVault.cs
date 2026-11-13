using EFT;
using UnityEngine;

namespace Aero.Movement.Climb
{
    internal static class MidairVault
    {
        private static bool _wasAirborne;
        private static float _triggeredAt = -1f;
        private static bool _engaged;
        private static float _nextAttemptAt;
        private static int _checksThisJump;
        private static float _airborneSince;

        internal static void Update(Player player, MovementContext mc)
        {
            if (!Settings.MidairGrab.Value) return;

            bool airborne = !mc.IsGrounded;
            if (airborne != _wasAirborne)
            {
                _wasAirborne = airborne;
                if (airborne)
                {
                    _checksThisJump = 0;
                    _airborneSince = Time.time;
                }
                else
                {
                    Diag.Log(DiagChannel.Climb, $"[MidairVault] landed after {Time.time - _airborneSince:F2}s with {_checksThisJump} grab check(s)");

                    if (_triggeredAt > 0f && !_engaged)
                    {
                        mc.PlayerAnimator.SetDoVault(doVaulting: false);
                        mc.PlayerAnimator.SetDoClimb(doClimb: false);
                    }
                    _triggeredAt = -1f;
                    _engaged = false;
                }
            }

            float checkInterval = 1f / Tuning.LedgeGrabCheckRate;
            float engageTimeout = Mathf.Max(0.25f, checkInterval);
            if (_triggeredAt > 0f && !_engaged)
            {
                if (mc.PlayerAnimatorGetIsVaulting())
                {
                    _engaged = true;
                    Diag.Log(DiagChannel.Climb, $"[MidairVault] engaged {Time.time - _triggeredAt:F2}s after trigger");
                }
                else if (Time.time > _triggeredAt + engageTimeout)
                {
                    mc.PlayerAnimator.SetDoVault(doVaulting: false);
                    mc.PlayerAnimator.SetDoClimb(doClimb: false);
                    _triggeredAt = -1f;
                }
            }

            if (!airborne || !Input.GetKey(Settings.GrabKey.Value.MainKey)) return;

            if (_triggeredAt > 0f || Time.time < _nextAttemptAt) return;

            _checksThisJump++;
            if (mc.TryVaulting())
            {
                bool isVault = mc.PlayerAnimator.GetDoVault();
                mc.PlayerAnimatorEnableJump(enabled: false);
                Animator body = BodyAnimator.Get(mc.PlayerAnimator);
                if (body != null)
                    body.CrossFade(isVault ? BodyAnimator.VaultStateHash : BodyAnimator.ClimbUpStateHash, 0.1f, 0);

                _triggeredAt = Time.time;
                _engaged = false;

                if (player.Physical?.HandsStamina != null)
                {
                    var hands = player.Physical.HandsStamina;
                    hands.UpdateStamina(Mathf.Max(0f, hands.Current - Tuning.LedgeGrabArmStaminaCost));
                }

                Plugin.BroadcastVault(isVault);
                Diag.Log(DiagChannel.Climb, $"[MidairVault] triggered {(isVault ? "Vault" : "Climb")} obstacleHeight={mc.VaultingHeight:F2} state={mc.CurrentState?.GetType().Name}");
            }
            else
            {
                _nextAttemptAt = Time.time + checkInterval;
            }
        }

        internal static void PlayOnObserved(Player player, bool isVault)
        {
            try
            {
                player.MovementContext?.PlayerAnimatorEnableJump(enabled: false);
                Animator body = BodyAnimator.Get(player.MovementContext?.PlayerAnimator);
                int hash = isVault ? BodyAnimator.VaultStateHash : BodyAnimator.ClimbUpStateHash;
                if (body != null && body.HasState(0, hash))
                    body.CrossFade(hash, 0.1f, 0);
            }
            catch { }
        }
    }
}
