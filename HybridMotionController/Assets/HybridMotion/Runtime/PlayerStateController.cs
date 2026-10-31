using UnityEngine;

namespace HybridMotion
{
    public enum MovementState { Idle, Walk, Jog, Sprint, Jump, Fall, Land }

    public sealed class PlayerStateController : MonoBehaviour
    {
        [SerializeField, Min(0)] private float landingDisplayTime = .16f;
        public MovementState Current { get; private set; }
        public float LastLandingSpeed { get; private set; }
        private float landUntil;

        public void UpdateState(bool grounded, float verticalSpeed, float speed, bool walking, bool sprinting, bool landed, float impact)
        {
            if (landed) { LastLandingSpeed = impact; landUntil = Time.time + landingDisplayTime; }
            if (!grounded) Current = verticalSpeed > .05f ? MovementState.Jump : MovementState.Fall;
            else if (Time.time < landUntil) Current = MovementState.Land;
            else if (speed < .08f) Current = MovementState.Idle;
            else Current = walking ? MovementState.Walk : sprinting ? MovementState.Sprint : MovementState.Jog;
        }
    }
}
