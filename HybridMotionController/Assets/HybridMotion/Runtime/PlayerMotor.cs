using UnityEngine;

namespace HybridMotion
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler), typeof(GroundDetector))]
    [RequireComponent(typeof(PlayerStateController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [Header("Speeds (m/s)")]
        [SerializeField, Min(0)] private float walkSpeed = 2;
        [SerializeField, Min(0)] private float jogSpeed = 4;
        [SerializeField, Min(0)] private float sprintSpeed = 6.5f;
        [SerializeField, Min(.1f)] private float acceleration = 18;
        [SerializeField, Min(.1f)] private float braking = 24;
        [SerializeField, Min(.01f)] private float rotationSmoothTime = .1f;
        [Header("Jump and ground")]
        [SerializeField, Min(.01f)] private float jumpHeight = .8f;
        [SerializeField, Min(.1f)] private float gravity = 22;
        [SerializeField, Range(0, 1)] private float airControl = .2f;
        [SerializeField, Min(1)] private float terminalSpeed = 35;
        [SerializeField, Min(0)] private float coyoteTime = .1f;
        [SerializeField, Min(0)] private float jumpBuffer = .12f;
        [SerializeField] private Vector3 respawnPoint = new Vector3(0, .2f, -7);
        private CharacterController controller;
        private PlayerInputHandler input;
        private GroundDetector ground;
        private PlayerStateController state;
        private Vector3 horizontalVelocity;
        private float verticalVelocity, rotationVelocity;
        private float lastGroundTime = -100, lastJumpPress = -100;
        private bool jumping;
        public bool Grounded { get; private set; }
        public Vector3 Velocity => controller ? controller.velocity : Vector3.zero;
        public PlayerStateController State => state;

        public void Configure(Transform cameraReference, Vector3 spawn)
        { cameraTransform = cameraReference; respawnPoint = spawn; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputHandler>();
            ground = GetComponent<GroundDetector>();
            state = GetComponent<PlayerStateController>();
            if (!cameraTransform) { Debug.LogError("Assign ThirdPersonCamera to PlayerMotor.", this); enabled = false; }
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, .05f);
            if (dt <= 0) return;
            if (transform.position.y < -20) { Respawn(); return; }
            bool wasGrounded = Grounded;
            bool support = ground.Probe(out float gap);
            bool groundedBefore = verticalVelocity <= 0 &&
                (controller.isGrounded || (support && gap <= .08f));
            if (groundedBefore) { lastGroundTime = Time.time; jumping = false; }
            if (input.JumpPressed) lastJumpPress = Time.time;

            Vector2 move = input.Move;
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            Vector3 direction = forward * move.y + Vector3.Cross(Vector3.up, forward) * move.x;
            float speed = input.Walk ? walkSpeed : input.Sprint ? sprintSpeed : jogSpeed;
            Vector3 desired = direction * speed;
            float rate = move.sqrMagnitude < .001f ? braking : acceleration;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desired, rate * dt * (groundedBefore ? 1 : airControl));
            if (direction.sqrMagnitude > .001f)
            {
                float target = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float yaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, target,
                    ref rotationVelocity, rotationSmoothTime, Mathf.Infinity, dt);
                transform.rotation = Quaternion.Euler(0, yaw, 0);
            }
            if (groundedBefore) verticalVelocity = -2;
            if (!jumping && Time.time - lastJumpPress <= jumpBuffer && Time.time - lastGroundTime <= coyoteTime)
            {
                verticalVelocity = Mathf.Sqrt(2 * gravity * jumpHeight);
                lastJumpPress = lastGroundTime = -100;
                groundedBefore = false;
                jumping = true;
            }
            else verticalVelocity = Mathf.Max(verticalVelocity - gravity * dt, -terminalSpeed);

            Vector3 travel = horizontalVelocity;
            if (groundedBefore && support)
                travel = Vector3.ProjectOnPlane(horizontalVelocity, ground.Normal).normalized * horizontalVelocity.magnitude;
            float impact = Mathf.Max(0, -verticalVelocity);
            var flags = controller.Move((travel + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0) verticalVelocity = 0;
            Grounded = (flags & CollisionFlags.Below) != 0 && verticalVelocity <= 0;
            if (Grounded) { verticalVelocity = -2; jumping = false; }
            // Remove blocked horizontal momentum; walls must not store acceleration.
            Vector3 actual = Vector3.ProjectOnPlane(controller.velocity, Vector3.up);
            if ((flags & CollisionFlags.Sides) != 0 && actual.magnitude < horizontalVelocity.magnitude)
                horizontalVelocity = actual;
            state.UpdateState(Grounded, verticalVelocity, actual.magnitude, input.Walk, input.Sprint,
                !wasGrounded && Grounded, impact);
        }

        private void Respawn()
        {
            controller.enabled = false;
            transform.position = respawnPoint;
            controller.enabled = true;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0;
            rotationVelocity = 0;
            lastGroundTime = lastJumpPress = -100;
            Grounded = jumping = false;
        }
    }
}
