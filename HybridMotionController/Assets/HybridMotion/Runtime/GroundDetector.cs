using UnityEngine;

namespace HybridMotion
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class GroundDetector : MonoBehaviour
    {
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField, Min(.02f)] private float probeDistance = .22f;
        private CharacterController controller;
        private readonly RaycastHit[] hits = new RaycastHit[24];
        public Vector3 Normal { get; private set; } = Vector3.up;
        private void Awake() { controller = GetComponent<CharacterController>(); }

        public bool Probe(out float gap)
        {
            Normal = Vector3.up;
            gap = float.PositiveInfinity;
            Vector3 center = transform.TransformPoint(controller.center);
            float radius = controller.radius * .9f;
            Vector3 foot = center - Vector3.up * controller.height * .5f;
            Vector3 origin = foot + Vector3.up * (radius + .1f);
            int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, hits,
                probeDistance + .1f, groundMask, QueryTriggerInteraction.Ignore);
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.transform.IsChildOf(transform) || Vector3.Angle(hit.normal, Vector3.up) > controller.slopeLimit) continue;
                float distance = hit.distance - .1f;
                if (distance >= gap) continue;
                gap = distance;
                Normal = hit.normal;
                found = true;
            }
            return found;
        }
    }
}
