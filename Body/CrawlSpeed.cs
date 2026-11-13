using EFT;
using UnityEngine;

namespace Aero.Movement.Body
{
    internal static class CrawlSpeed
    {
        private static bool _applied;

        internal static void Update(MovementContext mc)
        {
            bool scale = mc.CurrentState is ProneMovePlayerState
                && !Mathf.Approximately(Settings.CrawlSpeed.Value, 1f)
                && !Doors.Kick.HoldsAnimatorSpeed;

            if (scale)
            {
                Animator body = BodyAnimator.GetCached(mc);
                if (body != null)
                {
                    body.speed = Settings.CrawlSpeed.Value;
                    _applied = true;
                }
            }
            else if (_applied)
            {
                if (!Doors.Kick.HoldsAnimatorSpeed)
                {
                    Animator body = BodyAnimator.GetCached(mc);
                    if (body != null) body.speed = 1f;
                }
                _applied = false;
            }
        }
    }
}
