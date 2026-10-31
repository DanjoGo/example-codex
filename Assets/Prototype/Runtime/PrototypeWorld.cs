using UnityEngine;

namespace Patio
{
    public sealed class PrototypeWorld : MonoBehaviour
    {
        public static readonly Vector3 Spawn = new Vector3(0, 2, -7);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<PrototypeWorld>() == null)
                new GameObject("Patio").AddComponent<PrototypeWorld>();
        }

        private void Awake()
        {
            foreach (var camera in FindObjectsOfType<Camera>()) camera.gameObject.SetActive(false);
            foreach (var light in FindObjectsOfType<Light>()) light.enabled = false;
            Time.fixedDeltaTime = 1f / 60f;
            Physics.defaultSolverIterations = 12;
            Physics.defaultSolverVelocityIterations = 6;
            RenderSettings.ambientLight = new Color(.47f, .49f, .52f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.44f, .49f, .51f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 24;
            RenderSettings.fogEndDistance = 62;
            var sun = new GameObject("Overcast sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(48, -32, 0);

            var concrete = Material(new Color(.43f, .44f, .41f));
            var dark = Material(new Color(.24f, .28f, .27f));
            var brick = Material(new Color(.44f, .30f, .23f));
            var yellow = Material(new Color(.66f, .54f, .24f));
            Box("Courtyard", new Vector3(0, -.3f, 0), new Vector3(26, .6f, 26), concrete);
            Box("North wall", new Vector3(0, 2, 13), new Vector3(27, 4, .6f), dark);
            Box("West wall", new Vector3(-13, 2, 0), new Vector3(.6f, 4, 26), dark);
            Box("East wall", new Vector3(13, 2, 0), new Vector3(.6f, 4, 26), dark);
            Box("South wall", new Vector3(0, 2, -13), new Vector3(26, 4, .6f), dark);
            Box("Low cover", new Vector3(-3, .55f, -2), new Vector3(4, 1.1f, .7f), yellow);
            Box("Tall cover", new Vector3(3, 1.4f, 0), new Vector3(.7f, 2.8f, 5), brick);
            Box("Climb crate", new Vector3(-5, .4f, 3), new Vector3(2, .8f, 2), yellow);
            Box("Upper platform", new Vector3(-5, .9f, 6), new Vector3(4, 1.8f, 3), concrete);
            for (int i = 0; i < 5; i++)
                Box("Step " + i, new Vector3(6, (i + 1) * .15f, 3 + i * .65f),
                    new Vector3(2.5f, (i + 1) * .3f, .7f), concrete);
            var ramp = Box("Ramp", new Vector3(8, .65f, -6), new Vector3(3, .3f, 5), dark);
            ramp.transform.rotation = Quaternion.Euler(-16, 0, 0);
            for (int i = 0; i < 7; i++)
                Box("Paint stripe", new Vector3(-9 + i * 3, .006f, -10), new Vector3(1.4f, .01f, .09f), yellow);
            for (int i = 0; i < 6; i++)
                Box("Distant building", new Vector3(-16 + i * 6, 5, 18), new Vector3(4, 10 + i % 3 * 3, 5), dark);

            var player = new GameObject("Physical player");
            player.transform.position = Spawn;
            var body = player.AddComponent<ActiveBody>();
            body.Build();
            var motor = player.AddComponent<PhysicalMotor>();
            var cameraObject = new GameObject("Retro camera");
            cameraObject.tag = "MainCamera";
            var cameraComponent = cameraObject.AddComponent<Camera>();
            cameraComponent.backgroundColor = RenderSettings.fogColor;
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.fieldOfView = 58;
            cameraComponent.nearClipPlane = .12f;
            cameraComponent.farClipPlane = 90;
            var follow = cameraObject.AddComponent<RetroCamera>();
            follow.target = motor;
            motor.view = cameraObject.transform;
        }

        public static Material Material(Color color)
        {
            var material = new Material(Resources.Load<Shader>("PatioDiffuse"));
            material.color = color;
            return material;
        }

        private GameObject Box(string label, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = label;
            box.transform.SetParent(transform);
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }
    }
}
