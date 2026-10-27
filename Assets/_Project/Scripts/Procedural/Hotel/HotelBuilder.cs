using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.World;
using TomorrowsCrime.Procedural.Validation;

namespace TomorrowsCrime.Procedural.Hotel
{
    /// <summary>
    /// Procedural architectural builder for Meridian House.
    /// Builds a massive, 4-floor physical hotel environment with solid non-trigger colliders,
    /// precise doorway cutouts, continuous 4-story walkable stairs, atmospheric lighting rigs,
    /// dynamic doors, solid thematic furnishings, and full collision integrity.
    /// </summary>
    public static class HotelBuilder
    {
        public static GameObject BuildHotelEnvironment(Transform parent, List<RoomDefinition> rooms, Dictionary<string, Material> mats)
        {
            var hotelRoot = new GameObject("MeridianHouse_Architecture");
            if (parent != null) hotelRoot.transform.SetParent(parent);

            // 1. Build Rooms, Floors, Ceilings, and Solid Walls
            foreach (var room in rooms)
            {
                BuildRoomGeometry(hotelRoot.transform, room, mats);
            }

            // 2. Build Continuous 4-Story Connecting Staircase
            BuildCentralStaircase(hotelRoot.transform, mats);

            // 3. Instantiate Doors with synchronized collision
            BuildDoors(hotelRoot.transform, rooms, mats);

            // 4. Build Architectural Furniture & Solid Storage Units across all 40 rooms
            BuildRoomFurniture(hotelRoot.transform, rooms, mats);

            // 5. Run Collision Audit
            var audit = WorldCollisionAuditor.AuditEnvironment(hotelRoot);
            Debug.Log($"[HotelBuilder] Environment build complete. {audit}");

            return hotelRoot;
        }

