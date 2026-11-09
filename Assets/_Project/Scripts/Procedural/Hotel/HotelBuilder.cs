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

        public static GameObject BuildHotelEnvironment(Transform parent, List<RoomDefinition> rooms, Dictionary<string, Material> mats, TomorrowsCrime.Procedural.Core.SeededRandom rng = null)
        {
            var hotelRoot = new GameObject("MeridianHouse_Architecture");
            if (parent != null) hotelRoot.transform.SetParent(parent);

            if (rng == null) rng = new TomorrowsCrime.Procedural.Core.SeededRandom(1930);

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
            BuildRoomFurniture(hotelRoot.transform, rooms, mats, rng);

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
                // Exact architectural floor framing leaving stairwell (X: 3.8-11.4, Z: -2.65-0.05)
                // and elevator shaft (X: 12.75-15.35, Z: -1.35-1.35) 100% unobstructed

                // 1A. North walkway floor (X: -4 to 12.75, Z: 0.05 to 3.0)
                var floorNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorNorth.name = "Floor_Slab_NorthWalkway";
                floorNorth.transform.SetParent(roomGo.transform);
                floorNorth.transform.position = new Vector3(4.375f, yFloor - 0.10f, 1.525f);
                floorNorth.transform.localScale = new Vector3(16.75f, 0.2f, 2.95f);
                var fRendN = floorNorth.GetComponent<Renderer>();
                if (fRendN != null && floorMat != null) fRendN.sharedMaterial = floorMat;

                // 1B. West floor slab (X: -4 to 3.8, Z: -3.0 to 0.05)
                var floorWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorWest.name = "Floor_Slab_WestLanding";
                floorWest.transform.SetParent(roomGo.transform);
                floorWest.transform.position = new Vector3(-0.1f, yFloor - 0.10f, -1.475f);
                floorWest.transform.localScale = new Vector3(7.8f, 0.2f, 3.05f);
                var fRendW = floorWest.GetComponent<Renderer>();
                if (fRendW != null && floorMat != null) fRendW.sharedMaterial = floorMat;

                // 1C. East landing floor in front of elevator (X: 11.4 to 12.75, Z: -3.0 to 0.05)
                var floorEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorEast.name = "Floor_Slab_EastLanding";
                floorEast.transform.SetParent(roomGo.transform);
                floorEast.transform.position = new Vector3(12.075f, yFloor - 0.10f, -1.475f);
                floorEast.transform.localScale = new Vector3(1.35f, 0.2f, 3.05f);
                var fRendE = floorEast.GetComponent<Renderer>();
                if (fRendE != null && floorMat != null) fRendE.sharedMaterial = floorMat;

                // 1D. North perimeter walkway alongside elevator (X: 12.75 to 16.0, Z: 1.35 to 3.0)
                var floorElevN = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorElevN.name = "Floor_Slab_NorthElevatorSide";
                floorElevN.transform.SetParent(roomGo.transform);
                floorElevN.transform.position = new Vector3(14.375f, yFloor - 0.10f, 2.175f);
                floorElevN.transform.localScale = new Vector3(3.25f, 0.2f, 1.65f);
                if (floorElevN.GetComponent<Renderer>() != null && floorMat != null) floorElevN.GetComponent<Renderer>().sharedMaterial = floorMat;

                // 1E. South perimeter walkway alongside elevator (X: 12.75 to 16.0, Z: -3.0 to -1.35)
                var floorElevS = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorElevS.name = "Floor_Slab_SouthElevatorSide";
                floorElevS.transform.SetParent(roomGo.transform);
                floorElevS.transform.position = new Vector3(14.375f, yFloor - 0.10f, -2.175f);
                floorElevS.transform.localScale = new Vector3(3.25f, 0.2f, 1.65f);
                if (floorElevS.GetComponent<Renderer>() != null && floorMat != null) floorElevS.GetComponent<Renderer>().sharedMaterial = floorMat;

                // 1F. East shaft perimeter border (X: 15.35 to 16.0, Z: -1.35 to 1.35)
                var floorShaftE = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorShaftE.name = "Floor_Slab_EastShaftPerimeter";
                floorShaftE.transform.SetParent(roomGo.transform);
                floorShaftE.transform.position = new Vector3(15.675f, yFloor - 0.10f, 0f);
                floorShaftE.transform.localScale = new Vector3(0.65f, 0.2f, 2.7f);
                if (floorShaftE.GetComponent<Renderer>() != null && floorMat != null) floorShaftE.GetComponent<Renderer>().sharedMaterial = floorMat;

                // 1G. South stair margin border under south wall (X: 3.8 to 11.4, Z: -3.0 to -2.65)
                var floorStairMargin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorStairMargin.name = "Floor_Slab_SouthStairMargin";
                floorStairMargin.transform.SetParent(roomGo.transform);
                floorStairMargin.transform.position = new Vector3(7.6f, yFloor - 0.10f, -2.825f);
                floorStairMargin.transform.localScale = new Vector3(7.6f, 0.2f, 0.35f);
                if (floorStairMargin.GetComponent<Renderer>() != null && floorMat != null) floorStairMargin.GetComponent<Renderer>().sharedMaterial = floorMat;

                // 1H. Protective Art Deco Brass Balustrade Railing along the stair opening edge (Z = 0.05f, X: 3.8 to 11.4)
                BuildCabinRailing(roomGo.transform, new Vector3(7.6f, yFloor + 0.6f, 0.05f), new Vector3(7.6f, 1.1f, 0.1f), brassMat, "StairAtrium_GuardRailing");
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

            // 2. Room Ceiling Slab (With precise open shafts for Elevator and Central Staircase)
            if (room.Type == RoomType.Lobby)
            {
                // Floor 0 Lobby: Cutout for Stair Flight 0 rising into Floor 1 (X: 3.8 to 8.0, Z: -2.65 to 0.05)
                var ceilN = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilN.name = "Ceiling_Slab_North";
                ceilN.transform.SetParent(roomGo.transform);
                ceilN.transform.position = new Vector3(0f, yFloor + height + 0.25f, 3.025f);
                ceilN.transform.localScale = new Vector3(16f, 0.5f, 5.95f);
                if (ceilN.GetComponent<Renderer>() != null && ceilingMat != null) ceilN.GetComponent<Renderer>().sharedMaterial = ceilingMat;

                var ceilSW = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilSW.name = "Ceiling_Slab_SouthWest";
                ceilSW.transform.SetParent(roomGo.transform);
                ceilSW.transform.position = new Vector3(-2.1f, yFloor + height + 0.25f, -2.975f);
                ceilSW.transform.localScale = new Vector3(11.8f, 0.5f, 6.05f);
                if (ceilSW.GetComponent<Renderer>() != null && ceilingMat != null) ceilSW.GetComponent<Renderer>().sharedMaterial = ceilingMat;

                var ceilSMargin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilSMargin.name = "Ceiling_Slab_SouthMargin";
                ceilSMargin.transform.SetParent(roomGo.transform);
                ceilSMargin.transform.position = new Vector3(5.9f, yFloor + height + 0.25f, -4.325f);
                ceilSMargin.transform.localScale = new Vector3(4.2f, 0.5f, 3.35f);
                if (ceilSMargin.GetComponent<Renderer>() != null && ceilingMat != null) ceilSMargin.GetComponent<Renderer>().sharedMaterial = ceilingMat;
            }
            else if (room.Type == RoomType.GroundCorridor)
            {
                // Floor 0 Corridor: Stair Opening (X: 8.0-11.4, Z: -2.65-0.05) & Elevator Shaft (X: 12.75-15.35, Z: -1.35-1.35)
                var ceilN = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilN.name = "Ceiling_Slab_North";
                ceilN.transform.SetParent(roomGo.transform);
                ceilN.transform.position = new Vector3(12f, yFloor + height + 0.25f, 3.675f);
                ceilN.transform.localScale = new Vector3(8f, 0.5f, 4.65f);
                if (ceilN.GetComponent<Renderer>() != null && ceilingMat != null) ceilN.GetComponent<Renderer>().sharedMaterial = ceilingMat;

                var ceilW = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilW.name = "Ceiling_Slab_West";
                ceilW.transform.SetParent(roomGo.transform);
                ceilW.transform.position = new Vector3(10.375f, yFloor + height + 0.25f, 0.7f);
                ceilW.transform.localScale = new Vector3(4.75f, 0.5f, 1.3f);
                if (ceilW.GetComponent<Renderer>() != null && ceilingMat != null) ceilW.GetComponent<Renderer>().sharedMaterial = ceilingMat;

                var ceilS = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilS.name = "Ceiling_Slab_South";
                ceilS.transform.SetParent(roomGo.transform);
                ceilS.transform.position = new Vector3(12f, yFloor + height + 0.25f, -4.325f);
                ceilS.transform.localScale = new Vector3(8f, 0.5f, 3.35f);
                if (ceilS.GetComponent<Renderer>() != null && ceilingMat != null) ceilS.GetComponent<Renderer>().sharedMaterial = ceilingMat;

                var ceilElevN = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilElevN.name = "Ceiling_Slab_ElevatorN";
                ceilElevN.transform.SetParent(roomGo.transform);
                ceilElevN.transform.position = new Vector3(14.375f, yFloor + height + 0.25f, 2.175f);
                ceilElevN.transform.localScale = new Vector3(3.25f, 0.5f, 1.65f);
                if (ceilElevN.GetComponent<Renderer>() != null && ceilingMat != null) ceilElevN.GetComponent<Renderer>().sharedMaterial = ceilingMat;

                var ceilElevS = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilElevS.name = "Ceiling_Slab_ElevatorS";
                ceilElevS.transform.SetParent(roomGo.transform);
                ceilElevS.transform.position = new Vector3(14.375f, yFloor + height + 0.25f, -2.175f);
                ceilElevS.transform.localScale = new Vector3(3.25f, 0.5f, 1.65f);
                if (ceilElevS.GetComponent<Renderer>() != null && ceilingMat != null) ceilElevS.GetComponent<Renderer>().sharedMaterial = ceilingMat;

                var ceilShaftE = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilShaftE.name = "Ceiling_Slab_ShaftE";
                ceilShaftE.transform.SetParent(roomGo.transform);
                ceilShaftE.transform.position = new Vector3(15.675f, yFloor + height + 0.25f, 0f);
                ceilShaftE.transform.localScale = new Vector3(0.65f, 0.5f, 2.7f);
                if (ceilShaftE.GetComponent<Renderer>() != null && ceilingMat != null) ceilShaftE.GetComponent<Renderer>().sharedMaterial = ceilingMat;
            }
            else if (isUpperCorridor)
            {
                // Floors 1, 2, 3 Upper Corridors: Keep central stairwell and elevator shaft unobstructed
                var ceilWalkway = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilWalkway.name = "Ceiling_Slab_Walkway";
                ceilWalkway.transform.SetParent(roomGo.transform);
                ceilWalkway.transform.position = new Vector3(4.375f, yFloor + height + 0.25f, 1.525f);
                ceilWalkway.transform.localScale = new Vector3(16.75f, 0.5f, 2.95f);
                if (ceilWalkway.GetComponent<Renderer>() != null && ceilingMat != null) ceilWalkway.GetComponent<Renderer>().sharedMaterial = ceilingMat;

                var ceilWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilWest.name = "Ceiling_Slab_WestLanding";
                ceilWest.transform.SetParent(roomGo.transform);
                ceilWest.transform.position = new Vector3(-0.1f, yFloor + height + 0.25f, -1.475f);
                ceilWest.transform.localScale = new Vector3(7.8f, 0.5f, 3.05f);
                if (ceilWest.GetComponent<Renderer>() != null && ceilingMat != null) ceilWest.GetComponent<Renderer>().sharedMaterial = ceilingMat;

                var ceilEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilEast.name = "Ceiling_Slab_EastVestibule";
                ceilEast.transform.SetParent(roomGo.transform);
                ceilEast.transform.position = new Vector3(12.075f, yFloor + height + 0.25f, -1.475f);
                ceilEast.transform.localScale = new Vector3(1.35f, 0.5f, 3.05f);
                if (ceilEast.GetComponent<Renderer>() != null && ceilingMat != null) ceilEast.GetComponent<Renderer>().sharedMaterial = ceilingMat;

                var ceilElevN = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilElevN.name = "Ceiling_Slab_ElevatorN";
                ceilElevN.transform.SetParent(roomGo.transform);
                ceilElevN.transform.position = new Vector3(14.375f, yFloor + height + 0.25f, 2.175f);
                ceilElevN.transform.localScale = new Vector3(3.25f, 0.5f, 1.65f);
                if (ceilElevN.GetComponent<Renderer>() != null && ceilingMat != null) ceilElevN.GetComponent<Renderer>().sharedMaterial = ceilingMat;

                var ceilElevS = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilElevS.name = "Ceiling_Slab_ElevatorS";
                ceilElevS.transform.SetParent(roomGo.transform);
                ceilElevS.transform.position = new Vector3(14.375f, yFloor + height + 0.25f, -2.175f);
                ceilElevS.transform.localScale = new Vector3(3.25f, 0.5f, 1.65f);
                if (ceilElevS.GetComponent<Renderer>() != null && ceilingMat != null) ceilElevS.GetComponent<Renderer>().sharedMaterial = ceilingMat;

                var ceilShaftE = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilShaftE.name = "Ceiling_Slab_ShaftE";
                ceilShaftE.transform.SetParent(roomGo.transform);
                ceilShaftE.transform.position = new Vector3(15.675f, yFloor + height + 0.25f, 0f);
                ceilShaftE.transform.localScale = new Vector3(0.65f, 0.5f, 2.7f);
                if (ceilShaftE.GetComponent<Renderer>() != null && ceilingMat != null) ceilShaftE.GetComponent<Renderer>().sharedMaterial = ceilingMat;

                if (room.FloorIndex == 3)
                {
                    // Floor 3 Roof: Decorative Art Deco Atrium Stained-Glass Skylight directly above stairwell
                    var skylight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    skylight.name = "Atrium_Skylight_RoofFrame";
                    skylight.transform.SetParent(roomGo.transform);
                    skylight.transform.position = new Vector3(7.6f, yFloor + height + 0.45f, -1.30f);
                    skylight.transform.localScale = new Vector3(7.6f, 0.15f, 2.7f);
                    if (skylight.GetComponent<Renderer>() != null && brassMat != null) skylight.GetComponent<Renderer>().sharedMaterial = brassMat;
                }
            }
            else
            {
                var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceiling.name = "Ceiling_Slab";
                ceiling.transform.SetParent(roomGo.transform);
                ceiling.transform.position = new Vector3(center.x, yFloor + height + 0.25f, center.z);
                ceiling.transform.localScale = new Vector3(size.x, 0.5f, size.z);
                var ceilRend = ceiling.GetComponent<Renderer>();
                if (ceilRend != null && ceilingMat != null) ceilRend.sharedMaterial = ceilingMat;
            }

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
                    bool isStairArch = conn.DoorDisplayName.Contains("Grand Staircase Open Archway");
                    float doorOpeningHeight = isStairArch ? height : 2.8f;
                    float lintelHeight = height - doorOpeningHeight;
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
                    if (lintelHeight > 0.1f)
                    {
                        string lintelName = wallConns.Count == 1 ? $"{sideName}_DoorLintel" : $"{sideName}_DoorLintel_{i}";
                        BuildSolidWallSection(parent,
                            new Vector3(doorX, yFloor + doorOpeningHeight + lintelHeight * 0.5f, wallZ),
                            new Vector3(doorWidth, lintelHeight, wallThickness),
                            lowerMat, upperMat, trimMat, lintelName);
                    }

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
                    bool isStairArch = conn.DoorDisplayName.Contains("Grand Staircase Open Archway");
                    float doorOpeningHeight = isStairArch ? height : 2.8f;
                    float lintelHeight = height - doorOpeningHeight;
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
                    if (lintelHeight > 0.1f)
                    {
                        string lintelName = wallConns.Count == 1 ? $"{sideName}_DoorLintel" : $"{sideName}_DoorLintel_{i}";
                        BuildSolidWallSection(parent,
                            new Vector3(wallX, yFloor + doorOpeningHeight + lintelHeight * 0.5f, doorZ),
                            new Vector3(wallThickness, lintelHeight, doorWidth),
                            lowerMat, upperMat, trimMat, lintelName);
                    }

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
            // Track A at Z = -1.9m (width 1.3m, Z in [-2.55, -1.25])
            // Track B at Z = -0.6m (width 1.3m, Z in [-1.25, 0.05])
            // Walkway at Z in [0.05m, 3.0m] is clear and unobstructed on all 4 floors!

            // Flight 0: Floor 0 -> Floor 1 (Track A: X = 5.0 -> 11.0m, Y = 0.0 -> 5.0m, Z = -1.9m)
            BuildStairFlightX(stairsRoot.transform, 5.0f, 0.0f, -1.9f, 5.0f, 6.0f, 16, 1.3f, woodMat, "Flight_Floor0_to_1");

            // Landing 1 at Floor 1 connecting Track A and Track B (X in [10.6, 12.2], Z in [-2.55, 0.05])
            BuildStairLanding(stairsRoot.transform, new Vector3(11.4f, 4.85f, -1.25f), new Vector3(1.6f, 0.3f, 2.6f), woodMat, "Landing_Floor1");
            BuildLandingLight(stairsRoot.transform, new Vector3(11.4f, 8.5f, -1.25f), "StairLight_Floor1");

            // Flight 1: Floor 1 -> Floor 2 (Track B: X = 11.0 -> 5.0m, Y = 5.0 -> 10.0m, Z = -0.6f)
            BuildStairFlightX(stairsRoot.transform, 11.0f, 5.0f, -0.6f, 5.0f, -6.0f, 16, 1.3f, woodMat, "Flight_Floor1_to_2");

            // Landing 2 at Floor 2 connecting Track B and Track A (X in [3.8, 5.4], Z in [-2.55, 0.05])
            BuildStairLanding(stairsRoot.transform, new Vector3(4.6f, 9.85f, -1.25f), new Vector3(1.6f, 0.3f, 2.6f), woodMat, "Landing_Floor2");
            BuildLandingLight(stairsRoot.transform, new Vector3(4.6f, 13.5f, -1.25f), "StairLight_Floor2");

            // Flight 2: Floor 2 -> Floor 3 (Track A: X = 5.0 -> 11.0m, Y = 10.0 -> 15.0m, Z = -1.9f)
            BuildStairFlightX(stairsRoot.transform, 5.0f, 10.0f, -1.9f, 5.0f, 6.0f, 16, 1.3f, woodMat, "Flight_Floor2_to_3");

            // Landing 3 at Floor 3 (X in [10.6, 12.2], Z in [-2.55, 0.05])
            BuildStairLanding(stairsRoot.transform, new Vector3(11.4f, 14.85f, -1.25f), new Vector3(1.6f, 0.3f, 2.6f), woodMat, "Landing_Floor3");
            BuildLandingLight(stairsRoot.transform, new Vector3(11.4f, 18.5f, -1.25f), "StairLight_Floor3");
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
            float halfW = 1.35f;
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

            var cabinRb = cabinRoot.AddComponent<Rigidbody>();
            cabinRb.isKinematic = true;
            cabinRb.useGravity = false;

            var cabinTrigger = cabinRoot.AddComponent<BoxCollider>();
            cabinTrigger.isTrigger = true;
            cabinTrigger.center = new Vector3(0f, 1.6f, 0f);
            cabinTrigger.size = new Vector3(2.4f, 3.2f, 2.5f);

            var elevatorComp = cabinRoot.AddComponent<HotelElevator>();

            // Solid Floor Slab with BoxCollider
            var cabinFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinFloor.name = "Cabin_Floor_Slab";
            cabinFloor.transform.SetParent(cabinRoot.transform);
            cabinFloor.transform.localPosition = new Vector3(0f, -0.10f, 0f);
            cabinFloor.transform.localScale = new Vector3(2.4f, 0.2f, 2.5f);
            var fRend = cabinFloor.GetComponent<Renderer>();
            if (fRend != null && marbleMat != null) fRend.sharedMaterial = marbleMat;

            // Solid Ceiling Slab with Chandelier
            var cabinCeil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinCeil.name = "Cabin_Ceiling_Slab";
            cabinCeil.transform.SetParent(cabinRoot.transform);
            cabinCeil.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            cabinCeil.transform.localScale = new Vector3(2.4f, 0.25f, 2.5f);
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
            BuildCabinRailing(cabinRoot.transform, new Vector3(0f, 1.2f, 1.2f), new Vector3(2.2f, 2.2f, 0.1f), brassMat, "Cabin_Wall_North");
            BuildCabinRailing(cabinRoot.transform, new Vector3(0f, 1.2f, -1.2f), new Vector3(2.2f, 2.2f, 0.1f), brassMat, "Cabin_Wall_South");
            BuildCabinRailing(cabinRoot.transform, new Vector3(1.15f, 1.2f, 0f), new Vector3(0.1f, 2.2f, 2.4f), brassMat, "Cabin_Wall_East");

            // 3. Inside Cabin Interactive Control Panel
            var panelGo = new GameObject("Elevator_ControlPanel");
            panelGo.transform.SetParent(cabinRoot.transform);
            panelGo.transform.localPosition = new Vector3(0f, 1.4f, 1.12f);

            var panelPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panelPlate.name = "Panel_Backplate";
            panelPlate.transform.SetParent(panelGo.transform);
            panelPlate.transform.localPosition = Vector3.zero;
            panelPlate.transform.localScale = new Vector3(0.45f, 1.2f, 0.08f);
            var pRend = panelPlate.GetComponent<Renderer>();
            if (pRend != null && blackMarbleMat != null) pRend.sharedMaterial = blackMarbleMat;

            string[] floorNames = { "Grand Lobby", "Guest Suites & Library", "Deluxe Suites & Salon", "Sky Penthouse & Observatory" };
            for (int f = 0; f < 4; f++)
            {
                var btnGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                btnGo.name = $"Button_Floor_{f}";
                btnGo.transform.SetParent(panelGo.transform);
                btnGo.transform.localPosition = new Vector3(0f, 0.42f - f * 0.26f, -0.06f);
                btnGo.transform.localScale = new Vector3(0.32f, 0.16f, 0.08f);
                var bRend = btnGo.GetComponent<Renderer>();
                if (bRend != null && brassMat != null) bRend.sharedMaterial = brassMat;

                var btnInteract = btnGo.AddComponent<InteractableElevatorButton>();
                btnInteract.Elevator = elevatorComp;
                btnInteract.TargetFloor = f;
                btnInteract.FloorDisplayName = floorNames[f];
                btnInteract.IsCallButton = false;
            }

            // 4. Outside Call Buttons on Each Floor Landing (Floors 0, 1, 2, 3) facing WEST towards corridor
            for (int f = 0; f < 4; f++)
            {
                float y = f * 5.0f;
                var callStationGo = new GameObject($"Elevator_CallStation_Floor_{f}");
                callStationGo.transform.SetParent(elevatorRoot.transform);
                callStationGo.transform.position = new Vector3(12.7f, y + 1.2f, 1.45f);

                var callPedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                callPedestal.name = "Call_Pedestal";
                callPedestal.transform.SetParent(callStationGo.transform);
                callPedestal.transform.localPosition = Vector3.zero;
                callPedestal.transform.localScale = new Vector3(0.12f, 0.35f, 0.25f);
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

            Material mahoganyMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));
            Material brassMat = GetMaterial(mats, "BrassGold", new Color(0.88f, 0.75f, 0.35f));

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
                        // Open Archway: permanent architectural pilasters and lintel header with zero door leaf
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

                        // Brass architrave inlay
                        var archTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        archTrim.name = "Arch_Brass_Inlay";
                        archTrim.transform.SetParent(archCasing.transform);
                        archTrim.transform.localPosition = new Vector3(0f, 2.84f, 0f);
                        archTrim.transform.localScale = new Vector3(3.55f, 0.05f, 0.47f);
                        var trimRend = archTrim.GetComponent<Renderer>();
                        if (trimRend != null && brassMat != null) trimRend.sharedMaterial = brassMat;

                        continue;
                    }

                    // 1. Stationary Door Casing (Frames the 2.2m wall opening permanently with authentic jambs, header, sill, and plaque)
                    var doorCasing = new GameObject("Door_Casing");
                    doorCasing.transform.SetParent(doorRoot.transform);
                    doorCasing.transform.localPosition = Vector3.zero;

                    // Left Jamb Post
                    var jambLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    jambLeft.name = "Casing_Jamb_Left";
                    jambLeft.transform.SetParent(doorCasing.transform);
                    jambLeft.transform.localPosition = new Vector3(-0.925f, 1.375f, 0f);
                    jambLeft.transform.localScale = new Vector3(0.35f, 2.75f, 0.44f);
                    var jLCol = jambLeft.GetComponent<BoxCollider>();
                    if (jLCol != null) jLCol.isTrigger = false;
                    var jLRend = jambLeft.GetComponent<Renderer>();
                    if (jLRend != null) jLRend.sharedMaterial = mahoganyMat;

                    // Right Jamb Post
                    var jambRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    jambRight.name = "Casing_Jamb_Right";
                    jambRight.transform.SetParent(doorCasing.transform);
                    jambRight.transform.localPosition = new Vector3(0.925f, 1.375f, 0f);
                    jambRight.transform.localScale = new Vector3(0.35f, 2.75f, 0.44f);
                    var jRCol = jambRight.GetComponent<BoxCollider>();
                    if (jRCol != null) jRCol.isTrigger = false;
                    var jRRend = jambRight.GetComponent<Renderer>();
                    if (jRRend != null) jRRend.sharedMaterial = mahoganyMat;

                    // Inner Brass Reveal Trims
                    var revealL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    revealL.name = "Casing_Reveal_Left";
                    revealL.transform.SetParent(doorCasing.transform);
                    revealL.transform.localPosition = new Vector3(-0.75f, 1.375f, 0f);
                    revealL.transform.localScale = new Vector3(0.04f, 2.75f, 0.46f);
                    var rLRend = revealL.GetComponent<Renderer>();
                    if (rLRend != null) rLRend.sharedMaterial = brassMat;

                    var revealR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    revealR.name = "Casing_Reveal_Right";
                    revealR.transform.SetParent(doorCasing.transform);
                    revealR.transform.localPosition = new Vector3(0.75f, 1.375f, 0f);
                    revealR.transform.localScale = new Vector3(0.04f, 2.75f, 0.46f);
                    var rRRend = revealR.GetComponent<Renderer>();
                    if (rRRend != null) rRRend.sharedMaterial = brassMat;

                    // Overhead Lintel Header Beam
                    var lintelHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    lintelHead.name = "Casing_Lintel_Header";
                    lintelHead.transform.SetParent(doorCasing.transform);
                    lintelHead.transform.localPosition = new Vector3(0f, 2.65f, 0f);
                    lintelHead.transform.localScale = new Vector3(2.25f, 0.28f, 0.46f);
                    var lCol = lintelHead.GetComponent<BoxCollider>();
                    if (lCol != null) lCol.isTrigger = false;
                    var lhRend = lintelHead.GetComponent<Renderer>();
                    if (lhRend != null) lhRend.sharedMaterial = mahoganyMat;

                    // Stepped Cornice Crown
                    var crownCornice = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    crownCornice.name = "Casing_Crown_Cornice";
                    crownCornice.transform.SetParent(doorCasing.transform);
                    crownCornice.transform.localPosition = new Vector3(0f, 2.82f, 0f);
                    crownCornice.transform.localScale = new Vector3(2.35f, 0.10f, 0.50f);
                    var crRend = crownCornice.GetComponent<Renderer>();
                    if (crRend != null) crRend.sharedMaterial = walnutMat;

                    // Polished Brass Floor Sill Threshold
                    var threshold = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    threshold.name = "Casing_Threshold";
                    threshold.transform.SetParent(doorCasing.transform);
                    threshold.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    threshold.transform.localScale = new Vector3(1.50f, 0.04f, 0.40f);
                    var thRend = threshold.GetComponent<Renderer>();
                    if (thRend != null) thRend.sharedMaterial = brassMat;

                    // Polished Brass Identification Plaque
                    var plaque = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plaque.name = "Casing_Suite_Plaque";
                    plaque.transform.SetParent(doorCasing.transform);
                    plaque.transform.localPosition = new Vector3(0f, 2.62f, -0.235f);
                    plaque.transform.localScale = new Vector3(0.50f, 0.12f, 0.02f);
                    var plqRend = plaque.GetComponent<Renderer>();
                    if (plqRend != null) plqRend.sharedMaterial = brassMat;

                    // 2. Dynamic Door Leaf on Hinge
                    var hinge = new GameObject("Hinge");
                    hinge.transform.SetParent(doorRoot.transform);
                    hinge.transform.localPosition = new Vector3(-0.73f, 0f, 0f);

                    // Solid Wood Door Leaf with Beveled Profiling
                    var doorLeaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    doorLeaf.name = "Door_Leaf";
                    doorLeaf.transform.SetParent(hinge.transform);
                    doorLeaf.transform.localPosition = new Vector3(0.73f, 1.25f, 0f);
                    doorLeaf.transform.localScale = new Vector3(1.46f, 2.50f, 0.08f);

                    var leafRend = doorLeaf.GetComponent<Renderer>();
                    if (leafRend != null) leafRend.sharedMaterial = walnutMat;

                    var leafCol = doorLeaf.GetComponent<BoxCollider>();
                    if (leafCol == null) leafCol = doorLeaf.AddComponent<BoxCollider>();
                    leafCol.center = Vector3.zero;
                    leafCol.size = Vector3.one;
                    leafCol.isTrigger = false;

                    // Upper Recessed Panel Detail
                    var upperPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    upperPanel.name = "Door_Panel_Upper";
                    upperPanel.transform.SetParent(doorLeaf.transform);
                    upperPanel.transform.localPosition = new Vector3(0f, 0.48f, 0f);
                    upperPanel.transform.localScale = new Vector3(0.78f, 0.38f, 1.25f);
                    var upRend = upperPanel.GetComponent<Renderer>();
                    if (upRend != null) upRend.sharedMaterial = mahoganyMat;
                    var upCol = upperPanel.GetComponent<Collider>();
                    if (upCol != null) Object.DestroyImmediate(upCol);

                    // Lower Recessed Panel Detail
                    var lowerPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    lowerPanel.name = "Door_Panel_Lower";
                    lowerPanel.transform.SetParent(doorLeaf.transform);
                    lowerPanel.transform.localPosition = new Vector3(0f, -0.55f, 0f);
                    lowerPanel.transform.localScale = new Vector3(0.78f, 0.28f, 1.25f);
                    var lpRend = lowerPanel.GetComponent<Renderer>();
                    if (lpRend != null) lpRend.sharedMaterial = mahoganyMat;
                    var lpCol = lowerPanel.GetComponent<Collider>();
                    if (lpCol != null) Object.DestroyImmediate(lpCol);

                    // Brass Kickplate at Base of Door
                    var kickplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    kickplate.name = "Door_Brass_Kickplate";
                    kickplate.transform.SetParent(doorLeaf.transform);
                    kickplate.transform.localPosition = new Vector3(0f, -0.88f, 0f);
                    kickplate.transform.localScale = new Vector3(0.98f, 0.16f, 1.20f);
                    var kpRend = kickplate.GetComponent<Renderer>();
                    if (kpRend != null) kpRend.sharedMaterial = brassMat;
                    var kpCol = kickplate.GetComponent<Collider>();
                    if (kpCol != null) Object.DestroyImmediate(kpCol);

                    // Polished Brass Door Lever Handle Plate
                    var handleEscutcheon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    handleEscutcheon.name = "Door_Handle_Escutcheon";
                    handleEscutcheon.transform.SetParent(doorLeaf.transform);
                    handleEscutcheon.transform.localPosition = new Vector3(0.38f, -0.18f, 0f);
                    handleEscutcheon.transform.localScale = new Vector3(0.08f, 0.14f, 1.45f);
                    var hRend = handleEscutcheon.GetComponent<Renderer>();
                    if (hRend != null) hRend.sharedMaterial = brassMat;
                    var hCol = handleEscutcheon.GetComponent<Collider>();
                    if (hCol != null) Object.DestroyImmediate(hCol);

                    // Brass Lever Handle (Front)
                    var handleLeverFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    handleLeverFront.name = "Door_Handle_Lever_Front";
                    handleLeverFront.transform.SetParent(handleEscutcheon.transform);
                    handleLeverFront.transform.localPosition = new Vector3(-0.35f, 0.18f, 0.55f);
                    handleLeverFront.transform.localScale = new Vector3(1.8f, 0.22f, 0.22f);
                    var hlfRend = handleLeverFront.GetComponent<Renderer>();
                    if (hlfRend != null) hlfRend.sharedMaterial = brassMat;
                    var hlfCol = handleLeverFront.GetComponent<Collider>();
                    if (hlfCol != null) Object.DestroyImmediate(hlfCol);

                    // Brass Lever Handle (Back)
                    var handleLeverBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    handleLeverBack.name = "Door_Handle_Lever_Back";
                    handleLeverBack.transform.SetParent(handleEscutcheon.transform);
                    handleLeverBack.transform.localPosition = new Vector3(-0.35f, 0.18f, -0.55f);
                    handleLeverBack.transform.localScale = new Vector3(1.8f, 0.22f, 0.22f);
                    var hlbRend = handleLeverBack.GetComponent<Renderer>();
                    if (hlbRend != null) hlbRend.sharedMaterial = brassMat;
                    var hlbCol = handleLeverBack.GetComponent<Collider>();
                    if (hlbCol != null) Object.DestroyImmediate(hlbCol);

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

        private static void BuildRoomFurniture(Transform parent, List<RoomDefinition> rooms, Dictionary<string, Material> mats, TomorrowsCrime.Procedural.Core.SeededRandom rng)
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
            Material whiteMat = GetMaterial(mats, "ShirtWhite", Color.white);

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

                        // Switchboard console and executive safe in reception
                        BuildSolidStorageUnit(deskRoot.transform, new Vector3(-3.2f, y, 12.0f), new Vector3(1.2f, 1.8f, 0.6f), walnutMat, "Reception_Switchboard");
                        BuildSolidStorageUnit(deskRoot.transform, new Vector3(3.2f, y, 12.0f), new Vector3(1.0f, 1.4f, 0.8f), marbleBlackMat, "Reception_ExecutiveSafe");
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
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-16f, y, -10f), new Vector3(1.4f, 0.75f, 1.4f), brassMat, "Conservatory_BistroTable");
                        break;

                    case RoomType.StaffOffice:
                        BuildStaffOfficeFurnishings(furnitureRoot.transform, new Vector3(21f, y, 4f), mats);
                        break;

                    case RoomType.UtilityRoom:
                        BuildUtilityRoomFurnishings(furnitureRoot.transform, new Vector3(21f, y, -4f), mats);
                        break;

                    case RoomType.LuggageStorage:
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(6f, y, -14f), new Vector3(3.2f, 2.0f, 0.8f), mahoganyMat, "Luggage_StorageRack");
                        BuildBellhopLuggageCart(furnitureRoot.transform, new Vector3(4.5f, y, -10f), brassMat, walnutMat, "Luggage_TrolleyCart");
                        break;

                    case RoomType.Library:
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-16f, y, 6f), new Vector3(8.0f, 3.6f, 0.8f), mahoganyMat, "Library_Bookshelf_North");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-16f, y, -6f), new Vector3(8.0f, 3.6f, 0.8f), mahoganyMat, "Library_Bookshelf_South");
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-16f, y, 0f), new Vector3(3.0f, 0.85f, 1.6f), walnutMat, "Library_StudyReadingDesk");
                        BuildFloorGlobe(furnitureRoot.transform, new Vector3(-8.5f, y, 3.5f), brassMat, "Library_TerrestrialGlobe");
                        break;

                    case RoomType.CardRoom:
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-6f, y, -13f), new Vector3(2.2f, 0.82f, 2.2f), greenFeltMat, "CardRoom_GameTable");
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-11f, y, 10.5f), new Vector3(1.4f, 0.80f, 1.4f), walnutMat, "CardRoom_ChessTable");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-15f, y, 12f), new Vector3(1.4f, 1.2f, 0.5f), mahoganyMat, "CardRoom_HumidorCredenza");
                        break;

                    case RoomType.MusicSalon:
                        BuildSolidPiano(furnitureRoot.transform, new Vector3(0f, y, 11f), mats);
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-3f, y, 12f), new Vector3(0.6f, 1.4f, 0.6f), walnutMat, "MusicSalon_CelloStand");
                        break;

                    case RoomType.BilliardsRoom:
                        BuildSolidBilliardsTable(furnitureRoot.transform, new Vector3(-16f, y, 0f), mats);
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-21f, y, 0f), new Vector3(0.4f, 2.0f, 2.4f), mahoganyMat, "Billiards_CueRack");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-16f, y, -5.5f), new Vector3(3.0f, 0.95f, 0.8f), walnutMat, "Billiards_SpectatorBench");
                        break;

                    case RoomType.CigarLounge:
                        BuildSolidFireplace(furnitureRoot.transform, new Vector3(-6f, y, -15f), mats, "CigarLounge_Fireplace");
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-6f, y, -12f), new Vector3(2.0f, 0.55f, 1.2f), mahoganyMat, "CigarLounge_CoffeeTable");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-15f, y, -11f), new Vector3(1.8f, 2.2f, 0.6f), walnutMat, "CigarLounge_HumidorCabinet");
                        break;

                    case RoomType.CrownPenthouse:
                        BuildSolidFireplace(furnitureRoot.transform, new Vector3(-21f, y, 0f), mats, "Penthouse_GrandFireplace");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-16f, y, 0f), new Vector3(4.0f, 1.15f, 1.2f), walnutMat, "Penthouse_PrivateBar");
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-16f, y, 4f), new Vector3(2.4f, 0.50f, 1.4f), mahoganyMat, "Penthouse_LoungeTable");
                        break;

                    case RoomType.Observatory:
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(0f, y, 11f), new Vector3(1.4f, 2.2f, 1.4f), brassMat, "Observatory_TelescopePedestal");
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-3f, y, 9f), new Vector3(1.8f, 0.85f, 1.0f), walnutMat, "Observatory_StarChartDesk");
                        break;

                    case RoomType.Solarium:
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-6f, y, -14f), new Vector3(2.2f, 1.2f, 2.2f), marbleIvoryMat, "Solarium_StoneFountain");
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-3f, y, -10f), new Vector3(1.6f, 0.70f, 1.0f), brassMat, "Solarium_LoungerTable");
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
                        // Meaningful pseudorandom suite with King Canopy, Twin Beds (2 beds), Queen Vanity, or Executive layout
                        BuildLuxurySuiteWithEnSuiteShower(furnitureRoot.transform, room, mats, rng);
                        break;

                    case RoomType.PresidentialStudy:
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-16f, y, -11f), new Vector3(2.6f, 0.85f, 1.4f), walnutMat, "Presidential_StudyDesk");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-19f, y, -11f), new Vector3(1.2f, 1.6f, 0.9f), marbleBlackMat, "Presidential_FloorSafe");
                        BuildFloorGlobe(furnitureRoot.transform, new Vector3(-13f, y, -11f), brassMat, "Presidential_Globe");
                        break;

                    case RoomType.HousekeepingStorage:
                    case RoomType.LinenStorage:
                    case RoomType.ServiceAnnexFloor2:
                        BuildSolidStorageUnit(furnitureRoot.transform, room.Bounds.center + new Vector3(0f, -room.Bounds.size.y * 0.5f, 0f),
                            new Vector3(2.4f, 2.4f, 0.7f), walnutMat, $"{room.RoomId}_Shelves");
                        BuildBellhopLuggageCart(furnitureRoot.transform, room.Bounds.center + new Vector3(1.8f, -room.Bounds.size.y * 0.5f, 0f), brassMat, walnutMat, $"{room.RoomId}_LinenCart");
                        break;
                }
            }
        }

        /// <summary>
        /// Furnishes an authentic, pseudorandom luxury guest suite featuring either:
        /// 0: King Canopy Suite (Single King-size four-poster bed + writing desk + armoire + en-suite shower)
        /// 1: Twin Luxury Suite (2 separate twin beds side-by-side + center lamp + luggage trunks + armoire + en-suite shower)
        /// 2: Queen Vanity Suite (Queen bed + full tripartite dressing vanity table & mirror + armoire + en-suite shower)
        /// 3: Executive Diplomatic Suite (King bed + partner desk with banker lamp, typewriter & phone + globe + en-suite shower)
        /// </summary>
        private static void BuildLuxurySuiteWithEnSuiteShower(Transform parent, RoomDefinition room, Dictionary<string, Material> mats, TomorrowsCrime.Procedural.Core.SeededRandom rng)
        {
            var suiteRoot = new GameObject($"Suite_{room.RoomId}_Furnishings");
            suiteRoot.transform.SetParent(parent);

            float yFloor = room.FloorIndex * 5.0f;
            Vector3 center = room.Bounds.center;
            Vector3 size = room.Bounds.size;

            Material mahoganyMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));
            Material velvetGoldMat = GetMaterial(mats, "VelvetGold", new Color(0.85f, 0.70f, 0.25f));
            Material velvetEmeraldMat = GetMaterial(mats, "VelvetEmerald", new Color(0.12f, 0.40f, 0.22f));
            Material carpetBurgundyMat = GetMaterial(mats, "CarpetBurgundy", new Color(0.55f, 0.12f, 0.16f));
            Material carpetSapphireMat = GetMaterial(mats, "CarpetSapphire", new Color(0.12f, 0.20f, 0.42f));
            Material whiteMat = GetMaterial(mats, "ShirtWhite", Color.white);
            Material brassMat = GetMaterial(mats, "BrassGold", new Color(0.88f, 0.75f, 0.35f));
            Material greenFeltMat = GetMaterial(mats, "GreenFelt", new Color(0.12f, 0.45f, 0.22f));
            Material marbleIvoryMat = GetMaterial(mats, "MarbleIvory", new Color(0.92f, 0.90f, 0.85f));

            Material[] duvetPalette = { velvetGoldMat, carpetBurgundyMat, carpetSapphireMat, velvetEmeraldMat };
            Material duvetMat = (rng != null) ? rng.NextItem(duvetPalette) : velvetGoldMat;

            // Determine suite bed configuration:
            // 0 = King Canopy Suite (Single King bed with 4-poster canopy, bench, armoire, desk)
            // 1 = Twin Beds Suite (2 distinct single beds, center nightstand, twin luggage benches, armoire)
            // 2 = Queen Vanity Suite (Queen bed, dressing vanity unit with mirror, stool, perfume)
            // 3 = Executive Diplomatic Suite (King bed, large partner desk with banker lamp, typewriter, globe)
            int layoutType;
            if (room.RoomId == "room_guest_104" || room.RoomId == "room_guest_201" || room.RoomId == "room_guest_204" || room.RoomId == "room_vip_302")
            {
                // Guaranteed 2 Beds for these twin suites
                layoutType = 1;
            }
            else if (room.RoomId == "room_guest_101")
            {
                // Lord Harrington: Executive King Canopy with writing desk
                layoutType = 0;
            }
            else if (room.RoomId == "room_guest_102")
            {
                // Madame Valerie (Crime Scene): Opulent King Canopy with luxury vanity
                layoutType = 2;
            }
            else if (room.RoomId == "room_guest_103")
            {
                // Dr. Sterling: Executive Medical Study Suite
                layoutType = 3;
            }
            else if (room.RoomId == "room_guest_105")
            {
                // Inspector Cross: Detective Suite with typewriter & desk
                layoutType = 3;
            }
            else
            {
                layoutType = (rng != null) ? rng.NextInt(0, 4) : Mathf.Abs(room.RoomId.GetHashCode()) % 4;
            }

            if (size.x >= 12f)
            {
                // Wide Suite Layout (14m or 16m wide): Bedroom on East, En-Suite Bathroom on West
                Vector3 bedCenter = new Vector3(center.x + 2.5f, yFloor, center.z + size.z * 0.20f);

                if (layoutType == 1)
                {
                    // 2 TWIN BEDS!
                    BuildTwinLuxuryBeds(suiteRoot.transform, bedCenter, yFloor, mahoganyMat, duvetMat, whiteMat, brassMat, walnutMat, room.RoomId, isWide: true);
                }
                else if (layoutType == 2)
                {
                    // Queen Bed with Luxury Dressing Vanity Unit
                    BuildLuxuryFourPosterBed(suiteRoot.transform, bedCenter, yFloor, mahoganyMat, duvetMat, whiteMat, brassMat, $"{room.RoomId}_Bed");
                    BuildDressingVanityUnit(suiteRoot.transform, new Vector3(center.x + 5.5f, yFloor, center.z - size.z * 0.15f), walnutMat, brassMat, marbleIvoryMat, velvetEmeraldMat, $"{room.RoomId}_Vanity");
                }
                else if (layoutType == 3)
                {
                    // Executive Diplomatic Suite with Partner Desk, Typewriter, Banker Lamp & Globe
                    BuildLuxuryFourPosterBed(suiteRoot.transform, bedCenter, yFloor, mahoganyMat, duvetMat, whiteMat, brassMat, $"{room.RoomId}_Bed");
                    BuildExecutivePartnerDeskUnit(suiteRoot.transform, new Vector3(center.x + 5.2f, yFloor, center.z - size.z * 0.15f), walnutMat, greenFeltMat, brassMat, $"{room.RoomId}_Desk");
                    BuildFloorGlobe(suiteRoot.transform, new Vector3(center.x + 5.8f, yFloor, center.z + size.z * 0.38f), brassMat, $"{room.RoomId}_Globe");
                }
                else
                {
                    // Grand King Canopy Suite
                    BuildLuxuryFourPosterBed(suiteRoot.transform, bedCenter, yFloor, mahoganyMat, duvetMat, whiteMat, brassMat, $"{room.RoomId}_Bed");
                    BuildSolidTable(suiteRoot.transform, new Vector3(center.x + 5.5f, yFloor, center.z - size.z * 0.15f), new Vector3(2.0f, 0.85f, 1.1f), walnutMat, $"{room.RoomId}_WritingDesk");
                }

                // Wardrobe Armoire
                BuildSolidStorageUnit(suiteRoot.transform, new Vector3(center.x + 1.2f, yFloor, center.z - size.z * 0.28f),
                    new Vector3(1.6f, 2.3f, 0.65f), walnutMat, $"{room.RoomId}_Armoire");

                // En-Suite Luxury Bathroom Zone on West
                Vector3 showerPos = new Vector3(center.x - size.x * 0.35f, yFloor, center.z + size.z * 0.22f);
                Vector3 vanityPos = new Vector3(center.x - size.x * 0.18f, yFloor, center.z + size.z * 0.22f);
                Vector3 toiletPos = new Vector3(center.x - size.x * 0.35f, yFloor, center.z - size.z * 0.22f);
                Vector3 towelPos = new Vector3(center.x - size.x * 0.18f, yFloor, center.z - size.z * 0.22f);

                BuildEnSuiteBathroom(suiteRoot.transform, showerPos, vanityPos, toiletPos, towelPos, yFloor, mats, room.RoomId);
            }
            else
            {
                // Compact Suite Layout (8m x 8m): Bed on East, En-Suite on West
                Vector3 bedCenter = new Vector3(center.x + 1.8f, yFloor, center.z - 0.5f);

                if (layoutType == 1)
                {
                    // 2 TWIN BEDS!
                    BuildTwinLuxuryBeds(suiteRoot.transform, bedCenter, yFloor, mahoganyMat, duvetMat, whiteMat, brassMat, walnutMat, room.RoomId, isWide: false);
                }
                else if (layoutType == 2)
                {
                    // Queen Bed + Dressing Vanity
                    BuildLuxuryFourPosterBed(suiteRoot.transform, bedCenter, yFloor, mahoganyMat, duvetMat, whiteMat, brassMat, $"{room.RoomId}_Bed");
                    BuildDressingVanityUnit(suiteRoot.transform, new Vector3(center.x + 2.5f, yFloor, center.z + 2.4f), walnutMat, brassMat, marbleIvoryMat, velvetEmeraldMat, $"{room.RoomId}_Vanity");
                }
                else if (layoutType == 3)
                {
                    // Executive Suite with Desk
                    BuildLuxuryFourPosterBed(suiteRoot.transform, bedCenter, yFloor, mahoganyMat, duvetMat, whiteMat, brassMat, $"{room.RoomId}_Bed");
                    BuildExecutivePartnerDeskUnit(suiteRoot.transform, new Vector3(center.x + 2.4f, yFloor, center.z + 2.2f), walnutMat, greenFeltMat, brassMat, $"{room.RoomId}_Desk");
                }
                else
                {
                    // King Canopy Bed
                    BuildLuxuryFourPosterBed(suiteRoot.transform, bedCenter, yFloor, mahoganyMat, duvetMat, whiteMat, brassMat, $"{room.RoomId}_Bed");
                }

                // Wardrobe Armoire
                BuildSolidStorageUnit(suiteRoot.transform, new Vector3(center.x + 2.8f, yFloor, center.z - 2.8f),
                    new Vector3(1.3f, 2.3f, 0.60f), walnutMat, $"{room.RoomId}_Armoire");

                // En-Suite Bathroom Zone on West
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
        /// fluffy pillows, carved footboard, bedposts with brass finials, twin bedside nightstands, and warm bedside lamps.
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

            // Wooden Bed Base Frame
            var bedFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bedFrame.name = "Bed_Base_Frame";
            bedFrame.transform.SetParent(bedRoot.transform);
            bedFrame.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            bedFrame.transform.localScale = new Vector3(2.05f, 0.20f, 2.25f);
            if (bedFrame.GetComponent<Renderer>() != null && frameMat != null) bedFrame.GetComponent<Renderer>().sharedMaterial = frameMat;

            // Mattress
            var mattress = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mattress.name = "Mattress";
            mattress.transform.SetParent(bedRoot.transform);
            mattress.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            mattress.transform.localScale = new Vector3(1.95f, 0.40f, 2.15f);
            var mRend = mattress.GetComponent<Renderer>();
            if (mRend != null && sheetMat != null) mRend.sharedMaterial = sheetMat;

            // Quilted Duvet Runner
            var duvet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            duvet.name = "Duvet_Runner";
            duvet.transform.SetParent(bedRoot.transform);
            duvet.transform.localPosition = new Vector3(0f, 0.50f, -0.45f);
            duvet.transform.localScale = new Vector3(1.98f, 0.35f, 1.10f);
            var dRend = duvet.GetComponent<Renderer>();
            if (dRend != null && duvetMat != null) dRend.sharedMaterial = duvetMat;

            // Two White Linen Pillows propped against Headboard
            for (int i = 0; i < 2; i++)
            {
                var pillow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillow.name = $"Pillow_{i}";
                pillow.transform.SetParent(bedRoot.transform);
                pillow.transform.localPosition = new Vector3(i == 0 ? -0.52f : 0.52f, 0.68f, 0.75f);
                pillow.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
                pillow.transform.localScale = new Vector3(0.65f, 0.15f, 0.40f);
                if (pillow.GetComponent<Renderer>() != null && sheetMat != null) pillow.GetComponent<Renderer>().sharedMaterial = sheetMat;
            }

            // Carved Mahogany Headboard
            var headboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headboard.name = "Headboard";
            headboard.transform.SetParent(bedRoot.transform);
            headboard.transform.localPosition = new Vector3(0f, 0.85f, 1.05f);
            headboard.transform.localScale = new Vector3(2.15f, 1.10f, 0.14f);
            var hRend = headboard.GetComponent<Renderer>();
            if (hRend != null && frameMat != null) hRend.sharedMaterial = frameMat;

            // Carved Mahogany Footboard
            var footboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            footboard.name = "Footboard";
            footboard.transform.SetParent(bedRoot.transform);
            footboard.transform.localPosition = new Vector3(0f, 0.45f, -1.05f);
            footboard.transform.localScale = new Vector3(2.15f, 0.50f, 0.14f);
            if (footboard.GetComponent<Renderer>() != null && frameMat != null) footboard.GetComponent<Renderer>().sharedMaterial = frameMat;

            // 4 Turned Corner Bedposts with Brass Finials
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
                if (pRend != null && frameMat != null) pRend.sharedMaterial = frameMat;

                var finial = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                finial.name = $"Finial_{i}";
                finial.transform.SetParent(post.transform);
                finial.transform.localPosition = new Vector3(0f, 1.02f, 0f);
                finial.transform.localScale = new Vector3(1.5f, 0.14f, 1.5f);
                if (finial.GetComponent<Renderer>() != null && brassMat != null) finial.GetComponent<Renderer>().sharedMaterial = brassMat;
            }

            // Twin Bedside Nightstands with Glowing Lamps
            BuildNightstandWithLamp(bedRoot.transform, new Vector3(-1.55f, 0f, 0.70f), frameMat, brassMat, sheetMat, "Nightstand_L");
            BuildNightstandWithLamp(bedRoot.transform, new Vector3(1.55f, 0f, 0.70f), frameMat, brassMat, sheetMat, "Nightstand_R");
        }

        private static GameObject BuildDetailedChair(Transform parent, Vector3 localPos, Quaternion localRot, Material woodMat, Material fabricMat, string name)
        {
#if UNITY_EDITOR
            var armchairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/HotelLobby_Armchair.obj");
            if (armchairPrefab != null)
            {
                var chairInst = Object.Instantiate(armchairPrefab, parent);
                chairInst.name = name;
                chairInst.transform.localPosition = localPos;
                chairInst.transform.localRotation = localRot;
                chairInst.transform.localScale = Vector3.one;
                var r = chairInst.GetComponentInChildren<Renderer>();
                if (r != null && fabricMat != null) r.sharedMaterial = fabricMat;
                var col = chairInst.GetComponent<BoxCollider>();
                if (col == null) col = chairInst.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.5f, 0f);
                col.size = new Vector3(0.9f, 1.0f, 0.9f);
                return chairInst;
            }
