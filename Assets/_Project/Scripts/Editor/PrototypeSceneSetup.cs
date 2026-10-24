using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Debugging;
using TomorrowsCrime.Events;
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
                Debug.Log("[Tomorrow's Crime] Prototype scene missing. Building complete scenario prototype...");
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

            // Interior: Reception Desk
            var counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "Reception_Counter";
            counter.transform.SetParent(envRoot.transform);
            counter.transform.position = new Vector3(0f, 0.6f, 4f);
            counter.transform.localScale = new Vector3(6f, 1.2f, 1.2f);

            // Lobby Columns
            CreateWall("Pillar_West", envRoot.transform, new Vector3(-5f, 2f, 0f), new Vector3(1f, 4f, 1f));
            CreateWall("Pillar_East", envRoot.transform, new Vector3(5f, 2f, 0f), new Vector3(1f, 4f, 1f));

            // 3. Lobby Table (Holds the Keycard)
            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Table_LobbyKeyTable";
            table.transform.SetParent(envRoot.transform);
            table.transform.position = new Vector3(0f, 0.45f, -3f);
            table.transform.localScale = new Vector3(2.0f, 0.9f, 1.2f);

            // 4. Interactable Keycard (Room 102 Keycard resting on the table)
            var keyGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keyGo.name = "Prop_Room102_Keycard";
            keyGo.transform.SetParent(envRoot.transform);
            keyGo.transform.position = new Vector3(0f, 0.95f, -3f);
            keyGo.transform.localScale = new Vector3(0.35f, 0.06f, 0.22f);
            var interactableKey = keyGo.AddComponent<InteractableKey>();

            // Give key a distinct golden/amber appearance
            var keyRenderer = keyGo.GetComponent<Renderer>();
            if (keyRenderer != null)
            {
                keyRenderer.sharedMaterial.color = new Color(0.95f, 0.75f, 0.15f);
            }

            // 5. Destination Door (Room 102 Door at North Wall)
            var doorRoot = new GameObject("Destination_Door_Room102");
            doorRoot.transform.SetParent(envRoot.transform);
            doorRoot.transform.position = new Vector3(0f, 0f, 11.4f);

            var doorLeaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorLeaf.name = "Door_Leaf";
            doorLeaf.transform.SetParent(doorRoot.transform);
            doorLeaf.transform.localPosition = new Vector3(0.6f, 1.3f, 0f); // Pivot offset for realistic swing
            doorLeaf.transform.localScale = new Vector3(1.2f, 2.6f, 0.15f);

            var doorHinge = new GameObject("Door_Hinge");
            doorHinge.transform.SetParent(doorRoot.transform);
            doorHinge.transform.localPosition = Vector3.zero;
            doorLeaf.transform.SetParent(doorHinge.transform);

            var doorComponent = doorRoot.AddComponent<InteractableDoor>();
            var soDoor = new SerializedObject(doorComponent);
            soDoor.FindProperty("doorHingeTransform").objectReferenceValue = doorHinge.transform;
            soDoor.FindProperty("doorId").stringValue = "door_room_102";
            soDoor.FindProperty("doorDisplayName").stringValue = "Room 102 Door";
            soDoor.ApplyModifiedPropertiesWithoutUndo();

            // 6. Concierge NPC Placeholder (Positioned near Room 102)
            var npcGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npcGo.name = "NPC_Concierge";
            npcGo.transform.SetParent(envRoot.transform);
            npcGo.transform.position = new Vector3(2.5f, 1.0f, 9.5f);
            npcGo.transform.localScale = new Vector3(0.9f, 1.0f, 0.9f);
            var npcRenderer = npcGo.GetComponent<Renderer>();
            if (npcRenderer != null)
            {
                npcRenderer.sharedMaterial.color = new Color(0.2f, 0.35f, 0.7f); // Navy concierge uniform
            }

            // 7. Player Rig with Interaction System
            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(0f, 1.0f, -8f);

            var characterController = playerGo.AddComponent<CharacterController>();
            characterController.height = 2.0f;
            characterController.radius = 0.45f;
            characterController.center = new Vector3(0f, 0f, 0f);

            var playerController = playerGo.AddComponent<PlayerController>();
            var playerInteraction = playerGo.AddComponent<PlayerInteraction>();

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(playerGo.transform);
            cameraGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            cameraGo.transform.localRotation = Quaternion.identity;

            var cam = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();

            var soPlayer = new SerializedObject(playerController);
            soPlayer.FindProperty("cameraTransform").objectReferenceValue = cameraGo.transform;
            soPlayer.ApplyModifiedPropertiesWithoutUndo();

            var soInteract = new SerializedObject(playerInteraction);
            soInteract.FindProperty("playerCamera").objectReferenceValue = cam;
            soInteract.ApplyModifiedPropertiesWithoutUndo();

            // 8. Simulation & World State Systems
            var simSystemsGo = new GameObject("[SimulationSystems]");
            var simManager = simSystemsGo.AddComponent<SimulationManager>();
            var worldStateManager = simSystemsGo.AddComponent<WorldStateManager>();
            var scenarioController = simSystemsGo.AddComponent<KeyScenarioController>();
            simSystemsGo.AddComponent<SimulationDebugUI>();

            // Configure Scenario Controller
            var soScenario = new SerializedObject(scenarioController);
            soScenario.FindProperty("scheduledTimeSeconds").floatValue = 30.0f; // 00:30 into loop
            soScenario.FindProperty("requiredKeyId").stringValue = "key_room_102";
            soScenario.FindProperty("targetDoorId").stringValue = "door_room_102";
            soScenario.FindProperty("targetDoor").objectReferenceValue = doorComponent;
            soScenario.FindProperty("npcTransform").objectReferenceValue = npcGo.transform;
            soScenario.ApplyModifiedPropertiesWithoutUndo();

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