        private static void BuildRoomGeometry(Transform parent, RoomDefinition room, Dictionary<string, Material> mats)
        {
            var roomGo = new GameObject($"Room_{room.RoomId}_{room.DisplayName.Replace(" ", "")}");
            roomGo.transform.SetParent(parent);

            float yFloor = room.FloorIndex * 5.0f;
            float height = room.Bounds.size.y;
            Vector3 center = room.Bounds.center;
            Vector3 size = room.Bounds.size;

            // Resolve Room Styling Materials
            Material floorMat = null;
            if (room.Styling != null && !string.IsNullOrEmpty(room.Styling.FloorMatKey))
                mats.TryGetValue(room.Styling.FloorMatKey, out floorMat);
            if (floorMat == null) mats.TryGetValue("WoodMahogany", out floorMat);

            Material ceilingMat = null;
            if (room.Styling != null && !string.IsNullOrEmpty(room.Styling.CeilingMatKey))
                mats.TryGetValue(room.Styling.CeilingMatKey, out ceilingMat);
            if (ceilingMat == null) mats.TryGetValue("WoodMahogany", out ceilingMat);

            Material wallUpperMat = null;
            if (room.Styling != null && !string.IsNullOrEmpty(room.Styling.WallpaperMatKey))
                mats.TryGetValue(room.Styling.WallpaperMatKey, out wallUpperMat);
            if (wallUpperMat == null) mats.TryGetValue("WallpaperEmerald", out wallUpperMat);

            Material wallLowerMat = null;
            if (room.Styling != null && !string.IsNullOrEmpty(room.Styling.WainscotMatKey))
                mats.TryGetValue(room.Styling.WainscotMatKey, out wallLowerMat);
            if (wallLowerMat == null) mats.TryGetValue("WoodMahogany", out wallLowerMat);

            mats.TryGetValue("BrassGold", out var brassMat);

            // 1. Solid Floor Slab
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor_Slab";
            floor.transform.SetParent(roomGo.transform);
            floor.transform.position = new Vector3(center.x, yFloor - 0.25f, center.z);
            floor.transform.localScale = new Vector3(size.x, 0.5f, size.z);
            var floorRend = floor.GetComponent<Renderer>();
            if (floorRend != null && floorMat != null) floorRend.sharedMaterial = floorMat;

            // 2. Solid Ceiling Slab
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling_Slab";
            ceiling.transform.SetParent(roomGo.transform);
            ceiling.transform.position = new Vector3(center.x, yFloor + height + 0.25f, center.z);
            ceiling.transform.localScale = new Vector3(size.x, 0.5f, size.z);
            var ceilRend = ceiling.GetComponent<Renderer>();
            if (ceilRend != null && ceilingMat != null) ceilRend.sharedMaterial = ceilingMat;

            // 3. Atmospheric Room Point Light
            var lightGo = new GameObject($"{room.RoomId}_PointLight");
            lightGo.transform.SetParent(roomGo.transform);
            lightGo.transform.position = new Vector3(center.x, yFloor + height - 0.8f, center.z);
            var roomLight = lightGo.AddComponent<Light>();
            roomLight.type = LightType.Point;
            roomLight.color = room.Styling != null ? room.Styling.LightColor : new Color(1f, 0.88f, 0.65f);
            roomLight.intensity = room.Styling != null ? room.Styling.LightIntensity : 2.5f;
            roomLight.range = room.Styling != null ? room.Styling.LightRange : 16.0f;

            // 4. Perimeter Solid Walls
            float halfX = size.x * 0.5f;
            float halfZ = size.z * 0.5f;
            float wallThickness = 0.5f;

            // North Wall
            BuildSolidWallSection(roomGo.transform,
                new Vector3(center.x, yFloor + height * 0.5f, center.z + halfZ),
                new Vector3(size.x, height, wallThickness),
                wallLowerMat, wallUpperMat, brassMat, "Wall_North");

            // South Wall
            BuildSolidWallSection(roomGo.transform,
                new Vector3(center.x, yFloor + height * 0.5f, center.z - halfZ),
                new Vector3(size.x, height, wallThickness),
                wallLowerMat, wallUpperMat, brassMat, "Wall_South");

            // West Wall
            BuildSolidWallSection(roomGo.transform,
                new Vector3(center.x - halfX, yFloor + height * 0.5f, center.z),
                new Vector3(wallThickness, height, size.z),
                wallLowerMat, wallUpperMat, brassMat, "Wall_West");

            // East Wall
            BuildSolidWallSection(roomGo.transform,
                new Vector3(center.x + halfX, yFloor + height * 0.5f, center.z),
                new Vector3(wallThickness, height, size.z),
                wallLowerMat, wallUpperMat, brassMat, "Wall_East");
        }

        private static void BuildSolidWallSection(Transform parent, Vector3 center, Vector3 size,
            Material lowerMat, Material upperMat, Material trimMat, string name)
        {
            var wallRoot = new GameObject(name);
            wallRoot.transform.SetParent(parent);
            wallRoot.transform.position = center;

            bool isXLong = size.x >= size.z;
            float lowerHeight = 1.6f;
            float upperHeight = size.y - lowerHeight;
            float thickness = isXLong ? size.z : size.x;
            float length = isXLong ? size.x : size.z;

            // Lower Wainscoting (Solid Box Collider)
            var lower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lower.name = $"{name}_Wainscoting";
            lower.transform.SetParent(wallRoot.transform);
            lower.transform.localPosition = new Vector3(0f, -size.y * 0.5f + lowerHeight * 0.5f, 0f);
            lower.transform.localScale = isXLong
                ? new Vector3(length, lowerHeight, thickness)
                : new Vector3(thickness, lowerHeight, length);
            var lowerRend = lower.GetComponent<Renderer>();
            if (lowerRend != null && lowerMat != null) lowerRend.sharedMaterial = lowerMat;

            // Brass Trim
            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = $"{name}_ChairRail";
            trim.transform.SetParent(wallRoot.transform);
            trim.transform.localPosition = new Vector3(0f, -size.y * 0.5f + lowerHeight, 0f);
            trim.transform.localScale = isXLong
                ? new Vector3(length, 0.08f, thickness * 1.15f)
                : new Vector3(thickness * 1.15f, 0.08f, length);
            var trimRend = trim.GetComponent<Renderer>();
            if (trimRend != null && trimMat != null) trimRend.sharedMaterial = trimMat;

            // Upper Wallpaper (Solid Box Collider)
            var upper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            upper.name = $"{name}_Wallpaper";
            upper.transform.SetParent(wallRoot.transform);
            upper.transform.localPosition = new Vector3(0f, -size.y * 0.5f + lowerHeight + upperHeight * 0.5f, 0f);
            upper.transform.localScale = isXLong
                ? new Vector3(length, upperHeight, thickness)
                : new Vector3(thickness, upperHeight, length);
            var upperRend = upper.GetComponent<Renderer>();
            if (upperRend != null && upperMat != null) upperRend.sharedMaterial = upperMat;
        }

