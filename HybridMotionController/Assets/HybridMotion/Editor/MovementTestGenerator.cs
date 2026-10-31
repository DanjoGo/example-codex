using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HybridMotion.Editor
{
    public static class MovementTestGenerator
    {
        private const string BasePath = "Assets/HybridMotion/Generated";
        [MenuItem("Hybrid Motion/Generate MovementTest (Phase 1)")]
        public static void Generate()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play before generating the scene."); return; }
#if !ENABLE_INPUT_SYSTEM
            Debug.LogError("Enable Input System Package (New) or Both in Player > Active Input Handling, restart Unity, then generate MovementTest.");
            return;
#endif
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/HybridMotion/Input/Player.inputactions");
            if (!actions) { Debug.LogError("Player.inputactions is missing or Input System has not finished importing."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string scenePath = BasePath + "/MovementTest.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) && !EditorUtility.DisplayDialog(
                "Regenerate test scene?", "This replaces the generated scene, player prefab and materials. Save custom work elsewhere first.", "Regenerate", "Cancel")) return;
            Directory.CreateDirectory(BasePath);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = Material("Ground", new Color(.40f, .44f, .44f));
            var obstacles = Material("Obstacle", new Color(.40f, .48f, .57f));
            var accent = Material("Accent", new Color(.86f, .58f, .22f));
            var bodyMaterial = Material("Player", new Color(.24f, .55f, .69f));
            var skin = Material("Head", new Color(.73f, .61f, .48f));
            var geometry = new GameObject("Test geometry").transform;
            Box("Ground", new Vector3(0, -.25f, 0), new Vector3(36, .5f, 36), floor, geometry);
            Box("Wall", new Vector3(4, 1.5f, 0), new Vector3(.6f, 3, 8), obstacles, geometry);
            Box("Low obstacle (cover in future phase)", new Vector3(-4, .55f, -2), new Vector3(4, 1.1f, .7f), accent, geometry);
            var ramp = Box("Ramp 15 degrees", new Vector3(-7, .66f, 4), new Vector3(3, .25f, 5.5f), obstacles, geometry);
            ramp.transform.rotation = Quaternion.Euler(-15, 0, 0);
            Box("Ramp landing", new Vector3(-7, .69f, 7.7f), new Vector3(3, 1.38f, 2), obstacles, geometry);
            for (int i = 0; i < 12; i++)
                Box("Stair " + i, new Vector3(7, (i + 1) * .11f, -4 + i * .45f),
                    new Vector3(3, (i + 1) * .22f, .46f), obstacles, geometry);
            Box("Elevated fall platform", new Vector3(7, 1.32f, 2.8f), new Vector3(5, 2.64f, 3), accent, geometry);
            for (int i = 0; i < 3; i++)
                Box("Jump block " + i, new Vector3(-2 + i * 2, .15f + i * .1f, 6),
                    new Vector3(1.2f, .3f + i * .2f, 1.2f), obstacles, geometry);
            for (int i = 0; i < 3; i++)
            {
                var cube = Box("Dynamic prop " + i, new Vector3(-3 + i * 1.5f, 1, -6), Vector3.one * .65f, accent, geometry);
                var rb = cube.AddComponent<Rigidbody>();
                rb.mass = 3;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientLight = new Color(.5f, .5f, .5f);
            RenderSettings.fog = false;

            var player = new GameObject("HybridPlayer");
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.85f;
            cc.radius = .3f;
            cc.center = new Vector3(0, .925f, 0);
            cc.slopeLimit = 45;
            cc.stepOffset = .28f;
            cc.skinWidth = .035f;
            cc.minMoveDistance = 0;
            var input = player.AddComponent<PlayerInputHandler>();
            input.Configure(actions);
            player.AddComponent<GroundDetector>();
            player.AddComponent<PlayerStateController>();
            var motor = player.AddComponent<PlayerMotor>();
            var visuals = new GameObject("PlaceholderVisual (no animated rig in Phase 1)").transform;
            visuals.SetParent(player.transform, false);
            Visual("Torso", new Vector3(0, 1.15f, 0), new Vector3(.47f, .64f, .3f), bodyMaterial, visuals);
            Visual("Head", new Vector3(0, 1.68f, 0), Vector3.one * .31f, skin, visuals);
            Visual("Left arm", new Vector3(-.34f, 1.15f, 0), new Vector3(.17f, .6f, .21f), bodyMaterial, visuals);
            Visual("Right arm", new Vector3(.34f, 1.15f, 0), new Vector3(.17f, .6f, .21f), bodyMaterial, visuals);
            Visual("Left leg", new Vector3(-.14f, .43f, 0), new Vector3(.2f, .8f, .24f), bodyMaterial, visuals);
            Visual("Right leg", new Vector3(.14f, .43f, 0), new Vector3(.2f, .8f, .24f), bodyMaterial, visuals);
            Visual("Forward marker", new Vector3(0, 1.3f, .19f), new Vector3(.18f, .12f, .12f), accent, visuals);
            Vector3 spawn = new Vector3(0, .15f, -8);
            // Save a reusable prefab without references to scene-only objects.
            PrefabUtility.SaveAsPrefabAsset(player, BasePath + "/HybridPlayer.prefab");
            Object.DestroyImmediate(player);
            player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BasePath + "/HybridPlayer.prefab"));
            player.transform.position = spawn;
            motor = player.GetComponent<PlayerMotor>();
            input = player.GetComponent<PlayerInputHandler>();
            var cameraObject = new GameObject("ThirdPersonCamera");
            cameraObject.tag = "MainCamera";
            var lens = cameraObject.AddComponent<Camera>();
            lens.fieldOfView = 60;
            lens.farClipPlane = 150;
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = new Color(.56f, .65f, .7f);
            var camera = cameraObject.AddComponent<ThirdPersonCamera>();
            camera.Configure(motor, input);
            motor.Configure(cameraObject.transform, spawn);
            PrefabUtility.RecordPrefabInstancePropertyModifications(motor);
            var hud = new GameObject("Phase1 HUD").AddComponent<MovementDebugHUD>();
            hud.Configure(motor);
            EditorSceneManager.SaveScene(scene, scenePath);
            // Preserve other build scenes, but make MovementTest the first scene.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != scenePath)).ToArray();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            Debug.Log("MovementTest generated. Open Game and press Play. Validate Phase 1 before continuing.");
        }

        private static Material Material(string name, Color color)
        {
            string path = BasePath + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Glossiness", .05f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        private static void Visual(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var box = Box(name, position, scale, material, parent);
            Object.DestroyImmediate(box.GetComponent<Collider>());
        }
    }
}
