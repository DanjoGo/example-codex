using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Patio.Editor
{
    public static class PlaygroundTools
    {
        [MenuItem("Patio/Create playground scene")]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Patio").AddComponent<PrototypeWorld>();
            const string directory = "Assets/Prototype/Scenes";
            Directory.CreateDirectory(directory);
            const string path = directory + "/Playground.unity";
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
            AssetDatabase.Refresh();
        }
    }
}