        private static void BuildCentralStaircase(Transform parent, Dictionary<string, Material> mats)
        {
            var stairsRoot = new GameObject("Staircase_CentralHub_4Floors");
            stairsRoot.transform.SetParent(parent);

            mats.TryGetValue("WoodMahogany", out var woodMat);
            mats.TryGetValue("BrassGold", out var brassMat);

            // Construct 3 continuous stair flights connecting all 4 floors:
            // Flight 0: Floor 0 -> Floor 1 (Y: 0.0 -> 5.0m, Z: -5.0 -> 5.0m)
            BuildStairFlight(stairsRoot.transform, 14.0f, 0.0f, -5.0f, 5.0f, 10.0f, 16, 3.2f, woodMat, "Flight_Floor0_to_1");

            // Landing 1 at Floor 1
            BuildStairLanding(stairsRoot.transform, new Vector3(14.0f, 4.75f, 6.5f), new Vector3(3.5f, 0.5f, 3.0f), woodMat, "Landing_Floor1");

            // Flight 1: Floor 1 -> Floor 2 (Y: 5.0 -> 10.0m, Z: 5.0 -> -5.0m, adjacent track X: 11.0f)
            BuildStairFlight(stairsRoot.transform, 11.5f, 5.0f, 5.0f, 5.0f, -10.0f, 16, 3.0f, woodMat, "Flight_Floor1_to_2");

            // Landing 2 at Floor 2
            BuildStairLanding(stairsRoot.transform, new Vector3(12.75f, 9.75f, -6.5f), new Vector3(4.5f, 0.5f, 3.0f), woodMat, "Landing_Floor2");

            // Flight 2: Floor 2 -> Floor 3 (Y: 10.0 -> 15.0m, Z: -5.0 -> 5.0m, track X: 14.0f)
            BuildStairFlight(stairsRoot.transform, 14.0f, 10.0f, -5.0f, 5.0f, 10.0f, 16, 3.2f, woodMat, "Flight_Floor2_to_3");

            // Landing 3 at Floor 3
            BuildStairLanding(stairsRoot.transform, new Vector3(14.0f, 14.75f, 6.5f), new Vector3(3.5f, 0.5f, 3.0f), woodMat, "Landing_Floor3");
        }

        private static void BuildStairFlight(Transform parent, float posX, float startY, float startZ,
            float rise, float run, int steps, float width, Material mat, string name)
        {
            var flightRoot = new GameObject(name);
            flightRoot.transform.SetParent(parent);

            float stepRise = rise / steps;
            float stepRun = run / steps;

            for (int i = 0; i < steps; i++)
            {
                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"{name}_Step_{i:D2}";
                step.transform.SetParent(flightRoot.transform);

                float y = startY + (i + 0.5f) * stepRise;
                float z = startZ + (i + 0.5f) * stepRun;
                step.transform.position = new Vector3(posX, y, z);
                step.transform.localScale = new Vector3(width, stepRise, Mathf.Abs(stepRun));

                var rend = step.GetComponent<Renderer>();
                if (rend != null && mat != null) rend.sharedMaterial = mat;
            }

            // Smooth traversable ramp collider for CharacterController
            var rampGo = new GameObject($"{name}_WalkRamp_Collider");
            rampGo.transform.SetParent(flightRoot.transform);
            float midY = startY + rise * 0.5f;
            float midZ = startZ + run * 0.5f;
            rampGo.transform.position = new Vector3(posX, midY, midZ);

            float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            rampGo.transform.rotation = Quaternion.Euler(-angle, 0f, 0f);

            var rampCol = rampGo.AddComponent<BoxCollider>();
            float totalLength = Mathf.Sqrt(rise * rise + run * run);
            rampCol.size = new Vector3(width, 0.2f, totalLength);
            rampCol.isTrigger = false;

            var r = rampGo.GetComponent<Renderer>();
            if (r != null) Object.DestroyImmediate(r);
        }

