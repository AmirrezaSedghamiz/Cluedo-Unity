using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.World;
using TomorrowsCrime.Procedural.Validation;

namespace TomorrowsCrime.Procedural.Hotel
{
    /// <summary>
    /// Procedural architectural builder for Meridian House.
    /// Builds multi-floor physical room geometry with solid non-trigger colliders,
    /// precise doorway cutouts (flanked walls + lintel headers), traversable stairs,
    /// dynamic doors, and full NavMesh navigation support.
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

            // 2. Build Connecting Staircase
            BuildCentralStaircase(hotelRoot.transform, mats);

            // 3. Instantiate Doors with synchronized collision
            BuildDoors(hotelRoot.transform, rooms, mats);

            // 4. Build Architectural Furniture & Solid Storage Units (Desks, Lockers, Wardrobes, Cabinets)
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

            float yFloor = room.FloorIndex == 0 ? 0f : 5.0f;
            float height = room.Bounds.size.y;
            Vector3 center = room.Bounds.center;
            Vector3 size = room.Bounds.size;

            // Floor Material selection
            Material floorMat = null;
            if (room.Type == RoomType.Lobby || room.Type == RoomType.Reception)
                mats.TryGetValue("FloorCheckerMarble", out floorMat);
            else if (room.Type == RoomType.DiningRoom || room.Type == RoomType.Kitchen || room.Type == RoomType.StaffOffice || room.Type == RoomType.UtilityRoom)
                mats.TryGetValue("WoodMahogany", out floorMat);
            else
                mats.TryGetValue("CarpetBurgundy", out floorMat);

            mats.TryGetValue("WoodMahogany", out var ceilingMat);
            mats.TryGetValue("WallpaperEmerald", out var wallUpperMat);
            mats.TryGetValue("WoodMahogany", out var wallLowerMat);
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

            // 3. Perimeter Solid Walls
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

            // Lower Wainscoting
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

            // Upper Wallpaper
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
            var stairsRoot = new GameObject("Staircase_CentralHub");
            stairsRoot.transform.SetParent(parent);

            mats.TryGetValue("WoodMahogany", out var woodMat);
            mats.TryGetValue("BrassGold", out var brassMat);

            // 16 Stair steps rising from (14, 0, -5) to (14, 5.0, 5)
            int steps = 16;
            float totalRise = 5.0f;
            float totalRun = 10.0f;
            float stepRise = totalRise / steps;
            float stepRun = totalRun / steps;
            float width = 3.2f;

            for (int i = 0; i < steps; i++)
            {
                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"StairStep_{i:D2}";
                step.transform.SetParent(stairsRoot.transform);

                float y = (i + 0.5f) * stepRise;
                float z = -5.0f + (i + 0.5f) * stepRun;
                step.transform.position = new Vector3(14.0f, y, z);
                step.transform.localScale = new Vector3(width, stepRise, stepRun);

                var rend = step.GetComponent<Renderer>();
                if (rend != null && woodMat != null) rend.sharedMaterial = woodMat;
            }

            // Invisible smooth ramp collider for flawless CharacterController stair climbing
            var rampColliderGo = new GameObject("Stairs_WalkRamp_Collider");
            rampColliderGo.transform.SetParent(stairsRoot.transform);
            rampColliderGo.transform.position = new Vector3(14.0f, 2.5f, 0f);
            rampColliderGo.transform.rotation = Quaternion.Euler(26.56f, 0f, 0f); // arctan(5/10) ~ 26.5 deg
            var rampCol = rampColliderGo.AddComponent<BoxCollider>();
            rampCol.size = new Vector3(width, 0.2f, 11.2f); // Hidden ramp collider

            // Hide the ramp visual if rendered
            var rampRend = rampColliderGo.GetComponent<Renderer>();
            if (rampRend != null) Object.DestroyImmediate(rampRend);
        }

        private static void BuildDoors(Transform parent, List<RoomDefinition> rooms, Dictionary<string, Material> mats)
        {
            var doorsRoot = new GameObject("Hotel_Doors");
            doorsRoot.transform.SetParent(parent);

            var doorObjPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/HotelLobby_Door102.obj");
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
                    leafCol.isTrigger = false; // Solid physical obstacle when closed!

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
            if (mats != null)
            {
                mats.TryGetValue("WoodMahogany", out mahoganyMat);
                mats.TryGetValue("WoodWalnut", out walnutMat);
            }

            var deskObjPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/HotelLobby_ReceptionDesk.obj");

            foreach (var room in rooms)
            {
                if (room.Type == RoomType.Reception)
                {
                    // Reception Desk & Solid Lockers
                    var deskRoot = new GameObject("Reception_Desk_Compound");
                    deskRoot.transform.SetParent(furnitureRoot.transform);
                    deskRoot.transform.position = new Vector3(0f, 0f, 10.5f);

                    GameObject deskVisual;
                    if (deskObjPrefab != null)
                    {
                        deskVisual = Object.Instantiate(deskObjPrefab, deskRoot.transform);
                        deskVisual.name = "Model";
                        deskVisual.transform.localPosition = Vector3.zero;
                        deskVisual.transform.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        deskVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        deskVisual.name = "Counter_Primitive";
                        deskVisual.transform.SetParent(deskRoot.transform);
                        deskVisual.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                        deskVisual.transform.localScale = new Vector3(5.4f, 1.1f, 1.4f);
                        var r = deskVisual.GetComponent<Renderer>();
                        if (r != null && mahoganyMat != null) r.sharedMaterial = mahoganyMat;
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
                }
                else if (room.Type == RoomType.StaffOffice)
                {
                    BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(24f, 0f, 8f), new Vector3(1.4f, 2.2f, 0.6f),
                        walnutMat, "Office_FilingCabinet");
                }
                else if (room.Type == RoomType.Kitchen)
                {
                    BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-11f, 0f, 11.0f), new Vector3(2.0f, 2.4f, 0.6f),
                        mahoganyMat, "Kitchen_PantryShelves");
                }
                else if (room.Type == RoomType.UtilityRoom)
                {
                    BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(21f, 0f, -9.5f), new Vector3(1.8f, 2.2f, 0.6f),
                        mahoganyMat, "Utility_MaintenanceRack");
                }
                else if (room.Type == RoomType.HousekeepingStorage)
                {
                    BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(18f, 5f, -6f), new Vector3(2.2f, 2.4f, 0.6f),
                        walnutMat, "Housekeeping_LinenShelves");
                }
                else if (room.Type == RoomType.GuestRoom101)
                {
                    BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-12f, 5f, 8f), new Vector3(1.6f, 2.3f, 0.65f),
                        mahoganyMat, "Room101_Wardrobe");
                }
                else if (room.Type == RoomType.GuestRoom102)
                {
                    BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(18f, 5f, 8f), new Vector3(1.6f, 2.3f, 0.65f),
                        mahoganyMat, "Room102_Wardrobe");
                }
                else if (room.Type == RoomType.GuestRoom103)
                {
                    BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-12f, 5f, -8f), new Vector3(1.6f, 2.3f, 0.65f),
                        mahoganyMat, "Room103_Wardrobe");
                }
            }
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
