using System.Collections.Generic;
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
    /// <summary>
    /// Constructs the complete visual reboot of Meridian House for Tomorrow's Crime.
    /// Step 7: Modern Cluedo-Inspired Art Direction.
    /// Replaces graybox primitives with bespoke stylized 3D models, rich PBR materials,
    /// architectural wainscoting, checker marble flooring, fluted columns, Art Deco chandeliers,
    /// a fully realized stylized Concierge Pierre character model, and a multi-source lighting rig.
    /// </summary>
    [InitializeOnLoad]
    public static class PrototypeSceneSetup
    {
        // Trigger compile refresh
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
            Debug.Log("[Tomorrow's Crime] Commencing Step 7 Complete Visual Reboot Scene Build...");

            // 1. Setup PBR Materials & Model Assets
            var mats = ArtAssetSetup.CreateOrUpdateMaterials();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Chiaroscuro Lighting Rig
            // Soft cool midnight ambient moonlight (5500K)
            var moonLightGo = new GameObject("Moonlight_Directional");
            var moonLight = moonLightGo.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = new Color(0.55f, 0.65f, 0.85f); // Cool midnight blue
            moonLight.intensity = 0.45f;
            moonLightGo.transform.rotation = Quaternion.Euler(60f, -40f, 0f);

            // 3. Architectural Meridian House Lobby Environment
            var envRoot = new GameObject("[Environment_HotelLobby]");

            // Floor Base (Checker Italian Marble)
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor_CheckerMarble";
            floor.transform.SetParent(envRoot.transform);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(20f, 1f, 24f);
            var floorRenderer = floor.GetComponent<Renderer>();
            if (floorRenderer != null && mats.TryGetValue("FloorCheckerMarble", out var floorMat))
            {
                floorRenderer.sharedMaterial = floorMat;
            }

            // Central Luxury Velvet Carpet Runner (Leading to Reception & Room 102)
            var carpet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carpet.name = "Carpet_Runner_Burgundy";
            carpet.transform.SetParent(envRoot.transform);
            carpet.transform.position = new Vector3(0f, 0.015f, 0f);
            carpet.transform.localScale = new Vector3(4.2f, 0.03f, 23f);
            var carpetRenderer = carpet.GetComponent<Renderer>();
            if (carpetRenderer != null && mats.TryGetValue("CarpetBurgundy", out var carpetMat))
            {
                carpetRenderer.sharedMaterial = carpetMat;
            }

            // Ceiling (Dark Coffered Wood Ceiling)
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling_CofferedWood";
            ceiling.transform.SetParent(envRoot.transform);
            ceiling.transform.position = new Vector3(0f, 4.5f, 0f);
            ceiling.transform.localScale = new Vector3(20f, 0.5f, 24f);
            var ceilingRenderer = ceiling.GetComponent<Renderer>();
            if (ceilingRenderer != null && mats.TryGetValue("WoodMahogany", out var woodMat))
            {
                ceilingRenderer.sharedMaterial = woodMat;
            }

            // 4. Architectural Walls with Dark Mahogany Wainscoting & Emerald Wallpaper
            mats.TryGetValue("WallpaperEmerald", out var wallUpperMat);
            mats.TryGetValue("WoodMahogany", out var wallLowerMat);
            mats.TryGetValue("BrassGold", out var brassMat);

            // North Wall with Doorway Opening for Room 102 (Opening between X = -1.0 and +1.0, Y = 0 to 2.8)
            CreateArchitecturalWall("Wall_North_WestFlank", envRoot.transform, new Vector3(-5.5f, 0f, 12f), new Vector3(9f, 4.5f, 0.8f), wallLowerMat, wallUpperMat, brassMat, false);
            CreateArchitecturalWall("Wall_North_EastFlank", envRoot.transform, new Vector3(5.5f, 0f, 12f), new Vector3(9f, 4.5f, 0.8f), wallLowerMat, wallUpperMat, brassMat, false);
            CreateArchitecturalWall("Wall_North_DoorLintel", envRoot.transform, new Vector3(0f, 2.8f, 12f), new Vector3(2f, 1.7f, 0.8f), wallLowerMat, wallUpperMat, brassMat, false);

            CreateArchitecturalWall("Wall_South_Entrance", envRoot.transform, new Vector3(0f, 0f, -12f), new Vector3(20f, 4.5f, 0.8f), wallLowerMat, wallUpperMat, brassMat, true);
            CreateArchitecturalWall("Wall_West", envRoot.transform, new Vector3(-10f, 0f, 0f), new Vector3(0.8f, 4.5f, 24f), wallLowerMat, wallUpperMat, brassMat, false);
            CreateArchitecturalWall("Wall_East", envRoot.transform, new Vector3(10f, 0f, 0f), new Vector3(0.8f, 4.5f, 24f), wallLowerMat, wallUpperMat, brassMat, false);

            // 5. Fluted Architectural Columns with Corinthian Brass Capitals (Solid Colliders)
            var pillarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/HotelLobby_Pillar.obj");
            var colPillarWest = SpawnProp(pillarPrefab, "Pillar_West", envRoot.transform, new Vector3(-5f, 0f, 0f), Vector3.zero, Vector3.one, mats).AddComponent<BoxCollider>();
            colPillarWest.center = new Vector3(0f, 2.25f, 0f);
            colPillarWest.size = new Vector3(1.1f, 4.5f, 1.1f);

            var colPillarEast = SpawnProp(pillarPrefab, "Pillar_East", envRoot.transform, new Vector3(5f, 0f, 0f), Vector3.zero, Vector3.one, mats).AddComponent<BoxCollider>();
            colPillarEast.center = new Vector3(0f, 2.25f, 0f);
            colPillarEast.size = new Vector3(1.1f, 4.5f, 1.1f);

            // 6. Grand Art Deco Chandelier
            var chandelierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/HotelLobby_Chandelier.obj");
            var chandelier = SpawnProp(chandelierPrefab, "Lobby_GrandChandelier", envRoot.transform, new Vector3(0f, 4.3f, 0f), Vector3.zero, Vector3.one, mats);

            // Warm Radiant Chandelier Point Light
            var chandelierLightGo = new GameObject("Chandelier_PointLight");
            chandelierLightGo.transform.SetParent(chandelier.transform);
            chandelierLightGo.transform.localPosition = new Vector3(0f, -0.8f, 0f);
            var chandelierLight = chandelierLightGo.AddComponent<Light>();
            chandelierLight.type = LightType.Point;
            chandelierLight.color = new Color(1.0f, 0.88f, 0.65f); // 2700K warm tungsten
            chandelierLight.intensity = 3.2f;
            chandelierLight.range = 18f;

            // 7. Reception Desk, Banker's Lamp & Solid Key Cubbies / Staff Lockers
            var deskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/HotelLobby_ReceptionDesk.obj");
            var desk = SpawnProp(deskPrefab, "Reception_Desk", envRoot.transform, new Vector3(0f, 0f, 4.5f), new Vector3(0f, 180f, 0f), Vector3.one, mats);

            // Ensure exact solid mesh collision for all reception desk surfaces
            var deskFilters = desk.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in deskFilters)
            {
                if (mf.sharedMesh != null)
                {
                    var mc = mf.gameObject.GetComponent<MeshCollider>();
                    if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }

            // Front Mahogany Counter Solid Box Collider (Check-in side)
            var deskCollider = desk.AddComponent<BoxCollider>();
            deskCollider.center = new Vector3(0f, 0.55f, 0.1f);
            deskCollider.size = new Vector3(5.4f, 1.1f, 1.4f);
            deskCollider.isTrigger = false;

            // Inspectable Evidence on Front Counter (Registry Ledger)
            var registryEvidence = GetOrCreateEvidenceAsset(
                "evidence_reception_registry",
                "Reception Registry & Ledger",
                "Documents",
                "The leather-bound guest arrival register and room key distribution ledger.",
                "Official Meridian House front desk logbook. Entries confirm Madame Valerie checked into Room 102 earlier this evening. A cryptic notation beside Room 102 is scribbled in blue ink: 'Urgent: deliver parcel before midnight.'",
                "evt_concierge_retrieve_key"
            );
            var deskInspectable = desk.AddComponent<InspectableEvidence>();
            deskInspectable.Definition = registryEvidence;

            // Rear Key Cubbies & Staff Lockers Solid Collider (Blocks player and NPC navigation)
            var lockersGo = new GameObject("Reception_Lockers_Unit");
            lockersGo.transform.SetParent(desk.transform);
            lockersGo.transform.localPosition = Vector3.zero;
            lockersGo.transform.localRotation = Quaternion.identity;

            var lockersCollider = lockersGo.AddComponent<BoxCollider>();
            lockersCollider.center = new Vector3(0f, 1.15f, -1.77f);
            lockersCollider.size = new Vector3(4.25f, 2.30f, 0.55f);
            lockersCollider.isTrigger = false;

            // Inspectable Evidence for Key Cubbies & Staff Lockers
            var lockersEvidence = GetOrCreateEvidenceAsset(
                "evidence_reception_lockers",
                "Key Cubbies & Staff Lockers",
                "Locations",
                "A grand mahogany cabinet of room key pigeonholes and brass-locked staff cubbies behind the reception counter.",
                "Meridian House reception key rack and staff storage lockers. Compartments are numbered for rooms 101 through 103. Slot 101 contains a brass room key; slot 102 is vacant; slot 103 contains sealed guest mail. Lower staff lockers are secured with heavy brass padlocks.",
                "evt_concierge_retrieve_key"
            );
            var lockersInspectable = lockersGo.AddComponent<InspectableEvidence>();
            lockersInspectable.Definition = lockersEvidence;

            // Banker's Desk Lamp with glowing emerald shade
            var bankerLampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/Prop_BankerLamp.obj");
            var bankerLamp = SpawnProp(bankerLampPrefab, "Prop_BankerLamp", desk.transform, new Vector3(-1.8f, 1.12f, 0.2f), new Vector3(0f, 35f, 0f), new Vector3(1.1f, 1.1f, 1.1f), mats);

            var bankerLightGo = new GameObject("BankerLamp_Glow");
            bankerLightGo.transform.SetParent(bankerLamp.transform);
            bankerLightGo.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            var bankerLight = bankerLightGo.AddComponent<Light>();
            bankerLight.type = LightType.Point;
            bankerLight.color = new Color(0.6f, 0.95f, 0.7f); // Emerald tinted warm glow
            bankerLight.intensity = 1.4f;
            bankerLight.range = 4f;

            // 8. Immutable Evidence Definitions & World Props
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

            // Schedule Memo Evidence on Reception Desk (Linen Paper)
            var memoGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            memoGo.name = "Prop_Reception_ScheduleMemo";
            memoGo.transform.SetParent(envRoot.transform);
            memoGo.transform.position = new Vector3(1.2f, 1.14f, 4.2f);
            memoGo.transform.localScale = new Vector3(0.42f, 0.02f, 0.55f);
            memoGo.transform.rotation = Quaternion.Euler(0f, -12f, 0f);
            var memoRenderer = memoGo.GetComponent<Renderer>();
            if (memoRenderer != null && mats.TryGetValue("ShirtWhite", out var memoMat))
            {
                memoRenderer.sharedMaterial = memoMat;
            }
            var memoInspectable = memoGo.AddComponent<InspectableEvidence>();
            memoInspectable.Definition = memoEvidence;

            // 9. Round Burled Walnut Center Table
            var tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/HotelLobby_CenterTable.obj");
            var table = SpawnProp(tablePrefab, "Table_LobbyKeyTable", envRoot.transform, new Vector3(0f, 0f, -3f), Vector3.zero, Vector3.one, mats);
            var tableCollider = table.AddComponent<CylinderColliderSubstitute>();
            var colBox = table.AddComponent<BoxCollider>();
            colBox.center = new Vector3(0f, 0.4f, 0f);
            colBox.size = new Vector3(2.1f, 0.85f, 2.1f);

            // 10. Stylized Detailed Keycard on Table
            var keyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/Prop_Keycard_Detailed.obj");
            var keyGo = SpawnProp(keyPrefab, "Prop_Room102_Keycard", envRoot.transform, new Vector3(0f, 0.83f, -3f), new Vector3(0f, 25f, 0f), Vector3.one, mats);
            var keyCollider = keyGo.AddComponent<BoxCollider>();
            keyCollider.center = new Vector3(0f, 0.02f, 0f);
            keyCollider.size = new Vector3(0.45f, 0.15f, 0.35f);

            var interactableKey = keyGo.AddComponent<InteractableKey>();
            interactableKey.KeyEvidence = keyEvidence;

            // 11. Luxury Velvet Armchairs in Lounge Area
            var chairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/HotelLobby_Armchair.obj");
            var chair1 = SpawnProp(chairPrefab, "Armchair_West", envRoot.transform, new Vector3(-3.2f, 0f, -3.0f), new Vector3(0f, 55f, 0f), Vector3.one, mats);
            var chair1Col = chair1.AddComponent<BoxCollider>();
            chair1Col.center = new Vector3(0f, 0.5f, 0f);
            chair1Col.size = new Vector3(0.9f, 1.0f, 0.9f);

            var chair2 = SpawnProp(chairPrefab, "Armchair_East", envRoot.transform, new Vector3(3.2f, 0f, -3.0f), new Vector3(0f, -55f, 0f), Vector3.one, mats);
            var chair2Col = chair2.AddComponent<BoxCollider>();
            chair2Col.center = new Vector3(0f, 0.5f, 0f);
            chair2Col.size = new Vector3(0.9f, 1.0f, 0.9f);

            // 12. Destination Door: Room 102 with Fluted Architrave & Brass Hardware
            var doorRoot = new GameObject("Destination_Door_Room102");
            doorRoot.transform.SetParent(envRoot.transform);
            doorRoot.transform.position = new Vector3(0f, 0f, 11.4f);

            var doorHinge = new GameObject("Door_Hinge");
            doorHinge.transform.SetParent(doorRoot.transform);
            doorHinge.transform.localPosition = new Vector3(-0.65f, 0f, 0f); // Pivot at left jamb

            var doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/HotelLobby_Door102.obj");
            var doorLeaf = SpawnProp(doorPrefab, "Door_Leaf", doorHinge.transform, new Vector3(0.65f, 0f, 0f), Vector3.zero, Vector3.one, mats);

            var doorBoxCollider = doorLeaf.AddComponent<BoxCollider>();
            doorBoxCollider.center = new Vector3(0f, 1.4f, 0f);
            doorBoxCollider.size = new Vector3(1.6f, 2.8f, 0.3f);

            var doorComponent = doorRoot.AddComponent<InteractableDoor>();
            var soDoor = new SerializedObject(doorComponent);
            soDoor.FindProperty("doorHingeTransform").objectReferenceValue = doorHinge.transform;
            soDoor.FindProperty("doorId").stringValue = "door_room_102";
            soDoor.FindProperty("doorDisplayName").stringValue = "Room 102 Door";
            soDoor.FindProperty("requiredKeyId").stringValue = "key_room_102";
            soDoor.ApplyModifiedPropertiesWithoutUndo();

            // Wall Sconces Flanking Door 102
            var sconcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/HotelLobby_WallSconce.obj");
            var sconceWest = SpawnProp(sconcePrefab, "Sconce_DoorWest", envRoot.transform, new Vector3(-1.8f, 2.2f, 11.5f), new Vector3(0f, 180f, 0f), Vector3.one, mats);
            AddSconceLight(sconceWest.transform);

            var sconceEast = SpawnProp(sconcePrefab, "Sconce_DoorEast", envRoot.transform, new Vector3(1.8f, 2.2f, 11.5f), new Vector3(0f, 180f, 0f), Vector3.one, mats);
            AddSconceLight(sconceEast.transform);

            // Electronic Card Reader Plate
            var keypadGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keypadGo.name = "Prop_Door102_CardReader";
            keypadGo.transform.SetParent(doorRoot.transform);
            keypadGo.transform.localPosition = new Vector3(0.95f, 1.35f, -0.1f);
            keypadGo.transform.localScale = new Vector3(0.18f, 0.32f, 0.08f);
            var keypadRenderer = keypadGo.GetComponent<Renderer>();
            if (keypadRenderer != null && mats.TryGetValue("BrassGold", out var readerBrassMat))
            {
                keypadRenderer.sharedMaterial = readerBrassMat;
            }

            // Glowing Indicator Light on Card Reader (Crimson = Locked)
            var cardReaderLightGo = new GameObject("CardReader_LED");
            cardReaderLightGo.transform.SetParent(keypadGo.transform);
            cardReaderLightGo.transform.localPosition = new Vector3(0f, 0.3f, -0.6f);
            var cardReaderLight = cardReaderLightGo.AddComponent<Light>();
            cardReaderLight.type = LightType.Point;
            cardReaderLight.color = new Color(0.9f, 0.15f, 0.15f); // Crimson lock LED
            cardReaderLight.intensity = 1.0f;
            cardReaderLight.range = 1.5f;

            var doorInspectable = keypadGo.AddComponent<InspectableEvidence>();
            doorInspectable.Definition = doorEvidence;

            // 13. Room 102 Crime Scene Suite (Beyond Door 102)
            BuildRoom102Suite(envRoot.transform, mats);

            // 14. AI Navigation Surface & Waypoints
            var waypointsRoot = new GameObject("[Waypoints]");
            var wpLobbyDesk = new GameObject("Waypoint_LobbyDesk");
            wpLobbyDesk.transform.SetParent(waypointsRoot.transform);
            wpLobbyDesk.transform.position = new Vector3(0f, 0f, 5.3f);

            var wpLobbyTable = new GameObject("Waypoint_LobbyTable");
            wpLobbyTable.transform.SetParent(waypointsRoot.transform);
            wpLobbyTable.transform.position = new Vector3(0f, 0f, -1.6f);

            var wpRoom102Door = new GameObject("Waypoint_Room102Door");
            wpRoom102Door.transform.SetParent(waypointsRoot.transform);
            wpRoom102Door.transform.position = new Vector3(0f, 0f, 9.8f);

            // Bake Navigation Mesh
            var navSurface = envRoot.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
            navSurface.collectObjects = Unity.AI.Navigation.CollectObjects.All;
            navSurface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            navSurface.BuildNavMesh();

            // 15. Concierge Pierre Stylized Character Model
            var pierrePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/NPC_Concierge_Pierre.obj");
            var npcGo = SpawnProp(pierrePrefab, "NPC_Concierge", envRoot.transform, new Vector3(0f, 0f, 5.3f), new Vector3(0f, 180f, 0f), Vector3.one, mats);

            // Physics Collider for Interaction & Line of Sight
            var npcCollider = npcGo.AddComponent<CapsuleCollider>();
            npcCollider.radius = 0.45f;
            npcCollider.height = 2.0f;
            npcCollider.center = new Vector3(0f, 1.0f, 0f);

            // NavMeshAgent for Movement
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

            // 15. First-Person Player Rig (Collision Hardened)
            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(0f, 0.2f, -2.0f);

            var characterController = playerGo.AddComponent<CharacterController>();
            characterController.height = 2.0f;
            characterController.radius = 0.45f;
            characterController.center = new Vector3(0f, 1.0f, 0f);
            characterController.stepOffset = 0.35f;
            characterController.slopeLimit = 45f;
            characterController.skinWidth = 0.08f;
            characterController.minMoveDistance = 0.001f;

            var playerController = playerGo.AddComponent<PlayerController>();
            var playerInteraction = playerGo.AddComponent<PlayerInteraction>();

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(playerGo.transform);
            cameraGo.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            cameraGo.transform.localRotation = Quaternion.identity;

            var cam = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();

            var soPlayer = new SerializedObject(playerController);
            soPlayer.FindProperty("cameraTransform").objectReferenceValue = cameraGo.transform;
            soPlayer.ApplyModifiedPropertiesWithoutUndo();

            var soInteract = new SerializedObject(playerInteraction);
            soInteract.FindProperty("playerCamera").objectReferenceValue = cam;
            soInteract.ApplyModifiedPropertiesWithoutUndo();

            // 16. Simulation & World State Systems
            var simSystemsGo = new GameObject("[SimulationSystems]");
            var simManager = simSystemsGo.AddComponent<SimulationManager>();
            var worldStateManager = simSystemsGo.AddComponent<WorldStateManager>();
            var eventScheduler = simSystemsGo.AddComponent<CausalEventScheduler>();
            var scenarioController = simSystemsGo.AddComponent<KeyScenarioController>();
            simSystemsGo.AddComponent<SimulationDebugUI>();
            simSystemsGo.AddComponent<NotebookUIController>();
            simSystemsGo.AddComponent<TomorrowsCrime.Audio.AudioManager>();
            simSystemsGo.AddComponent<TomorrowsCrime.Investigation.CaseAccusationService>();
            simSystemsGo.AddComponent<TomorrowsCrime.Procedural.Core.GameSessionManager>();

            var soScenario = new SerializedObject(scenarioController);
            soScenario.FindProperty("retrieveKeyTime").floatValue = 30.0f;
            soScenario.FindProperty("unlockDoorTime").floatValue = 35.0f;
            soScenario.FindProperty("keyId").stringValue = "key_room_102";
            soScenario.FindProperty("doorId").stringValue = "door_room_102";
            soScenario.FindProperty("npcTransform").objectReferenceValue = npcGo.transform;
            soScenario.ApplyModifiedPropertiesWithoutUndo();

            // 17. User Interface Hierarchy
            var uiRootGo = new GameObject("[UserInterface]");
            uiRootGo.AddComponent<UIManager>();
            uiRootGo.AddComponent<GameHUDView>();
            uiRootGo.AddComponent<InvestigationWorkspaceView>();
            uiRootGo.AddComponent<EvidenceInspectionModalView>();
            uiRootGo.AddComponent<TomorrowsCrime.UI.Modals.NPCDialogueModalView>();
            uiRootGo.AddComponent<TomorrowsCrime.UI.Modals.CaseResolutionModalView>();
            uiRootGo.AddComponent<PauseMenuView>();
            uiRootGo.AddComponent<SettingsMenuView>();
            uiRootGo.AddComponent<MainMenuView>();
            uiRootGo.AddComponent<TomorrowsCrime.Procedural.Debugging.GenerationInspectorUI>();

            // Run Collision Audit
            var audit = TomorrowsCrime.Procedural.Validation.WorldCollisionAuditor.AuditEnvironment(envRoot);
            Debug.Log($"[PrototypeSceneSetup] Collision Audit: {audit}");

            // Save Scene
            var dir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Tomorrow's Crime] Modern Cluedo-Reboot Prototype scene saved successfully to: {ScenePath}");

            var scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = scenes;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void BuildPrototypeSceneBatchmode()
        {
            try
            {
                BuildPrototypeScene();
                Debug.Log("[Tomorrow's Crime] BuildPrototypeSceneBatchmode completed successfully.");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Tomorrow's Crime] BuildPrototypeSceneBatchmode failed: {ex}");
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Tomorrow's Crime/Bake 4-Floor Procedural Hotel", priority = 3)]
        public static void BakeFourFloorProceduralHotel()
        {
            var mats = ArtAssetSetup.CreateOrUpdateMaterials();
            var existing = GameObject.Find("MeridianHouse_Architecture");
            if (existing != null) Object.DestroyImmediate(existing);

            var rooms = Procedural.Hotel.HotelLayoutTemplate.CreateMeridianHouseLayout();
            var hotel = Procedural.Hotel.HotelBuilder.BuildHotelEnvironment(null, rooms, mats);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[Tomorrow's Crime] Baked 4-Floor Hotel with {rooms.Count} rooms into the active scene.");
        }

        private static GameObject SpawnProp(GameObject prefab, string name, Transform parent,
            Vector3 localPos, Vector3 localRot, Vector3 localScale, Dictionary<string, Material> mats)
        {
            GameObject instance;
            if (prefab != null)
            {
                instance = Object.Instantiate(prefab, parent);
            }
            else
            {
                instance = new GameObject(name);
                instance.transform.SetParent(parent);
            }

            instance.name = name;
            instance.transform.localPosition = localPos;
            instance.transform.localRotation = Quaternion.Euler(localRot);
            instance.transform.localScale = localScale;

            // Apply PBR materials by matching mesh name or submesh index
            AssignMaterialsRecursively(instance, mats);

            return instance;
        }

        private static void AssignMaterialsRecursively(GameObject go, Dictionary<string, Material> mats)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var sharedMats = r.sharedMaterials;
                for (int i = 0; i < sharedMats.Length; i++)
                {
                    if (sharedMats[i] != null)
                    {
                        string matName = sharedMats[i].name.Replace(" (Instance)", "");
                        if (mats.TryGetValue(matName, out var foundMat))
                        {
                            sharedMats[i] = foundMat;
                        }
                    }
                }
                r.sharedMaterials = sharedMats;
            }
        }

        private static void CreateArchitecturalWall(string name, Transform parent, Vector3 position, Vector3 scale,
            Material lowerMat, Material upperMat, Material trimMat, bool hasDoorway)
        {
            var wallRoot = new GameObject(name);
            wallRoot.transform.SetParent(parent);
            wallRoot.transform.position = position;

            // Determine if north/south or east/west
            bool isNorthSouth = scale.x > scale.z;
            float length = isNorthSouth ? scale.x : scale.z;
            float thickness = isNorthSouth ? scale.z : scale.x;

            float lowerHeight = 1.8f;
            float upperHeight = scale.y - lowerHeight;

            // Lower Wainscoting (Dark Mahogany)
            var lower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lower.name = "Wainscoting_Mahogany";
            lower.transform.SetParent(wallRoot.transform);
            lower.transform.localPosition = new Vector3(0f, lowerHeight * 0.5f, 0f);
            lower.transform.localScale = isNorthSouth
                ? new Vector3(length, lowerHeight, thickness)
                : new Vector3(thickness, lowerHeight, length);
            var lowerRend = lower.GetComponent<Renderer>();
            if (lowerRend != null && lowerMat != null) lowerRend.sharedMaterial = lowerMat;

            // Chair Rail Brass Trim
            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "ChairRail_Brass";
            trim.transform.SetParent(wallRoot.transform);
            trim.transform.localPosition = new Vector3(0f, lowerHeight, 0f);
            trim.transform.localScale = isNorthSouth
                ? new Vector3(length, 0.08f, thickness * 1.15f)
                : new Vector3(thickness * 1.15f, 0.08f, length);
            var trimRend = trim.GetComponent<Renderer>();
            if (trimRend != null && trimMat != null) trimRend.sharedMaterial = trimMat;

            // Upper Wall (Emerald Art Deco Wallpaper)
            var upper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            upper.name = "Wallpaper_Emerald";
            upper.transform.SetParent(wallRoot.transform);
            upper.transform.localPosition = new Vector3(0f, lowerHeight + upperHeight * 0.5f, 0f);
            upper.transform.localScale = isNorthSouth
                ? new Vector3(length, upperHeight, thickness)
                : new Vector3(thickness, upperHeight, length);
            var upperRend = upper.GetComponent<Renderer>();
            if (upperRend != null && upperMat != null) upperRend.sharedMaterial = upperMat;
        }

        private static void BuildRoom102Suite(Transform parent, Dictionary<string, Material> mats)
        {
            var suiteRoot = new GameObject("[Room_102_CrimeScene]");
            suiteRoot.transform.SetParent(parent);

            mats.TryGetValue("CarpetBurgundy", out var carpetMat);
            mats.TryGetValue("WoodMahogany", out var woodMat);
            mats.TryGetValue("WallpaperEmerald", out var wallUpperMat);
            mats.TryGetValue("BrassGold", out var brassMat);
            mats.TryGetValue("ShirtWhite", out var paperMat);

            // 1. Solid Room Floor Slab
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor_Room102_Carpet";
            floor.transform.SetParent(suiteRoot.transform);
            floor.transform.position = new Vector3(0f, -0.5f, 16.5f);
            floor.transform.localScale = new Vector3(10f, 1f, 9f);
            var fRend = floor.GetComponent<Renderer>();
            if (fRend != null && carpetMat != null) fRend.sharedMaterial = carpetMat;

            // 2. Solid Ceiling Slab
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling_Room102_Wood";
            ceiling.transform.SetParent(suiteRoot.transform);
            ceiling.transform.position = new Vector3(0f, 4.5f, 16.5f);
            ceiling.transform.localScale = new Vector3(10f, 0.5f, 9f);
            var cRend = ceiling.GetComponent<Renderer>();
            if (cRend != null && woodMat != null) cRend.sharedMaterial = woodMat;

            // 3. Perimeter Solid Walls
            CreateArchitecturalWall("Wall_Room102_North", suiteRoot.transform, new Vector3(0f, 0f, 21f), new Vector3(10f, 4.5f, 0.6f), woodMat, wallUpperMat, brassMat, false);
            CreateArchitecturalWall("Wall_Room102_West", suiteRoot.transform, new Vector3(-5f, 0f, 16.5f), new Vector3(0.6f, 4.5f, 9f), woodMat, wallUpperMat, brassMat, false);
            CreateArchitecturalWall("Wall_Room102_East", suiteRoot.transform, new Vector3(5f, 0f, 16.5f), new Vector3(0.6f, 4.5f, 9f), woodMat, wallUpperMat, brassMat, false);

            // 4. Four-Poster Bed with Luxury Velvet Duvet
            var bedRoot = new GameObject("Prop_Room102_KingBed");
            bedRoot.transform.SetParent(suiteRoot.transform);
            bedRoot.transform.position = new Vector3(0f, 0f, 19.2f);

            var bedBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bedBase.name = "Bed_Mattress_Duvet";
            bedBase.transform.SetParent(bedRoot.transform);
            bedBase.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            bedBase.transform.localScale = new Vector3(2.6f, 0.9f, 2.8f);
            var bedRend = bedBase.GetComponent<Renderer>();
            if (bedRend != null && carpetMat != null) bedRend.sharedMaterial = carpetMat;

            var headboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headboard.name = "Bed_Headboard";
            headboard.transform.SetParent(bedRoot.transform);
            headboard.transform.localPosition = new Vector3(0f, 1.3f, 1.35f);
            headboard.transform.localScale = new Vector3(2.8f, 1.8f, 0.25f);
            var hRend = headboard.GetComponent<Renderer>();
            if (hRend != null && woodMat != null) hRend.sharedMaterial = woodMat;

            // 5. Solid Mahogany Wardrobe
            var wardrobe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wardrobe.name = "Prop_Room102_Wardrobe";
            wardrobe.transform.SetParent(suiteRoot.transform);
            wardrobe.transform.position = new Vector3(3.8f, 1.25f, 14.5f);
            wardrobe.transform.localScale = new Vector3(1.6f, 2.5f, 0.7f);
            var wRend = wardrobe.GetComponent<Renderer>();
            if (wRend != null && woodMat != null) wRend.sharedMaterial = woodMat;

            var wardrobeEvidence = GetOrCreateEvidenceAsset(
                "evidence_room102_wardrobe",
                "Madame Valerie's Wardrobe",
                "Locations",
                "A towering carved mahogany armoire with brass filigree handles.",
                "Madame Valerie's wardrobe. Several evening gowns hang inside. On the top shelf, an ornate jewelry case has had its lock forced; the interior velvet is bare, and Madame Valerie's prized heirloom diamond necklace is missing.",
                "evt_concierge_unlock_door"
            );
            var wardrobeInspect = wardrobe.AddComponent<InspectableEvidence>();
            wardrobeInspect.Definition = wardrobeEvidence;

            // 6. Mahogany Writing Bureau Desk
            var deskBureau = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deskBureau.name = "Prop_Room102_WritingDesk";
            deskBureau.transform.SetParent(suiteRoot.transform);
            deskBureau.transform.position = new Vector3(-3.5f, 0.55f, 16.5f);
            deskBureau.transform.localScale = new Vector3(1.4f, 1.1f, 2.2f);
            var dbRend = deskBureau.GetComponent<Renderer>();
            if (dbRend != null && woodMat != null) dbRend.sharedMaterial = woodMat;

            // 7. Physical Clue: Torn Blackmail Letter (On Bureau)
            var letterGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            letterGo.name = "Prop_TornBlackmailLetter";
            letterGo.transform.SetParent(suiteRoot.transform);
            letterGo.transform.position = new Vector3(-3.4f, 1.14f, 16.8f);
            letterGo.transform.localScale = new Vector3(0.35f, 0.02f, 0.45f);
            letterGo.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
            var lRend = letterGo.GetComponent<Renderer>();
            if (lRend != null && paperMat != null) lRend.sharedMaterial = paperMat;

            var letterEvidence = GetOrCreateEvidenceAsset(
                "evidence_blackmail_note",
                "Torn Blackmail Letter",
                "Documents",
                "A scorched and torn letter bearing a threatening ultimatum.",
                "Scorched scrap of hotel stationery found upon the writing bureau:\n\n'Madame Valerie,\nYour silence cannot be bought twice. Tonight at midnight, your secrets die with you. Meridian House will keep its peace.'\n\nSigned only with a solitary initial: '— P.'",
                "evt_concierge_unlock_door"
            );
            var letterInspect = letterGo.AddComponent<InspectableEvidence>();
            letterInspect.Definition = letterEvidence;

            // 8. Physical Clue: Monogrammed Silk Handkerchief (On Floor Beside Bed)
            var hankyGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hankyGo.name = "Prop_SilkHandkerchief";
            hankyGo.transform.SetParent(suiteRoot.transform);
            hankyGo.transform.position = new Vector3(1.6f, 0.02f, 17.5f);
            hankyGo.transform.localScale = new Vector3(0.3f, 0.02f, 0.3f);
            hankyGo.transform.rotation = Quaternion.Euler(0f, -35f, 0f);
            var hankyRend = hankyGo.GetComponent<Renderer>();
            if (hankyRend != null && mats.TryGetValue("UniformNavy", out var navyMat)) hankyRend.sharedMaterial = navyMat;

            var hankyEvidence = GetOrCreateEvidenceAsset(
                "evidence_silk_handkerchief",
                "Monogrammed Silk Handkerchief",
                "Items",
                "A fine silk handkerchief embroidered with gold thread.",
                "A pristine midnight-navy silk handkerchief embroidered with the Meridian crest and the initial 'P'. Smudged with faint traces of machine oil and soot from the concierge key cabinet.",
                "evt_concierge_unlock_door"
            );
            var hankyInspect = hankyGo.AddComponent<InspectableEvidence>();
            hankyInspect.Definition = hankyEvidence;

            // 9. Physical Clue: Heavy Brass Candlestick Murder Weapon
            var candlestickPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/HotelLobby_WallSconce.obj");
            var candleGo = SpawnProp(candlestickPrefab, "Prop_MurderWeapon_Candlestick", suiteRoot.transform,
                new Vector3(-2.8f, 0.08f, 15.2f), new Vector3(85f, 40f, 0f), new Vector3(0.7f, 0.7f, 0.7f), mats);
            var candleCol = candleGo.AddComponent<BoxCollider>();
            candleCol.center = new Vector3(0f, 0.1f, 0f);
            candleCol.size = new Vector3(0.4f, 0.4f, 0.4f);

            var weaponEvidence = GetOrCreateEvidenceAsset(
                "evidence_brass_candlestick",
                "Heavy Brass Candlestick",
                "Weapons",
                "A solid cast-brass candlestick with severe impact denting.",
                "A heavy, ornate Art Deco brass candlestick found discarded upon the carpet. The heavy base shows distinct impact blunt-force trauma markings and traces of candle wax. Matches the fatal blow delivered inside Room 102.",
                "evt_concierge_unlock_door"
            );
            var candleInspect = candleGo.AddComponent<InspectableEvidence>();
            candleInspect.Definition = weaponEvidence;

            // 10. Atmospheric Suite Lighting
            var bedLightGo = new GameObject("Room102_BedsideLamp_Light");
            bedLightGo.transform.SetParent(suiteRoot.transform);
            bedLightGo.transform.position = new Vector3(1.8f, 1.4f, 19.5f);
            var bedLight = bedLightGo.AddComponent<Light>();
            bedLight.type = LightType.Point;
            bedLight.color = new Color(1.0f, 0.85f, 0.6f);
            bedLight.intensity = 1.8f;
            bedLight.range = 7f;

            var moonGlowGo = new GameObject("Room102_WindowMoonlight");
            moonGlowGo.transform.SetParent(suiteRoot.transform);
            moonGlowGo.transform.position = new Vector3(4.8f, 2.8f, 16.5f);
            var moonLight = moonGlowGo.AddComponent<Light>();
            moonLight.type = LightType.Point;
            moonLight.color = new Color(0.45f, 0.60f, 0.90f);
            moonLight.intensity = 1.6f;
            moonLight.range = 8f;
        }

        private static void AddSconceLight(Transform parent)
        {
            var lightGo = new GameObject("Sconce_PointLight");
            lightGo.transform.SetParent(parent);
            lightGo.transform.localPosition = new Vector3(0f, 0.1f, -0.2f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.82f, 0.55f);
            light.intensity = 1.8f;
            light.range = 6.0f;
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
    }

    /// <summary>
    /// Tag marker component for cylindrical furniture.
    /// </summary>
    public class CylinderColliderSubstitute : MonoBehaviour { }
}
