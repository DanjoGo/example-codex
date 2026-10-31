using System.Collections.Generic;
using UnityEngine;

namespace Patio
{
    public sealed class ActiveBody : MonoBehaviour
    {
        [Range(10, 160)] public float jointStrength = 65;
        public Rigidbody Root { get; private set; }
        public Rigidbody[] Bodies { get; private set; }
        public float LimpUntil { get; private set; }
        public bool Limp => Time.time < LimpUntil;
        private readonly List<ConfigurableJoint> joints = new List<ConfigurableJoint>();
        private readonly List<Collider> colliders = new List<Collider>();
        private float phase;
        private Vector3[] restOffsets;

        public void Build()
        {
            Root = gameObject.AddComponent<Rigidbody>();
            Root.mass = 12;
            Root.interpolation = RigidbodyInterpolation.Interpolate;
            Root.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Root.maxAngularVelocity = 12;
            Root.drag = .15f;
            Root.angularDrag = 2;
            var capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = 1.05f;
            capsule.radius = .27f;
            colliders.Add(capsule);
            var coat = PrototypeWorld.Material(new Color(.26f, .34f, .30f));
            var skin = PrototypeWorld.Material(new Color(.64f, .53f, .41f));
            var trousers = PrototypeWorld.Material(new Color(.22f, .24f, .27f));
            var torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Coat visual";
            torso.transform.SetParent(transform, false);
            torso.transform.localScale = new Vector3(.55f, .8f, .38f);
            var visualCollider = torso.GetComponent<Collider>();
            visualCollider.enabled = false;
            Destroy(visualCollider);
            torso.GetComponent<Renderer>().sharedMaterial = coat;
            AddPart("Head", new Vector3(0, .66f, 0), new Vector3(.36f, .36f, .36f),
                new Vector3(0, .45f, 0), skin, 1.2f, 25);
            AddPart("Left arm", new Vector3(-.43f, -.05f, 0), new Vector3(.19f, .65f, .22f),
                new Vector3(-.36f, .23f, 0), coat, 1.2f, 65);
            AddPart("Right arm", new Vector3(.43f, -.05f, 0), new Vector3(.19f, .65f, .22f),
                new Vector3(.36f, .23f, 0), coat, 1.2f, 65);
            AddPart("Left leg", new Vector3(-.16f, -.67f, 0), new Vector3(.23f, .65f, .26f),
                new Vector3(-.16f, -.37f, 0), trousers, 2, 45);
            AddPart("Right leg", new Vector3(.16f, -.67f, 0), new Vector3(.23f, .65f, .26f),
                new Vector3(.16f, -.37f, 0), trousers, 2, 45);
            // No self-collision: adjacent body parts otherwise fight the joint solver.
            for (int i = 0; i < colliders.Count; i++)
                for (int j = i + 1; j < colliders.Count; j++)
                    Physics.IgnoreCollision(colliders[i], colliders[j]);
            Bodies = GetComponentsInChildren<Rigidbody>();
            restOffsets = new Vector3[Bodies.Length];
            for (int i = 0; i < Bodies.Length; i++)
                restOffsets[i] = transform.InverseTransformPoint(Bodies[i].position);
        }

        private void AddPart(string label, Vector3 offset, Vector3 scale, Vector3 pivot,
            Material material, float mass, float limit)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = label;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = offset;
            // Keep physics transforms unscaled; scale only the mesh child.
            var mesh = new GameObject("Mesh");
            mesh.transform.SetParent(part.transform, false);
            mesh.transform.localScale = scale;
            mesh.AddComponent<MeshFilter>().sharedMesh = part.GetComponent<MeshFilter>().sharedMesh;
            mesh.AddComponent<MeshRenderer>().sharedMaterial = material;
            Destroy(part.GetComponent<MeshRenderer>());
            Destroy(part.GetComponent<MeshFilter>());
            ((BoxCollider)part.GetComponent<Collider>()).size = scale;
            colliders.Add(part.GetComponent<Collider>());
            var rigidbody = part.AddComponent<Rigidbody>();
            rigidbody.mass = mass;
            rigidbody.angularDrag = 1;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var joint = part.AddComponent<ConfigurableJoint>();
            joint.connectedBody = Root;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = pivot - offset;
            joint.connectedAnchor = pivot;
            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Limited;
            joint.lowAngularXLimit = new SoftJointLimit { limit = -limit };
            joint.highAngularXLimit = new SoftJointLimit { limit = limit };
            joint.angularYLimit = new SoftJointLimit { limit = limit * .5f };
            joint.angularZLimit = new SoftJointLimit { limit = limit * .5f };
            joint.rotationDriveMode = RotationDriveMode.Slerp;
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = .08f;
            joint.projectionAngle = 12;
            joints.Add(joint);
        }

        public void Animate(float speed, bool cover)
        {
            phase += speed * Time.fixedDeltaTime * 3.5f;
            float swing = Mathf.Sin(phase) * Mathf.Clamp01(speed / 3) * 28;
            for (int i = 0; i < joints.Count; i++)
            {
                joints[i].slerpDrive = new JointDrive
                {
                    positionSpring = Limp ? 0 : jointStrength,
                    positionDamper = Limp ? .2f : 8,
                    maximumForce = Limp ? 0 : 180
                };
                float angle = i == 3 ? swing : i == 4 ? -swing : i == 1 ? -swing : i == 2 ? swing : 0;
                joints[i].targetRotation = Quaternion.Euler(angle + (cover && i < 3 ? -12 : 0), 0, 0);
            }
        }

        public void GoLimp(float seconds)
        {
            LimpUntil = Time.time + seconds;
        }

        public void ResetBody()
        {
            LimpUntil = 0;
            Root.velocity = Root.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(PrototypeWorld.Spawn, Quaternion.identity);
            Physics.SyncTransforms();
            for (int i = 0; i < Bodies.Length; i++)
            {
                var part = Bodies[i];
                part.position = PrototypeWorld.Spawn + restOffsets[i];
                part.rotation = Quaternion.identity;
                part.velocity = part.angularVelocity = Vector3.zero;
                part.WakeUp();
            }
        }
    }
}
