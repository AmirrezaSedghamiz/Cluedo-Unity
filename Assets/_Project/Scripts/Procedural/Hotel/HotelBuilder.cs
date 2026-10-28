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
    /// Builds a massive, 4-floor physical hotel environment with continuous solid foundation plinths,
    /// precise doorway cutouts, open sightline archways, a functional 4-floor passenger elevator,
    /// a continuous 4-story walkable staircase, atmospheric lighting, dynamic doors, and solid furnishings.
    /// </summary>
    public static class HotelBuilder
    {
        private enum WallSide { North, South, West, East }

        public static GameObject BuildHotelEnvironment(Transform parent, List<RoomDefinition> rooms, Dictionary<string, Material> mats)
        {
            var hotelRoot = new GameObject("MeridianHouse_Architecture");
            if (parent != null) hotelRoot.transform.SetParent(parent);

            // 0. Ensure Reciprocal Connections across all rooms so no doorway is ever walled over from either side!
            var roomMap = new Dictionary<string, RoomDefinition>();
            foreach (var r in rooms) roomMap[r.RoomId] = r;

            foreach (var r in rooms)
            {
                var connsSnapshot = new List<DoorwayConnection>(r.Connections);
                foreach (var conn in connsSnapshot)
                {
                    if (roomMap.TryGetValue(conn.ToRoomId, out var targetRoom))
                    {
                        bool exists = false;
                        foreach (var tc in targetRoom.Connections)
                        {
                            if (tc.ConnectionId == conn.ConnectionId || Vector3.Distance(tc.WorldPosition, conn.WorldPosition) < 0.6f)
                            {
                                exists = true;
                                break;
                            }
                        }
                        if (!exists)
                        {
                            targetRoom.AddConnection(new DoorwayConnection(
                                conn.ConnectionId,
                                conn.ToRoomId,
                                conn.FromRoomId,
                                conn.WorldPosition,
                                conn.DoorOrientationEuler,
                                conn.RequiresKey,
                                conn.RequiredKeyId,
                                conn.DoorDisplayName
                            ));
                        }
                    }
                }
            }

            // 1. Build Subterranean Safety Plinths for all 4 floors (Zero falling through floor, zero z-fighting)
            BuildFloorFoundationPlinths(hotelRoot.transform, mats);

            // 2. Build Rooms, Floors, Ceilings, and Solid Walls with Multi-Doorway Cutouts
            foreach (var room in rooms)
            {
                BuildRoomGeometry(hotelRoot.transform, room, mats);
            }

            // 3. Build Continuous 4-Story Switchback Grand Staircase
            BuildCentralStaircase(hotelRoot.transform, mats);

            // 4. Build Interactive 4-Floor Passenger Elevator
            BuildHotelElevator(hotelRoot.transform, mats);

            // 5. Instantiate Doors with inward room swing
            BuildDoors(hotelRoot.transform, rooms, roomMap, mats);

            // 6. Build Architectural Furniture & Solid Storage Units across all 40 rooms
            BuildRoomFurniture(hotelRoot.transform, rooms, mats);

            // 7. Run Collision Audit
            var audit = WorldCollisionAuditor.AuditEnvironment(hotelRoot);
            Debug.Log($"[HotelBuilder] Environment build complete. {audit}");

            return hotelRoot;
        }

        private static void BuildFloorFoundationPlinths(Transform parent, Dictionary<string, Material> mats)
        {
            var plinthsRoot = new GameObject("Hotel_ContinuousFloorPlinths");
            plinthsRoot.transform.SetParent(parent);

            for (int f = 0; f < 4; f++)
            {
                float yFloor = f * 5.0f;
                var plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plinth.name = $"FoundationPlinth_Floor_{f}";
                plinth.transform.SetParent(plinthsRoot.transform);
                // Positioned subterranean: top face at yFloor - 0.25f (strictly below floor slab top at yFloor)
                plinth.transform.position = new Vector3(3.0f, yFloor - 0.50f, 0.0f);
                plinth.transform.localScale = new Vector3(60.0f, 0.5f, 44.0f);

                var col = plinth.GetComponent<BoxCollider>();
                if (col == null) col = plinth.AddComponent<BoxCollider>();
                col.center = Vector3.zero;
                col.size = Vector3.one;
                col.isTrigger = false;

                // Disable renderer entirely: pure bedrock collision shield, zero rendering conflicts or z-fighting!
                var rend = plinth.GetComponent<Renderer>();
                if (rend != null) rend.enabled = false;
            }
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
            if (room.Styling != null && !string.IsNullOrEmpty(room.Styling.FloorMatKey) && mats != null)
                mats.TryGetValue(room.Styling.FloorMatKey, out floorMat);
            if (floorMat == null && mats != null) mats.TryGetValue("WoodMahogany", out floorMat);

            Material ceilingMat = null;
            if (room.Styling != null && !string.IsNullOrEmpty(room.Styling.CeilingMatKey) && mats != null)
                mats.TryGetValue(room.Styling.CeilingMatKey, out ceilingMat);
            if (ceilingMat == null && mats != null) mats.TryGetValue("WoodMahogany", out ceilingMat);

            Material wallUpperMat = null;
            if (room.Styling != null && !string.IsNullOrEmpty(room.Styling.WallpaperMatKey) && mats != null)
                mats.TryGetValue(room.Styling.WallpaperMatKey, out wallUpperMat);
            if (wallUpperMat == null && mats != null) mats.TryGetValue("WallpaperEmerald", out wallUpperMat);

            Material wallLowerMat = null;
            if (room.Styling != null && !string.IsNullOrEmpty(room.Styling.WainscotMatKey) && mats != null)
                mats.TryGetValue(room.Styling.WainscotMatKey, out wallLowerMat);
            if (wallLowerMat == null && mats != null) mats.TryGetValue("WoodMahogany", out wallLowerMat);

            Material brassMat = null;
            if (mats != null) mats.TryGetValue("BrassGold", out brassMat);

            // 1. Room Floor Slabs
            bool isUpperCorridor = room.FloorIndex > 0 &&
                (room.Type == RoomType.UpperLanding || room.Type == RoomType.Floor2Promenade || room.Type == RoomType.Floor3SkyFoyer);

            if (isUpperCorridor)
            {
                // Leave open stairwell atrium at X in [4.5, 12.5], Z in [-3.0, 0.2]
                // 1A. North walkway floor (Z in [0.2, 3.0], X in [-4, 16])
                float northFloorCenterZ = 1.6f;
                float northFloorDepth = 2.8f;
                var floorNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorNorth.name = "Floor_Slab_NorthWalkway";
                floorNorth.transform.SetParent(roomGo.transform);
                floorNorth.transform.position = new Vector3(center.x, yFloor - 0.10f, northFloorCenterZ);
                floorNorth.transform.localScale = new Vector3(size.x, 0.2f, northFloorDepth);
                var fRendN = floorNorth.GetComponent<Renderer>();
                if (fRendN != null && floorMat != null) fRendN.sharedMaterial = floorMat;

                // 1B. West floor slab (X in [-4.0, 4.5], Z in [-3.0, 0.2])
                var floorWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorWest.name = "Floor_Slab_WestLanding";
                floorWest.transform.SetParent(roomGo.transform);
                floorWest.transform.position = new Vector3(0.25f, yFloor - 0.10f, -1.4f);
                floorWest.transform.localScale = new Vector3(8.5f, 0.2f, 3.2f);
                var fRendW = floorWest.GetComponent<Renderer>();
                if (fRendW != null && floorMat != null) fRendW.sharedMaterial = floorMat;

                // 1C. East elevator landing floor slab (X in [12.5, 16.0], Z in [-3.0, 0.2])
                var floorEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorEast.name = "Floor_Slab_EastLanding";
                floorEast.transform.SetParent(roomGo.transform);
                floorEast.transform.position = new Vector3(14.25f, yFloor - 0.10f, -1.4f);
                floorEast.transform.localScale = new Vector3(3.5f, 0.2f, 3.2f);
                var fRendE = floorEast.GetComponent<Renderer>();
                if (fRendE != null && floorMat != null) fRendE.sharedMaterial = floorMat;

                // 1D. Protective Art Deco Brass Balustrade Railing along the stair opening edge (Z = 0.2f, X in [4.5, 12.5])
                BuildCabinRailing(roomGo.transform, new Vector3(8.5f, yFloor + 0.6f, 0.2f), new Vector3(8.0f, 1.1f, 0.1f), brassMat, "StairAtrium_GuardRailing");
            }
            else
            {
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Floor_Slab";
                floor.transform.SetParent(roomGo.transform);
                floor.transform.position = new Vector3(center.x, yFloor - 0.10f, center.z);
                floor.transform.localScale = new Vector3(size.x, 0.2f, size.z);
                var floorRend = floor.GetComponent<Renderer>();
                if (floorRend != null && floorMat != null) floorRend.sharedMaterial = floorMat;
            }

            // 2. Room Ceiling Slab
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

            // 4. Perimeter Walls with Multi-Doorway Cutouts & Inward Placement
            BuildWallWithDoorwayCutouts(roomGo.transform, room, WallSide.North, yFloor, height, wallLowerMat, wallUpperMat, brassMat);
            BuildWallWithDoorwayCutouts(roomGo.transform, room, WallSide.South, yFloor, height, wallLowerMat, wallUpperMat, brassMat);
            BuildWallWithDoorwayCutouts(roomGo.transform, room, WallSide.West, yFloor, height, wallLowerMat, wallUpperMat, brassMat);
            BuildWallWithDoorwayCutouts(roomGo.transform, room, WallSide.East, yFloor, height, wallLowerMat, wallUpperMat, brassMat);
        }

        private static void BuildWallWithDoorwayCutouts(Transform parent, RoomDefinition room, WallSide side,
            float yFloor, float height, Material lowerMat, Material upperMat, Material trimMat)
        {
            Vector3 center = room.Bounds.center;
            Vector3 size = room.Bounds.size;
            float halfX = size.x * 0.5f;
            float halfZ = size.z * 0.5f;
            float wallThickness = 0.20f;
            float halfThick = wallThickness * 0.5f;

            // Gather ALL doorway connections on this wall
            var wallConns = new List<DoorwayConnection>();
            foreach (var conn in room.Connections)
            {
                if (side == WallSide.North && Mathf.Abs(conn.WorldPosition.z - (center.z + halfZ)) < 1.0f && conn.WorldPosition.x >= center.x - halfX - 0.5f && conn.WorldPosition.x <= center.x + halfX + 0.5f)
                {
                    wallConns.Add(conn);
                }
                else if (side == WallSide.South && Mathf.Abs(conn.WorldPosition.z - (center.z - halfZ)) < 1.0f && conn.WorldPosition.x >= center.x - halfX - 0.5f && conn.WorldPosition.x <= center.x + halfX + 0.5f)
                {
                    wallConns.Add(conn);
                }
                else if (side == WallSide.West && Mathf.Abs(conn.WorldPosition.x - (center.x - halfX)) < 1.0f && conn.WorldPosition.z >= center.z - halfZ - 0.5f && conn.WorldPosition.z <= center.z + halfZ + 0.5f)
                {
                    wallConns.Add(conn);
                }
                else if (side == WallSide.East && Mathf.Abs(conn.WorldPosition.x - (center.x + halfX)) < 1.0f && conn.WorldPosition.z >= center.z - halfZ - 0.5f && conn.WorldPosition.z <= center.z + halfZ + 0.5f)
                {
                    wallConns.Add(conn);
                }
            }

            string sideName = $"Wall_{side}";
            float doorOpeningHeight = 2.8f;
            float lintelHeight = height - doorOpeningHeight;

            if (side == WallSide.North || side == WallSide.South)
            {
                // Inward wall placement: offset inward so adjacent partition walls sit back-to-back at boundary with ZERO overlap
                float wallZ = side == WallSide.North ? (center.z + halfZ - halfThick) : (center.z - halfZ + halfThick);
                float minX = center.x - halfX;
                float maxX = center.x + halfX;

                // Sort doorways from West to East (ascending X)
                wallConns.Sort((a, b) => a.WorldPosition.x.CompareTo(b.WorldPosition.x));

                if (wallConns.Count == 0)
                {
                    BuildSolidWallSection(parent,
                        new Vector3(center.x, yFloor + height * 0.5f, wallZ),
                        new Vector3(size.x, height, wallThickness),
                        lowerMat, upperMat, trimMat, sideName);
                    return;
                }

                float currX = minX;
                for (int i = 0; i < wallConns.Count; i++)
                {
                    var conn = wallConns[i];
                    float doorX = conn.WorldPosition.x;
                    float doorWidth = (conn.DoorDisplayName.Contains("Archway") || conn.DoorDisplayName.Contains("Double")) ? 3.4f : 2.2f;
                    float doorLeft = doorX - doorWidth * 0.5f;
                    float doorRight = doorX + doorWidth * 0.5f;

                    // Wall segment before door
                    float segWidth = doorLeft - currX;
                    if (segWidth > 0.15f)
                    {
                        float segCenterX = currX + segWidth * 0.5f;
                        BuildSolidWallSection(parent,
                            new Vector3(segCenterX, yFloor + height * 0.5f, wallZ),
                            new Vector3(segWidth, height, wallThickness),
                            lowerMat, upperMat, trimMat, $"{sideName}_Segment_{i}");
                    }

                    // Overhead lintel header
                    string lintelName = wallConns.Count == 1 ? $"{sideName}_DoorLintel" : $"{sideName}_DoorLintel_{i}";
                    BuildSolidWallSection(parent,
                        new Vector3(doorX, yFloor + doorOpeningHeight + lintelHeight * 0.5f, wallZ),
                        new Vector3(doorWidth, lintelHeight, wallThickness),
                        lowerMat, upperMat, trimMat, lintelName);

                    currX = doorRight;
                }

                // Final wall segment after last door
                float finalWidth = maxX - currX;
                if (finalWidth > 0.15f)
                {
                    float finalCenterX = currX + finalWidth * 0.5f;
                    BuildSolidWallSection(parent,
                        new Vector3(finalCenterX, yFloor + height * 0.5f, wallZ),
                        new Vector3(finalWidth, height, wallThickness),
                        lowerMat, upperMat, trimMat, $"{sideName}_Segment_End");
                }
            }
            else // West or East Wall
            {
                // Inward wall placement: offset inward so adjacent partition walls sit back-to-back at boundary with ZERO overlap
                float wallX = side == WallSide.West ? (center.x - halfX + halfThick) : (center.x + halfX - halfThick);
                float minZ = center.z - halfZ;
                float maxZ = center.z + halfZ;

                // Sort doorways from South to North (ascending Z)
                wallConns.Sort((a, b) => a.WorldPosition.z.CompareTo(b.WorldPosition.z));

                if (wallConns.Count == 0)
                {
                    BuildSolidWallSection(parent,
                        new Vector3(wallX, yFloor + height * 0.5f, center.z),
                        new Vector3(wallThickness, height, size.z),
                        lowerMat, upperMat, trimMat, sideName);
                    return;
                }

                float currZ = minZ;
                for (int i = 0; i < wallConns.Count; i++)
                {
                    var conn = wallConns[i];
                    float doorZ = conn.WorldPosition.z;
                    float doorWidth = (conn.DoorDisplayName.Contains("Archway") || conn.DoorDisplayName.Contains("Double")) ? 3.4f : 2.2f;
                    float doorSouth = doorZ - doorWidth * 0.5f;
                    float doorNorth = doorZ + doorWidth * 0.5f;

                    // Wall segment before door
                    float segLength = doorSouth - currZ;
                    if (segLength > 0.15f)
                    {
                        float segCenterZ = currZ + segLength * 0.5f;
                        BuildSolidWallSection(parent,
                            new Vector3(wallX, yFloor + height * 0.5f, segCenterZ),
                            new Vector3(wallThickness, height, segLength),
                            lowerMat, upperMat, trimMat, $"{sideName}_Segment_{i}");
                    }

                    // Overhead lintel header
                    string lintelName = wallConns.Count == 1 ? $"{sideName}_DoorLintel" : $"{sideName}_DoorLintel_{i}";
                    BuildSolidWallSection(parent,
                        new Vector3(wallX, yFloor + doorOpeningHeight + lintelHeight * 0.5f, doorZ),
                        new Vector3(wallThickness, lintelHeight, doorWidth),
                        lowerMat, upperMat, trimMat, lintelName);

                    currZ = doorNorth;
                }

                // Final wall segment after last door
                float finalLength = maxZ - currZ;
                if (finalLength > 0.15f)
                {
                    float finalCenterZ = currZ + finalLength * 0.5f;
                    BuildSolidWallSection(parent,
                        new Vector3(wallX, yFloor + height * 0.5f, finalCenterZ),
                        new Vector3(wallThickness, height, finalLength),
                        lowerMat, upperMat, trimMat, $"{sideName}_Segment_End");
                }
            }
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

            // Brass Trim (Chair Rail)
            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = $"{name}_ChairRail";
            trim.transform.SetParent(wallRoot.transform);
            trim.transform.localPosition = new Vector3(0f, -size.y * 0.5f + lowerHeight, 0f);
            trim.transform.localScale = isXLong
                ? new Vector3(length, 0.08f, thickness * 1.08f)
                : new Vector3(thickness * 1.08f, 0.08f, length);
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

            Material woodMat = null;
            if (mats != null) mats.TryGetValue("WoodMahogany", out woodMat);

            // Grand Switchback Staircase connecting all 4 floors:
            // Track A at Z = -2.0m (width 1.8m), Track B at Z = -0.5m (width 1.8m)
            // Walkway at Z in [0.5m, 3.0m] is clear and unobstructed on all 4 floors!

            // Flight 0: Floor 0 -> Floor 1 (Track A: X = 5.0 -> 12.0m, Y = 0.0 -> 5.0m, Z = -2.0m)
            BuildStairFlightX(stairsRoot.transform, 5.0f, 0.0f, -2.0f, 5.0f, 7.0f, 16, 1.8f, woodMat, "Flight_Floor0_to_1");

            // Landing 1 at Floor 1 connecting Track A and Track B (X in [12.0, 14.5], Z in [-2.9, 0.4])
            BuildStairLanding(stairsRoot.transform, new Vector3(13.25f, 4.85f, -1.25f), new Vector3(2.5f, 0.3f, 3.2f), woodMat, "Landing_Floor1");
            BuildLandingLight(stairsRoot.transform, new Vector3(13.25f, 8.5f, -1.25f), "StairLight_Floor1");

            // Flight 1: Floor 1 -> Floor 2 (Track B: X = 12.0 -> 5.0m, Y = 5.0 -> 10.0m, Z = -0.5m)
            BuildStairFlightX(stairsRoot.transform, 12.0f, 5.0f, -0.5f, 5.0f, -7.0f, 16, 1.8f, woodMat, "Flight_Floor1_to_2");

            // Landing 2 at Floor 2 connecting Track B and Track A (X in [3.0, 5.5], Z in [-2.9, 0.4])
            BuildStairLanding(stairsRoot.transform, new Vector3(4.25f, 9.85f, -1.25f), new Vector3(2.5f, 0.3f, 3.2f), woodMat, "Landing_Floor2");
            BuildLandingLight(stairsRoot.transform, new Vector3(4.25f, 13.5f, -1.25f), "StairLight_Floor2");

            // Flight 2: Floor 2 -> Floor 3 (Track A: X = 5.0 -> 12.0m, Y = 10.0 -> 15.0m, Z = -2.0f)
            BuildStairFlightX(stairsRoot.transform, 5.0f, 10.0f, -2.0f, 5.0f, 7.0f, 16, 1.8f, woodMat, "Flight_Floor2_to_3");

            // Landing 3 at Floor 3 (X in [12.0, 14.5], Z in [-2.9, 0.4])
            BuildStairLanding(stairsRoot.transform, new Vector3(13.25f, 14.85f, -1.25f), new Vector3(2.5f, 0.3f, 3.2f), woodMat, "Landing_Floor3");
            BuildLandingLight(stairsRoot.transform, new Vector3(13.25f, 18.5f, -1.25f), "StairLight_Floor3");
        }

        private static void BuildLandingLight(Transform parent, Vector3 position, string name)
        {
            var lightGo = new GameObject(name);
            lightGo.transform.SetParent(parent);
            lightGo.transform.position = position;
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.90f, 0.72f);
            l.intensity = 2.2f;
            l.range = 14.0f;
        }

        private static void BuildStairFlightX(Transform parent, float startX, float startY, float posZ,
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

                float x = startX + (i + 0.5f) * stepRun;
                float y = startY + (i + 0.5f) * stepRise;
                step.transform.position = new Vector3(x, y, posZ);
                step.transform.localScale = new Vector3(Mathf.Abs(stepRun), stepRise, width);

                var rend = step.GetComponent<Renderer>();
                if (rend != null && mat != null) rend.sharedMaterial = mat;
            }

            // Smooth traversable ramp collider for CharacterController
            var rampGo = new GameObject($"{name}_WalkRamp_Collider");
            rampGo.transform.SetParent(flightRoot.transform);
            float midX = startX + run * 0.5f;
            float midY = startY + rise * 0.5f;
            rampGo.transform.position = new Vector3(midX, midY, posZ);

            float angle = Mathf.Atan2(rise, Mathf.Abs(run)) * Mathf.Rad2Deg;
            rampGo.transform.rotation = Quaternion.Euler(0f, 0f, (run > 0f ? -angle : angle));

            var rampCol = rampGo.AddComponent<BoxCollider>();
            float totalLength = Mathf.Sqrt(rise * rise + run * run);
            rampCol.size = new Vector3(totalLength, 0.2f, width);
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

        private static void BuildHotelElevator(Transform parent, Dictionary<string, Material> mats)
        {
            var elevatorRoot = new GameObject("Hotel_ArtDecoElevator");
            elevatorRoot.transform.SetParent(parent);

            Material brassMat = null;
            Material woodMat = null;
            Material blackMarbleMat = null;
            Material marbleMat = null;

            if (mats != null)
            {
                mats.TryGetValue("BrassGold", out brassMat);
                mats.TryGetValue("WoodMahogany", out woodMat);
                mats.TryGetValue("MarbleBlack", out blackMarbleMat);
                mats.TryGetValue("FloorCheckerMarble", out marbleMat);
            }

            Vector3 elevatorPos = new Vector3(14.0f, 0f, 0f);

            // 1. Vertical Elevator Shaft Frame (4 Brass Columns rising 20m)
            var shaftRoot = new GameObject("Elevator_Shaft_Frame");
            shaftRoot.transform.SetParent(elevatorRoot.transform);
            shaftRoot.transform.position = elevatorPos;

            float shaftHeight = 19.5f;
            float halfW = 1.55f;
            Vector3[] colOffsets = {
                new Vector3(-halfW, shaftHeight * 0.5f, -halfW),
                new Vector3(halfW, shaftHeight * 0.5f, -halfW),
                new Vector3(-halfW, shaftHeight * 0.5f, halfW),
                new Vector3(halfW, shaftHeight * 0.5f, halfW)
            };

            for (int i = 0; i < 4; i++)
            {
                var col = GameObject.CreatePrimitive(PrimitiveType.Cube);
                col.name = $"Shaft_Column_{i}";
                col.transform.SetParent(shaftRoot.transform);
                col.transform.localPosition = colOffsets[i];
                col.transform.localScale = new Vector3(0.25f, shaftHeight, 0.25f);
                var r = col.GetComponent<Renderer>();
                if (r != null && brassMat != null) r.sharedMaterial = brassMat;
            }

            // 2. Elevator Moving Cabin
            var cabinRoot = new GameObject("Elevator_Cabin");
            cabinRoot.transform.SetParent(elevatorRoot.transform);
            cabinRoot.transform.position = elevatorPos;

            var elevatorComp = cabinRoot.AddComponent<HotelElevator>();

            // Solid Floor Slab with BoxCollider
            var cabinFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinFloor.name = "Cabin_Floor_Slab";
            cabinFloor.transform.SetParent(cabinRoot.transform);
            cabinFloor.transform.localPosition = new Vector3(0f, -0.10f, 0f);
            cabinFloor.transform.localScale = new Vector3(2.8f, 0.2f, 2.8f);
            var fRend = cabinFloor.GetComponent<Renderer>();
            if (fRend != null && marbleMat != null) fRend.sharedMaterial = marbleMat;

            // Solid Ceiling Slab with Chandelier
            var cabinCeil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinCeil.name = "Cabin_Ceiling_Slab";
            cabinCeil.transform.SetParent(cabinRoot.transform);
            cabinCeil.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            cabinCeil.transform.localScale = new Vector3(2.8f, 0.25f, 2.8f);
            var cRend = cabinCeil.GetComponent<Renderer>();
            if (cRend != null && woodMat != null) cRend.sharedMaterial = woodMat;

            // Interior Crystal Chandelier Light
            var cabinLightGo = new GameObject("Cabin_CrystalLight");
            cabinLightGo.transform.SetParent(cabinRoot.transform);
            cabinLightGo.transform.localPosition = new Vector3(0f, 2.8f, 0f);
            var cLight = cabinLightGo.AddComponent<Light>();
            cLight.type = LightType.Point;
            cLight.color = new Color(1.0f, 0.90f, 0.70f);
            cLight.intensity = 2.4f;
            cLight.range = 8.0f;

            // Panoramic Brass Railing Walls (North, South, East sides - open West entrance facing corridor)
            BuildCabinRailing(cabinRoot.transform, new Vector3(0f, 1.2f, 1.35f), new Vector3(2.6f, 2.2f, 0.1f), brassMat, "Cabin_Wall_North");
            BuildCabinRailing(cabinRoot.transform, new Vector3(0f, 1.2f, -1.35f), new Vector3(2.6f, 2.2f, 0.1f), brassMat, "Cabin_Wall_South");
            BuildCabinRailing(cabinRoot.transform, new Vector3(1.35f, 1.2f, 0f), new Vector3(0.1f, 2.2f, 2.6f), brassMat, "Cabin_Wall_East");

            // 3. Inside Cabin Interactive Control Panel
            var panelGo = new GameObject("Elevator_ControlPanel");
            panelGo.transform.SetParent(cabinRoot.transform);
            panelGo.transform.localPosition = new Vector3(1.15f, 1.4f, 0.5f);

            var panelPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panelPlate.name = "Panel_Backplate";
            panelPlate.transform.SetParent(panelGo.transform);
            panelPlate.transform.localPosition = Vector3.zero;
            panelPlate.transform.localScale = new Vector3(0.08f, 1.2f, 0.45f);
            var pRend = panelPlate.GetComponent<Renderer>();
            if (pRend != null && blackMarbleMat != null) pRend.sharedMaterial = blackMarbleMat;

            string[] floorNames = { "Grand Lobby", "Guest Suites & Library", "Deluxe Suites & Salon", "Sky Penthouse & Observatory" };
            for (int f = 0; f < 4; f++)
            {
                var btnGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                btnGo.name = $"Button_Floor_{f}";
                btnGo.transform.SetParent(panelGo.transform);
                btnGo.transform.localPosition = new Vector3(-0.06f, 0.42f - f * 0.26f, 0f);
                btnGo.transform.localScale = new Vector3(0.08f, 0.16f, 0.32f);
                var bRend = btnGo.GetComponent<Renderer>();
                if (bRend != null && brassMat != null) bRend.sharedMaterial = brassMat;

                var btnInteract = btnGo.AddComponent<InteractableElevatorButton>();
                btnInteract.Elevator = elevatorComp;
                btnInteract.TargetFloor = f;
                btnInteract.FloorDisplayName = floorNames[f];
                btnInteract.IsCallButton = false;
            }

            // 4. Outside Call Buttons on Each Floor Landing (Floors 0, 1, 2, 3)
            for (int f = 0; f < 4; f++)
            {
                float y = f * 5.0f;
                var callStationGo = new GameObject($"Elevator_CallStation_Floor_{f}");
                callStationGo.transform.SetParent(elevatorRoot.transform);
                callStationGo.transform.position = new Vector3(12.0f, y + 1.2f, 1.8f);

                var callPedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                callPedestal.name = "Call_Pedestal";
                callPedestal.transform.SetParent(callStationGo.transform);
                callPedestal.transform.localPosition = Vector3.zero;
                callPedestal.transform.localScale = new Vector3(0.15f, 0.35f, 0.25f);
                var callRend = callPedestal.GetComponent<Renderer>();
                if (callRend != null && brassMat != null) callRend.sharedMaterial = brassMat;

                var callInteract = callStationGo.AddComponent<InteractableElevatorButton>();
                callInteract.Elevator = elevatorComp;
                callInteract.TargetFloor = f;
                callInteract.FloorDisplayName = floorNames[f];
                callInteract.IsCallButton = true;
            }

            elevatorComp.Initialize(cabinRoot.transform);
        }

        private static void BuildCabinRailing(Transform parent, Vector3 localPos, Vector3 size, Material mat, string name)
        {
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = name;
            rail.transform.SetParent(parent);
            rail.transform.localPosition = localPos;
            rail.transform.localScale = size;

            var r = rail.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            var col = rail.GetComponent<BoxCollider>();
            if (col == null) col = rail.AddComponent<BoxCollider>();
            col.isTrigger = false;
        }

        private static void BuildDoors(Transform parent, List<RoomDefinition> rooms, Dictionary<string, RoomDefinition> roomMap, Dictionary<string, Material> mats)
        {
            var doorsRoot = new GameObject("Hotel_Doors");
            doorsRoot.transform.SetParent(parent);

#if UNITY_EDITOR
            var doorObjPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/HotelLobby_Door102.obj");
#else
            GameObject doorObjPrefab = null;
#endif
            Material mahoganyMat = null;
            if (mats != null) mats.TryGetValue("WoodMahogany", out mahoganyMat);

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

                    bool isArchway = conn.DoorDisplayName.Contains("Archway");
                    if (isArchway)
                    {
                        // Open Archway: decorative framing pillars and lintel beam, NO door leaf, completely open and walkable
                        var archCasing = new GameObject("Archway_Casing");
                        archCasing.transform.SetParent(doorRoot.transform);
                        archCasing.transform.localPosition = Vector3.zero;

                        // Left decorative column
                        var leftCol = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        leftCol.name = "Arch_Pillar_Left";
                        leftCol.transform.SetParent(archCasing.transform);
                        leftCol.transform.localPosition = new Vector3(-1.6f, 1.4f, 0f);
                        leftCol.transform.localScale = new Vector3(0.3f, 2.8f, 0.45f);
                        var lRend = leftCol.GetComponent<Renderer>();
                        if (lRend != null && mahoganyMat != null) lRend.sharedMaterial = mahoganyMat;

                        // Right decorative column
                        var rightCol = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        rightCol.name = "Arch_Pillar_Right";
                        rightCol.transform.SetParent(archCasing.transform);
                        rightCol.transform.localPosition = new Vector3(1.6f, 1.4f, 0f);
                        rightCol.transform.localScale = new Vector3(0.3f, 2.8f, 0.45f);
                        var rRend = rightCol.GetComponent<Renderer>();
                        if (rRend != null && mahoganyMat != null) rRend.sharedMaterial = mahoganyMat;

                        // Overhead Arch Header
                        var topArch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        topArch.name = "Arch_Lintel_Beam";
                        topArch.transform.SetParent(archCasing.transform);
                        topArch.transform.localPosition = new Vector3(0f, 2.7f, 0f);
                        topArch.transform.localScale = new Vector3(3.5f, 0.25f, 0.45f);
                        var tRend = topArch.GetComponent<Renderer>();
                        if (tRend != null && mahoganyMat != null) tRend.sharedMaterial = mahoganyMat;

                        continue;
                    }

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

                        var leafCol = doorLeaf.GetComponent<BoxCollider>();
                        if (leafCol == null) leafCol = doorLeaf.AddComponent<BoxCollider>();
                        leafCol.center = new Vector3(0f, 1.4f, 0f);
                        leafCol.size = new Vector3(1.3f, 2.8f, 0.15f);
                        leafCol.isTrigger = false;
                    }
                    else
                    {
                        doorLeaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        doorLeaf.name = "Door_Leaf";
                        doorLeaf.transform.SetParent(hinge.transform);
                        doorLeaf.transform.localPosition = new Vector3(0.65f, 1.4f, 0f);
                        doorLeaf.transform.localScale = new Vector3(1.3f, 2.8f, 0.15f);

                        var leafRend = doorLeaf.GetComponent<Renderer>();
                        if (leafRend != null && mahoganyMat != null) leafRend.sharedMaterial = mahoganyMat;

                        var leafCol = doorLeaf.GetComponent<BoxCollider>();
                        if (leafCol == null) leafCol = doorLeaf.AddComponent<BoxCollider>();
                        leafCol.center = Vector3.zero;
                        leafCol.size = Vector3.one;
                        leafCol.isTrigger = false;
                    }

                    // Calculate Inward Swing Angle so door ALWAYS swings into the destination room's open space
                    float openAngle = -90f;
                    if (roomMap.TryGetValue(conn.ToRoomId, out var targetRoom))
                    {
                        Vector3 roomDir = targetRoom.Bounds.center - conn.WorldPosition;
                        Vector3 localDir = Quaternion.Inverse(doorRoot.transform.rotation) * roomDir;
                        openAngle = (localDir.z >= 0f) ? -90f : 90f;
                    }

                    // For unlocked communal doors, start open so player can see inside rooms
                    bool startsOpen = !conn.RequiresKey;
                    if (startsOpen)
                    {
                        hinge.transform.localRotation = Quaternion.Euler(0f, openAngle, 0f);
                    }
                    else
                    {
                        hinge.transform.localRotation = Quaternion.identity;
                    }

                    // InteractableDoor component
                    var doorComp = doorRoot.AddComponent<InteractableDoor>();
                    doorComp.OpenAngle = openAngle;

