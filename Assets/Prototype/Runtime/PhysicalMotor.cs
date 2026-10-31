using System.Collections;
using UnityEngine;

namespace Patio
{
    [RequireComponent(typeof(ActiveBody))]
    public sealed class PhysicalMotor : MonoBehaviour
    {
        public Transform view;
        public float walkSpeed = 2.8f;
        public float runSpeed = 4.8f;
        public float acceleration = 13;
        public float balanceStrength = 45;
        public float balanceDamping = 9;
        public float jumpSpeed = 5;
        public bool Grounded { get; private set; }
        public bool InCover { get; private set; }
        public bool Climbing { get; private set; }
        public string Hint { get; private set; } = "Explora el patio";
        public ActiveBody Body { get; private set; }
        private Vector2 input;
        private bool jumpQueued;
        private bool sprint;
        private Vector3 coverNormal;
        private float yaw;
        private float lastJump = -10;
        private Coroutine climbRoutine;
        private const float StandingHeight = 1.06f;

        private void Awake()
        {
            Body = GetComponent<ActiveBody>();
        }

        private void Update()
        {
            input = Vector2.ClampMagnitude(new Vector2(
                (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0)), 1);
            sprint = Input.GetKey(KeyCode.LeftShift);
            if (Input.GetKeyDown(KeyCode.T)) Respawn();
            if (Climbing) return;
            if (Input.GetKeyDown(KeyCode.R))
            {
                InCover = false;
                Body.GoLimp(2);
                Body.Root.AddTorque(transform.right * 3, ForceMode.VelocityChange);
            }
            if (Body.Limp) return;
            if (Input.GetKeyDown(KeyCode.Space)) jumpQueued = true;
            if (Input.GetKeyDown(KeyCode.C)) ToggleCover();
            if (Input.GetKeyDown(KeyCode.E)) TryClimb();
        }

        private void FixedUpdate()
        {
            if (Climbing) return;
            var rb = Body.Root;
            if (rb.position.y < -12) { Respawn(); return; }
            Grounded = Cast(rb.position + Vector3.up * .15f, Vector3.down, 1.48f, out var ground)
                && ground.normal.y > .55f && ground.distance < 1.38f && Time.time - lastJump > .22f;
            Body.Animate(Vector3.ProjectOnPlane(rb.velocity, Vector3.up).magnitude, InCover);
            if (Body.Limp) { Grounded = false; jumpQueued = false; return; }

            Vector3 forward = view ? Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 desired = forward * input.y + right * input.x;
            float speed = sprint ? runSpeed : walkSpeed;
            if (InCover)
            {
                if (input.y > .4f || !Cast(rb.position, -coverNormal, 1.05f, out var wall)
                    || Mathf.Abs(wall.normal.y) > .25f || Vector3.Dot(wall.normal, coverNormal) < .8f)
                    InCover = false;
                else
                {
                    desired = Vector3.Cross(coverNormal, Vector3.up) * input.x;
                    speed = 1.5f;
                    // Spring to a safe distance from the cover surface.
                    rb.AddForce(coverNormal * ((.48f - wall.distance) * 35
                        - Vector3.Dot(rb.velocity, coverNormal) * 7), ForceMode.Acceleration);
                    yaw = Quaternion.LookRotation(-coverNormal).eulerAngles.y;
                }
            }
            if (!InCover && desired.sqrMagnitude > .01f)
                yaw = Quaternion.LookRotation(desired).eulerAngles.y;
            var horizontal = Vector3.ProjectOnPlane(rb.velocity, Vector3.up);
            var change = Vector3.ClampMagnitude(desired * speed - horizontal, acceleration * Time.fixedDeltaTime);
            rb.AddForce(change * (Grounded ? 1 : .22f), ForceMode.VelocityChange);

            // An assisted physical balance controller: no frozen rotation axes.
            Quaternion target = Quaternion.Euler(0, yaw, 0);
            Quaternion error = target * Quaternion.Inverse(rb.rotation);
            if (error.w < 0) error = new Quaternion(-error.x, -error.y, -error.z, -error.w);
            error.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180) angle -= 360;
            if (axis.sqrMagnitude > .01f)
                rb.AddTorque(Vector3.ClampMagnitude(axis * (angle * Mathf.Deg2Rad * balanceStrength)
                    - rb.angularVelocity * balanceDamping, 100), ForceMode.Acceleration);
            if (Grounded)
            {
                float height = rb.position.y - ground.point.y;
                float lift = Mathf.Clamp((StandingHeight - height) * 100 - rb.velocity.y * 14, -20, 55);
                rb.AddForce(Vector3.up * (lift + Physics.gravity.magnitude), ForceMode.Acceleration);
                if (jumpQueued)
                {
                    InCover = false;
                    rb.velocity = new Vector3(rb.velocity.x, jumpSpeed, rb.velocity.z);
                    lastJump = Time.time;
                    Grounded = false;
                }
            }
            jumpQueued = false;
            Hint = InCover ? "En cobertura · A/D deslizar · C o W salir"
                : Grounded ? "C cobertura · E subir · R soltar el cuerpo" : "En el aire";
        }

