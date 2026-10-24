using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TomorrowsCrime.Debugging;
using TomorrowsCrime.Player;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.World;

namespace TomorrowsCrime.EditorTools
{
    [InitializeOnLoad]
    public static class PrototypeSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";

        static PrototypeSceneSetup()
        {
            EditorApplication.delayCall += CheckAndAutoGenerateScene;
        }

        private static void CheckAndAutoGenerateScene()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.Log("[Tomorrow's Crime] Prototype scene missing. Automatically generating initial graybox prototype...");
                BuildPrototypeScene();
            }
        }

        [MenuItem("Tomorrow's Crime/Build Prototype Scene", priority = 1)]
        public static void BuildPrototypeScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Lighting
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.96f, 0.90f);
            light.intensity = 1.2f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 2. Graybox Hotel Lobby Environment
            var envRoot = new GameObject("[Environment_HotelLobby]");

            // Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(envRoot.transform);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(20f, 1f, 24f);

            // Boundary Walls
            CreateWall("Wall_North_Back", envRoot.transform, new Vector3(0f, 2f, 12f), new Vector3(20f, 4f, 1f));
            CreateWall("Wall_South_Entrance", envRoot.transform, new Vector3(0f, 2f, -12f), new Vector3(20f, 4f, 1f));
            CreateWall("Wall_West", envRoot.transform, new Vector3(-10f, 2f, 0f), new Vector3(1f, 4f, 24f));
            CreateWall("Wall_East", envRoot.transform, new Vector3(10f, 2f, 0f), new Vector3(1f, 4f, 24f));

            // Interior Architecture: Reception Counter
            var counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "Reception_Counter";
            counter.transform.SetParent(envRoot.transform);
            counter.transform.position = new Vector3(0f, 0.6f, 3f);
            counter.transform.localScale = new Vector3(6f, 1.2f, 1.2f);

            // Lobby Columns
            CreateWall("Pillar_West", envRoot.transform, new Vector3(-5f, 2f, 0f), new Vector3(1f, 4f, 1f));
            CreateWall("Pillar_East", envRoot.transform, new Vector3(5f, 2f, 0f), new Vector3(1f, 4f, 1f));

            // 3. Test Interactable Prop on Reception Desk (Demonstrates World State Reset)
            var propGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            propGo.name = "Prop_Room102_Keycard";
            propGo.transform.position = new Vector3(0f, 1.25f, 3f);
            propGo.transform.localScale = new Vector3(0.35f, 0.08f, 0.22f);
            propGo.AddComponent<TestInteractableProp>();

            // 4. Player Rig
            var playerGo = new GameObject("Player");
            playerGo.transform.position = new Vector3(0f, 1.0f, -8f);

            var characterController = playerGo.AddComponent<CharacterController>();
            characterController.height = 2.0f;
            characterController.radius = 0.45f;
            characterController.center = new Vector3(0f, 0f, 0f);

            var playerController = playerGo.AddComponent<PlayerController>();

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(playerGo.transform);
            cameraGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            cameraGo.transform.localRotation = Quaternion.identity;

            cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();

            var soPlayer = new SerializedObject(playerController);
            soPlayer.FindProperty("cameraTransform").objectReferenceValue = cameraGo.transform;
            soPlayer.ApplyModifiedPropertiesWithoutUndo();

            // 5. Simulation Systems & Diagnostics
            var simSystemsGo = new GameObject("[SimulationSystems]");
            simSystemsGo.AddComponent<SimulationManager>();
            simSystemsGo.AddComponent<SimulationDebugUI>();

            // Ensure parent directory exists
            var dir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Tomorrow's Crime] Prototype scene saved successfully to: {ScenePath}");

            // Register in EditorBuildSettings
            var scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = scenes;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateWall(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = position;
            wall.transform.localScale = scale;
        }
    }
}