#if UNITY_EDITOR
                    var soDoor = new SerializedObject(doorComp);
                    soDoor.FindProperty("doorHingeTransform").objectReferenceValue = hinge.transform;
                    soDoor.FindProperty("doorId").stringValue = conn.RequiredKeyId != null ? $"door_{conn.ConnectionId}" : $"door_unlocked_{conn.ConnectionId}";
                    soDoor.FindProperty("doorDisplayName").stringValue = conn.DoorDisplayName;
                    soDoor.FindProperty("requiredKeyId").stringValue = conn.RequiredKeyId ?? string.Empty;
                    soDoor.FindProperty("lockState").enumValueIndex = conn.RequiresKey ? (int)DoorLockState.Locked : (int)DoorLockState.Unlocked;
                    soDoor.FindProperty("openState").enumValueIndex = startsOpen ? (int)DoorOpenState.Open : (int)DoorOpenState.Closed;
                    soDoor.FindProperty("openAngle").floatValue = openAngle;
                    soDoor.ApplyModifiedPropertiesWithoutUndo();
#endif
                }
            }
        }

        private static void BuildRoomFurniture(Transform parent, List<RoomDefinition> rooms, Dictionary<string, Material> mats)
        {
            var furnitureRoot = new GameObject("Hotel_SolidFurniture");
            furnitureRoot.transform.SetParent(parent);

            Material mahoganyMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));
            Material greenFeltMat = GetMaterial(mats, "GreenFelt", new Color(0.12f, 0.45f, 0.22f));
            Material velvetGoldMat = GetMaterial(mats, "VelvetGold", new Color(0.85f, 0.70f, 0.25f));
            Material marbleIvoryMat = GetMaterial(mats, "MarbleIvory", new Color(0.92f, 0.90f, 0.85f));
            Material marbleBlackMat = GetMaterial(mats, "MarbleBlack", new Color(0.15f, 0.15f, 0.18f));
            Material brassMat = GetMaterial(mats, "BrassGold", new Color(0.88f, 0.75f, 0.35f));

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
                    case RoomType.Lobby:
                        BuildLobbyFurnishings(furnitureRoot.transform, new Vector3(0f, y, 0f), mats);
                        break;

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

                        // Front Counter Solid Box Collider (Staff stands behind at Z=11.6m)
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
                        BuildDiningFurnishings(furnitureRoot.transform, new Vector3(-15f, y, 0f), mats);
                        break;

                    case RoomType.Kitchen:
                        BuildCommercialHotelKitchen(furnitureRoot.transform, new Vector3(-15f, y, 10f), mats);
                        break;

                    case RoomType.BarLounge:
                        BuildBarFurnishings(furnitureRoot.transform, new Vector3(-3f, y, -10f), mats);
                        break;

                    case RoomType.Conservatory:
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-19f, y, -11f), new Vector3(2.0f, 1.0f, 1.0f), marbleIvoryMat, "Conservatory_StonePlanter");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-13f, y, -11f), new Vector3(2.2f, 0.85f, 0.8f), walnutMat, "Conservatory_GardenBench");
                        break;

                    case RoomType.StaffOffice:
                        BuildStaffOfficeFurnishings(furnitureRoot.transform, new Vector3(21f, y, 4f), mats);
                        break;

                    case RoomType.UtilityRoom:
                        BuildUtilityRoomFurnishings(furnitureRoot.transform, new Vector3(21f, y, -4f), mats);
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
                        // Every guest suite features a luxury four-poster bed, nightstands with glowing lamps, AND a full en-suite walk-in shower bathroom!
                        BuildLuxurySuiteWithEnSuiteShower(furnitureRoot.transform, room, mats);
                        break;

                    case RoomType.PresidentialStudy:
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-16f, y, -11f), new Vector3(2.6f, 0.85f, 1.4f), walnutMat, "Presidential_StudyDesk");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-19f, y, -11f), new Vector3(1.2f, 1.6f, 0.9f), marbleBlackMat, "Presidential_FloorSafe");
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

        /// <summary>
        /// Furnishes an authentic luxury guest suite featuring a 4-poster mahogany bed, twin nightstands with lamps,
        /// wardrobe armoire, and an en-suite luxury bathroom with a tiled walk-in rain shower stall, vanity sink, and toilet.
        /// </summary>
        private static void BuildLuxurySuiteWithEnSuiteShower(Transform parent, RoomDefinition room, Dictionary<string, Material> mats)
        {
            var suiteRoot = new GameObject($"Suite_{room.RoomId}_Furnishings");
            suiteRoot.transform.SetParent(parent);

            float yFloor = room.FloorIndex * 5.0f;
            Vector3 center = room.Bounds.center;
            Vector3 size = room.Bounds.size;

            Material mahoganyMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));
            Material velvetGoldMat = GetMaterial(mats, "VelvetGold", new Color(0.85f, 0.70f, 0.25f));
            Material whiteMat = GetMaterial(mats, "ShirtWhite", Color.white);
            Material brassMat = GetMaterial(mats, "BrassGold", new Color(0.88f, 0.75f, 0.35f));

            if (size.x >= 12f)
            {
                // Wide Suite Layout (14m or 16m wide): Bedroom on East, En-Suite Bathroom on West
                Vector3 bedCenter = new Vector3(center.x + 2.5f, yFloor, center.z + size.z * 0.22f);
                BuildLuxuryFourPosterBed(suiteRoot.transform, bedCenter, yFloor, mahoganyMat, velvetGoldMat, whiteMat, brassMat, $"{room.RoomId}_Bed");

                // Wardrobe Armoire
                BuildSolidStorageUnit(suiteRoot.transform, new Vector3(center.x + 5.5f, yFloor, center.z + size.z * 0.22f),
                    new Vector3(1.6f, 2.3f, 0.65f), walnutMat, $"{room.RoomId}_Armoire");

                // En-Suite Luxury Bathroom Zone
                Vector3 showerPos = new Vector3(center.x - size.x * 0.35f, yFloor, center.z + size.z * 0.22f);
                Vector3 vanityPos = new Vector3(center.x - size.x * 0.18f, yFloor, center.z + size.z * 0.22f);
                Vector3 toiletPos = new Vector3(center.x - size.x * 0.35f, yFloor, center.z - size.z * 0.22f);
                Vector3 towelPos = new Vector3(center.x - size.x * 0.18f, yFloor, center.z - size.z * 0.22f);

                BuildEnSuiteBathroom(suiteRoot.transform, showerPos, vanityPos, toiletPos, towelPos, yFloor, mats, room.RoomId);
            }
            else
            {
                // Compact Suite Layout (8m x 8m, e.g. Room 102 VIP Suite): Bed on East, En-Suite on West
                Vector3 bedCenter = new Vector3(center.x + 1.8f, yFloor, center.z - 0.5f);
                BuildLuxuryFourPosterBed(suiteRoot.transform, bedCenter, yFloor, mahoganyMat, velvetGoldMat, whiteMat, brassMat, $"{room.RoomId}_Bed");

                // Wardrobe Armoire
                BuildSolidStorageUnit(suiteRoot.transform, new Vector3(center.x + 2.6f, yFloor, center.z + 2.4f),
                    new Vector3(1.4f, 2.3f, 0.65f), walnutMat, $"{room.RoomId}_Armoire");

                // En-Suite Bathroom Zone
                Vector3 showerPos = new Vector3(center.x - 2.2f, yFloor, center.z + 2.2f);
                Vector3 vanityPos = new Vector3(center.x - 0.6f, yFloor, center.z + 2.6f);
                Vector3 toiletPos = new Vector3(center.x - 2.2f, yFloor, center.z + 0.6f);
                Vector3 towelPos = new Vector3(center.x - 0.6f, yFloor, center.z + 0.6f);

                BuildEnSuiteBathroom(suiteRoot.transform, showerPos, vanityPos, toiletPos, towelPos, yFloor, mats, room.RoomId);
            }
        }

        /// <summary>
        /// Builds an authentic luxury en-suite hotel bathroom featuring:
        /// 1. Tiled Walk-In Shower Stall with rain showerhead, brass riser pipe, and glass enclosure.
        /// 2. Porcelain Vanity Console Sink with brass faucets and wall mirror.
        /// 3. Porcelain Water Closet (Toilet).
        /// 4. Brass Wall Towel Rail with plush white towels.
        /// </summary>
        private static void BuildEnSuiteBathroom(Transform parent, Vector3 showerPos, Vector3 vanityPos, Vector3 toiletPos, Vector3 towelPos,
            float yFloor, Dictionary<string, Material> mats, string suiteName)
        {
            var bathRoot = new GameObject($"{suiteName}_EnSuite_Bathroom");
            bathRoot.transform.SetParent(parent);

            Material slateMat = GetMaterial(mats, "FloorSlate", new Color(0.18f, 0.20f, 0.22f));
            Material whiteMat = GetMaterial(mats, "ShirtWhite", Color.white);
            Material brassMat = GetMaterial(mats, "BrassGold", new Color(0.88f, 0.75f, 0.35f));
            Material marbleIvoryMat = GetMaterial(mats, "MarbleIvory", new Color(0.92f, 0.90f, 0.85f));
            Material mahoganyMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));

            // 1. Walk-In Tiled Shower Stall
            var showerRoot = new GameObject($"{suiteName}_ShowerStall");
            showerRoot.transform.SetParent(bathRoot.transform);
            showerRoot.transform.position = showerPos;

            // Slate Shower Tray Base
            var tray = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tray.name = "Shower_TrayBase";
            tray.transform.SetParent(showerRoot.transform);
            tray.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            tray.transform.localScale = new Vector3(1.4f, 0.16f, 1.4f);
            var trayRend = tray.GetComponent<Renderer>();
            if (trayRend != null) trayRend.sharedMaterial = slateMat;

            // Metallic Chrome Floor Drain
            var drain = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drain.name = "Shower_Drain";
            drain.transform.SetParent(showerRoot.transform);
            drain.transform.localPosition = new Vector3(0f, 0.17f, 0f);
            drain.transform.localScale = new Vector3(0.18f, 0.01f, 0.18f);
            var dRend = drain.GetComponent<Renderer>();
            if (dRend != null) dRend.sharedMaterial = brassMat;

            // Glass Shower Screen Enclosure
            var glassScreen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glassScreen.name = "Shower_GlassScreen";
            glassScreen.transform.SetParent(showerRoot.transform);
            glassScreen.transform.localPosition = new Vector3(0f, 1.25f, -0.68f);
            glassScreen.transform.localScale = new Vector3(1.35f, 2.2f, 0.04f);
            var gRend = glassScreen.GetComponent<Renderer>();
            if (gRend != null) gRend.sharedMaterial = marbleIvoryMat;

            // Brass Shower Riser Pipe
            var riser = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            riser.name = "Shower_RiserPipe";
            riser.transform.SetParent(showerRoot.transform);
            riser.transform.localPosition = new Vector3(-0.62f, 1.35f, 0f);
            riser.transform.localScale = new Vector3(0.04f, 1.1f, 0.04f);
            var rRend = riser.GetComponent<Renderer>();
            if (rRend != null) rRend.sharedMaterial = brassMat;

            // Sunflower Rain Showerhead
            var head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            head.name = "Rain_Showerhead";
            head.transform.SetParent(showerRoot.transform);
            head.transform.localPosition = new Vector3(-0.35f, 2.25f, 0f);
            head.transform.localScale = new Vector3(0.32f, 0.03f, 0.32f);
            var hRend = head.GetComponent<Renderer>();
            if (hRend != null) hRend.sharedMaterial = brassMat;

            // Solid Shower Box Collider
            var sCol = showerRoot.AddComponent<BoxCollider>();
            sCol.center = new Vector3(0f, 1.1f, 0f);
            sCol.size = new Vector3(1.4f, 2.2f, 1.4f);
            sCol.isTrigger = false;

            // 2. Porcelain Vanity Console Sink & Wall Mirror
            var vanityRoot = new GameObject($"{suiteName}_VanityUnit");
            vanityRoot.transform.SetParent(bathRoot.transform);
            vanityRoot.transform.position = vanityPos;

            // Vanity Basin Console
            var basin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basin.name = "Vanity_Basin";
            basin.transform.SetParent(vanityRoot.transform);
            basin.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            basin.transform.localScale = new Vector3(0.85f, 0.85f, 0.55f);
            var bRend = basin.GetComponent<Renderer>();
            if (bRend != null) bRend.sharedMaterial = whiteMat;

            // Brass Faucet Spout
            var spout = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spout.name = "Brass_Faucet";
            spout.transform.SetParent(vanityRoot.transform);
            spout.transform.localPosition = new Vector3(0f, 0.95f, 0.15f);
            spout.transform.localScale = new Vector3(0.04f, 0.12f, 0.04f);
            var spRend = spout.GetComponent<Renderer>();
            if (spRend != null) spRend.sharedMaterial = brassMat;

            // Framed Vanity Wall Mirror
            var mirror = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mirror.name = "Vanity_FramedMirror";
            mirror.transform.SetParent(vanityRoot.transform);
            mirror.transform.localPosition = new Vector3(0f, 1.80f, 0.25f);
            mirror.transform.localScale = new Vector3(0.75f, 1.05f, 0.04f);
            var mRend = mirror.GetComponent<Renderer>();
            if (mRend != null) mRend.sharedMaterial = marbleIvoryMat;

            var vCol = vanityRoot.AddComponent<BoxCollider>();
            vCol.center = new Vector3(0f, 0.45f, 0f);
            vCol.size = new Vector3(0.85f, 0.9f, 0.55f);
            vCol.isTrigger = false;

            // 3. Porcelain Toilet (Water Closet)
            var toiletRoot = new GameObject($"{suiteName}_PorcelainToilet");
            toiletRoot.transform.SetParent(bathRoot.transform);
            toiletRoot.transform.position = toiletPos;

            var bowl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bowl.name = "Toilet_Bowl";
            bowl.transform.SetParent(toiletRoot.transform);
            bowl.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            bowl.transform.localScale = new Vector3(0.44f, 0.45f, 0.60f);
            var boRend = bowl.GetComponent<Renderer>();
            if (boRend != null) boRend.sharedMaterial = whiteMat;

            var tank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tank.name = "Toilet_CisternTank";
            tank.transform.SetParent(toiletRoot.transform);
            tank.transform.localPosition = new Vector3(0f, 0.65f, 0.20f);
            tank.transform.localScale = new Vector3(0.48f, 0.45f, 0.22f);
            var tRend = tank.GetComponent<Renderer>();
            if (tRend != null) tRend.sharedMaterial = whiteMat;

            var tCol = toiletRoot.AddComponent<BoxCollider>();
            tCol.center = new Vector3(0f, 0.45f, 0.05f);
            tCol.size = new Vector3(0.48f, 0.9f, 0.65f);
            tCol.isTrigger = false;

            // 4. Brass Towel Rail with Folded White Towels
            var towelRoot = new GameObject($"{suiteName}_TowelRail");
            towelRoot.transform.SetParent(bathRoot.transform);
            towelRoot.transform.position = towelPos;

            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "Brass_Rail";
            rail.transform.SetParent(towelRoot.transform);
            rail.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            rail.transform.localScale = new Vector3(0.75f, 0.04f, 0.08f);
            var rRend2 = rail.GetComponent<Renderer>();
            if (rRend2 != null) rRend2.sharedMaterial = brassMat;

            var towel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            towel.name = "Folded_PlushTowels";
            towel.transform.SetParent(towelRoot.transform);
            towel.transform.localPosition = new Vector3(0f, 0.98f, 0f);
            towel.transform.localScale = new Vector3(0.60f, 0.24f, 0.12f);
            var toRend = towel.GetComponent<Renderer>();
            if (toRend != null) toRend.sharedMaterial = whiteMat;
        }

        /// <summary>
        /// Builds a majestic 4-poster mahogany luxury bed with plush mattress, quilted velvet duvet,
        /// twin bedside nightstands, and warm glowing Art Deco bedside lamps.
        /// </summary>
        private static void BuildLuxuryFourPosterBed(Transform parent, Vector3 bedCenter, float yFloor,
            Material frameMat, Material duvetMat, Material sheetMat, Material brassMat, string name)
        {
            var bedRoot = new GameObject(name);
            bedRoot.transform.SetParent(parent);
            bedRoot.transform.position = bedCenter;

            // Solid Bed Box Collider
            var col = bedRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.70f, 0f);
            col.size = new Vector3(2.2f, 1.4f, 2.4f);
            col.isTrigger = false;

            // Mattress
            var mattress = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mattress.name = "Mattress";
            mattress.transform.SetParent(bedRoot.transform);
            mattress.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            mattress.transform.localScale = new Vector3(1.95f, 0.40f, 2.15f);
            var mRend = mattress.GetComponent<Renderer>();
            if (mRend != null) mRend.sharedMaterial = sheetMat;

            // Quilted Duvet Runner
            var duvet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            duvet.name = "Duvet_Runner";
            duvet.transform.SetParent(bedRoot.transform);
            duvet.transform.localPosition = new Vector3(0f, 0.50f, -0.45f);
            duvet.transform.localScale = new Vector3(1.98f, 0.35f, 1.10f);
            var dRend = duvet.GetComponent<Renderer>();
            if (dRend != null) dRend.sharedMaterial = duvetMat;

            // Carved Mahogany Headboard
            var headboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headboard.name = "Headboard";
            headboard.transform.SetParent(bedRoot.transform);
            headboard.transform.localPosition = new Vector3(0f, 0.85f, 1.05f);
            headboard.transform.localScale = new Vector3(2.15f, 1.10f, 0.14f);
            var hRend = headboard.GetComponent<Renderer>();
            if (hRend != null) hRend.sharedMaterial = frameMat;

            // 4 Turned Corner Bedposts
            Vector3[] postPositions = {
                new Vector3(-1.05f, 0.95f, 1.10f),
                new Vector3(1.05f, 0.95f, 1.10f),
                new Vector3(-1.05f, 0.95f, -1.10f),
                new Vector3(1.05f, 0.95f, -1.10f)
            };
            for (int i = 0; i < 4; i++)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = $"Bedpost_{i}";
                post.transform.SetParent(bedRoot.transform);
                post.transform.localPosition = postPositions[i];
                post.transform.localScale = new Vector3(0.08f, 0.95f, 0.08f);
                var pRend = post.GetComponent<Renderer>();
                if (pRend != null) pRend.sharedMaterial = frameMat;
            }

            // Twin Bedside Nightstands with Glowing Lamps
            BuildNightstandWithLamp(bedRoot.transform, new Vector3(-1.55f, 0f, 0.70f), frameMat, brassMat, sheetMat, "Nightstand_L");
            BuildNightstandWithLamp(bedRoot.transform, new Vector3(1.55f, 0f, 0.70f), frameMat, brassMat, sheetMat, "Nightstand_R");
        }

        private static void BuildNightstandWithLamp(Transform parent, Vector3 localPos, Material woodMat, Material brassMat, Material shadeMat, string name)
        {
            var standGo = new GameObject(name);
            standGo.transform.SetParent(parent);
            standGo.transform.localPosition = localPos;

            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Table";
            table.transform.SetParent(standGo.transform);
            table.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            table.transform.localScale = new Vector3(0.55f, 0.65f, 0.50f);
            var tRend = table.GetComponent<Renderer>();
            if (tRend != null) tRend.sharedMaterial = woodMat;

            var col = standGo.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.32f, 0f);
            col.size = new Vector3(0.55f, 0.65f, 0.50f);
            col.isTrigger = false;

            // Lamp Base
            var baseGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseGo.name = "Lamp_Base";
            baseGo.transform.SetParent(standGo.transform);
            baseGo.transform.localPosition = new Vector3(0f, 0.70f, 0f);
            baseGo.transform.localScale = new Vector3(0.14f, 0.08f, 0.14f);
            var bRend = baseGo.GetComponent<Renderer>();
            if (bRend != null) bRend.sharedMaterial = brassMat;

            // Lamp Shade
            var shade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shade.name = "Lamp_Shade";
            shade.transform.SetParent(standGo.transform);
            shade.transform.localPosition = new Vector3(0f, 0.88f, 0f);
            shade.transform.localScale = new Vector3(0.25f, 0.18f, 0.25f);
            var sRend = shade.GetComponent<Renderer>();
            if (sRend != null) sRend.sharedMaterial = shadeMat;

            // Warm Bedside Glow Light
            var lightGo = new GameObject("Bedside_Light");
            lightGo.transform.SetParent(standGo.transform);
            lightGo.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.88f, 0.68f);
            l.intensity = 1.3f;
            l.range = 2.4f;
        }

        /// <summary>
        /// Builds a full commercial hotel kitchen featuring:
        /// 1. Heavy 6-burner commercial range & ovens with overhead extraction hood.
        /// 2. Central butcher-block prep island with cutting boards and cutlery.
        /// 3. Double-basin scullery wash sink with high-arch pre-rinse faucet.
        /// 4. 4-tier industrial pantry storage shelving.
        /// </summary>
        private static void BuildCommercialHotelKitchen(Transform parent, Vector3 center, Dictionary<string, Material> mats)
        {
            var kitchenRoot = new GameObject("Kitchen_Commercial_Compound");
            kitchenRoot.transform.SetParent(parent);
            kitchenRoot.transform.position = center;

            Material slateMat = GetMaterial(mats, "FloorSlate", new Color(0.18f, 0.20f, 0.22f));
            Material marbleIvoryMat = GetMaterial(mats, "MarbleIvory", new Color(0.92f, 0.90f, 0.85f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));
            Material mahoganyMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material brassMat = GetMaterial(mats, "BrassGold", new Color(0.88f, 0.75f, 0.35f));
            Material whiteMat = GetMaterial(mats, "ShirtWhite", Color.white);

            // 1. Commercial 6-Burner Range against West wall (X = center.x - 5.5m = -20.5m)
            var rangeRoot = new GameObject("Kitchen_CommercialStove");
            rangeRoot.transform.SetParent(kitchenRoot.transform);
            rangeRoot.transform.localPosition = new Vector3(-5.2f, 0f, 0f);

            var rangeBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rangeBody.name = "Stove_OvenBody";
            rangeBody.transform.SetParent(rangeRoot.transform);
            rangeBody.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            rangeBody.transform.localScale = new Vector3(1.6f, 1.1f, 3.2f);
            var rbRend = rangeBody.GetComponent<Renderer>();
            if (rbRend != null) rbRend.sharedMaterial = slateMat;

            // 6 Cast-Iron Burner Grates
            for (int bz = -1; bz <= 1; bz++)
            {
                for (int bx = 0; bx <= 1; bx++)
                {
                    var burner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    burner.name = $"Burner_{bx}_{bz}";
                    burner.transform.SetParent(rangeRoot.transform);
                    burner.transform.localPosition = new Vector3(-0.35f + bx * 0.7f, 1.12f, bz * 0.9f);
                    burner.transform.localScale = new Vector3(0.40f, 0.02f, 0.40f);
                    var bRend = burner.GetComponent<Renderer>();
                    if (bRend != null) bRend.sharedMaterial = slateMat;
                }
            }

            // Overhead Commercial Extractor Hood Canopy
            var hood = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hood.name = "Extractor_Hood";
            hood.transform.SetParent(rangeRoot.transform);
            hood.transform.localPosition = new Vector3(0f, 2.7f, 0f);
            hood.transform.localScale = new Vector3(1.8f, 0.6f, 3.4f);
            var hRend = hood.GetComponent<Renderer>();
            if (hRend != null) hRend.sharedMaterial = slateMat;

            var rCol = rangeRoot.AddComponent<BoxCollider>();
            rCol.center = new Vector3(0f, 0.55f, 0f);
            rCol.size = new Vector3(1.6f, 1.1f, 3.2f);
            rCol.isTrigger = false;

            // 2. Central Butcher-Block Prep Island (center at X=0, Z=0)
            var islandRoot = new GameObject("Kitchen_ButcherBlock_Island");
            islandRoot.transform.SetParent(kitchenRoot.transform);
            islandRoot.transform.localPosition = new Vector3(0f, 0f, 0f);

            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "ButcherBlock_Top";
            block.transform.SetParent(islandRoot.transform);
            block.transform.localPosition = new Vector3(0f, 0.88f, 0f);
            block.transform.localScale = new Vector3(1.4f, 0.20f, 2.6f);
            var blRend = block.GetComponent<Renderer>();
            if (blRend != null) blRend.sharedMaterial = walnutMat;

            // 4 Table Legs
            for (int lx = -1; lx <= 1; lx += 2)
            {
                for (int lz = -1; lz <= 1; lz += 2)
                {
                    var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    leg.name = $"Island_Leg_{lx}_{lz}";
                    leg.transform.SetParent(islandRoot.transform);
                    leg.transform.localPosition = new Vector3(lx * 0.58f, 0.39f, lz * 1.15f);
                    leg.transform.localScale = new Vector3(0.12f, 0.78f, 0.12f);
                    var lRend = leg.GetComponent<Renderer>();
                    if (lRend != null) lRend.sharedMaterial = slateMat;
                }
            }

            var iCol = islandRoot.AddComponent<BoxCollider>();
            iCol.center = new Vector3(0f, 0.48f, 0f);
            iCol.size = new Vector3(1.4f, 0.95f, 2.6f);
            iCol.isTrigger = false;

            // 3. Scullery Double-Basin Wash Sink against North wall (Z = +2.8m)
            var sinkRoot = new GameObject("Kitchen_SculleryDoubleSink");
            sinkRoot.transform.SetParent(kitchenRoot.transform);
            sinkRoot.transform.localPosition = new Vector3(3.5f, 0f, 2.6f);

            var sinkBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sinkBody.name = "Sink_Body";
            sinkBody.transform.SetParent(sinkRoot.transform);
            sinkBody.transform.localPosition = new Vector3(0f, 0.50f, 0f);
            sinkBody.transform.localScale = new Vector3(2.4f, 1.0f, 0.85f);
            var sRend = sinkBody.GetComponent<Renderer>();
            if (sRend != null) sRend.sharedMaterial = marbleIvoryMat;

            // Commercial Pre-Rinse Faucet
            var faucet = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            faucet.name = "Sink_CommercialFaucet";
            faucet.transform.SetParent(sinkRoot.transform);
            faucet.transform.localPosition = new Vector3(0f, 1.25f, 0.25f);
            faucet.transform.localScale = new Vector3(0.05f, 0.30f, 0.05f);
            var fRend = faucet.GetComponent<Renderer>();
            if (fRend != null) fRend.sharedMaterial = brassMat;

            var skCol = sinkRoot.AddComponent<BoxCollider>();
            skCol.center = new Vector3(0f, 0.50f, 0f);
            skCol.size = new Vector3(2.4f, 1.0f, 0.85f);
            skCol.isTrigger = false;

            // 4. Walk-In Industrial Pantry Shelving against North-West corner
            BuildSolidStorageUnit(kitchenRoot.transform, center + new Vector3(-1.5f, 0f, 2.8f),
                new Vector3(2.6f, 2.4f, 0.70f), mahoganyMat, "Kitchen_PantryShelvingUnit");
        }

        private static void BuildStaffOfficeFurnishings(Transform parent, Vector3 center, Dictionary<string, Material> mats)
        {
            var officeRoot = new GameObject("StaffOffice_Furnishings");
            officeRoot.transform.SetParent(parent);
            officeRoot.transform.position = center;

            Material mahoganyMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));
            Material marbleBlackMat = GetMaterial(mats, "MarbleBlack", new Color(0.15f, 0.15f, 0.18f));
            Material greenFeltMat = GetMaterial(mats, "GreenFelt", new Color(0.12f, 0.45f, 0.22f));
            Material brassMat = GetMaterial(mats, "BrassGold", new Color(0.88f, 0.75f, 0.35f));

            // Executive Desk: Center at (0.5m, 0m, 1.0m) -> World (21.5m, 0m, 5.0m)
            var desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "Office_ExecutiveDesk";
            desk.transform.SetParent(officeRoot.transform);
            desk.transform.localPosition = new Vector3(0.5f, 0.42f, 1.0f);
            desk.transform.localScale = new Vector3(2.4f, 0.85f, 1.4f);
            var dRend = desk.GetComponent<Renderer>();
            if (dRend != null) dRend.sharedMaterial = mahoganyMat;

            // Green Leather Blotter Inlay
            var blotter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blotter.name = "Desk_LeatherBlotter";
            blotter.transform.SetParent(officeRoot.transform);
            blotter.transform.localPosition = new Vector3(0.5f, 0.86f, 1.0f);
            blotter.transform.localScale = new Vector3(1.2f, 0.02f, 0.8f);
            var blRend = blotter.GetComponent<Renderer>();
            if (blRend != null) blRend.sharedMaterial = greenFeltMat;

            // Manager's Chair (Behind Desk at Z = 2.5m -> World Z = 6.5m)
            var chair = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chair.name = "Manager_ExecutiveChair";
            chair.transform.SetParent(officeRoot.transform);
            chair.transform.localPosition = new Vector3(0.5f, 0.55f, 2.5f);
            chair.transform.localScale = new Vector3(0.70f, 1.10f, 0.65f);
            var cRend = chair.GetComponent<Renderer>();
            if (cRend != null) cRend.sharedMaterial = walnutMat;

            // Solid Desk Collider
            var dCol = desk.GetComponent<BoxCollider>();
            if (dCol == null) dCol = desk.AddComponent<BoxCollider>();
            dCol.isTrigger = false;

            // Heavy Steel Bank Floor Safe in Corner
            var safe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            safe.name = "Office_FloorSafe";
            safe.transform.SetParent(officeRoot.transform);
            safe.transform.localPosition = new Vector3(3.8f, 0.70f, -2.0f);
            safe.transform.localScale = new Vector3(1.1f, 1.4f, 0.9f);
            var sRend = safe.GetComponent<Renderer>();
            if (sRend != null) sRend.sharedMaterial = marbleBlackMat;

            var safeCol = safe.GetComponent<BoxCollider>();
            if (safeCol == null) safeCol = safe.AddComponent<BoxCollider>();
            safeCol.isTrigger = false;

            // Tall Filing Cabinet against East Wall
            BuildSolidStorageUnit(officeRoot.transform, center + new Vector3(3.8f, 0f, 2.0f),
                new Vector3(1.2f, 2.2f, 0.65f), walnutMat, "Office_FilingCabinet");
        }

        private static void BuildUtilityRoomFurnishings(Transform parent, Vector3 center, Dictionary<string, Material> mats)
        {
            var utilRoot = new GameObject("UtilityRoom_Furnishings");
            utilRoot.transform.SetParent(parent);
            utilRoot.transform.position = center;

            Material slateMat = GetMaterial(mats, "FloorSlate", new Color(0.18f, 0.20f, 0.22f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));
            Material brassMat = GetMaterial(mats, "BrassGold", new Color(0.88f, 0.75f, 0.35f));

            // Industrial Steam Boiler at (3.0m, 0m, 0m) -> World (24.0m, 0m, -4.0m)
            var boiler = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            boiler.name = "Utility_SteamBoiler";
            boiler.transform.SetParent(utilRoot.transform);
            boiler.transform.localPosition = new Vector3(3.0f, 1.25f, 0f);
            boiler.transform.localScale = new Vector3(1.8f, 1.25f, 1.8f);
            var bRend = boiler.GetComponent<Renderer>();
            if (bRend != null) bRend.sharedMaterial = slateMat;

            var bCol = boiler.GetComponent<BoxCollider>();
            if (bCol == null) bCol = boiler.AddComponent<BoxCollider>();
            bCol.center = Vector3.zero;
            bCol.size = new Vector3(2.0f, 2.5f, 2.0f);
            bCol.isTrigger = false;

            // Electrical Breaker Distribution Panel on Wall
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Utility_BreakerPanel";
            panel.transform.SetParent(utilRoot.transform);
            panel.transform.localPosition = new Vector3(-1.0f, 1.6f, 3.6f);
            panel.transform.localScale = new Vector3(1.2f, 1.4f, 0.25f);
            var pRend = panel.GetComponent<Renderer>();
            if (pRend != null) pRend.sharedMaterial = slateMat;

            // Maintenance Workbench with Tool Rack
            BuildSolidTable(utilRoot.transform, center + new Vector3(-1.0f, 0f, -2.8f),
                new Vector3(2.4f, 0.95f, 1.2f), walnutMat, "Utility_Workbench");
        }

        private static void BuildLobbyFurnishings(Transform parent, Vector3 center, Dictionary<string, Material> mats)
        {
            var lobbyRoot = new GameObject("Lobby_Furnishings");
            lobbyRoot.transform.SetParent(parent);
            lobbyRoot.transform.position = center;

            Material blackMarble = GetMaterial(mats, "MarbleBlack", new Color(0.15f, 0.15f, 0.18f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));
            Material velvetEmerald = GetMaterial(mats, "VelvetEmerald", new Color(0.12f, 0.40f, 0.22f));
            Material carpetBurgundy = GetMaterial(mats, "CarpetBurgundy", new Color(0.55f, 0.12f, 0.18f));

            // Grand Art Deco Center Table (World 0, 0, 0)
            BuildSolidTable(lobbyRoot.transform, center, new Vector3(2.2f, 0.85f, 2.2f), blackMarble, "Lobby_CenterMarbleTable");

            // West Lounge: Velvet Sofas & Coffee Table at World (-4.8m, 0m, -2.5m)
            BuildSolidTable(lobbyRoot.transform, center + new Vector3(-4.8f, 0f, -2.5f),
                new Vector3(1.6f, 0.50f, 0.9f), walnutMat, "Lobby_WestLounge_CoffeeTable");
            BuildSolidStorageUnit(lobbyRoot.transform, center + new Vector3(-4.8f, 0f, -3.8f),
                new Vector3(2.4f, 0.85f, 0.85f), velvetEmerald, "Lobby_WestLounge_SofaSouth");
            BuildSolidStorageUnit(lobbyRoot.transform, center + new Vector3(-4.8f, 0f, -1.2f),
                new Vector3(2.4f, 0.85f, 0.85f), velvetEmerald, "Lobby_WestLounge_SofaNorth");

            // East Lounge: Fireplace & Wingback Chairs at World (4.8m, 0m, -2.5m)
            BuildSolidFireplace(lobbyRoot.transform, center + new Vector3(6.5f, 0f, -2.5f), mats, "Lobby_GrandFireplace");
            BuildSolidStorageUnit(lobbyRoot.transform, center + new Vector3(4.5f, 0f, -3.4f),
                new Vector3(1.0f, 1.10f, 0.9f), carpetBurgundy, "Lobby_WingbackChair_South");
            BuildSolidStorageUnit(lobbyRoot.transform, center + new Vector3(4.5f, 0f, -1.6f),
                new Vector3(1.0f, 1.10f, 0.9f), carpetBurgundy, "Lobby_WingbackChair_North");
        }

        private static void BuildDiningFurnishings(Transform parent, Vector3 center, Dictionary<string, Material> mats)
        {
            Material mahoganyMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));

            // North Banquet Table (-16m, y, 3m)
            BuildSolidTable(parent, center + new Vector3(-1.0f, 0f, 3.0f), new Vector3(2.2f, 0.85f, 5.8f), mahoganyMat, "Dining_BanquetTable_North");

            // South Banquet Table (-16m, y, -3m)
            BuildSolidTable(parent, center + new Vector3(-1.0f, 0f, -3.0f), new Vector3(2.2f, 0.85f, 5.8f), mahoganyMat, "Dining_BanquetTable_South");

            // Sideboard Cabinet against West Wall
            BuildSolidStorageUnit(parent, center + new Vector3(-6.0f, 0f, 0f), new Vector3(1.2f, 1.6f, 3.5f), walnutMat, "Dining_SideboardCabinet");
        }

        private static void BuildBarFurnishings(Transform parent, Vector3 center, Dictionary<string, Material> mats)
        {
            Material mahoganyMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));

            // Main Bar Counter
            BuildSolidStorageUnit(parent, center + new Vector3(-1.5f, 0f, -3.5f), new Vector3(5.2f, 1.15f, 1.2f), mahoganyMat, "Bar_ServiceCounter");

            // Back-Bar Liquor Shelf Unit
            BuildSolidStorageUnit(parent, center + new Vector3(-1.5f, 0f, -5.5f), new Vector3(5.0f, 2.4f, 0.6f), walnutMat, "Bar_BackShelves");

            // Cocktail Tables
            BuildSolidTable(parent, center + new Vector3(-3.0f, 0f, 0f), new Vector3(1.4f, 0.75f, 1.4f), walnutMat, "Bar_LoungeTable_A");
            BuildSolidTable(parent, center + new Vector3(1.0f, 0f, 0f), new Vector3(1.4f, 0.75f, 1.4f), walnutMat, "Bar_LoungeTable_B");
        }

        private static Material GetMaterial(Dictionary<string, Material> mats, string key, Color fallbackColor)
        {
            if (mats != null && mats.TryGetValue(key, out var mat) && mat != null)
            {
                return mat;
            }
            var fallback = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                name = $"Mat_Fallback_{key}",
                color = fallbackColor
            };
            return fallback;
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
            Material woodMat = null;
            Material whiteMat = null;
            if (mats != null)
            {
                mats.TryGetValue("WoodMahogany", out woodMat);
                mats.TryGetValue("ShirtWhite", out whiteMat);
            }

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
            Material woodMat = null;
            Material feltMat = null;
            if (mats != null)
            {
                mats.TryGetValue("WoodMahogany", out woodMat);
                mats.TryGetValue("GreenFelt", out feltMat);
            }

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
            Material marbleMat = null;
            if (mats != null) mats.TryGetValue("MarbleBlack", out marbleMat);

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
