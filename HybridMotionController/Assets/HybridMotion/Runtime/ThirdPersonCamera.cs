using UnityEngine;

namespace HybridMotion
{
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private PlayerMotor target;
        [SerializeField] private PlayerInputHandler input;
        [SerializeField, Min(.5f)] private float distance = 4.5f;
        [SerializeField] private float shoulderOffset = .45f;
        [SerializeField] private float targetHeight = 1.4f;
        [SerializeField, Min(.01f)] private float followSmoothTime = .08f;
        [SerializeField, Min(.01f)] private float rotationSmoothTime = .035f;
        [SerializeField, Min(0)] private float mouseSensitivity = .12f;
        [SerializeField] private Vector2 pitchLimits = new Vector2(-20, 65);
        [SerializeField, Min(.05f)] private float collisionRadius = .22f;
        [SerializeField] private LayerMask obstacleMask = ~0;
        [SerializeField, Min(0)] private float sprintDistance = .3f;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private Vector3 focus, followVelocity;
        private float yaw, pitch = 18, smoothYaw, smoothPitch = 18, yawVelocity, pitchVelocity;
        private float currentDistance;
        private bool initialized;

        public void Configure(PlayerMotor motor, PlayerInputHandler handler) { target = motor; input = handler; }

        private void Awake()
        {
            if (!target || !input)
            { Debug.LogError("Assign player and input to ThirdPersonCamera.", this); enabled = false; return; }
            GetComponent<Camera>().nearClipPlane = .08f;
        }

        private void LateUpdate()
        {
            float dt = Mathf.Min(Time.deltaTime, .05f);
            if (dt <= 0) return;
            var delta = input.Look;
            // Mouse delta is already displacement this frame: do not multiply by deltaTime.
            yaw += delta.x * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, pitchLimits.x, pitchLimits.y);
            Vector3 desiredFocus = target.transform.position + Vector3.up * targetHeight;
            if (!initialized || Vector3.Distance(focus, desiredFocus) > 10)
            { focus = desiredFocus; followVelocity = Vector3.zero; currentDistance = distance; initialized = true; }
            focus = Vector3.SmoothDamp(focus, desiredFocus, ref followVelocity, followSmoothTime, Mathf.Infinity, dt);
            smoothYaw = Mathf.SmoothDampAngle(smoothYaw, yaw, ref yawVelocity, rotationSmoothTime, Mathf.Infinity, dt);
            smoothPitch = Mathf.SmoothDampAngle(smoothPitch, pitch, ref pitchVelocity, rotationSmoothTime, Mathf.Infinity, dt);
            Quaternion rotation = Quaternion.Euler(smoothPitch, smoothYaw, 0);
            float desiredDistance = distance + (target.State.Current == MovementState.Sprint ? sprintDistance : 0);
            currentDistance = Mathf.Lerp(currentDistance, desiredDistance, 1 - Mathf.Exp(-5 * dt));
            Vector3 offset = rotation * new Vector3(shoulderOffset, 0, -currentDistance);
            float length = offset.magnitude;
            Vector3 direction = offset / length;
            float safe = length;
            int count = Physics.SphereCastNonAlloc(focus, collisionRadius, direction, hits, length,
                obstacleMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].transform.IsChildOf(target.transform)) continue;
                safe = Mathf.Min(safe, Mathf.Max(.05f, hits[i].distance - .04f));
            }
            // Retraction is immediate for safety. The focus and orbit remain smoothed.
            transform.SetPositionAndRotation(focus + direction * safe, rotation);
        }

        private void OnValidate()
        {
            pitchLimits.y = Mathf.Max(pitchLimits.x + 1, pitchLimits.y);
        }
    }
}