#endif
            var chairGo = new GameObject(name);
            chairGo.transform.SetParent(parent, false);
            chairGo.transform.localPosition = localPos;
            chairGo.transform.localRotation = localRot;

            Vector3[] legOffsets = {
                new Vector3(-0.30f, 0.20f, -0.28f),
                new Vector3(0.30f, 0.20f, -0.28f),
                new Vector3(-0.30f, 0.20f, 0.28f),
                new Vector3(0.30f, 0.20f, 0.28f)
            };
            for (int i = 0; i < 4; i++)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = $"Leg_{i}";
                leg.transform.SetParent(chairGo.transform, false);
                leg.transform.localPosition = legOffsets[i];
                leg.transform.localScale = new Vector3(0.06f, 0.20f, 0.06f);
                if (woodMat != null) leg.GetComponent<Renderer>().sharedMaterial = woodMat;
            }

            var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "Seat_Cushion";
            seat.transform.SetParent(chairGo.transform, false);
            seat.transform.localPosition = new Vector3(0f, 0.44f, 0f);
            seat.transform.localScale = new Vector3(0.74f, 0.16f, 0.70f);
            if (fabricMat != null) seat.GetComponent<Renderer>().sharedMaterial = fabricMat;

            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "Backrest";
            back.transform.SetParent(chairGo.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.82f, 0.30f);
            back.transform.localScale = new Vector3(0.74f, 0.65f, 0.14f);
            if (fabricMat != null) back.GetComponent<Renderer>().sharedMaterial = fabricMat;

            var armL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armL.name = "Armrest_L";
            armL.transform.SetParent(chairGo.transform, false);
            armL.transform.localPosition = new Vector3(-0.36f, 0.62f, 0.02f);
            armL.transform.localScale = new Vector3(0.12f, 0.26f, 0.62f);
            if (fabricMat != null) armL.GetComponent<Renderer>().sharedMaterial = fabricMat;

            var armR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armR.name = "Armrest_R";
            armR.transform.SetParent(chairGo.transform, false);
            armR.transform.localPosition = new Vector3(0.36f, 0.62f, 0.02f);
            armR.transform.localScale = new Vector3(0.12f, 0.26f, 0.62f);
            if (fabricMat != null) armR.GetComponent<Renderer>().sharedMaterial = fabricMat;

            var colBox = chairGo.AddComponent<BoxCollider>();
            colBox.center = new Vector3(0f, 0.52f, 0f);
            colBox.size = new Vector3(0.85f, 1.05f, 0.80f);
            return chairGo;
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

        private static void BuildTwinLuxuryBeds(Transform parent, Vector3 center, float yFloor, Material woodMat, Material duvetMat, Material sheetMat, Material brassMat, Material walnutMat, string suiteName, bool isWide)
        {
            var twinsRoot = new GameObject($"{suiteName}_TwinBeds_Set");
            twinsRoot.transform.SetParent(parent);
            twinsRoot.transform.position = center;

            float bedSpacing = isWide ? 1.55f : 1.35f;

            // Bed 1 (Left / West)
            BuildSingleTwinBed(twinsRoot.transform, new Vector3(-bedSpacing, 0f, 0f), woodMat, duvetMat, sheetMat, brassMat, $"{suiteName}_TwinBed_A");
            // Bed 2 (Right / East)
            BuildSingleTwinBed(twinsRoot.transform, new Vector3(bedSpacing, 0f, 0f), woodMat, duvetMat, sheetMat, brassMat, $"{suiteName}_TwinBed_B");

            // Shared Central Nightstand with Lamp
            BuildNightstandWithLamp(twinsRoot.transform, new Vector3(0f, 0f, 0.65f), walnutMat, brassMat, sheetMat, $"{suiteName}_CenterNightstand");

            // Twin Footboard Luggage Benches with Leather Suitcases
            BuildLuggageBenchWithSuitcase(twinsRoot.transform, new Vector3(-bedSpacing, 0f, -1.35f), walnutMat, brassMat, $"{suiteName}_LuggageBench_A");
            BuildLuggageBenchWithSuitcase(twinsRoot.transform, new Vector3(bedSpacing, 0f, -1.35f), walnutMat, brassMat, $"{suiteName}_LuggageBench_B");
        }

        private static void BuildSingleTwinBed(Transform parent, Vector3 localPos, Material frameMat, Material duvetMat, Material sheetMat, Material brassMat, string name)
        {
            var bedRoot = new GameObject(name);
            bedRoot.transform.SetParent(parent);
            bedRoot.transform.localPosition = localPos;

            var col = bedRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.45f, 0f);
            col.size = new Vector3(1.25f, 0.9f, 2.1f);
            col.isTrigger = false;

            // Mattress
            var mattress = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mattress.name = "Twin_Mattress";
            mattress.transform.SetParent(bedRoot.transform);
            mattress.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            mattress.transform.localScale = new Vector3(1.15f, 0.35f, 2.0f);
            var mRend = mattress.GetComponent<Renderer>();
            if (mRend != null) mRend.sharedMaterial = sheetMat;

            // Duvet
            var duvet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            duvet.name = "Twin_Duvet";
            duvet.transform.SetParent(bedRoot.transform);
            duvet.transform.localPosition = new Vector3(0f, 0.48f, -0.4f);
            duvet.transform.localScale = new Vector3(1.18f, 0.30f, 1.05f);
            var dRend = duvet.GetComponent<Renderer>();
            if (dRend != null) dRend.sharedMaterial = duvetMat;

            // Headboard
            var headboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headboard.name = "Twin_Headboard";
            headboard.transform.SetParent(bedRoot.transform);
            headboard.transform.localPosition = new Vector3(0f, 0.75f, 0.98f);
            headboard.transform.localScale = new Vector3(1.25f, 0.95f, 0.12f);
            var hRend = headboard.GetComponent<Renderer>();
            if (hRend != null) hRend.sharedMaterial = frameMat;
        }

        private static void BuildLuggageBenchWithSuitcase(Transform parent, Vector3 localPos, Material woodMat, Material brassMat, string name)
        {
            var benchRoot = new GameObject(name);
            benchRoot.transform.SetParent(parent);
            benchRoot.transform.localPosition = localPos;

            // Wooden Bench
            var bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "Bench_Frame";
            bench.transform.SetParent(benchRoot.transform);
            bench.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            bench.transform.localScale = new Vector3(0.95f, 0.44f, 0.45f);
            var bRend = bench.GetComponent<Renderer>();
            if (bRend != null) bRend.sharedMaterial = woodMat;

            var col = benchRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.35f, 0f);
            col.size = new Vector3(0.95f, 0.7f, 0.45f);
            col.isTrigger = false;

            // Leather Luggage Trunk resting on bench
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trunk.name = "Leather_Trunk";
            trunk.transform.SetParent(benchRoot.transform);
            trunk.transform.localPosition = new Vector3(0f, 0.54f, 0f);
            trunk.transform.localScale = new Vector3(0.70f, 0.26f, 0.38f);
            var tRend = trunk.GetComponent<Renderer>();
            if (tRend != null) tRend.sharedMaterial = woodMat;

            // Brass latches
            var latch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            latch.name = "Trunk_Latch";
            latch.transform.SetParent(benchRoot.transform);
            latch.transform.localPosition = new Vector3(0f, 0.54f, 0.195f);
            latch.transform.localScale = new Vector3(0.12f, 0.08f, 0.02f);
            var lRend = latch.GetComponent<Renderer>();
            if (lRend != null) lRend.sharedMaterial = brassMat;
        }

        private static void BuildDressingVanityUnit(Transform parent, Vector3 position, Material woodMat, Material brassMat, Material mirrorMat, Material velvetMat, string name)
        {
            var vanityRoot = new GameObject(name);
            vanityRoot.transform.SetParent(parent);
            vanityRoot.transform.position = position;

            var col = vanityRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.85f, 0f);
            col.size = new Vector3(1.6f, 1.7f, 0.7f);
            col.isTrigger = false;

            // Vanity Table Base with Drawers
            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Vanity_Table";
            table.transform.SetParent(vanityRoot.transform);
            table.transform.localPosition = new Vector3(0f, 0.40f, 0f);
            table.transform.localScale = new Vector3(1.5f, 0.76f, 0.65f);
            var tRend = table.GetComponent<Renderer>();
            if (tRend != null) tRend.sharedMaterial = woodMat;

            // Center Large Mirror
            var centerMirror = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerMirror.name = "Center_Mirror";
            centerMirror.transform.SetParent(vanityRoot.transform);
            centerMirror.transform.localPosition = new Vector3(0f, 1.25f, 0.26f);
            centerMirror.transform.localScale = new Vector3(0.75f, 0.88f, 0.04f);
            var cmRend = centerMirror.GetComponent<Renderer>();
            if (cmRend != null) cmRend.sharedMaterial = mirrorMat;

            // Left Wing Mirror
            var leftMirror = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftMirror.name = "Wing_Mirror_L";
            leftMirror.transform.SetParent(vanityRoot.transform);
            leftMirror.transform.localPosition = new Vector3(-0.48f, 1.20f, 0.23f);
            leftMirror.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
            leftMirror.transform.localScale = new Vector3(0.32f, 0.75f, 0.03f);
            var lmiRend = leftMirror.GetComponent<Renderer>();
            if (lmiRend != null) lmiRend.sharedMaterial = mirrorMat;

            // Right Wing Mirror
            var rightMirror = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightMirror.name = "Wing_Mirror_R";
            rightMirror.transform.SetParent(vanityRoot.transform);
            rightMirror.transform.localPosition = new Vector3(0.48f, 1.20f, 0.23f);
            rightMirror.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);
            rightMirror.transform.localScale = new Vector3(0.32f, 0.75f, 0.03f);
            var rmiRend = rightMirror.GetComponent<Renderer>();
            if (rmiRend != null) rmiRend.sharedMaterial = mirrorMat;

            // Round Velvet Vanity Stool
            var stool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stool.name = "Vanity_Stool";
            stool.transform.SetParent(vanityRoot.transform);
            stool.transform.localPosition = new Vector3(0f, 0.24f, -0.60f);
            stool.transform.localScale = new Vector3(0.44f, 0.24f, 0.44f);
            var sRend = stool.GetComponent<Renderer>();
            if (sRend != null) sRend.sharedMaterial = velvetMat;
        }

        private static void BuildExecutivePartnerDeskUnit(Transform parent, Vector3 position, Material woodMat, Material blotterMat, Material brassMat, string name)
        {
            var deskRoot = new GameObject(name);
            deskRoot.transform.SetParent(parent);
            deskRoot.transform.position = position;

            var col = deskRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.55f, 0f);
            col.size = new Vector3(1.9f, 1.1f, 1.2f);
            col.isTrigger = false;

            // Solid Walnut Partner Desk Top & Pedestals
            var deskTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deskTop.name = "Desk_Top";
            deskTop.transform.SetParent(deskRoot.transform);
            deskTop.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            deskTop.transform.localScale = new Vector3(1.85f, 0.12f, 1.05f);
            var dtRend = deskTop.GetComponent<Renderer>();
            if (dtRend != null) dtRend.sharedMaterial = woodMat;

            // Left Pedestal
            var pedL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedL.name = "Pedestal_L";
            pedL.transform.SetParent(deskRoot.transform);
            pedL.transform.localPosition = new Vector3(-0.65f, 0.35f, 0f);
            pedL.transform.localScale = new Vector3(0.48f, 0.70f, 0.95f);
            var plRend = pedL.GetComponent<Renderer>();
            if (plRend != null) plRend.sharedMaterial = woodMat;

            // Right Pedestal
            var pedR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedR.name = "Pedestal_R";
            pedR.transform.SetParent(deskRoot.transform);
            pedR.transform.localPosition = new Vector3(0.65f, 0.35f, 0f);
            pedR.transform.localScale = new Vector3(0.48f, 0.70f, 0.95f);
            var prRend = pedR.GetComponent<Renderer>();
            if (prRend != null) prRend.sharedMaterial = woodMat;

            // Green Leather Desk Blotter
            var blotter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blotter.name = "Leather_Blotter";
            blotter.transform.SetParent(deskRoot.transform);
            blotter.transform.localPosition = new Vector3(0f, 0.815f, 0f);
            blotter.transform.localScale = new Vector3(0.85f, 0.015f, 0.55f);
            var bRend = blotter.GetComponent<Renderer>();
            if (bRend != null) bRend.sharedMaterial = blotterMat;

            // Banker Lamp with Emerald Glass Shade and Brass Stem
            var lampBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lampBase.name = "BankerLamp_Base";
            lampBase.transform.SetParent(deskRoot.transform);
            lampBase.transform.localPosition = new Vector3(0.55f, 0.84f, 0.25f);
            lampBase.transform.localScale = new Vector3(0.14f, 0.03f, 0.14f);
            var lbRend = lampBase.GetComponent<Renderer>();
            if (lbRend != null) lbRend.sharedMaterial = brassMat;

            var lampShade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lampShade.name = "BankerLamp_EmeraldShade";
            lampShade.transform.SetParent(deskRoot.transform);
            lampShade.transform.localPosition = new Vector3(0.55f, 1.08f, 0.25f);
            lampShade.transform.localScale = new Vector3(0.24f, 0.10f, 0.12f);
            var lsRend = lampShade.GetComponent<Renderer>();
            if (lsRend != null) lsRend.sharedMaterial = blotterMat;

            var lightGo = new GameObject("BankerLamp_Glow");
            lightGo.transform.SetParent(deskRoot.transform);
            lightGo.transform.localPosition = new Vector3(0.55f, 1.02f, 0.25f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.85f, 1.0f, 0.82f);
            l.intensity = 1.4f;
            l.range = 2.5f;

            // Executive Chair behind desk
            BuildDetailedChair(deskRoot.transform, new Vector3(0f, 0f, 0.65f), Quaternion.identity, woodMat, blotterMat, "Executive_Chair");
        }

        private static void BuildFloorGlobe(Transform parent, Vector3 position, Material brassMat, string name)
        {
            var globeRoot = new GameObject(name);
            globeRoot.transform.SetParent(parent);
            globeRoot.transform.position = position;

            var col = globeRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.65f, 0f);
            col.size = new Vector3(0.7f, 1.3f, 0.7f);
            col.isTrigger = false;

            // Brass Tripod Stand Base
            var stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stand.name = "Stand_Column";
            stand.transform.SetParent(globeRoot.transform);
            stand.transform.localPosition = new Vector3(0f, 0.40f, 0f);
            stand.transform.localScale = new Vector3(0.12f, 0.40f, 0.12f);
            var sRend = stand.GetComponent<Renderer>();
            if (sRend != null) sRend.sharedMaterial = brassMat;

            // Gimbal Ring
            var gimbal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gimbal.name = "Gimbal_Ring";
            gimbal.transform.SetParent(globeRoot.transform);
            gimbal.transform.localPosition = new Vector3(0f, 0.88f, 0f);
            gimbal.transform.localScale = new Vector3(0.58f, 0.04f, 0.58f);
            var gRend = gimbal.GetComponent<Renderer>();
            if (gRend != null) gRend.sharedMaterial = brassMat;

            // Terrestrial Sphere
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Globe_Sphere";
            sphere.transform.SetParent(globeRoot.transform);
            sphere.transform.localPosition = new Vector3(0f, 0.88f, 0f);
            sphere.transform.localScale = new Vector3(0.48f, 0.48f, 0.48f);
            var spRend = sphere.GetComponent<Renderer>();
            if (spRend != null) spRend.sharedMaterial = brassMat;
        }

        private static void BuildBellhopLuggageCart(Transform parent, Vector3 position, Material brassMat, Material trunkMat, string name)
        {
            var cartRoot = new GameObject(name);
            cartRoot.transform.SetParent(parent);
            cartRoot.transform.position = position;

            var col = cartRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.size = new Vector3(1.2f, 1.9f, 1.8f);
            col.isTrigger = false;

            // Tubular Brass Platform
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Cart_Platform";
            platform.transform.SetParent(cartRoot.transform);
            platform.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            platform.transform.localScale = new Vector3(1.05f, 0.12f, 1.65f);
            var pfRend = platform.GetComponent<Renderer>();
            if (pfRend != null) pfRend.sharedMaterial = brassMat;

            // Overhead Brass Arch Frame
            var archTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            archTop.name = "Brass_Arch_Hanger";
            archTop.transform.SetParent(cartRoot.transform);
            archTop.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            archTop.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            archTop.transform.localScale = new Vector3(0.06f, 0.5f, 0.06f);
            var atRend = archTop.GetComponent<Renderer>();
            if (atRend != null) atRend.sharedMaterial = brassMat;

            // Stacked Luggage Trunks (3 vintage leather trunks)
            var trunk1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trunk1.name = "Steamer_Trunk_Large";
            trunk1.transform.SetParent(cartRoot.transform);
            trunk1.transform.localPosition = new Vector3(0f, 0.52f, 0.15f);
            trunk1.transform.localScale = new Vector3(0.85f, 0.45f, 0.95f);
            var t1Rend = trunk1.GetComponent<Renderer>();
            if (t1Rend != null) t1Rend.sharedMaterial = trunkMat;

            var trunk2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trunk2.name = "Steamer_Trunk_Medium";
            trunk2.transform.SetParent(cartRoot.transform);
            trunk2.transform.localPosition = new Vector3(0.05f, 0.88f, 0.12f);
            trunk2.transform.localScale = new Vector3(0.75f, 0.32f, 0.80f);
            var t2Rend = trunk2.GetComponent<Renderer>();
            if (t2Rend != null) t2Rend.sharedMaterial = trunkMat;

            var suitcase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            suitcase.name = "Leather_Valise";
            suitcase.transform.SetParent(cartRoot.transform);
            suitcase.transform.localPosition = new Vector3(-0.02f, 1.15f, 0.10f);
            suitcase.transform.localScale = new Vector3(0.55f, 0.22f, 0.50f);
            var sRend = suitcase.GetComponent<Renderer>();
            if (sRend != null) sRend.sharedMaterial = brassMat;
        }

        private static void BuildGrandfatherClock(Transform parent, Vector3 position, Material woodMat, Material brassMat, Material faceMat, string name)
        {
            var clockRoot = new GameObject(name);
            clockRoot.transform.SetParent(parent);
            clockRoot.transform.position = position;

            var col = clockRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.4f, 0f);
            col.size = new Vector3(0.85f, 2.8f, 0.65f);
            col.isTrigger = false;

            // Tall Mahogany Clock Case
            var clockCase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clockCase.name = "Clock_Case";
            clockCase.transform.SetParent(clockRoot.transform);
            clockCase.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            clockCase.transform.localScale = new Vector3(0.75f, 2.7f, 0.55f);
            var cRend = clockCase.GetComponent<Renderer>();
            if (cRend != null) cRend.sharedMaterial = woodMat;

            // Clock Face
            var face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            face.name = "Clock_Dial";
            face.transform.SetParent(clockRoot.transform);
            face.transform.localPosition = new Vector3(0f, 2.25f, 0.28f);
            face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            face.transform.localScale = new Vector3(0.40f, 0.02f, 0.40f);
            var fRend = face.GetComponent<Renderer>();
            if (fRend != null) fRend.sharedMaterial = faceMat;

            // Brass Pendulum in glass waist
            var pendulum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pendulum.name = "Clock_Pendulum";
            pendulum.transform.SetParent(clockRoot.transform);
            pendulum.transform.localPosition = new Vector3(0f, 1.15f, 0.22f);
            pendulum.transform.localScale = new Vector3(0.22f, 0.02f, 0.22f);
            var pRend = pendulum.GetComponent<Renderer>();
            if (pRend != null) pRend.sharedMaterial = brassMat;
        }

        private static void BuildVintageGramophone(Transform parent, Vector3 position, Material woodMat, Material brassMat, string name)
        {
            var gramRoot = new GameObject(name);
            gramRoot.transform.SetParent(parent);
            gramRoot.transform.position = position;

            var col = gramRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.65f, 0f);
            col.size = new Vector3(0.85f, 1.3f, 0.85f);
            col.isTrigger = false;

            // Wood Cabinet Base
            var cabinet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinet.name = "Gramophone_Cabinet";
            cabinet.transform.SetParent(gramRoot.transform);
            cabinet.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            cabinet.transform.localScale = new Vector3(0.75f, 0.70f, 0.75f);
            var cRend = cabinet.GetComponent<Renderer>();
            if (cRend != null) cRend.sharedMaterial = woodMat;

            // Vinyl Platter Turntable
            var platter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platter.name = "Vinyl_Platter";
            platter.transform.SetParent(gramRoot.transform);
            platter.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            platter.transform.localScale = new Vector3(0.48f, 0.02f, 0.48f);
            var pRend = platter.GetComponent<Renderer>();
            if (pRend != null) pRend.sharedMaterial = woodMat;

            // Brass Fluted Horn
            var horn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            horn.name = "Brass_Horn";
            horn.transform.SetParent(gramRoot.transform);
            horn.transform.localPosition = new Vector3(0.12f, 1.05f, -0.08f);
            horn.transform.localRotation = Quaternion.Euler(35f, 25f, 0f);
            horn.transform.localScale = new Vector3(0.42f, 0.35f, 0.42f);
            var hRend = horn.GetComponent<Renderer>();
            if (hRend != null) hRend.sharedMaterial = brassMat;
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
            BuildDetailedChair(officeRoot.transform, new Vector3(0.5f, 0f, 2.5f), Quaternion.Euler(0f, 180f, 0f), walnutMat, greenFeltMat, "Manager_ExecutiveChair");

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
            Material brassMat = GetMaterial(mats, "BrassGold", new Color(0.88f, 0.75f, 0.35f));
            Material whiteMat = GetMaterial(mats, "ShirtWhite", Color.white);

            // Grand Art Deco Center Table (World 0, 0, 0)
            BuildSolidTable(lobbyRoot.transform, center, new Vector3(2.2f, 0.85f, 2.2f), blackMarble, "Lobby_CenterMarbleTable");
            BuildDetailedChair(lobbyRoot.transform, center + new Vector3(0f, 0f, 1.6f), Quaternion.Euler(0f, 180f, 0f), walnutMat, velvetEmerald, "Lobby_CenterChair_N");
            BuildDetailedChair(lobbyRoot.transform, center + new Vector3(0f, 0f, -1.6f), Quaternion.Euler(0f, 0f, 0f), walnutMat, velvetEmerald, "Lobby_CenterChair_S");
            BuildDetailedChair(lobbyRoot.transform, center + new Vector3(-1.6f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), walnutMat, velvetEmerald, "Lobby_CenterChair_W");
            BuildDetailedChair(lobbyRoot.transform, center + new Vector3(1.6f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), walnutMat, velvetEmerald, "Lobby_CenterChair_E");

            // Display Console Table (World 0, 0, 3) for display anchor
            BuildSolidTable(lobbyRoot.transform, center + new Vector3(0f, 0f, 3.0f), new Vector3(2.0f, 0.85f, 0.8f), walnutMat, "Lobby_DisplayConsoleTable");

            // West Lounge: Velvet Sofas & Coffee Table at World (-4.8m, 0m, -2.5m)
            BuildSolidTable(lobbyRoot.transform, center + new Vector3(-4.8f, 0f, -2.5f),
                new Vector3(1.6f, 0.50f, 0.9f), walnutMat, "Lobby_WestLounge_CoffeeTable");
            BuildSolidStorageUnit(lobbyRoot.transform, center + new Vector3(-4.8f, 0f, -3.8f),
                new Vector3(2.4f, 0.85f, 0.85f), velvetEmerald, "Lobby_WestLounge_SofaSouth");
            BuildSolidStorageUnit(lobbyRoot.transform, center + new Vector3(-4.8f, 0f, -1.2f),
                new Vector3(2.4f, 0.85f, 0.85f), velvetEmerald, "Lobby_WestLounge_SofaNorth");

            // East Lounge: Fireplace & Wingback Armchairs at World (4.8m, 0m, -2.5m)
            BuildSolidFireplace(lobbyRoot.transform, center + new Vector3(6.5f, 0f, -2.5f), mats, "Lobby_GrandFireplace");
            BuildDetailedChair(lobbyRoot.transform, center + new Vector3(4.5f, 0f, -3.4f),
                Quaternion.Euler(0f, -55f, 0f), walnutMat, carpetBurgundy, "Lobby_WingbackChair_South");
            BuildDetailedChair(lobbyRoot.transform, center + new Vector3(4.5f, 0f, -1.6f),
                Quaternion.Euler(0f, 55f, 0f), walnutMat, carpetBurgundy, "Lobby_WingbackChair_North");

            // Grandfather Clock in Lobby Corner (Inside lobby bounds Z in [-6, 6])
            BuildGrandfatherClock(lobbyRoot.transform, center + new Vector3(-6.5f, 0f, 4.5f), walnutMat, brassMat, whiteMat, "Lobby_GrandfatherClock");

            // Bellhop Luggage Trolley Cart in Lobby Entrance (Inside lobby bounds Z in [-6, 6])
            BuildBellhopLuggageCart(lobbyRoot.transform, center + new Vector3(5.5f, 0f, 4.5f), brassMat, walnutMat, "Lobby_LuggageTrolley");
        }

        private static void BuildDiningFurnishings(Transform parent, Vector3 center, Dictionary<string, Material> mats)
        {
            Material mahoganyMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));

            // North Banquet Table (-16m, y, 3m)
            BuildSolidTable(parent, center + new Vector3(-1.0f, 0f, 3.0f), new Vector3(2.2f, 0.85f, 5.8f), mahoganyMat, "Dining_BanquetTable_North");
            BuildDetailedChair(parent, center + new Vector3(-2.4f, 0f, 3.0f), Quaternion.Euler(0f, 90f, 0f), walnutMat, mahoganyMat, "Dining_Chair_North_W");
            BuildDetailedChair(parent, center + new Vector3(0.4f, 0f, 3.0f), Quaternion.Euler(0f, -90f, 0f), walnutMat, mahoganyMat, "Dining_Chair_North_E");

            // South Banquet Table (-16m, y, -3m)
            BuildSolidTable(parent, center + new Vector3(-1.0f, 0f, -3.0f), new Vector3(2.2f, 0.85f, 5.8f), mahoganyMat, "Dining_BanquetTable_South");
            BuildDetailedChair(parent, center + new Vector3(-2.4f, 0f, -3.0f), Quaternion.Euler(0f, 90f, 0f), walnutMat, mahoganyMat, "Dining_Chair_South_W");
            BuildDetailedChair(parent, center + new Vector3(0.4f, 0f, -3.0f), Quaternion.Euler(0f, -90f, 0f), walnutMat, mahoganyMat, "Dining_Chair_South_E");

            // Sideboard Cabinet against West Wall
            BuildSolidStorageUnit(parent, center + new Vector3(-6.0f, 0f, 0f), new Vector3(1.2f, 1.6f, 3.5f), walnutMat, "Dining_SideboardCabinet");

            // Rolling Champagne / Dessert Trolley
            BuildSolidStorageUnit(parent, center + new Vector3(3.5f, 0f, 0f), new Vector3(1.2f, 0.9f, 2.0f), mahoganyMat, "Dining_DessertTrolley");
        }

        private static void BuildBarFurnishings(Transform parent, Vector3 center, Dictionary<string, Material> mats)
        {
            Material mahoganyMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));
            Material brassMat = GetMaterial(mats, "BrassGold", new Color(0.88f, 0.75f, 0.35f));

            // Main Bar Counter
            BuildSolidStorageUnit(parent, center + new Vector3(-1.5f, 0f, -3.5f), new Vector3(5.2f, 1.15f, 1.2f), mahoganyMat, "Bar_ServiceCounter");

            // Back-Bar Liquor Shelf Unit
            BuildSolidStorageUnit(parent, center + new Vector3(-1.5f, 0f, -5.5f), new Vector3(5.0f, 2.4f, 0.6f), walnutMat, "Bar_BackShelves");

            // Cocktail Tables
            BuildSolidTable(parent, center + new Vector3(-3.0f, 0f, 0f), new Vector3(1.4f, 0.75f, 1.4f), walnutMat, "Bar_LoungeTable_A");
            BuildSolidTable(parent, center + new Vector3(1.0f, 0f, 0f), new Vector3(1.4f, 0.75f, 1.4f), walnutMat, "Bar_LoungeTable_B");

            // Vintage Brass Gramophone on Credenza
            BuildVintageGramophone(parent, center + new Vector3(-5.5f, 0f, 2.0f), walnutMat, brassMat, "Bar_VintageGramophone");
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
