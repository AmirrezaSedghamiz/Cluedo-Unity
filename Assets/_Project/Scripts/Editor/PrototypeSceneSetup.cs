using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Debugging;
using TomorrowsCrime.Events;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.NPC;
using TomorrowsCrime.Player;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.UI.Core;
using TomorrowsCrime.UI.HUD;
using TomorrowsCrime.UI.Menu;
using TomorrowsCrime.UI.Modals;
using TomorrowsCrime.UI.Notebook;
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
            var floorRenderer = floor.GetComponent<Renderer>();
            if (floorRenderer != null)
            {
                floorRenderer.sharedMaterial.color = new Color(0.12f, 0.13f, 0.15f); // Deep charcoal
            }

            // Atmospheric Warm Chandelier / Pendant Light
            var pendantLightGo = new GameObject("Lobby_PendantLight");
            pendantLightGo.transform.SetParent(envRoot.transform);
            pendantLightGo.transform.position = new Vector3(0f, 3.4f, 0f);
            var pendantLight = pendantLightGo.AddComponent<Light>();
            pendantLight.type = LightType.Point;
            pendantLight.color = new Color(1.0f, 0.88f, 0.65f);
            pendantLight.intensity = 2.4f;
            pendantLight.range = 16f;

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
            var counterRenderer = counter.GetComponent<Renderer>();
            if (counterRenderer != null)
            {
                counterRenderer.sharedMaterial.color = new Color(0.24f, 0.14f, 0.08f); // Rich mahogany
            }

            // Create or load immutable evidence definitions
            var keyEvidence = GetOrCreateEvidenceAsset(
                "evidence_room102_keycard",
                "Room 102 Keycard",
                "Keys & Access",
                "A magnetic hotel keycard stamped with 'Room 102 - Meridian House'.",
                "Standard electronic guest keycard for Room 102. Found resting on the lobby table. Concierge is scheduled to retrieve it at 00:30 for turn-down inspection.",
                "evt_concierge_retrieve_key"
            );

            var memoEvidence = GetOrCreateEvidenceAsset(
                "evidence_concierge_schedule_memo",
                "Concierge Duty Memo",
                "Documents",
                "A duty memo on the reception desk noting midnight staff routines.",
                "Handwritten memo on Meridian House stationery: 'Concierge Night Shift: 00:00 - Midnight inspection begins. 00:30 - Retrieve spare Room 102 keycard from central lobby table. 00:35 - Inspect Room 102 suite.' Signed by the Head Concierge.",
                "evt_concierge_retrieve_key"
            );

            var doorEvidence = GetOrCreateEvidenceAsset(
                "evidence_room102_door",
                "Room 102 Door & Lock",
                "Locations",
                "The entrance to Room 102 with an electronic deadbolt lock.",
                "Heavy mahogany door leading into Room 102. An electronic card reader glows red, requiring the Room 102 keycard. Secured at the start of every midnight loop.",
                "evt_concierge_unlock_door"
            );

            // Schedule Memo Evidence on Reception Desk
            var memoGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            memoGo.name = "Prop_Reception_ScheduleMemo";
            memoGo.transform.SetParent(envRoot.transform);
            memoGo.transform.position = new Vector3(1.5f, 1.22f, 4f);
            memoGo.transform.localScale = new Vector3(0.4f, 0.04f, 0.5f);
            var memoRenderer = memoGo.GetComponent<Renderer>();
            if (memoRenderer != null)
            {
                memoRenderer.sharedMaterial.color = new Color(0.92f, 0.90f, 0.82f);
            }
            var memoInspectable = memoGo.AddComponent<InspectableEvidence>();
            memoInspectable.Definition = memoEvidence;

            // Lobby Columns
            CreateWall("Pillar_West", envRoot.transform, new Vector3(-5f, 2f, 0f), new Vector3(1f, 4f, 1f));
            CreateWall("Pillar_East", envRoot.transform, new Vector3(5f, 2f, 0f), new Vector3(1f, 4f, 1f));

            // 3. Lobby Table (Holds the Keycard)
            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Table_LobbyKeyTable";
            table.transform.SetParent(envRoot.transform);
            table.transform.position = new Vector3(0f, 0.45f, -3f);
            table.transform.localScale = new Vector3(2.0f, 0.9f, 1.2f);
            var tableRenderer = table.GetComponent<Renderer>();
            if (tableRenderer != null)
            {
                tableRenderer.sharedMaterial.color = new Color(0.28f, 0.18f, 0.10f); // Warm walnut
            }

            // 4. Interactable Keycard (Room 102 Keycard resting on the table)
            var keyGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keyGo.name = "Prop_Room102_Keycard";
            keyGo.transform.SetParent(envRoot.transform);
            keyGo.transform.position = new Vector3(0f, 0.95f, -3f);
            keyGo.transform.localScale = new Vector3(0.35f, 0.06f, 0.22f);
            var interactableKey = keyGo.AddComponent<InteractableKey>();
            interactableKey.KeyEvidence = keyEvidence;

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
            var doorLeafRenderer = doorLeaf.GetComponent<Renderer>();
            if (doorLeafRenderer != null)
            {
                doorLeafRenderer.sharedMaterial.color = new Color(0.22f, 0.12f, 0.07f); // Rich mahogany
            }

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

            // Door Card Reader / Lockplate Evidence
            var keypadGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keypadGo.name = "Prop_Door102_CardReader";
            keypadGo.transform.SetParent(doorRoot.transform);
            keypadGo.transform.localPosition = new Vector3(0.85f, 1.3f, -0.05f);
            keypadGo.transform.localScale = new Vector3(0.18f, 0.28f, 0.08f);
            var keypadRenderer = keypadGo.GetComponent<Renderer>();
            if (keypadRenderer != null)
            {
                keypadRenderer.sharedMaterial.color = new Color(0.25f, 0.28f, 0.32f);
            }
            var doorInspectable = keypadGo.AddComponent<InspectableEvidence>();
            doorInspectable.Definition = doorEvidence;

            // 6. Navigation Surface & Waypoints
            var waypointsRoot = new GameObject("[Waypoints]");
            var wpLobbyDesk = new GameObject("Waypoint_LobbyDesk");
            wpLobbyDesk.transform.SetParent(waypointsRoot.transform);
            wpLobbyDesk.transform.position = new Vector3(0f, 0f, 5.5f);

            var wpLobbyTable = new GameObject("Waypoint_LobbyTable");
            wpLobbyTable.transform.SetParent(waypointsRoot.transform);
            wpLobbyTable.transform.position = new Vector3(0f, 0f, -1.8f);

            var wpRoom102Door = new GameObject("Waypoint_Room102Door");
            wpRoom102Door.transform.SetParent(waypointsRoot.transform);
            wpRoom102Door.transform.position = new Vector3(0f, 0f, 10.2f);

            // Bake NavMesh Surface across lobby
            var navSurface = envRoot.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
            navSurface.collectObjects = Unity.AI.Navigation.CollectObjects.All;
            navSurface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            navSurface.BuildNavMesh();

            // 7. Concierge NPC with NavMeshAgent & NPCController
            var npcGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npcGo.name = "NPC_Concierge";
            npcGo.transform.SetParent(envRoot.transform);
            npcGo.transform.position = new Vector3(0f, 1.0f, 5.5f); // Starts at reception desk
            npcGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            npcGo.transform.localScale = new Vector3(0.9f, 1.0f, 0.9f);
            var npcRenderer = npcGo.GetComponent<Renderer>();
            if (npcRenderer != null)
            {
                npcRenderer.sharedMaterial.color = new Color(0.2f, 0.35f, 0.7f); // Navy concierge uniform
            }

            var agent = npcGo.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 3.0f;
            agent.stoppingDistance = 0.8f;
            agent.radius = 0.45f;
            agent.height = 2.0f;

            var npcController = npcGo.AddComponent<NPCController>();
            var soNpc = new SerializedObject(npcController);
            soNpc.FindProperty("waypointLobbyDesk").objectReferenceValue = wpLobbyDesk.transform;
            soNpc.FindProperty("waypointLobbyTable").objectReferenceValue = wpLobbyTable.transform;
            soNpc.FindProperty("waypointRoom102Door").objectReferenceValue = wpRoom102Door.transform;
            soNpc.ApplyModifiedPropertiesWithoutUndo();

            // 8. Player Rig with Interaction System
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

            // 9. Simulation & World State Systems
            var simSystemsGo = new GameObject("[SimulationSystems]");
            var simManager = simSystemsGo.AddComponent<SimulationManager>();
            var worldStateManager = simSystemsGo.AddComponent<WorldStateManager>();
            var eventScheduler = simSystemsGo.AddComponent<CausalEventScheduler>();
            var scenarioController = simSystemsGo.AddComponent<KeyScenarioController>();
            simSystemsGo.AddComponent<SimulationDebugUI>();
            simSystemsGo.AddComponent<NotebookUIController>();

            // Configure Scenario Controller
            var soScenario = new SerializedObject(scenarioController);
            soScenario.FindProperty("retrieveKeyTime").floatValue = 30.0f; // 00:30 into loop
            soScenario.FindProperty("unlockDoorTime").floatValue = 35.0f;  // 00:35 into loop
            soScenario.FindProperty("keyId").stringValue = "key_room_102";
            soScenario.FindProperty("doorId").stringValue = "door_room_102";
            soScenario.FindProperty("npcTransform").objectReferenceValue = npcGo.transform;
            soScenario.ApplyModifiedPropertiesWithoutUndo();

            // 10. Major UI Overhaul Systems
            var uiRootGo = new GameObject("[UserInterface]");
            uiRootGo.AddComponent<UIManager>();
            uiRootGo.AddComponent<GameHUDView>();
            uiRootGo.AddComponent<InvestigationWorkspaceView>();
            uiRootGo.AddComponent<EvidenceInspectionModalView>();
            uiRootGo.AddComponent<PauseMenuView>();
            uiRootGo.AddComponent<SettingsMenuView>();
            uiRootGo.AddComponent<MainMenuView>();

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

        private static EvidenceDefinition GetOrCreateEvidenceAsset(
            string id,
            string displayName,
            string category,
            string shortDesc,
            string detailText,
            string eventId = null)
        {
            string folder = "Assets/_Project/Data/Evidence";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            string assetPath = $"{folder}/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<EvidenceDefinition>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<EvidenceDefinition>();
                asset.Initialize(id, displayName, shortDesc, detailText, category, eventId);
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
            }
            else
            {
                asset.Initialize(id, displayName, shortDesc, detailText, category, eventId);
                EditorUtility.SetDirty(asset);
            }

            return asset;
        }

        private static void CreateWall(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            var renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.color = new Color(0.32f, 0.31f, 0.28f); // Warm muted stone
            }
        }
    }
}
