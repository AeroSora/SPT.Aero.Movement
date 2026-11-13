using Comfort.Common;
using EFT;
using UnityEngine;

namespace Aero.Movement.Climb
{
    internal static class ClimbAssist
    {
        private static readonly float[] ProbeOffsets = { 0.35f, 0.6f, 0.85f };

        private static bool _wasClimbingUp;
        private static float _startY;
        private static float _targetTop;
        private static Vector3 _target;
        private static float _surfaceY;
        private static float _speed;
        private static float _until = -1f;

        internal static bool Active => _until > Time.time;

        private static float ServerClimbMaxHeight()
        {
            if (!Singleton<GlobalConfiguration>.Instantiated) return 1.31f;
            GlobalConfiguration.ClimbSettings climb = Singleton<GlobalConfiguration>.Instance?.VaultingSettings?.MovesSettings?.ClimbSettings;
            float h = 0f;
            if (climb?.MoveRestrictions != null) h = Mathf.Max(h, climb.MoveRestrictions.MaxHeight);
            if (climb?.AutoMoveRestrictions != null) h = Mathf.Max(h, climb.AutoMoveRestrictions.MaxHeight);
            return h > 0.1f ? h : 1.31f;
        }

        private static bool ProbeSurface(Vector3 feet, Vector3 fwd, float searchTop, float searchBottom, out float surfaceY, out Vector3 standPoint)
        {
            surfaceY = 0f;
            standPoint = feet;
            float rayLength = searchTop - searchBottom;
            if (rayLength <= 0f) return false;
            foreach (float ahead in ProbeOffsets)
            {
                Vector3 over = feet + fwd * ahead;
                Vector3 from = new Vector3(over.x, searchTop, over.z);
                if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, rayLength, LayersMaskController.PlayerStaticCollisionsMask)
                    && hit.normal.y > 0.5f)
                {
                    surfaceY = hit.point.y;
                    standPoint = new Vector3(over.x, surfaceY, over.z);
                    return true;
                }
                if (fwd == Vector3.zero) break;
            }
            return false;
        }

        internal static void Update(Player player, MovementContext mc)
        {
            bool climbingUp = mc.CurrentState is ClimbState;

            if (climbingUp && !_wasClimbingUp)
            {
                _startY = player.Position.y;
                _targetTop = _startY + mc.VaultingHeight;
                _until = -1f;
                Diag.Log($"[ClimbAssist] climb start: y={_startY:F2} obstacleH={mc.VaultingHeight:F2} expectedTop={_targetTop:F2} serverMax={ServerClimbMaxHeight():F2}");
            }
            else if (!climbingUp && _wasClimbingUp)
            {
                Vector3 end = player.Position;
                Vector3 fwd = mc.TransformForwardVector;
                fwd.y = 0f;
                fwd = fwd.sqrMagnitude > 0.01f ? fwd.normalized : Vector3.zero;

                float maxLift = Mathf.Min(Tuning.ClimbAssistMax, ServerClimbMaxHeight() + 0.25f);
                float searchTop = Mathf.Min(_targetTop + 0.5f, _startY + maxLift);
                bool found = ProbeSurface(end, fwd, searchTop, end.y - 0.6f, out float surfaceY, out Vector3 standPoint);
                float lift = found ? surfaceY - end.y : 0f;

                if (Settings.ClimbAssist.Value && found && lift > Tuning.ClimbSeatTolerance && lift <= maxLift)
                {
                    _surfaceY = surfaceY;
                    _target = new Vector3(standPoint.x, surfaceY + 0.1f, standPoint.z);
                    Vector3 flat = _target - end;
                    flat.y = 0f;
                    float total = lift + flat.magnitude;

                    _speed = Mathf.Clamp(total / 0.15f, 1.5f, Tuning.ClimbAssistSpeed);
                    _until = Time.time + Mathf.Min(0.8f, total / _speed + 0.15f);
                    Diag.Log($"[ClimbAssist] climb end: y={end.y:F2} surfaceY={surfaceY:F2} lift={lift:F2} fwd={flat.magnitude:F2} speed={_speed:F1}, seating for {_until - Time.time:F2}s");
                }
                else
                {
                    Diag.Log($"[ClimbAssist] climb end: y={end.y:F2} found={found} lift={lift:F2} budget={maxLift:F2}, no assist");
                }
            }

            if (_until > Time.time && !climbingUp)
            {
                Vector3 current = player.Position;
                float dt = Time.deltaTime;
                if (current.y < _surfaceY + 0.05f)
                {
                    float step = Mathf.Min(_surfaceY + 0.1f - current.y, _speed * dt);
                    mc.CharacterController.Move(Vector3.up * step, dt);
                }
                else
                {
                    Vector3 toTarget = _target - current;
                    toTarget.y = 0f;
                    if (toTarget.magnitude < 0.05f)
                    {
                        _until = -1f;
                        Diag.Log($"[ClimbAssist] seated: y={current.y:F2}");
                    }
                    else
                    {
                        mc.CharacterController.Move(Vector3.ClampMagnitude(toTarget, _speed * dt), dt);
                    }
                }
            }
            else if (_until > 0f && Time.time >= _until)
            {
                if (player.Position.y < _surfaceY - 0.1f)
                    Diag.Warn($"[ClimbAssist] seating incomplete: y={player.Position.y:F2} surfaceY={_surfaceY:F2}");
                _until = -1f;
            }

            _wasClimbingUp = climbingUp;
        }
    }
}