        private void ToggleCover()
        {
            if (InCover) { InCover = false; return; }
            if (!Grounded) return;
            Vector3 direction = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            if (Cast(Body.Root.position, direction, 1.1f, out var hit) && Mathf.Abs(hit.normal.y) < .2f)
            {
                coverNormal = hit.normal;
                InCover = true;
            }
            else Hint = "Acércate mirando a una pared para cubrirte";
        }

        private void TryClimb()
        {
            if (!Grounded) return;
            Vector3 forward = InCover ? -coverNormal : Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            Vector3 feet = Body.Root.position - Vector3.up * StandingHeight;
            if (!Cast(feet + Vector3.up * .35f, forward, 1.1f, out var wall)
                || Mathf.Abs(wall.normal.y) > .25f) return;
            Vector3 probe = wall.point + forward * .65f;
            probe.y = feet.y + 2.25f;
            if (!Cast(probe, Vector3.down, 2.1f, out var top) || top.normal.y < .85f) return;
            float rise = top.point.y - feet.y;
            if (rise < .3f || rise > 1.9f) return;
            Vector3 destination = top.point + Vector3.up * StandingHeight;
            // Conservatively reserve the full height and arm width of the puppet.
            if (!ClearAt(destination)) { Hint = "No hay espacio sobre el borde"; return; }
            Vector3 start = Body.Root.position;
            Vector3 high = new Vector3(start.x, destination.y + .3f, start.z);
            Vector3 across = destination + Vector3.up * .3f;
            if (!ClearPath(start, high) || !ClearPath(high, across) || !ClearPath(across, destination))
            { Hint = "La subida está bloqueada"; return; }
            climbRoutine = StartCoroutine(Climb(destination, high, across));
        }

        private bool ClearAt(Vector3 center)
        {
            foreach (var collider in Physics.OverlapCapsule(center - Vector3.up * .5f,
                center + Vector3.up * .5f, .55f, ~0, QueryTriggerInteraction.Ignore))
                if (!collider.transform.IsChildOf(transform)) return false;
            return true;
        }

        private bool ClearPath(Vector3 start, Vector3 end)
        {
            var delta = end - start;
            if (delta.sqrMagnitude < .0001f) return true;
            foreach (var hit in Physics.CapsuleCastAll(start - Vector3.up * .45f,
                start + Vector3.up * .45f, .32f, delta.normalized, delta.magnitude,
                ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform)) return false;
            return true;
        }

        private IEnumerator Climb(Vector3 destination, Vector3 high, Vector3 across)
        {
            Climbing = true;
            InCover = false;
            jumpQueued = false;
            Hint = "Subiendo";
            var bodies = Body.Bodies;
            var positions = new Vector3[bodies.Length];
            var rotations = new Quaternion[bodies.Length];
            for (int i = 0; i < bodies.Length; i++)
            {
                positions[i] = bodies[i].position;
                rotations[i] = bodies[i].rotation;
                bodies[i].velocity = bodies[i].angularVelocity = Vector3.zero;
                bodies[i].collisionDetectionMode = CollisionDetectionMode.Discrete;
                bodies[i].isKinematic = true;
            }
            Vector3 start = Body.Root.position;
            for (float elapsed = 0; elapsed < .8f; elapsed += Time.fixedDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / .8f) * 3;
                Vector3 next = t < 1 ? Vector3.Lerp(start, high, t)
                    : t < 2 ? Vector3.Lerp(high, across, t - 1) : Vector3.Lerp(across, destination, t - 2);
                for (int i = 0; i < bodies.Length; i++)
                {
                    bodies[i].MovePosition(positions[i] + next - start);
                    bodies[i].MoveRotation(rotations[i]);
                }
                yield return new WaitForFixedUpdate();
            }
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].position = positions[i] + destination - start;
                bodies[i].isKinematic = false;
                bodies[i].collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                bodies[i].velocity = bodies[i].angularVelocity = Vector3.zero;
            }
            Climbing = false;
            climbRoutine = null;
        }

        // Ignore every collider of this articulated character in environment queries.
        public bool Cast(Vector3 origin, Vector3 direction, float distance, out RaycastHit closest)
        {
            closest = default;
            float best = float.PositiveInfinity;
            foreach (var hit in Physics.RaycastAll(origin, direction, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(transform) || hit.distance >= best) continue;
                closest = hit;
                best = hit.distance;
            }
            return !float.IsPositiveInfinity(best);
        }

        private void Respawn()
        {
            if (climbRoutine != null) StopCoroutine(climbRoutine);
            climbRoutine = null;
            foreach (var rb in Body.Bodies)
            {
                rb.isKinematic = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            Climbing = InCover = Grounded = jumpQueued = false;
            yaw = 0;
            Body.ResetBody();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!Climbing && collision.relativeVelocity.magnitude > 8)
            {
                InCover = false;
                Body.GoLimp(1.2f);
            }
        }
    }
}