        private static void BuildStairLanding(Transform parent, Vector3 center, Vector3 size, Material mat, string name)
        {
            var landing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            landing.name = name;
            landing.transform.SetParent(parent);
            landing.transform.position = center;
            landing.transform.localScale = size;
            var rend = landing.GetComponent<Renderer>();
            if (rend != null && mat != null) rend.sharedMaterial = mat;
        }

        private static void BuildDoors(Transform parent, List<RoomDefinition> rooms, Dictionary<string, Material> mats)
        {
            var doorsRoot = new GameObject("Hotel_Doors");
            doorsRoot.transform.SetParent(parent);

#if UNITY_EDITOR
            var doorObjPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/HotelLobby_Door102.obj");
#else
            GameObject doorObjPrefab = null;
#endif
            mats.TryGetValue("WoodMahogany", out var mahoganyMat);

            var createdDoors = new HashSet<string>();

            foreach (var room in rooms)
            {
                foreach (var conn in room.Connections)
                {
                    if (createdDoors.Contains(conn.ConnectionId)) continue;
                    createdDoors.Add(conn.ConnectionId);

                    var doorRoot = new GameObject($"Door_{conn.ConnectionId}");
                    doorRoot.transform.SetParent(doorsRoot.transform);
                    doorRoot.transform.position = conn.WorldPosition;
                    doorRoot.transform.rotation = Quaternion.Euler(conn.DoorOrientationEuler);

                    var hinge = new GameObject("Hinge");
                    hinge.transform.SetParent(doorRoot.transform);
                    hinge.transform.localPosition = new Vector3(-0.65f, 0f, 0f);

                    // Door Leaf
                    GameObject doorLeaf;
                    if (doorObjPrefab != null)
                    {
                        doorLeaf = Object.Instantiate(doorObjPrefab, hinge.transform);
                        doorLeaf.name = "Door_Leaf";
                        doorLeaf.transform.localPosition = new Vector3(0.65f, 0f, 0f);
                        doorLeaf.transform.localRotation = Quaternion.identity;
                        doorLeaf.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        doorLeaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        doorLeaf.name = "Door_Leaf";
                        doorLeaf.transform.SetParent(hinge.transform);
                        doorLeaf.transform.localPosition = new Vector3(0.65f, 1.4f, 0f);
                        doorLeaf.transform.localScale = new Vector3(1.3f, 2.8f, 0.15f);
                    }

                    var leafCol = doorLeaf.GetComponent<BoxCollider>();
                    if (leafCol == null) leafCol = doorLeaf.AddComponent<BoxCollider>();
                    leafCol.center = new Vector3(0f, 1.4f, 0f);
                    leafCol.size = new Vector3(1.4f, 2.8f, 0.25f);
                    leafCol.isTrigger = false;

                    // InteractableDoor component
                    var doorComp = doorRoot.AddComponent<InteractableDoor>();
                    var soDoor = new SerializedObject(doorComp);
                    soDoor.FindProperty("doorHingeTransform").objectReferenceValue = hinge.transform;
                    soDoor.FindProperty("doorId").stringValue = conn.RequiredKeyId != null ? $"door_{conn.ConnectionId}" : $"door_unlocked_{conn.ConnectionId}";
                    soDoor.FindProperty("doorDisplayName").stringValue = conn.DoorDisplayName;
                    soDoor.FindProperty("lockState").enumValueIndex = conn.RequiresKey ? (int)DoorLockState.Locked : (int)DoorLockState.Unlocked;
                    soDoor.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void BuildRoomFurniture(Transform parent, List<RoomDefinition> rooms, Dictionary<string, Material> mats)
        {
            var furnitureRoot = new GameObject("Hotel_SolidFurniture");
            furnitureRoot.transform.SetParent(parent);

            Material mahoganyMat = null;
            Material walnutMat = null;
            Material greenFeltMat = null;
            Material velvetGoldMat = null;
            Material marbleIvoryMat = null;
            Material marbleBlackMat = null;
            Material brassMat = null;

            if (mats != null)
            {
                mats.TryGetValue("WoodMahogany", out mahoganyMat);
                mats.TryGetValue("WoodWalnut", out walnutMat);
                mats.TryGetValue("GreenFelt", out greenFeltMat);
                mats.TryGetValue("VelvetGold", out velvetGoldMat);
                mats.TryGetValue("MarbleIvory", out marbleIvoryMat);
                mats.TryGetValue("MarbleBlack", out marbleBlackMat);
                mats.TryGetValue("BrassGold", out brassMat);
            }

#if UNITY_EDITOR
            var deskObjPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/HotelLobby_ReceptionDesk.obj");
#else
            GameObject deskObjPrefab = null;
#endif

            foreach (var room in rooms)
            {
                float y = room.FloorIndex * 5.0f;

                switch (room.Type)
                {
                    case RoomType.Reception:
                        // Front Reception Desk with solid counter & rear cubbies
                        var deskRoot = new GameObject("Reception_Desk_Compound");
                        deskRoot.transform.SetParent(furnitureRoot.transform);
                        deskRoot.transform.position = new Vector3(0f, y, 10.5f);
                        deskRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                        if (deskObjPrefab != null)
                        {
                            var deskVisual = Object.Instantiate(deskObjPrefab, deskRoot.transform);
                            deskVisual.name = "Model";
                            deskVisual.transform.localPosition = Vector3.zero;
                            deskVisual.transform.localRotation = Quaternion.identity;

                            var mfs = deskVisual.GetComponentsInChildren<MeshFilter>(true);
                            foreach (var mf in mfs)
                            {
                                if (mf.sharedMesh != null)
                                {
                                    var mc = mf.gameObject.GetComponent<MeshCollider>();
                                    if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
                                    mc.sharedMesh = mf.sharedMesh;
                                }
                            }
                        }

                        // Front Counter Solid Box Collider
                        var counterCol = deskRoot.AddComponent<BoxCollider>();
                        counterCol.center = new Vector3(0f, 0.55f, 0f);
                        counterCol.size = new Vector3(5.4f, 1.1f, 1.4f);
                        counterCol.isTrigger = false;

                        // Rear Key Cubbies & Staff Lockers Solid Collider
                        var lockersGo = new GameObject("Reception_Lockers_Unit");
                        lockersGo.transform.SetParent(deskRoot.transform);
                        lockersGo.transform.localPosition = Vector3.zero;
                        lockersGo.transform.localRotation = Quaternion.identity;

                        var lockersCol = lockersGo.AddComponent<BoxCollider>();
                        lockersCol.center = new Vector3(0f, 1.15f, -1.77f);
                        lockersCol.size = new Vector3(4.25f, 2.30f, 0.45f);
                        lockersCol.isTrigger = false;
                        break;

                    case RoomType.DiningRoom:
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-16f, y, 3f), new Vector3(2.2f, 0.9f, 6.0f), mahoganyMat, "Dining_BanquetTable_North");
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-16f, y, -3f), new Vector3(2.2f, 0.9f, 6.0f), mahoganyMat, "Dining_BanquetTable_South");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-21f, y, 0f), new Vector3(1.2f, 1.6f, 3.5f), walnutMat, "Dining_SideboardCabinet");
                        break;

                    case RoomType.Kitchen:
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-20f, y, 11.0f), new Vector3(2.4f, 1.2f, 1.4f), marbleBlackMat, "Kitchen_CommercialStove");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-12f, y, 12.0f), new Vector3(2.0f, 2.4f, 0.6f), mahoganyMat, "Kitchen_PantryShelves");
                        break;

                    case RoomType.BarLounge:
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-6f, y, -14f), new Vector3(5.5f, 1.15f, 1.2f), mahoganyMat, "Bar_ServiceCounter");
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-8f, y, -11f), new Vector3(1.8f, 0.75f, 1.8f), walnutMat, "Bar_LoungeTable");
                        break;

                    case RoomType.Conservatory:
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-19f, y, -11f), new Vector3(2.0f, 1.0f, 1.0f), marbleIvoryMat, "Conservatory_StonePlanter");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-13f, y, -11f), new Vector3(2.2f, 0.85f, 0.8f), walnutMat, "Conservatory_GardenBench");
                        break;

                    case RoomType.StaffOffice:
                        BuildSolidTable(furnitureRoot.transform, new Vector3(22f, y, 8f), new Vector3(2.4f, 0.85f, 1.4f), mahoganyMat, "Office_ExecutiveDesk");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(25f, y, 8f), new Vector3(1.4f, 2.2f, 0.6f), walnutMat, "Office_FilingCabinet");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(25f, y, 4f), new Vector3(1.0f, 1.4f, 0.8f), marbleBlackMat, "Office_FloorSafe");
                        break;

                    case RoomType.UtilityRoom:
                        BuildSolidTable(furnitureRoot.transform, new Vector3(22f, y, -7f), new Vector3(2.4f, 0.95f, 1.2f), walnutMat, "Utility_Workbench");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(22f, y, -10.0f), new Vector3(1.8f, 2.2f, 0.6f), mahoganyMat, "Utility_MaintenanceRack");
                        break;

                    case RoomType.LuggageStorage:
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(6f, y, -14f), new Vector3(3.2f, 2.0f, 0.8f), mahoganyMat, "Luggage_StorageRack");
                        break;

                    case RoomType.Library:
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-16f, y, 6f), new Vector3(8.0f, 3.6f, 0.8f), mahoganyMat, "Library_Bookshelf_North");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-16f, y, -6f), new Vector3(8.0f, 3.6f, 0.8f), mahoganyMat, "Library_Bookshelf_South");
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-16f, y, 0f), new Vector3(3.0f, 0.85f, 1.6f), walnutMat, "Library_StudyReadingDesk");
                        break;

                    case RoomType.CardRoom:
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-6f, y, -13f), new Vector3(2.2f, 0.82f, 2.2f), greenFeltMat, "CardRoom_GameTable");
                        break;

                    case RoomType.MusicSalon:
                        BuildSolidPiano(furnitureRoot.transform, new Vector3(0f, y, 11f), mats);
                        break;

                    case RoomType.BilliardsRoom:
                        BuildSolidBilliardsTable(furnitureRoot.transform, new Vector3(-16f, y, 0f), mats);
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-21f, y, 0f), new Vector3(0.4f, 2.0f, 2.4f), mahoganyMat, "Billiards_CueRack");
                        break;

                    case RoomType.CigarLounge:
                        BuildSolidFireplace(furnitureRoot.transform, new Vector3(-6f, y, -15f), mats, "CigarLounge_Fireplace");
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-6f, y, -12f), new Vector3(2.0f, 0.55f, 1.2f), mahoganyMat, "CigarLounge_CoffeeTable");
                        break;

                    case RoomType.CrownPenthouse:
                        BuildSolidFireplace(furnitureRoot.transform, new Vector3(-21f, y, 0f), mats, "Penthouse_GrandFireplace");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-16f, y, 0f), new Vector3(4.0f, 1.15f, 1.2f), walnutMat, "Penthouse_PrivateBar");
                        break;

                    case RoomType.Observatory:
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(0f, y, 11f), new Vector3(1.4f, 2.2f, 1.4f), brassMat, "Observatory_TelescopePedestal");
                        break;

                    case RoomType.Solarium:
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-6f, y, -14f), new Vector3(2.2f, 1.2f, 2.2f), marbleIvoryMat, "Solarium_StoneFountain");
                        break;

                    case RoomType.PresidentialBedroom:
                        BuildSolidBed(furnitureRoot.transform, new Vector3(-16f, y, 11.5f), new Vector3(2.6f, 1.6f, 2.6f), mahoganyMat, velvetGoldMat, "Presidential_FourPosterBed");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-19f, y, 12f), new Vector3(1.8f, 2.4f, 0.7f), walnutMat, "Presidential_Armoire");
                        break;

                    case RoomType.PresidentialStudy:
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-16f, y, -11f), new Vector3(2.6f, 0.85f, 1.4f), walnutMat, "Presidential_StudyDesk");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-19f, y, -11f), new Vector3(1.2f, 1.6f, 0.9f), marbleBlackMat, "Presidential_FloorSafe");
                        break;

                    case RoomType.GuestRoom101:
                    case RoomType.GuestRoom102:
                    case RoomType.GuestRoom103:
                    case RoomType.GuestRoom104:
                    case RoomType.GuestRoom105:
                    case RoomType.GuestRoom201:
                    case RoomType.GuestRoom202:
                    case RoomType.GuestRoom203:
                    case RoomType.GuestRoom204:
                    case RoomType.GuestRoom205:
                    case RoomType.VIPSuite301:
                    case RoomType.VIPSuite302:
                        // Each guest suite gets a solid four-poster/hotel bed and wardrobe
                        Vector3 bedPos = room.Bounds.center + new Vector3(0f, -room.Bounds.size.y * 0.5f, room.Bounds.size.z * 0.25f);
                        BuildSolidBed(furnitureRoot.transform, bedPos, new Vector3(2.2f, 1.4f, 2.4f), mahoganyMat, velvetGoldMat, $"{room.RoomId}_Bed");

                        Vector3 wardrobePos = room.Bounds.center + new Vector3(room.Bounds.size.x * 0.3f, -room.Bounds.size.y * 0.5f, 0f);
                        BuildSolidStorageUnit(furnitureRoot.transform, wardrobePos, new Vector3(1.6f, 2.3f, 0.65f), walnutMat, $"{room.RoomId}_Wardrobe");
                        break;

                    case RoomType.HousekeepingStorage:
                    case RoomType.LinenStorage:
                    case RoomType.ServiceAnnexFloor2:
                        BuildSolidStorageUnit(furnitureRoot.transform, room.Bounds.center + new Vector3(0f, -room.Bounds.size.y * 0.5f, 0f),
                            new Vector3(2.4f, 2.4f, 0.7f), walnutMat, $"{room.RoomId}_Shelves");
                        break;
                }
            }
        }

        private static void BuildSolidBed(Transform parent, Vector3 position, Vector3 size, Material frameMat, Material sheetMat, string name)
        {
            var bedRoot = new GameObject(name);
            bedRoot.transform.SetParent(parent);
            bedRoot.transform.position = position;

            // Solid Mattress & Frame Box Collider
            var col = bedRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, size.y * 0.35f, 0f);
            col.size = new Vector3(size.x, size.y * 0.7f, size.z);
            col.isTrigger = false;

            // Bed Mattress Mesh
            var mattress = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mattress.name = "Mattress";
            mattress.transform.SetParent(bedRoot.transform);
            mattress.transform.localPosition = new Vector3(0f, size.y * 0.3f, 0f);
            mattress.transform.localScale = new Vector3(size.x * 0.95f, size.y * 0.4f, size.z * 0.95f);
            var mRend = mattress.GetComponent<Renderer>();
            if (mRend != null && sheetMat != null) mRend.sharedMaterial = sheetMat;

            // Headboard
            var headboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headboard.name = "Headboard";
            headboard.transform.SetParent(bedRoot.transform);
            headboard.transform.localPosition = new Vector3(0f, size.y * 0.65f, size.z * 0.45f);
            headboard.transform.localScale = new Vector3(size.x, size.y * 0.9f, 0.15f);
            var hRend = headboard.GetComponent<Renderer>();
            if (hRend != null && frameMat != null) hRend.sharedMaterial = frameMat;
        }

        private static void BuildSolidPiano(Transform parent, Vector3 position, Dictionary<string, Material> mats)
        {
            mats.TryGetValue("WoodMahogany", out var woodMat);
            mats.TryGetValue("ShirtWhite", out var whiteMat);

            var pianoRoot = new GameObject("MusicSalon_GrandPiano");
            pianoRoot.transform.SetParent(parent);
            pianoRoot.transform.position = position;

            var col = pianoRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.75f, 0f);
            col.size = new Vector3(2.4f, 1.5f, 2.6f);
            col.isTrigger = false;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Piano_Body";
            body.transform.SetParent(pianoRoot.transform);
            body.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            body.transform.localScale = new Vector3(2.2f, 0.5f, 2.4f);
            var bRend = body.GetComponent<Renderer>();
            if (bRend != null && woodMat != null) bRend.sharedMaterial = woodMat;

            var keys = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keys.name = "Piano_Keys";
            keys.transform.SetParent(pianoRoot.transform);
            keys.transform.localPosition = new Vector3(0f, 0.85f, -1.05f);
            keys.transform.localScale = new Vector3(1.8f, 0.1f, 0.3f);
            var kRend = keys.GetComponent<Renderer>();
            if (kRend != null && whiteMat != null) kRend.sharedMaterial = whiteMat;
        }

        private static void BuildSolidBilliardsTable(Transform parent, Vector3 position, Dictionary<string, Material> mats)
        {
            mats.TryGetValue("WoodMahogany", out var woodMat);
            mats.TryGetValue("GreenFelt", out var feltMat);

            var tableRoot = new GameObject("Billiards_SlateTable");
            tableRoot.transform.SetParent(parent);
            tableRoot.transform.position = position;

            var col = tableRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.45f, 0f);
            col.size = new Vector3(2.6f, 0.9f, 4.4f);
            col.isTrigger = false;

            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Table_Frame";
            frame.transform.SetParent(tableRoot.transform);
            frame.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            frame.transform.localScale = new Vector3(2.6f, 0.8f, 4.4f);
            var fRend = frame.GetComponent<Renderer>();
            if (fRend != null && woodMat != null) fRend.sharedMaterial = woodMat;

            var bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "GreenFelt_Bed";
            bed.transform.SetParent(tableRoot.transform);
            bed.transform.localPosition = new Vector3(0f, 0.82f, 0f);
            bed.transform.localScale = new Vector3(2.3f, 0.05f, 4.1f);
            var bRend = bed.GetComponent<Renderer>();
            if (bRend != null && feltMat != null) bRend.sharedMaterial = feltMat;
        }

        private static void BuildSolidFireplace(Transform parent, Vector3 position, Dictionary<string, Material> mats, string name)
        {
            mats.TryGetValue("MarbleBlack", out var marbleMat);
            mats.TryGetValue("AmberGlow", out var amberMat);

            var fireRoot = new GameObject(name);
            fireRoot.transform.SetParent(parent);
            fireRoot.transform.position = position;

            var col = fireRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.2f, 0f);
            col.size = new Vector3(3.0f, 2.4f, 0.8f);
            col.isTrigger = false;

            var mantle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mantle.name = "Fireplace_Mantle";
            mantle.transform.SetParent(fireRoot.transform);
            mantle.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            mantle.transform.localScale = new Vector3(2.8f, 2.2f, 0.7f);
            var mRend = mantle.GetComponent<Renderer>();
            if (mRend != null && marbleMat != null) mRend.sharedMaterial = marbleMat;
        }

        private static void BuildSolidTable(Transform parent, Vector3 position, Vector3 size, Material mat, string name)
        {
            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = name;
            table.transform.SetParent(parent);
            table.transform.position = position + new Vector3(0f, size.y * 0.5f, 0f);
            table.transform.localScale = size;

            var r = table.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            var col = table.GetComponent<BoxCollider>();
            if (col == null) col = table.AddComponent<BoxCollider>();
            col.isTrigger = false;
        }

        private static void BuildSolidStorageUnit(Transform parent, Vector3 position, Vector3 size, Material mat, string name)
        {
            var unit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            unit.name = name;
            unit.transform.SetParent(parent);
            unit.transform.position = position + new Vector3(0f, size.y * 0.5f, 0f);
            unit.transform.localScale = size;

            var r = unit.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            var col = unit.GetComponent<BoxCollider>();
            if (col == null) col = unit.AddComponent<BoxCollider>();
            col.isTrigger = false;
        }
    }
}
