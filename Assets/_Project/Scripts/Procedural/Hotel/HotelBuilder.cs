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

            // Precompute bidirectional doorway connections for every room so both adjoining rooms cut out doorways cleanly
            var roomAllConnsMap = new Dictionary<string, List<DoorwayConnection>>();
            foreach (var r in rooms)
            {
                roomAllConnsMap[r.RoomId] = new List<DoorwayConnection>();
            }
            foreach (var r in rooms)
            {
                foreach (var c in r.Connections)
                {
                    if (roomAllConnsMap.TryGetValue(c.FromRoomId, out var fromList) && !fromList.Exists(x => x.ConnectionId == c.ConnectionId))
                    {
                        fromList.Add(c);
                    }
                    if (roomAllConnsMap.TryGetValue(c.ToRoomId, out var toList) && !toList.Exists(x => x.ConnectionId == c.ConnectionId))
                    {
                        toList.Add(c);
                    }
                }
            }

            // 2. Build Rooms, Floors, Ceilings, and Solid Walls with Multi-Doorway Cutouts
            foreach (var room in rooms)
            {
                BuildRoomGeometry(hotelRoot.transform, room, roomAllConnsMap[room.RoomId], mats);
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
            foreach (var err in audit.Errors) Debug.LogError($"[CollisionAudit Error] {err}");
            foreach (var warn in audit.Warnings) Debug.LogWarning($"[CollisionAudit Warning] {warn}");

            return hotelRoot;
        }

        private static void BuildFloorFoundationPlinths(Transform parent, Dictionary<string, Material> mats)
        {
            var plinthsRoot = new GameObject("Hotel_ContinuousFloorPlinths");
            plinthsRoot.transform.SetParent(parent);

            for (int f = 0; f < 4; f++)
            {
                var plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plinth.name = $"FoundationPlinth_Floor_{f}";
                plinth.transform.SetParent(plinthsRoot.transform);
                // Subterranean bedrock shields positioned strictly below ground level Y = 0 to prevent void fall while leaving all upper vertical atriums 100% open
                plinth.transform.position = new Vector3(3.0f, -0.60f - f * 0.40f, 0.0f);
                plinth.transform.localScale = new Vector3(60.0f, 0.4f, 44.0f);

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

        private static void BuildRoomGeometry(Transform parent, RoomDefinition room, List<DoorwayConnection> allRoomConnections, Dictionary<string, Material> mats)
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
                // Exact architectural floor framing leaving wide 2.0m stairwell (X: 3.6-11.2, Z: -2.65-0.95)
                // and elevator shaft (X: 12.75-15.35, Z: -1.35-1.35) 100% unobstructed

                // 1A. North walkway floor (X: -4 to 16, Z: 0.95 to 3.0)
                var floorNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorNorth.name = "Floor_Slab_NorthWalkway";
                floorNorth.transform.SetParent(roomGo.transform);
                floorNorth.transform.position = new Vector3(6.0f, yFloor - 0.10f, 1.975f);
                floorNorth.transform.localScale = new Vector3(20.0f, 0.2f, 2.05f);
                var fRendN = floorNorth.GetComponent<Renderer>();
                if (fRendN != null && floorMat != null) fRendN.sharedMaterial = floorMat;

                // 1B. West floor slab (X: -4 to 3.6, Z: -3.0 to 0.95)
                var floorWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorWest.name = "Floor_Slab_WestLanding";
                floorWest.transform.SetParent(roomGo.transform);
                floorWest.transform.position = new Vector3(-0.2f, yFloor - 0.10f, -1.025f);
                floorWest.transform.localScale = new Vector3(7.6f, 0.2f, 3.95f);
                var fRendW = floorWest.GetComponent<Renderer>();
                if (fRendW != null && floorMat != null) fRendW.sharedMaterial = floorMat;

                // 1C. East landing floor in front of elevator & connecting to Landing 1/3 (X: 11.2 to 12.75, Z: -3.0 to 0.95)
                var floorEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorEast.name = "Floor_Slab_EastLanding";
                floorEast.transform.SetParent(roomGo.transform);
                floorEast.transform.position = new Vector3(11.975f, yFloor - 0.10f, -1.025f);
                floorEast.transform.localScale = new Vector3(1.55f, 0.2f, 3.95f);
                var fRendE = floorEast.GetComponent<Renderer>();
                if (fRendE != null && floorMat != null) fRendE.sharedMaterial = floorMat;

                // 1D. North perimeter walkway alongside elevator (X: 12.75 to 16.0, Z: 1.35 to 3.0)
                var floorElevN = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorElevN.name = "Floor_Slab_NorthElevatorSide";
                floorElevN.transform.SetParent(roomGo.transform);
                floorElevN.transform.position = new Vector3(14.375f, yFloor - 0.10f, 2.175f);
                floorElevN.transform.localScale = new Vector3(3.25f, 0.2f, 1.65f);
                if (floorElevN.GetComponent<Renderer>() != null && floorMat != null) floorElevN.GetComponent<Renderer>().sharedMaterial = floorMat;

                // 1E. South perimeter walkway alongside elevator & Housekeeping entrance (X: 12.75 to 16.0, Z: -3.25 to -1.25)
                var floorElevS = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorElevS.name = "Floor_Slab_SouthElevatorSide";
                floorElevS.transform.SetParent(roomGo.transform);
                floorElevS.transform.position = new Vector3(14.375f, yFloor - 0.10f, -2.25f);
                floorElevS.transform.localScale = new Vector3(3.25f, 0.2f, 2.0f);
                if (floorElevS.GetComponent<Renderer>() != null && floorMat != null) floorElevS.GetComponent<Renderer>().sharedMaterial = floorMat;

                // 1F. East shaft perimeter border (X: 15.35 to 16.0, Z: -1.35 to 1.35)
                var floorShaftE = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorShaftE.name = "Floor_Slab_EastShaftPerimeter";
                floorShaftE.transform.SetParent(roomGo.transform);
                floorShaftE.transform.position = new Vector3(15.675f, yFloor - 0.10f, 0f);
                floorShaftE.transform.localScale = new Vector3(0.65f, 0.2f, 2.7f);
                if (floorShaftE.GetComponent<Renderer>() != null && floorMat != null) floorShaftE.GetComponent<Renderer>().sharedMaterial = floorMat;

                // 1G. South stair margin border under south wall (X: 3.6 to 11.2, Z: -3.0 to -2.65)
                var floorStairMargin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorStairMargin.name = "Floor_Slab_SouthStairMargin";
                floorStairMargin.transform.SetParent(roomGo.transform);
                floorStairMargin.transform.position = new Vector3(7.4f, yFloor - 0.10f, -2.825f);
                floorStairMargin.transform.localScale = new Vector3(7.6f, 0.2f, 0.35f);
                if (floorStairMargin.GetComponent<Renderer>() != null && floorMat != null) floorStairMargin.GetComponent<Renderer>().sharedMaterial = floorMat;

                // 1H. Protective Art Deco Brass Balustrades along upper corridor voids (Never blocks stair landing entry or intersects flights!)
                BuildMezzanineBalustrade(roomGo.transform, new Vector3(3.6f, yFloor, 0.95f), new Vector3(11.2f, yFloor, 0.95f), wallLowerMat, brassMat, "StairAtrium_GuardRailing");
                if (room.FloorIndex == 1 || room.FloorIndex == 3)
                {
                    BuildMezzanineBalustrade(roomGo.transform, new Vector3(3.6f, yFloor, -2.65f), new Vector3(3.6f, yFloor, 0.95f), wallLowerMat, brassMat, "StairAtrium_WestRailing");
                }
                if (room.FloorIndex == 2)
                {
                    BuildMezzanineBalustrade(roomGo.transform, new Vector3(11.2f, yFloor, -2.65f), new Vector3(11.2f, yFloor, 0.95f), wallLowerMat, brassMat, "StairAtrium_EastRailing");
                }
                BuildMezzanineBalustrade(roomGo.transform, new Vector3(12.75f, yFloor, 1.35f), new Vector3(15.35f, yFloor, 1.35f), wallLowerMat, brassMat, "ElevatorShaft_NorthRailing");
                BuildMezzanineBalustrade(roomGo.transform, new Vector3(12.75f, yFloor, -1.35f), new Vector3(15.35f, yFloor, -1.35f), wallLowerMat, brassMat, "ElevatorShaft_SouthRailing");
                BuildMezzanineBalustrade(roomGo.transform, new Vector3(15.35f, yFloor, -1.35f), new Vector3(15.35f, yFloor, 1.35f), wallLowerMat, brassMat, "ElevatorShaft_EastRailing");
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
                ceilWalkway.transform.position = new Vector3(6.0f, yFloor + height + 0.25f, 1.975f);
                ceilWalkway.transform.localScale = new Vector3(20.0f, 0.5f, 2.05f);
                if (ceilWalkway.GetComponent<Renderer>() != null && ceilingMat != null) ceilWalkway.GetComponent<Renderer>().sharedMaterial = ceilingMat;

                var ceilWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilWest.name = "Ceiling_Slab_WestLanding";
                ceilWest.transform.SetParent(roomGo.transform);
                ceilWest.transform.position = new Vector3(-0.2f, yFloor + height + 0.25f, -1.025f);
                ceilWest.transform.localScale = new Vector3(7.6f, 0.5f, 3.95f);
                if (ceilWest.GetComponent<Renderer>() != null && ceilingMat != null) ceilWest.GetComponent<Renderer>().sharedMaterial = ceilingMat;

                var ceilEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilEast.name = "Ceiling_Slab_EastVestibule";
                ceilEast.transform.SetParent(roomGo.transform);
                ceilEast.transform.position = new Vector3(11.975f, yFloor + height + 0.25f, -1.025f);
                ceilEast.transform.localScale = new Vector3(1.55f, 0.5f, 3.95f);
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
                ceilElevS.transform.position = new Vector3(14.375f, yFloor + height + 0.25f, -2.25f);
                ceilElevS.transform.localScale = new Vector3(3.25f, 0.5f, 2.0f);
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
                    skylight.transform.position = new Vector3(7.4f, yFloor + height + 0.45f, -1.025f);
                    skylight.transform.localScale = new Vector3(7.6f, 0.15f, 3.95f);
                    if (skylight.GetComponent<Renderer>() != null && brassMat != null) skylight.GetComponent<Renderer>().sharedMaterial = brassMat;

                    // Solid Roof Cap sealing the elevator shaft top (X: 12.75 to 15.35, Z: -1.35 to 1.35)
                    var ceilElevRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    ceilElevRoof.name = "Ceiling_Slab_ElevatorRoofCap";
                    ceilElevRoof.transform.SetParent(roomGo.transform);
                    ceilElevRoof.transform.position = new Vector3(14.05f, yFloor + height + 0.25f, 0f);
                    ceilElevRoof.transform.localScale = new Vector3(2.6f, 0.5f, 2.7f);
                    if (ceilElevRoof.GetComponent<Renderer>() != null && ceilingMat != null) ceilElevRoof.GetComponent<Renderer>().sharedMaterial = ceilingMat;

                    // Solid Roof Cap sealing the central stairwell atrium
                    var ceilStairRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    ceilStairRoof.name = "Ceiling_Slab_StaircaseRoofCap";
                    ceilStairRoof.transform.SetParent(roomGo.transform);
                    ceilStairRoof.transform.position = new Vector3(7.4f, yFloor + height + 0.25f, -1.025f);
                    ceilStairRoof.transform.localScale = new Vector3(7.6f, 0.5f, 4.3f);
                    if (ceilStairRoof.GetComponent<Renderer>() != null && ceilingMat != null) ceilStairRoof.GetComponent<Renderer>().sharedMaterial = ceilingMat;
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
            var conns = allRoomConnections ?? room.Connections;
            BuildWallWithDoorwayCutouts(roomGo.transform, room, conns, WallSide.North, yFloor, height, wallLowerMat, wallUpperMat, brassMat);
            BuildWallWithDoorwayCutouts(roomGo.transform, room, conns, WallSide.South, yFloor, height, wallLowerMat, wallUpperMat, brassMat);
            BuildWallWithDoorwayCutouts(roomGo.transform, room, conns, WallSide.West, yFloor, height, wallLowerMat, wallUpperMat, brassMat);
            BuildWallWithDoorwayCutouts(roomGo.transform, room, conns, WallSide.East, yFloor, height, wallLowerMat, wallUpperMat, brassMat);
        }

        private static void BuildWallWithDoorwayCutouts(Transform parent, RoomDefinition room, List<DoorwayConnection> allRoomConnections, WallSide side,
            float yFloor, float height, Material lowerMat, Material upperMat, Material trimMat)
        {
            Vector3 center = room.Bounds.center;
            Vector3 size = room.Bounds.size;
            float halfX = size.x * 0.5f;
            float halfZ = size.z * 0.5f;
            float wallThickness = 0.20f;
            float halfThick = wallThickness * 0.5f;

            // Gather ALL doorway connections on this wall (bidirectional across adjoining rooms)
            var wallConns = new List<DoorwayConnection>();
            var sourceConns = allRoomConnections ?? room.Connections;
            foreach (var conn in sourceConns)
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
                    float doorWidth = (conn.DoorDisplayName.Contains("Archway") || conn.DoorDisplayName.Contains("Double") || conn.DoorDisplayName.Contains("French")) ? 3.4f : 2.2f;
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
                    float doorWidth = (conn.DoorDisplayName.Contains("Archway") || conn.DoorDisplayName.Contains("Double") || conn.DoorDisplayName.Contains("French")) ? 3.4f : 2.2f;
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
            float lowerHeight = 1.56f;
            float trimHeight = 0.08f;
            float upperHeight = Mathf.Max(0.1f, size.y - lowerHeight - trimHeight);
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

            // Brass Trim (Chair Rail) - exactly fits between lower and upper with zero vertical overlap and flush partition thickness
            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = $"{name}_ChairRail";
            trim.transform.SetParent(wallRoot.transform);
            trim.transform.localPosition = new Vector3(0f, -size.y * 0.5f + lowerHeight + trimHeight * 0.5f, 0f);
            trim.transform.localScale = isXLong
                ? new Vector3(length, trimHeight, thickness)
                : new Vector3(thickness, trimHeight, length);
            var trimRend = trim.GetComponent<Renderer>();
            if (trimRend != null && trimMat != null) trimRend.sharedMaterial = trimMat;

            // Upper Wallpaper (Solid Box Collider)
            var upper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            upper.name = $"{name}_Wallpaper";
            upper.transform.SetParent(wallRoot.transform);
            upper.transform.localPosition = new Vector3(0f, -size.y * 0.5f + lowerHeight + trimHeight + upperHeight * 0.5f, 0f);
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

            Material woodMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));
            Material brassMat = GetMaterial(mats, "BrassGold", new Color(0.88f, 0.75f, 0.35f));
            Material carpetMat = GetMaterial(mats, "VelvetGold", new Color(0.65f, 0.12f, 0.15f)); // Rich ruby crimson runner
            Material marbleMat = GetMaterial(mats, "FloorCheckerMarble", new Color(0.92f, 0.90f, 0.85f));

            // Grand 2.0m-Wide Switchback Staircase connecting all 4 floors:
            // Track A at Z = -2.10m (width 2.0m, Z in [-3.10, -1.10])
            // Track B at Z = -0.10m (width 2.0m, Z in [-1.10, +0.90])
            // Walkway at Z in [0.95m, 3.0m] is clear and unobstructed on all 4 floors!

            // Flight 0: Floor 0 -> Floor 1 (Track A: X = 4.8 -> 11.2m, Y = 0.0 -> 5.0m, Z = -2.10m, width = 2.0m)
            BuildStairFlightX(stairsRoot.transform, 4.8f, 0.0f, -2.10f, 5.0f, 6.4f, 20, 2.0f, woodMat, walnutMat, brassMat, carpetMat, "Flight_Floor0_to_1");

            // Landing 1 at Floor 1 connecting Track A and Track B (X in [11.2, 13.6], Z in [-3.10, 0.90])
            BuildStairLanding(stairsRoot.transform, new Vector3(12.4f, 4.85f, -1.10f), new Vector3(2.4f, 0.3f, 4.0f), woodMat, walnutMat, brassMat, marbleMat, "Landing_Floor1");
            BuildLandingLight(stairsRoot.transform, new Vector3(12.4f, 8.5f, -1.10f), "StairLight_Floor1");

            // Flight 1: Floor 1 -> Floor 2 (Track B: X = 11.2 -> 4.8m, Y = 5.0 -> 10.0m, Z = -0.10f, width = 2.0m)
            BuildStairFlightX(stairsRoot.transform, 11.2f, 5.0f, -0.10f, 5.0f, -6.4f, 20, 2.0f, woodMat, walnutMat, brassMat, carpetMat, "Flight_Floor1_to_2");

            // Landing 2 at Floor 2 connecting Track B and Track A (X in [2.4, 4.8], Z in [-3.10, 0.90])
            BuildStairLanding(stairsRoot.transform, new Vector3(3.6f, 9.85f, -1.10f), new Vector3(2.4f, 0.3f, 4.0f), woodMat, walnutMat, brassMat, marbleMat, "Landing_Floor2");
            BuildLandingLight(stairsRoot.transform, new Vector3(3.6f, 13.5f, -1.10f), "StairLight_Floor2");

            // Flight 2: Floor 2 -> Floor 3 (Track A: X = 4.8 -> 11.2m, Y = 10.0 -> 15.0m, Z = -2.10f, width = 2.0m)
            BuildStairFlightX(stairsRoot.transform, 4.8f, 10.0f, -2.10f, 5.0f, 6.4f, 20, 2.0f, woodMat, walnutMat, brassMat, carpetMat, "Flight_Floor2_to_3");

            // Landing 3 at Floor 3 (X in [11.2, 13.6], Z in [-3.10, 0.90])
            BuildStairLanding(stairsRoot.transform, new Vector3(12.4f, 14.85f, -1.10f), new Vector3(2.4f, 0.3f, 4.0f), woodMat, walnutMat, brassMat, marbleMat, "Landing_Floor3");
            BuildLandingLight(stairsRoot.transform, new Vector3(12.4f, 18.5f, -1.10f), "StairLight_Floor3");
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
            float rise, float run, int steps, float width, Material woodMat, Material walnutMat, Material brassMat, Material carpetMat, string name)
        {
            var flightRoot = new GameObject(name);
            flightRoot.transform.SetParent(parent);

            float stepRise = rise / steps;
            float stepRun = run / steps;
            float absRun = Mathf.Abs(stepRun);
            float angle = Mathf.Atan2(rise, Mathf.Abs(run)) * Mathf.Rad2Deg;
            float rotZ = run > 0f ? angle : -angle;
            float midX = startX + run * 0.5f;
            float midY = startY + rise * 0.5f;
            float totalLength = Mathf.Sqrt(rise * rise + run * run);

            // 1. Individual Architectural Steps with Treads, Brass Nosing, and Velvet Carpet Runner
            for (int i = 0; i < steps; i++)
            {
                float x = startX + (i + 0.5f) * stepRun;
                float y = startY + (i + 0.5f) * stepRise;

                // Base Step Block
                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"{name}_Step_{i:D2}";
                step.transform.SetParent(flightRoot.transform);
                step.transform.position = new Vector3(x, y, posZ);
                step.transform.localScale = new Vector3(absRun, stepRise, width);
                var rend = step.GetComponent<Renderer>();
                if (rend != null && woodMat != null) rend.sharedMaterial = woodMat;

                // Walnut Step Tread Nosing Lip (slight overhang)
                var tread = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tread.name = $"{name}_Tread_{i:D2}";
                tread.transform.SetParent(step.transform);
                tread.transform.localPosition = new Vector3(0f, 0.5f - 0.015f / stepRise, 0f);
                tread.transform.localScale = new Vector3(1.10f, 0.035f / stepRise, 1.02f);
                var tRend = tread.GetComponent<Renderer>();
                if (tRend != null && walnutMat != null) tRend.sharedMaterial = walnutMat;
                var tCol = tread.GetComponent<Collider>();
                if (tCol != null) Object.DestroyImmediate(tCol);

                // Polished Brass Front Edge Nosing Strip
                var nosing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                nosing.name = $"{name}_BrassNosing_{i:D2}";
                nosing.transform.SetParent(step.transform);
                float nosingX = (run > 0f ? 0.5f : -0.5f);
                nosing.transform.localPosition = new Vector3(nosingX, 0.5f - 0.02f / stepRise, 0f);
                nosing.transform.localScale = new Vector3(0.12f, 0.045f / stepRise, 1.02f);
                var nRend = nosing.GetComponent<Renderer>();
                if (nRend != null && brassMat != null) nRend.sharedMaterial = brassMat;
                var nCol = nosing.GetComponent<Collider>();
                if (nCol != null) Object.DestroyImmediate(nCol);

                // Center Plush Carpet Runner
                var runner = GameObject.CreatePrimitive(PrimitiveType.Cube);
                runner.name = $"{name}_Runner_{i:D2}";
                runner.transform.SetParent(step.transform);
                runner.transform.localPosition = new Vector3(0f, 0.5f + 0.008f / stepRise, 0f);
                runner.transform.localScale = new Vector3(1.08f, 0.02f / stepRise, 0.65f);
                var runRend = runner.GetComponent<Renderer>();
                if (runRend != null && carpetMat != null) runRend.sharedMaterial = carpetMat;
                var runCol = runner.GetComponent<Collider>();
                if (runCol != null) Object.DestroyImmediate(runCol);
            }

            // 2. Solid Hardwood Side Stringers (encloses the stair flight)
            var strLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strLeft.name = $"{name}_Stringer_Left";
            strLeft.transform.SetParent(flightRoot.transform);
            strLeft.transform.position = new Vector3(midX, midY - 0.10f, posZ - width * 0.5f - 0.04f);
            strLeft.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
            strLeft.transform.localScale = new Vector3(totalLength + 0.25f, 0.35f, 0.08f);
            var slRend = strLeft.GetComponent<Renderer>();
            if (slRend != null && walnutMat != null) slRend.sharedMaterial = walnutMat;
            var slCol = strLeft.GetComponent<Collider>();
            if (slCol != null) Object.DestroyImmediate(slCol);

            var strRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strRight.name = $"{name}_Stringer_Right";
            strRight.transform.SetParent(flightRoot.transform);
            strRight.transform.position = new Vector3(midX, midY - 0.10f, posZ + width * 0.5f + 0.04f);
            strRight.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
            strRight.transform.localScale = new Vector3(totalLength + 0.25f, 0.35f, 0.08f);
            var srRend = strRight.GetComponent<Renderer>();
            if (srRend != null && walnutMat != null) srRend.sharedMaterial = walnutMat;
            var srCol = strRight.GetComponent<Collider>();
            if (srCol != null) Object.DestroyImmediate(srCol);

            // 3. Finished Architectural Underside Soffit Ceiling
            var soffit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            soffit.name = $"{name}_Underside_Soffit";
            soffit.transform.SetParent(flightRoot.transform);
            soffit.transform.position = new Vector3(midX, midY - stepRise * 0.5f - 0.05f, posZ);
            soffit.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
            soffit.transform.localScale = new Vector3(totalLength, 0.06f, width + 0.16f);
            var sofRend = soffit.GetComponent<Renderer>();
            if (sofRend != null && woodMat != null) sofRend.sharedMaterial = woodMat;
            var sofCol = soffit.GetComponent<Collider>();
            if (sofCol != null) Object.DestroyImmediate(sofCol);

            // 4. Art Deco Balustrade & Polished Handrail along the inner open side (facing atrium)
            float railZ = name.Contains("Floor1_to_2") ? (posZ - width * 0.5f - 0.04f) : (posZ + width * 0.5f + 0.04f);

            var handrail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handrail.name = $"{name}_Handrail";
            handrail.transform.SetParent(flightRoot.transform);
            handrail.transform.position = new Vector3(midX, midY + 1.05f, railZ);
            handrail.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
            handrail.transform.localScale = new Vector3(totalLength + 0.1f, 0.09f, 0.10f);
            var hrRend = handrail.GetComponent<Renderer>();
            if (hrRend != null && walnutMat != null) hrRend.sharedMaterial = walnutMat;
            var hrCol = handrail.GetComponent<Collider>();
            if (hrCol != null) Object.DestroyImmediate(hrCol);

            // Decorative Brass Baluster Spindles along the flight
            int spindleCount = 8;
            for (int s = 0; s <= spindleCount; s++)
            {
                float t = (float)s / spindleCount;
                float sx = startX + t * run;
                float sy = startY + t * rise + 0.52f;
                var spindle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spindle.name = $"{name}_Baluster_{s:D2}";
                spindle.transform.SetParent(flightRoot.transform);
                spindle.transform.position = new Vector3(sx, sy, railZ);
                spindle.transform.localScale = new Vector3(0.04f, 0.96f, 0.04f);
                var spRend = spindle.GetComponent<Renderer>();
                if (spRend != null && brassMat != null) spRend.sharedMaterial = brassMat;
                var spCol = spindle.GetComponent<Collider>();
                if (spCol != null) Object.DestroyImmediate(spCol);
            }

            // Bottom Newel Post
            var newelBottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newelBottom.name = $"{name}_Newel_Bottom";
            newelBottom.transform.SetParent(flightRoot.transform);
            newelBottom.transform.position = new Vector3(startX, startY + 0.60f, railZ);
            newelBottom.transform.localScale = new Vector3(0.18f, 1.20f, 0.18f);
            var nbRend = newelBottom.GetComponent<Renderer>();
            if (nbRend != null && brassMat != null) nbRend.sharedMaterial = brassMat;
            var nbCol = newelBottom.GetComponent<Collider>();
            if (nbCol != null) Object.DestroyImmediate(nbCol);

            // Top Newel Post
            var newelTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newelTop.name = $"{name}_Newel_Top";
            newelTop.transform.SetParent(flightRoot.transform);
            newelTop.transform.position = new Vector3(startX + run, startY + rise + 0.60f, railZ);
            newelTop.transform.localScale = new Vector3(0.18f, 1.20f, 0.18f);
            var ntRend = newelTop.GetComponent<Renderer>();
            if (ntRend != null && brassMat != null) ntRend.sharedMaterial = brassMat;
            var ntCol = newelTop.GetComponent<Collider>();
            if (ntCol != null) Object.DestroyImmediate(ntCol);

            // 5. Silky Smooth Traversable Ramp Collider for CharacterController (slopes up exactly parallel to the stairs)
            var rampGo = new GameObject($"{name}_WalkRamp_Collider");
            rampGo.transform.SetParent(flightRoot.transform);
            rampGo.transform.position = new Vector3(midX, midY + 0.03f, posZ);
            rampGo.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);

            var rampCol = rampGo.AddComponent<BoxCollider>();
            rampCol.size = new Vector3(totalLength + 0.2f, 0.12f, width);
            rampCol.isTrigger = false;

            var r = rampGo.GetComponent<Renderer>();
            if (r != null) Object.DestroyImmediate(r);
        }

        private static void BuildStairLanding(Transform parent, Vector3 center, Vector3 size, Material woodMat, Material walnutMat, Material brassMat, Material marbleMat, string name)
        {
            var landing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            landing.name = name;
            landing.transform.SetParent(parent);
            landing.transform.position = center;
            landing.transform.localScale = size;
            var rend = landing.GetComponent<Renderer>();
            if (rend != null && marbleMat != null) rend.sharedMaterial = marbleMat;

            // Polished Brass Perimeter Border Trim
            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = $"{name}_Brass_Trim";
            trim.transform.SetParent(landing.transform);
            trim.transform.localPosition = new Vector3(0f, 0.5f + 0.01f / size.y, 0f);
            trim.transform.localScale = new Vector3(1.02f, 0.02f / size.y, 1.02f);
            var trRend = trim.GetComponent<Renderer>();
            if (trRend != null && brassMat != null) trRend.sharedMaterial = brassMat;
            var trCol = trim.GetComponent<Collider>();
            if (trCol != null) Object.DestroyImmediate(trCol);

            // Structural Support Column beneath the landing
            var column = GameObject.CreatePrimitive(PrimitiveType.Cube);
            column.name = $"{name}_Support_Column";
            column.transform.SetParent(landing.transform);
            column.transform.localPosition = new Vector3(0f, -0.5f - 1.25f / size.y, 0f);
            column.transform.localScale = new Vector3(0.40f / size.x, 2.50f / size.y, 0.40f / size.z);
            var colRend = column.GetComponent<Renderer>();
            if (colRend != null && walnutMat != null) colRend.sharedMaterial = walnutMat;
            var colCol = column.GetComponent<Collider>();
            if (colCol != null) Object.DestroyImmediate(colCol);
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
            cabinTrigger.center = new Vector3(0f, 1.4f, 0f);
            cabinTrigger.size = new Vector3(2.4f, 2.8f, 2.5f);

            var elevatorComp = cabinRoot.AddComponent<HotelElevator>();

            // Solid Floor Slab with BoxCollider (Floor surface at y = 0.0m)
            var cabinFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinFloor.name = "Cabin_Floor_Slab";
            cabinFloor.transform.SetParent(cabinRoot.transform);
            cabinFloor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            cabinFloor.transform.localScale = new Vector3(2.4f, 0.10f, 2.5f);
            var fRend = cabinFloor.GetComponent<Renderer>();
            if (fRend != null && marbleMat != null) fRend.sharedMaterial = marbleMat;

            // Solid Ceiling Slab (Bottom at y = 2.75m, fully connected to walls and corner posts)
            var cabinCeil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinCeil.name = "Cabin_Ceiling_Slab";
            cabinCeil.transform.SetParent(cabinRoot.transform);
            cabinCeil.transform.localPosition = new Vector3(0f, 2.85f, 0f);
            cabinCeil.transform.localScale = new Vector3(2.4f, 0.20f, 2.5f);
            var cRend = cabinCeil.GetComponent<Renderer>();
            if (cRend != null && woodMat != null) cRend.sharedMaterial = woodMat;

            // Outer Decorative Brass Roof Cornice & Crest
            var roofCornice = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofCornice.name = "Cabin_Roof_Cornice";
            roofCornice.transform.SetParent(cabinRoot.transform);
            roofCornice.transform.localPosition = new Vector3(0f, 2.95f, 0f);
            roofCornice.transform.localScale = new Vector3(2.55f, 0.12f, 2.65f);
            if (roofCornice.GetComponent<Renderer>() != null && brassMat != null) roofCornice.GetComponent<Renderer>().sharedMaterial = brassMat;
            var rcCol = roofCornice.GetComponent<Collider>();
            if (rcCol != null) Object.DestroyImmediate(rcCol);

            // Interior Crystal Chandelier Light
            var cabinLightGo = new GameObject("Cabin_CrystalLight");
            cabinLightGo.transform.SetParent(cabinRoot.transform);
            cabinLightGo.transform.localPosition = new Vector3(0f, 2.60f, 0f);
            var cLight = cabinLightGo.AddComponent<Light>();
            cLight.type = LightType.Point;
            cLight.color = new Color(1.0f, 0.90f, 0.70f);
            cLight.intensity = 2.4f;
            cLight.range = 8.0f;

            // 4 Full-Height Corner Columns connecting floor directly to ceiling
            float cabinH = 2.75f;
            float cabinHalfH = cabinH * 0.5f;
            Vector3[] cabinCorners = {
                new Vector3(-1.14f, cabinHalfH, -1.18f),
                new Vector3(1.14f, cabinHalfH, -1.18f),
                new Vector3(-1.14f, cabinHalfH, 1.18f),
                new Vector3(1.14f, cabinHalfH, 1.18f)
            };
            for (int i = 0; i < 4; i++)
            {
                var cornerCol = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cornerCol.name = $"Cabin_CornerPost_{i}";
                cornerCol.transform.SetParent(cabinRoot.transform);
                cornerCol.transform.localPosition = cabinCorners[i];
                cornerCol.transform.localScale = new Vector3(0.14f, cabinH, 0.14f);
                var r = cornerCol.GetComponent<Renderer>();
                if (r != null && brassMat != null) r.sharedMaterial = brassMat;
                var c = cornerCol.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }

            // Solid Full-Height Enclosed Walls (North, South, East) connecting floor seamlessly to ceiling
            BuildCabinSolidWall(cabinRoot.transform, new Vector3(0f, cabinHalfH, 1.20f), new Vector3(2.4f, cabinH, 0.08f), woodMat, brassMat, "Cabin_Wall_North");
            BuildCabinSolidWall(cabinRoot.transform, new Vector3(0f, cabinHalfH, -1.20f), new Vector3(2.4f, cabinH, 0.08f), woodMat, brassMat, "Cabin_Wall_South");
            BuildCabinSolidWall(cabinRoot.transform, new Vector3(1.15f, cabinHalfH, 0f), new Vector3(0.08f, cabinH, 2.4f), woodMat, brassMat, "Cabin_Wall_East");

            // Front West Entrance Portal Frame (Facing corridor with wide, framed opening)
            var frontJambL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontJambL.name = "Cabin_Portal_Jamb_L";
            frontJambL.transform.SetParent(cabinRoot.transform);
            frontJambL.transform.localPosition = new Vector3(-1.15f, cabinHalfH, -0.92f);
            frontJambL.transform.localScale = new Vector3(0.08f, cabinH, 0.52f);
            if (frontJambL.GetComponent<Renderer>() != null && woodMat != null) frontJambL.GetComponent<Renderer>().sharedMaterial = woodMat;

            var frontJambR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontJambR.name = "Cabin_Portal_Jamb_R";
            frontJambR.transform.SetParent(cabinRoot.transform);
            frontJambR.transform.localPosition = new Vector3(-1.15f, cabinHalfH, 0.92f);
            frontJambR.transform.localScale = new Vector3(0.08f, cabinH, 0.52f);
            if (frontJambR.GetComponent<Renderer>() != null && woodMat != null) frontJambR.GetComponent<Renderer>().sharedMaterial = woodMat;

            var frontLintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontLintel.name = "Cabin_Portal_Lintel";
            frontLintel.transform.SetParent(cabinRoot.transform);
            frontLintel.transform.localPosition = new Vector3(-1.15f, 2.55f, 0f);
            frontLintel.transform.localScale = new Vector3(0.08f, 0.40f, 1.40f);
            if (frontLintel.GetComponent<Renderer>() != null && woodMat != null) frontLintel.GetComponent<Renderer>().sharedMaterial = woodMat;

            // Interior Brass Handrails at waist height
            BuildCabinRailing(cabinRoot.transform, new Vector3(0f, 1.0f, 1.13f), new Vector3(2.2f, 0.05f, 0.05f), brassMat, "Cabin_Handrail_N");
            BuildCabinRailing(cabinRoot.transform, new Vector3(0f, 1.0f, -1.13f), new Vector3(2.2f, 0.05f, 0.05f), brassMat, "Cabin_Handrail_S");
            BuildCabinRailing(cabinRoot.transform, new Vector3(1.08f, 1.0f, 0f), new Vector3(0.05f, 0.05f, 2.2f), brassMat, "Cabin_Handrail_E");

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

                // 5. Outside Art Deco Elevator Landing Portal & Safety Gates (Floors 0, 1, 2, 3)
                var portalRoot = new GameObject($"Elevator_LandingPortal_Floor_{f}");
                portalRoot.transform.SetParent(elevatorRoot.transform);
                portalRoot.transform.position = new Vector3(12.75f, y, 0f);

                // North Jamb
                var pJambN = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pJambN.name = "Portal_Jamb_North";
                pJambN.transform.SetParent(portalRoot.transform);
                pJambN.transform.localPosition = new Vector3(0f, 1.4f, 1.10f);
                pJambN.transform.localScale = new Vector3(0.14f, 2.8f, 0.45f);
                if (pJambN.GetComponent<Renderer>() != null && brassMat != null) pJambN.GetComponent<Renderer>().sharedMaterial = brassMat;

                // South Jamb
                var pJambS = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pJambS.name = "Portal_Jamb_South";
                pJambS.transform.SetParent(portalRoot.transform);
                pJambS.transform.localPosition = new Vector3(0f, 1.4f, -1.10f);
                pJambS.transform.localScale = new Vector3(0.14f, 2.8f, 0.45f);
                if (pJambS.GetComponent<Renderer>() != null && brassMat != null) pJambS.GetComponent<Renderer>().sharedMaterial = brassMat;

                // Overhead Lintel
                var pLintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pLintel.name = "Portal_Lintel";
                pLintel.transform.SetParent(portalRoot.transform);
                pLintel.transform.localPosition = new Vector3(0f, 2.65f, 0f);
                pLintel.transform.localScale = new Vector3(0.14f, 0.35f, 2.65f);
                if (pLintel.GetComponent<Renderer>() != null && brassMat != null) pLintel.GetComponent<Renderer>().sharedMaterial = brassMat;

                // Floor Indicator Header Plaque
                var pPlaque = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pPlaque.name = "Portal_FloorPlaque";
                pPlaque.transform.SetParent(portalRoot.transform);
                pPlaque.transform.localPosition = new Vector3(-0.06f, 2.92f, 0f);
                pPlaque.transform.localScale = new Vector3(0.06f, 0.22f, 0.85f);
                if (pPlaque.GetComponent<Renderer>() != null && brassMat != null) pPlaque.GetComponent<Renderer>().sharedMaterial = brassMat;

                // Shaft Safety Gate Barrier (Active when elevator is on another floor to prevent falling into shaft!)
                var gateBarrier = new GameObject($"Elevator_ShaftGateBarrier_Floor_{f}");
                gateBarrier.transform.SetParent(portalRoot.transform);
                gateBarrier.transform.localPosition = new Vector3(0f, 1.25f, 0f);

                var gateCol = gateBarrier.AddComponent<BoxCollider>();
                gateCol.center = Vector3.zero;
                gateCol.size = new Vector3(0.15f, 2.5f, 1.75f);
                gateCol.isTrigger = (f == 0);

                // Left Gate Leaf (Ornate Art Deco Brass Safety Balustrade / Scissor Gate)
                var gateL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gateL.name = "Gate_Leaf_North";
                gateL.transform.SetParent(gateBarrier.transform);
                gateL.transform.localPosition = new Vector3(0f, 0f, f == 0 ? 1.05f : 0.44f);
                gateL.transform.localScale = new Vector3(0.06f, 2.45f, 0.88f);
                if (gateL.GetComponent<Renderer>() != null && brassMat != null) gateL.GetComponent<Renderer>().sharedMaterial = brassMat;
                var glCol = gateL.GetComponent<Collider>();
                if (glCol != null) Object.DestroyImmediate(glCol);

                // Ornate horizontal safety rail trim on North gate
                var railTopL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                railTopL.name = "Gate_Rail_Top";
                railTopL.transform.SetParent(gateL.transform);
                railTopL.transform.localPosition = new Vector3(-0.55f, 0.45f, 0f);
                railTopL.transform.localScale = new Vector3(0.6f, 0.04f, 1.02f);
                if (railTopL.GetComponent<Renderer>() != null && brassMat != null) railTopL.GetComponent<Renderer>().sharedMaterial = brassMat;
                var rtlCol = railTopL.GetComponent<Collider>();
                if (rtlCol != null) Object.DestroyImmediate(rtlCol);

                var railMidL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                railMidL.name = "Gate_Rail_Mid";
                railMidL.transform.SetParent(gateL.transform);
                railMidL.transform.localPosition = new Vector3(-0.55f, 0.0f, 0f);
                railMidL.transform.localScale = new Vector3(0.6f, 0.04f, 1.02f);
                if (railMidL.GetComponent<Renderer>() != null && brassMat != null) railMidL.GetComponent<Renderer>().sharedMaterial = brassMat;
                var rmlCol = railMidL.GetComponent<Collider>();
                if (rmlCol != null) Object.DestroyImmediate(rmlCol);

                // Right Gate Leaf (Ornate Art Deco Brass Safety Balustrade / Scissor Gate)
                var gateR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gateR.name = "Gate_Leaf_South";
                gateR.transform.SetParent(gateBarrier.transform);
                gateR.transform.localPosition = new Vector3(0f, 0f, f == 0 ? -1.05f : -0.44f);
                gateR.transform.localScale = new Vector3(0.06f, 2.45f, 0.88f);
                if (gateR.GetComponent<Renderer>() != null && brassMat != null) gateR.GetComponent<Renderer>().sharedMaterial = brassMat;
                var grCol = gateR.GetComponent<Collider>();
                if (grCol != null) Object.DestroyImmediate(grCol);

                // Ornate horizontal safety rail trim on South gate
                var railTopR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                railTopR.name = "Gate_Rail_Top";
                railTopR.transform.SetParent(gateR.transform);
                railTopR.transform.localPosition = new Vector3(-0.55f, 0.45f, 0f);
                railTopR.transform.localScale = new Vector3(0.6f, 0.04f, 1.02f);
                if (railTopR.GetComponent<Renderer>() != null && brassMat != null) railTopR.GetComponent<Renderer>().sharedMaterial = brassMat;
                var rtrCol = railTopR.GetComponent<Collider>();
                if (rtrCol != null) Object.DestroyImmediate(rtrCol);

                var railMidR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                railMidR.name = "Gate_Rail_Mid";
                railMidR.transform.SetParent(gateR.transform);
                railMidR.transform.localPosition = new Vector3(-0.55f, 0.0f, 0f);
                railMidR.transform.localScale = new Vector3(0.6f, 0.04f, 1.02f);
                if (railMidR.GetComponent<Renderer>() != null && brassMat != null) railMidR.GetComponent<Renderer>().sharedMaterial = brassMat;
                var rmrCol = railMidR.GetComponent<Collider>();
                if (rmrCol != null) Object.DestroyImmediate(rmrCol);

                // Register with elevator controller (Floor 0 starts open; Floors 1, 2, 3 start closed)
                elevatorComp.RegisterLandingGate(f, gateBarrier);
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

        private static void BuildCabinSolidWall(Transform parent, Vector3 localPos, Vector3 size, Material wallMat, Material trimMat, string name)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.localPosition = localPos;
            wall.transform.localScale = size;

            var r = wall.GetComponent<Renderer>();
            if (r != null && wallMat != null) r.sharedMaterial = wallMat;

            var col = wall.GetComponent<BoxCollider>();
            if (col == null) col = wall.AddComponent<BoxCollider>();
            col.isTrigger = false;

            // Inward brass accent banding detail
            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = $"{name}_Trim";
            trim.transform.SetParent(wall.transform);
            trim.transform.localPosition = Vector3.zero;
            trim.transform.localScale = new Vector3(1.02f, 0.04f, 1.05f);
            var trRend = trim.GetComponent<Renderer>();
            if (trRend != null && trimMat != null) trRend.sharedMaterial = trimMat;
            var trCol = trim.GetComponent<Collider>();
            if (trCol != null) Object.DestroyImmediate(trCol);
        }

        private static void BuildMezzanineBalustrade(Transform parent, Vector3 start, Vector3 end,
            Material woodMat, Material brassMat, string name)
        {
            var balustradeRoot = new GameObject(name);
            balustradeRoot.transform.SetParent(parent);

            Vector3 delta = end - start;
            float length = delta.magnitude;
            if (length < 0.2f) return;

            Vector3 dir = delta.normalized;
            Vector3 mid = (start + end) * 0.5f;
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            // Top Molded Hardwood Handrail (Height = 1.05m)
            var topRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topRail.name = "Balustrade_TopRail";
            topRail.transform.SetParent(balustradeRoot.transform);
            topRail.transform.position = new Vector3(mid.x, mid.y + 1.05f, mid.z);
            topRail.transform.rotation = rot;
            topRail.transform.localScale = new Vector3(0.10f, 0.08f, length);
            var trRend = topRail.GetComponent<Renderer>();
            if (trRend != null && woodMat != null) trRend.sharedMaterial = woodMat;
            var trCol = topRail.GetComponent<Collider>();
            if (trCol != null) Object.DestroyImmediate(trCol);

            // Bottom Hardwood Plinth / Base Rail (Height = 0.06m)
            var baseRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseRail.name = "Balustrade_BaseRail";
            baseRail.transform.SetParent(balustradeRoot.transform);
            baseRail.transform.position = new Vector3(mid.x, mid.y + 0.06f, mid.z);
            baseRail.transform.rotation = rot;
            baseRail.transform.localScale = new Vector3(0.10f, 0.08f, length);
            var brRend = baseRail.GetComponent<Renderer>();
            if (brRend != null && woodMat != null) brRend.sharedMaterial = woodMat;
            var brCol = baseRail.GetComponent<Collider>();
            if (brCol != null) Object.DestroyImmediate(brCol);

            // Start Newel Post
            var newelStart = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newelStart.name = "Balustrade_Newel_Start";
            newelStart.transform.SetParent(balustradeRoot.transform);
            newelStart.transform.position = new Vector3(start.x, start.y + 0.55f, start.z);
            newelStart.transform.localScale = new Vector3(0.16f, 1.15f, 0.16f);
            var nsRend = newelStart.GetComponent<Renderer>();
            if (nsRend != null && brassMat != null) nsRend.sharedMaterial = brassMat;
            var nsCol = newelStart.GetComponent<Collider>();
            if (nsCol != null) Object.DestroyImmediate(nsCol);

            // End Newel Post
            var newelEnd = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newelEnd.name = "Balustrade_Newel_End";
            newelEnd.transform.SetParent(balustradeRoot.transform);
            newelEnd.transform.position = new Vector3(end.x, end.y + 0.55f, end.z);
            newelEnd.transform.localScale = new Vector3(0.16f, 1.15f, 0.16f);
            var neRend = newelEnd.GetComponent<Renderer>();
            if (neRend != null && brassMat != null) neRend.sharedMaterial = brassMat;
            var neCol = newelEnd.GetComponent<Collider>();
            if (neCol != null) Object.DestroyImmediate(neCol);

            // Decorative Brass Baluster Spindles (Spaced every ~0.28m)
            int numSpindles = Mathf.Max(1, Mathf.RoundToInt(length / 0.28f));
            for (int i = 1; i < numSpindles; i++)
            {
                float t = (float)i / numSpindles;
                Vector3 pos = Vector3.Lerp(start, end, t);
                var spindle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spindle.name = $"Balustrade_Spindle_{i:D2}";
                spindle.transform.SetParent(balustradeRoot.transform);
                spindle.transform.position = new Vector3(pos.x, pos.y + 0.55f, pos.z);
                spindle.transform.localScale = new Vector3(0.04f, 0.92f, 0.04f);
                var spRend = spindle.GetComponent<Renderer>();
                if (spRend != null && brassMat != null) spRend.sharedMaterial = brassMat;
                var spCol = spindle.GetComponent<Collider>();
                if (spCol != null) Object.DestroyImmediate(spCol);
            }

            // Invisible Safety Box Collider (Prevents player and NPC falls over edge)
            var colGo = new GameObject("SafetyCollider");
            colGo.transform.SetParent(balustradeRoot.transform);
            colGo.transform.position = new Vector3(mid.x, mid.y + 0.60f, mid.z);
            colGo.transform.rotation = rot;
            var boxCol = colGo.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(0.14f, 1.20f, length);
            boxCol.isTrigger = false;
        }

        private static void BuildBacklitArchitecturalWindow(Transform parent, Vector3 localPos, Quaternion localRot,
            Vector2 windowSize, Material frameMat, Material brassMat, string name)
        {
            var winRoot = new GameObject(name);
            winRoot.transform.SetParent(parent);
            winRoot.transform.localPosition = localPos;
            winRoot.transform.localRotation = localRot;

            float width = windowSize.x;
            float height = windowSize.y;

            // Outer Molded Hardwood Window Frame Casing
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = $"{name}_Frame";
            frame.transform.SetParent(winRoot.transform);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = new Vector3(width + 0.16f, height + 0.16f, 0.12f);
            var frRend = frame.GetComponent<Renderer>();
            if (frRend != null && frameMat != null) frRend.sharedMaterial = frameMat;
            var frCol = frame.GetComponent<Collider>();
            if (frCol != null) Object.DestroyImmediate(frCol);

            // Frosted Recessed Glass Pane
            var pane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pane.name = $"{name}_GlassPane";
            pane.transform.SetParent(winRoot.transform);
            pane.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            pane.transform.localScale = new Vector3(width, height, 0.02f);
            var pRend = pane.GetComponent<Renderer>();
            if (pRend != null && frameMat != null) pRend.sharedMaterial = frameMat;
            var pCol = pane.GetComponent<Collider>();
            if (pCol != null) Object.DestroyImmediate(pCol);

            // Center Horizontal Brass Mullion
            var mullionH = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mullionH.name = $"{name}_Mullion_H";
            mullionH.transform.SetParent(winRoot.transform);
            mullionH.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            mullionH.transform.localScale = new Vector3(width, 0.04f, 0.05f);
            var mhRend = mullionH.GetComponent<Renderer>();
            if (mhRend != null && brassMat != null) mhRend.sharedMaterial = brassMat;
            var mhCol = mullionH.GetComponent<Collider>();
            if (mhCol != null) Object.DestroyImmediate(mhCol);

            // Vertical Brass Mullions (2 vertical dividers -> 6 panes total)
            for (int i = -1; i <= 1; i += 2)
            {
                var mullionV = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mullionV.name = $"{name}_Mullion_V_{(i < 0 ? "L" : "R")}";
                mullionV.transform.SetParent(winRoot.transform);
                mullionV.transform.localPosition = new Vector3(i * width * 0.25f, 0f, -0.01f);
                mullionV.transform.localScale = new Vector3(0.04f, height, 0.05f);
                var mvRend = mullionV.GetComponent<Renderer>();
                if (mvRend != null && brassMat != null) mvRend.sharedMaterial = brassMat;
                var mvCol = mullionV.GetComponent<Collider>();
                if (mvCol != null) Object.DestroyImmediate(mvCol);
            }

            // Window Sill Shelf
            var sill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sill.name = $"{name}_Sill";
            sill.transform.SetParent(winRoot.transform);
            sill.transform.localPosition = new Vector3(0f, -height * 0.5f - 0.04f, -0.04f);
            sill.transform.localScale = new Vector3(width + 0.24f, 0.08f, 0.20f);
            var sillRend = sill.GetComponent<Renderer>();
            if (sillRend != null && frameMat != null) sillRend.sharedMaterial = frameMat;
            var sillCol = sill.GetComponent<Collider>();
            if (sillCol != null) Object.DestroyImmediate(sillCol);
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

                    // Determine which room is the approach corridor / public circulation space
                    // Local -Z faces approach room where plaque and casing are viewed
                    // Local +Z enters destination room where door swings inward
                    bool isApproachFrom = conn.FromRoomId.Contains("corridor") || conn.FromRoomId.Contains("landing") || conn.FromRoomId.Contains("lobby") || conn.FromRoomId.Contains("promenade") || conn.FromRoomId.Contains("foyer");
                    bool isApproachTo = conn.ToRoomId.Contains("corridor") || conn.ToRoomId.Contains("landing") || conn.ToRoomId.Contains("lobby") || conn.ToRoomId.Contains("promenade") || conn.ToRoomId.Contains("foyer");
                    string approachId = isApproachTo && !isApproachFrom ? conn.ToRoomId : conn.FromRoomId;
                    string destId = approachId == conn.FromRoomId ? conn.ToRoomId : conn.FromRoomId;

                    Quaternion doorRot;
                    RoomDefinition approachRoom = null;
                    if (!roomMap.TryGetValue(approachId, out approachRoom))
                    {
                        roomMap.TryGetValue(conn.FromRoomId, out approachRoom);
                    }

                    if (approachRoom != null)
                    {
                        Vector3 c = approachRoom.Bounds.center;
                        Vector3 s = approachRoom.Bounds.size;
                        float pX = conn.WorldPosition.x;
                        float pZ = conn.WorldPosition.z;

                        float distNorth = Mathf.Abs(pZ - (c.z + s.z * 0.5f));
                        float distSouth = Mathf.Abs(pZ - (c.z - s.z * 0.5f));
                        float distEast = Mathf.Abs(pX - (c.x + s.x * 0.5f));
                        float distWest = Mathf.Abs(pX - (c.x - s.x * 0.5f));

                        float minDist = Mathf.Min(distNorth, Mathf.Min(distSouth, Mathf.Min(distEast, distWest)));

                        if (minDist == distNorth)
                        {
                            // Door sits on North wall: wall normal is +Z (North), wall spans along X axis
                            // Local +Z points North (+Z), local X spans East-West flush with wall
                            doorRot = Quaternion.identity;
                        }
                        else if (minDist == distSouth)
                        {
                            // Door sits on South wall: wall normal is -Z (South), wall spans along X axis
                            // Local +Z points South (-Z), local X spans East-West flush with wall
                            doorRot = Quaternion.Euler(0f, 180f, 0f);
                        }
                        else if (minDist == distEast)
                        {
                            // Door sits on East wall: wall normal is +X (East), wall spans along Z axis
                            // Local +Z points East (+X), local X spans North-South flush with wall
                            doorRot = Quaternion.Euler(0f, 90f, 0f);
                        }
                        else
                        {
                            // Door sits on West wall: wall normal is -X (West), wall spans along Z axis
                            // Local +Z points West (-X), local X spans North-South flush with wall
                            doorRot = Quaternion.Euler(0f, 270f, 0f);
                        }
                    }
                    else if (conn.DoorOrientationEuler != Vector3.zero)
                    {
                        doorRot = Quaternion.Euler(conn.DoorOrientationEuler);
                    }
                    else
                    {
                        doorRot = Quaternion.identity;
                    }
                    doorRoot.transform.rotation = doorRot;

                    bool isArchway = conn.DoorDisplayName.Contains("Archway");
                    if (isArchway)
                    {
                        if (conn.DoorDisplayName.Contains("Grand Staircase") || conn.ConnectionId.Contains("staircase"))
                        {
                            // Grand Staircase is an open 4-story architectural atrium with continuous steps;
                            // Skip visual casing so wooden pillars never slice through Flight 0 steps!
                            continue;
                        }

                        // Open Archway: permanent architectural pilasters and lintel header with zero door leaf
                        var archCasing = new GameObject("Archway_Casing");
                        archCasing.transform.SetParent(doorRoot.transform);
                        archCasing.transform.localPosition = Vector3.zero;

                        bool isStairArch = conn.DoorDisplayName.Contains("Grand Staircase");
                        float pillarHeight = isStairArch ? 4.5f : 2.8f;
                        float pillarCenterY = pillarHeight * 0.5f;
                        float lintelY = isStairArch ? 4.625f : 2.7f;
                        float trimY = isStairArch ? 4.775f : 2.84f;

                        // Left decorative column
                        var leftCol = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        leftCol.name = "Arch_Pillar_Left";
                        leftCol.transform.SetParent(archCasing.transform);
                        leftCol.transform.localPosition = new Vector3(-1.6f, pillarCenterY, 0f);
                        leftCol.transform.localScale = new Vector3(0.3f, pillarHeight, 0.45f);
                        var lRend = leftCol.GetComponent<Renderer>();
                        if (lRend != null && mahoganyMat != null) lRend.sharedMaterial = mahoganyMat;

                        // Right decorative column
                        var rightCol = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        rightCol.name = "Arch_Pillar_Right";
                        rightCol.transform.SetParent(archCasing.transform);
                        rightCol.transform.localPosition = new Vector3(1.6f, pillarCenterY, 0f);
                        rightCol.transform.localScale = new Vector3(0.3f, pillarHeight, 0.45f);
                        var rRend = rightCol.GetComponent<Renderer>();
                        if (rRend != null && mahoganyMat != null) rRend.sharedMaterial = mahoganyMat;

                        // Overhead Arch Header
                        var topArch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        topArch.name = "Arch_Lintel_Beam";
                        topArch.transform.SetParent(archCasing.transform);
                        topArch.transform.localPosition = new Vector3(0f, lintelY, 0f);
                        topArch.transform.localScale = new Vector3(3.5f, 0.25f, 0.45f);
                        var tRend = topArch.GetComponent<Renderer>();
                        if (tRend != null && mahoganyMat != null) tRend.sharedMaterial = mahoganyMat;

                        // Brass architrave inlay
                        var archTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        archTrim.name = "Arch_Brass_Inlay";
                        archTrim.transform.SetParent(archCasing.transform);
                        archTrim.transform.localPosition = new Vector3(0f, trimY, 0f);
                        archTrim.transform.localScale = new Vector3(3.55f, 0.05f, 0.47f);
                        var trimRend = archTrim.GetComponent<Renderer>();
                        if (trimRend != null && brassMat != null) trimRend.sharedMaterial = brassMat;

                        continue;
                    }

                    bool isDoubleDoor = conn.DoorDisplayName.Contains("Double") || conn.DoorDisplayName.Contains("French");
                    float frameWidth = isDoubleDoor ? 3.4f : 2.2f;
                    float jambX = isDoubleDoor ? 1.525f : 0.925f;
                    float clearWidth = isDoubleDoor ? 2.70f : 1.50f;

                    // 1. Stationary Door Casing (Frames wall opening permanently with authentic jambs, header, sill, and plaque)
                    var doorCasing = new GameObject("Door_Casing");
                    doorCasing.transform.SetParent(doorRoot.transform);
                    doorCasing.transform.localPosition = Vector3.zero;

                    // Left Jamb Post
                    var jambLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    jambLeft.name = "Casing_Jamb_Left";
                    jambLeft.transform.SetParent(doorCasing.transform);
                    jambLeft.transform.localPosition = new Vector3(-jambX, 1.375f, 0f);
                    jambLeft.transform.localScale = new Vector3(0.35f, 2.75f, 0.44f);
                    var jLCol = jambLeft.GetComponent<BoxCollider>();
                    if (jLCol != null) jLCol.isTrigger = false;
                    var jLRend = jambLeft.GetComponent<Renderer>();
                    if (jLRend != null) jLRend.sharedMaterial = mahoganyMat;

                    // Right Jamb Post
                    var jambRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    jambRight.name = "Casing_Jamb_Right";
                    jambRight.transform.SetParent(doorCasing.transform);
                    jambRight.transform.localPosition = new Vector3(jambX, 1.375f, 0f);
                    jambRight.transform.localScale = new Vector3(0.35f, 2.75f, 0.44f);
                    var jRCol = jambRight.GetComponent<BoxCollider>();
                    if (jRCol != null) jRCol.isTrigger = false;
                    var jRRend = jambRight.GetComponent<Renderer>();
                    if (jRRend != null) jRRend.sharedMaterial = mahoganyMat;

                    // Inner Brass Reveal Trims
                    float revealX = isDoubleDoor ? 1.35f : 0.75f;
                    var revealL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    revealL.name = "Casing_Reveal_Left";
                    revealL.transform.SetParent(doorCasing.transform);
                    revealL.transform.localPosition = new Vector3(-revealX, 1.375f, 0f);
                    revealL.transform.localScale = new Vector3(0.04f, 2.75f, 0.46f);
                    var rLRend = revealL.GetComponent<Renderer>();
                    if (rLRend != null) rLRend.sharedMaterial = brassMat;

                    var revealR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    revealR.name = "Casing_Reveal_Right";
                    revealR.transform.SetParent(doorCasing.transform);
                    revealR.transform.localPosition = new Vector3(revealX, 1.375f, 0f);
                    revealR.transform.localScale = new Vector3(0.04f, 2.75f, 0.46f);
                    var rRRend = revealR.GetComponent<Renderer>();
                    if (rRRend != null) rRRend.sharedMaterial = brassMat;

                    // Overhead Lintel Header Beam
                    var lintelHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    lintelHead.name = "Casing_Lintel_Header";
                    lintelHead.transform.SetParent(doorCasing.transform);
                    lintelHead.transform.localPosition = new Vector3(0f, 2.65f, 0f);
                    lintelHead.transform.localScale = new Vector3(frameWidth + 0.05f, 0.28f, 0.46f);
                    var lCol = lintelHead.GetComponent<BoxCollider>();
                    if (lCol != null) lCol.isTrigger = false;
                    var lhRend = lintelHead.GetComponent<Renderer>();
                    if (lhRend != null) lhRend.sharedMaterial = mahoganyMat;

                    // Stepped Cornice Crown
                    var crownCornice = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    crownCornice.name = "Casing_Crown_Cornice";
                    crownCornice.transform.SetParent(doorCasing.transform);
                    crownCornice.transform.localPosition = new Vector3(0f, 2.82f, 0f);
                    crownCornice.transform.localScale = new Vector3(frameWidth + 0.15f, 0.10f, 0.50f);
                    var crRend = crownCornice.GetComponent<Renderer>();
                    if (crRend != null) crRend.sharedMaterial = walnutMat;

                    // Polished Brass Floor Sill Threshold
                    var threshold = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    threshold.name = "Casing_Threshold";
                    threshold.transform.SetParent(doorCasing.transform);
                    threshold.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    threshold.transform.localScale = new Vector3(clearWidth, 0.04f, 0.40f);
                    var thRend = threshold.GetComponent<Renderer>();
                    if (thRend != null) thRend.sharedMaterial = brassMat;

                    // Polished Brass Identification Plaque (Faces corridor / approach room at local -Z)
                    var plaque = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plaque.name = "Casing_Suite_Plaque";
                    plaque.transform.SetParent(doorCasing.transform);
                    plaque.transform.localPosition = new Vector3(0f, 2.62f, -0.235f);
                    plaque.transform.localScale = new Vector3(isDoubleDoor ? 0.65f : 0.50f, 0.12f, 0.02f);
                    var plqRend = plaque.GetComponent<Renderer>();
                    if (plqRend != null) plqRend.sharedMaterial = brassMat;

                    // Door Leaves & Hinges
                    float openAngle = -90f;
                    Transform hingeL = null;
                    Transform hingeR = null;

                    if (isDoubleDoor)
                    {
                        // Double French Doors: Two mirrored leaves meeting at center x = 0
                        float leafW = clearWidth * 0.50f;
                        float hingeX = leafW;

                        // Left Hinge & Leaf
                        var hLGo = new GameObject("Hinge_Left");
                        hLGo.transform.SetParent(doorRoot.transform);
                        hLGo.transform.localPosition = new Vector3(-hingeX, 0f, 0f);
                        hingeL = hLGo.transform;

                        BuildDoorLeafMesh(hLGo.transform, new Vector3(leafW * 0.5f, 1.25f, 0f), new Vector3(leafW, 2.50f, 0.08f),
                            walnutMat, mahoganyMat, brassMat, isRightSide: false);

                        // Right Hinge & Leaf
                        var hRGo = new GameObject("Hinge_Right");
                        hRGo.transform.SetParent(doorRoot.transform);
                        hRGo.transform.localPosition = new Vector3(hingeX, 0f, 0f);
                        hingeR = hRGo.transform;

                        BuildDoorLeafMesh(hRGo.transform, new Vector3(-leafW * 0.5f, 1.25f, 0f), new Vector3(leafW, 2.50f, 0.08f),
                            walnutMat, mahoganyMat, brassMat, isRightSide: true);
                    }
                    else
                    {
                        // Single Door Leaf
                        var hingeGo = new GameObject("Hinge");
                        hingeGo.transform.SetParent(doorRoot.transform);
                        hingeGo.transform.localPosition = new Vector3(-0.73f, 0f, 0f);
                        hingeL = hingeGo.transform;

                        BuildDoorLeafMesh(hingeGo.transform, new Vector3(0.73f, 1.25f, 0f), new Vector3(1.46f, 2.50f, 0.08f),
                            walnutMat, mahoganyMat, brassMat, isRightSide: false);
                    }

                    // All doors start flush closed inside their frames
                    hingeL.localRotation = Quaternion.identity;
                    if (hingeR != null) hingeR.localRotation = Quaternion.identity;

                    // InteractableDoor component
                    var doorComp = doorRoot.AddComponent<InteractableDoor>();
                    doorComp.OpenAngle = openAngle;

#if UNITY_EDITOR
                    var soDoor = new SerializedObject(doorComp);
                    soDoor.FindProperty("doorHingeTransform").objectReferenceValue = hingeL;
                    if (hingeR != null) soDoor.FindProperty("doorHingeRightTransform").objectReferenceValue = hingeR;
                    soDoor.FindProperty("doorId").stringValue = conn.RequiredKeyId != null ? $"door_{conn.ConnectionId}" : $"door_unlocked_{conn.ConnectionId}";
                    soDoor.FindProperty("doorDisplayName").stringValue = conn.DoorDisplayName;
                    soDoor.FindProperty("requiredKeyId").stringValue = conn.RequiredKeyId ?? string.Empty;
                    soDoor.FindProperty("lockState").enumValueIndex = conn.RequiresKey ? (int)DoorLockState.Locked : (int)DoorLockState.Unlocked;
                    soDoor.FindProperty("openState").enumValueIndex = (int)DoorOpenState.Closed;
                    soDoor.FindProperty("openAngle").floatValue = openAngle;
                    soDoor.ApplyModifiedPropertiesWithoutUndo();
#endif
                }
            }
        }

        private static void BuildDoorLeafMesh(Transform hingeParent, Vector3 localPos, Vector3 leafSize,
            Material woodMat, Material panelMat, Material brassMat, bool isRightSide)
        {
            var doorLeaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorLeaf.name = "Door_Leaf";
            doorLeaf.transform.SetParent(hingeParent);
            doorLeaf.transform.localPosition = localPos;
            doorLeaf.transform.localScale = leafSize;

            var leafRend = doorLeaf.GetComponent<Renderer>();
            if (leafRend != null && woodMat != null) leafRend.sharedMaterial = woodMat;

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
            if (upperPanel.GetComponent<Renderer>() != null && panelMat != null) upperPanel.GetComponent<Renderer>().sharedMaterial = panelMat;
            var upCol = upperPanel.GetComponent<Collider>();
            if (upCol != null) Object.DestroyImmediate(upCol);

            // Lower Recessed Panel Detail
            var lowerPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lowerPanel.name = "Door_Panel_Lower";
            lowerPanel.transform.SetParent(doorLeaf.transform);
            lowerPanel.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            lowerPanel.transform.localScale = new Vector3(0.78f, 0.28f, 1.25f);
            if (lowerPanel.GetComponent<Renderer>() != null && panelMat != null) lowerPanel.GetComponent<Renderer>().sharedMaterial = panelMat;
            var lpCol = lowerPanel.GetComponent<Collider>();
            if (lpCol != null) Object.DestroyImmediate(lpCol);

            // Brass Kickplate at Base of Door
            var kickplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kickplate.name = "Door_Brass_Kickplate";
            kickplate.transform.SetParent(doorLeaf.transform);
            kickplate.transform.localPosition = new Vector3(0f, -0.88f, 0f);
            kickplate.transform.localScale = new Vector3(0.98f, 0.16f, 1.20f);
            if (kickplate.GetComponent<Renderer>() != null && brassMat != null) kickplate.GetComponent<Renderer>().sharedMaterial = brassMat;
            var kpCol = kickplate.GetComponent<Collider>();
            if (kpCol != null) Object.DestroyImmediate(kpCol);

            // Polished Brass Door Lever Handle Plate
            float handleX = isRightSide ? -0.38f : 0.38f;
            var handleEscutcheon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handleEscutcheon.name = "Door_Handle_Escutcheon";
            handleEscutcheon.transform.SetParent(doorLeaf.transform);
            handleEscutcheon.transform.localPosition = new Vector3(handleX, -0.18f, 0f);
            handleEscutcheon.transform.localScale = new Vector3(0.08f, 0.14f, 1.45f);
            if (handleEscutcheon.GetComponent<Renderer>() != null && brassMat != null) handleEscutcheon.GetComponent<Renderer>().sharedMaterial = brassMat;
            var hCol = handleEscutcheon.GetComponent<Collider>();
            if (hCol != null) Object.DestroyImmediate(hCol);

            // Brass Lever Handle (Front)
            var handleLeverFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handleLeverFront.name = "Door_Handle_Lever_Front";
            handleLeverFront.transform.SetParent(handleEscutcheon.transform);
            handleLeverFront.transform.localPosition = new Vector3(isRightSide ? 0.35f : -0.35f, 0.18f, 0.55f);
            handleLeverFront.transform.localScale = new Vector3(1.8f, 0.22f, 0.22f);
            if (handleLeverFront.GetComponent<Renderer>() != null && brassMat != null) handleLeverFront.GetComponent<Renderer>().sharedMaterial = brassMat;
            var hlfCol = handleLeverFront.GetComponent<Collider>();
            if (hlfCol != null) Object.DestroyImmediate(hlfCol);

            // Brass Lever Handle (Back)
            var handleLeverBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handleLeverBack.name = "Door_Handle_Lever_Back";
            handleLeverBack.transform.SetParent(handleEscutcheon.transform);
            handleLeverBack.transform.localPosition = new Vector3(isRightSide ? 0.35f : -0.35f, 0.18f, -0.55f);
            handleLeverBack.transform.localScale = new Vector3(1.8f, 0.22f, 0.22f);
            if (handleLeverBack.GetComponent<Renderer>() != null && brassMat != null) handleLeverBack.GetComponent<Renderer>().sharedMaterial = brassMat;
            var hlbCol = handleLeverBack.GetComponent<Collider>();
            if (hlbCol != null) Object.DestroyImmediate(hlbCol);
        }

        private static void BuildRoomFurniture(Transform parent, List<RoomDefinition> rooms, Dictionary<string, Material> mats, TomorrowsCrime.Procedural.Core.SeededRandom rng)
        {
            var furnitureRoot = new GameObject("Hotel_SolidFurniture");
            furnitureRoot.transform.SetParent(parent);

            Material mahoganyMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));
            Material greenFeltMat = GetMaterial(mats, "GreenFelt", new Color(0.12f, 0.45f, 0.22f));
            Material velvetGoldMat = GetMaterial(mats, "VelvetGold", new Color(0.85f, 0.70f, 0.25f));
            Material velvetEmeraldMat = GetMaterial(mats, "VelvetEmerald", new Color(0.12f, 0.40f, 0.22f));
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
                        // Front Reception Desk with solid counter (Grounded flush with floor)
                        var deskRoot = new GameObject("Reception_Desk_Compound");
                        deskRoot.transform.SetParent(furnitureRoot.transform);
                        deskRoot.transform.position = new Vector3(0f, y, 9.2f);
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

                        // Front Counter Solid Box Collider (Staff stands behind at Z > 10.2m)
                        var counterCol = deskRoot.AddComponent<BoxCollider>();
                        counterCol.center = new Vector3(0f, 0.55f, 0f);
                        counterCol.size = new Vector3(5.4f, 1.1f, 1.4f);
                        counterCol.isTrigger = false;

                        // Solid grounded base plinth so counter is firmly seated on marble floor with zero gap
                        var deskPlinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        deskPlinth.name = "Reception_Counter_Plinth";
                        deskPlinth.transform.SetParent(deskRoot.transform);
                        deskPlinth.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                        deskPlinth.transform.localScale = new Vector3(5.2f, 0.16f, 1.3f);
                        if (deskPlinth.GetComponent<Renderer>() != null && walnutMat != null) deskPlinth.GetComponent<Renderer>().sharedMaterial = walnutMat;
                        var dpCol = deskPlinth.GetComponent<Collider>();
                        if (dpCol != null) Object.DestroyImmediate(dpCol);

                        // Rear Key Cubbies & Staff Lockers Unit flush against North back wall at world Z = 13.5m (Parented under deskRoot for collision audit)
                        BuildSolidStorageUnit(deskRoot.transform, new Vector3(0f, 0f, -4.3f), new Vector3(4.5f, 2.4f, 0.5f), walnutMat, "Reception_Lockers_Unit");

                        // Switchboard console and executive safe against North back wall corners (wide open staff area)
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-5.2f, y, 13.5f), new Vector3(1.4f, 1.8f, 0.6f), walnutMat, "Reception_Switchboard");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(5.2f, y, 13.5f), new Vector3(1.2f, 1.4f, 0.8f), marbleBlackMat, "Reception_ExecutiveSafe");
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
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-16f, y, -9.1f), Quaternion.Euler(0f, 180f, 0f), walnutMat, greenFeltMat, "Conservatory_BistroChair_N");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-16f, y, -10.9f), Quaternion.Euler(0f, 0f, 0f), walnutMat, greenFeltMat, "Conservatory_BistroChair_S");
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
                        // Bookshelves flush along North and South walls
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-11f, y, 6.4f), new Vector3(10.0f, 3.6f, 0.7f), mahoganyMat, "Library_Bookshelf_North");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-11f, y, -6.4f), new Vector3(10.0f, 3.6f, 0.7f), mahoganyMat, "Library_Bookshelf_South");
                        // Central study reading desk & chairs
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-11f, y, 0f), new Vector3(3.2f, 0.85f, 1.6f), walnutMat, "Library_StudyReadingDesk");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-11f, y, 1.2f), Quaternion.Euler(0f, 180f, 0f), walnutMat, greenFeltMat, "Library_ReadingChair_N");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-11f, y, -1.2f), Quaternion.Euler(0f, 0f, 0f), walnutMat, greenFeltMat, "Library_ReadingChair_S");
                        // Terrestrial globe in NW corner
                        BuildFloorGlobe(furnitureRoot.transform, new Vector3(-16.5f, y, 4.5f), brassMat, "Library_TerrestrialGlobe");
                        // Faux backlit architectural windows on West exterior wall
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(-17.9f, y + 2.2f, 3.5f), Quaternion.Euler(0f, 90f, 0f), new Vector2(2.4f, 1.8f), mahoganyMat, brassMat, "Library_Window_West_N");
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(-17.9f, y + 2.2f, -3.5f), Quaternion.Euler(0f, 90f, 0f), new Vector2(2.4f, 1.8f), mahoganyMat, brassMat, "Library_Window_West_S");
                        break;

                    case RoomType.CardRoom:
                        // Poker table centered in West half with 4 surrounding chairs
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-14f, y, 10.5f), new Vector3(2.2f, 0.82f, 2.2f), greenFeltMat, "CardRoom_GameTable");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-14f, y, 11.8f), Quaternion.Euler(0f, 180f, 0f), walnutMat, velvetEmeraldMat, "CardRoom_GameChair_N");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-14f, y, 9.2f), Quaternion.Euler(0f, 0f, 0f), walnutMat, velvetEmeraldMat, "CardRoom_GameChair_S");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-15.3f, y, 10.5f), Quaternion.Euler(0f, 90f, 0f), walnutMat, velvetEmeraldMat, "CardRoom_GameChair_W");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-12.7f, y, 10.5f), Quaternion.Euler(0f, -90f, 0f), walnutMat, velvetEmeraldMat, "CardRoom_GameChair_E");

                        // Chess table centered in East half with 2 chairs
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-8f, y, 10.5f), new Vector3(1.4f, 0.80f, 1.4f), walnutMat, "CardRoom_ChessTable");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-8f, y, 11.5f), Quaternion.Euler(0f, 180f, 0f), walnutMat, velvetGoldMat, "CardRoom_ChessChair_N");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-8f, y, 9.5f), Quaternion.Euler(0f, 0f, 0f), walnutMat, velvetGoldMat, "CardRoom_ChessChair_S");

                        // Humidor Credenza flush in SW corner against West wall
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-17.3f, y, 8.5f), new Vector3(0.6f, 1.5f, 1.8f), mahoganyMat, "CardRoom_HumidorCredenza");
                        // Faux backlit architectural windows on North exterior wall
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(-14f, y + 2.2f, 13.9f), Quaternion.Euler(0f, 180f, 0f), new Vector2(2.4f, 1.8f), walnutMat, brassMat, "CardRoom_Window_N1");
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(-8f, y + 2.2f, 13.9f), Quaternion.Euler(0f, 180f, 0f), new Vector2(2.4f, 1.8f), walnutMat, brassMat, "CardRoom_Window_N2");
                        break;

                    case RoomType.MusicSalon:
                        // Grand Piano placed elegantly in East salon area
                        BuildSolidPiano(furnitureRoot.transform, new Vector3(-8f, y, 10.5f), mats);
                        // Cello / Music stand in NW corner against North wall
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-16.5f, y, 13.2f), new Vector3(0.6f, 1.4f, 0.6f), walnutMat, "MusicSalon_CelloStand");
                        // Tea table & conversation armchairs in West salon area
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-14f, y, 10.5f), new Vector3(1.4f, 0.65f, 1.4f), mahoganyMat, "MusicSalon_TeaTable");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-15.3f, y, 10.5f), Quaternion.Euler(0f, 90f, 0f), walnutMat, velvetGoldMat, "MusicSalon_Armchair_L");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-12.7f, y, 10.5f), Quaternion.Euler(0f, -90f, 0f), walnutMat, velvetGoldMat, "MusicSalon_Armchair_R");
                        // Sheet music cabinet flush against West wall
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-17.3f, y, 10.5f), new Vector3(0.6f, 1.8f, 2.4f), walnutMat, "MusicSalon_MusicCabinet");
                        // Faux backlit architectural windows on North exterior wall
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(-14f, y + 2.2f, 13.9f), Quaternion.Euler(0f, 180f, 0f), new Vector2(2.4f, 1.8f), walnutMat, brassMat, "MusicSalon_Window_N1");
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(-8f, y + 2.2f, 13.9f), Quaternion.Euler(0f, 180f, 0f), new Vector2(2.4f, 1.8f), walnutMat, brassMat, "MusicSalon_Window_N2");
                        break;

                    case RoomType.BilliardsRoom:
                        // Billiards table in center of room
                        BuildSolidBilliardsTable(furnitureRoot.transform, new Vector3(-11f, y, 0f), mats);
                        // Cue rack flush against West wall
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-17.5f, y, 0f), new Vector3(0.4f, 2.0f, 2.4f), mahoganyMat, "Billiards_CueRack");
                        // Spectator benches along North and South walls
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-11f, y, 6.2f), new Vector3(3.2f, 0.95f, 0.8f), walnutMat, "Billiards_SpectatorBench_N");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-11f, y, -6.2f), new Vector3(3.2f, 0.95f, 0.8f), walnutMat, "Billiards_SpectatorBench");
                        // Faux backlit architectural windows on West exterior wall
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(-17.9f, y + 2.2f, 3.5f), Quaternion.Euler(0f, 90f, 0f), new Vector2(2.4f, 1.8f), mahoganyMat, brassMat, "Billiards_Window_W1");
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(-17.9f, y + 2.2f, -3.5f), Quaternion.Euler(0f, 90f, 0f), new Vector2(2.4f, 1.8f), mahoganyMat, brassMat, "Billiards_Window_W2");
                        break;

                    case RoomType.CigarLounge:
                        // Fireplace centered on South exterior wall
                        BuildSolidFireplace(furnitureRoot.transform, new Vector3(-11f, y, -13.5f), mats, "CigarLounge_Fireplace");
                        // Central conversation coffee table and 4 armchairs
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-11f, y, -10.5f), new Vector3(2.2f, 0.55f, 1.4f), mahoganyMat, "CigarLounge_CoffeeTable");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-11f, y, -9.2f), Quaternion.Euler(0f, 180f, 0f), walnutMat, velvetGoldMat, "CigarLounge_Armchair_N");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-11f, y, -11.8f), Quaternion.Euler(0f, 0f, 0f), walnutMat, velvetGoldMat, "CigarLounge_Armchair_S");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-12.6f, y, -10.5f), Quaternion.Euler(0f, 90f, 0f), walnutMat, velvetGoldMat, "CigarLounge_Armchair_W");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-9.4f, y, -10.5f), Quaternion.Euler(0f, -90f, 0f), walnutMat, velvetGoldMat, "CigarLounge_Armchair_E");
                        // Humidor cabinet flush against West wall in SW corner (facing East into room)
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-17.5f, y, -12.5f), new Vector3(1.8f, 2.4f, 0.65f), walnutMat, "CigarLounge_HumidorCabinet", Quaternion.Euler(0f, 90f, 0f));
                        // Cognac bar flush against West wall in NW corner (facing East into room)
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-17.5f, y, -8.5f), new Vector3(1.8f, 1.6f, 0.65f), walnutMat, "CigarLounge_CognacBar", Quaternion.Euler(0f, 90f, 0f));
                        // Faux backlit architectural windows on South exterior wall
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(-14.5f, y + 2.2f, -13.9f), Quaternion.Euler(0f, 0f, 0f), new Vector2(2.4f, 1.8f), walnutMat, brassMat, "CigarLounge_Window_S1");
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(-7.5f, y + 2.2f, -13.9f), Quaternion.Euler(0f, 0f, 0f), new Vector2(2.4f, 1.8f), walnutMat, brassMat, "CigarLounge_Window_S2");
                        break;

                    case RoomType.CrownPenthouse:
                        // Grand fireplace flush against West exterior wall
                        BuildSolidFireplace(furnitureRoot.transform, new Vector3(-17.4f, y, 0f), mats, "Penthouse_GrandFireplace");
                        // Private penthouse bar in NE corner with 2 barstools
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-7.5f, y, 4.5f), new Vector3(3.2f, 1.15f, 1.0f), walnutMat, "Penthouse_PrivateBar");
                        BuildBarStool(furnitureRoot.transform, new Vector3(-8.2f, y, 3.5f), walnutMat, brassMat, velvetGoldMat, "Penthouse_BarStool_1");
                        BuildBarStool(furnitureRoot.transform, new Vector3(-6.8f, y, 3.5f), walnutMat, brassMat, velvetGoldMat, "Penthouse_BarStool_2");
                        // Central luxury lounge seating with velvet sofa, coffee table, and armchairs
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-13.5f, y, 0f), new Vector3(2.4f, 0.50f, 1.4f), mahoganyMat, "Penthouse_LoungeTable");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-13.5f, y, 1.5f), new Vector3(2.8f, 0.85f, 0.9f), velvetGoldMat, "Penthouse_VelvetSofa");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-15.5f, y, 0f), Quaternion.Euler(0f, 90f, 0f), walnutMat, velvetGoldMat, "Penthouse_Armchair_W");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-11.5f, y, 0f), Quaternion.Euler(0f, -90f, 0f), walnutMat, velvetGoldMat, "Penthouse_Armchair_E");
                        // Faux backlit architectural windows on West exterior wall
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(-17.9f, y + 2.2f, 4.0f), Quaternion.Euler(0f, 90f, 0f), new Vector2(2.6f, 2.0f), mahoganyMat, brassMat, "Penthouse_Window_W1");
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(-17.9f, y + 2.2f, -4.0f), Quaternion.Euler(0f, 90f, 0f), new Vector2(2.6f, 2.0f), mahoganyMat, brassMat, "Penthouse_Window_W2");
                        break;

                    case RoomType.Observatory:
                        // Telescope in center pointing toward sky/north
                        BuildAstronomicalTelescope(furnitureRoot.transform, new Vector3(1f, y, 8.5f), brassMat, mahoganyMat, "Observatory_Telescope");
                        // Star chart desk along West wall
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-2.6f, y, 7.0f), new Vector3(1.8f, 0.85f, 1.0f), walnutMat, "Observatory_StarChartDesk");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-2.6f, y, 7.8f), Quaternion.Euler(0f, 180f, 0f), walnutMat, greenFeltMat, "Observatory_AstronomerChair");
                        // Armillary globe in NE corner
                        BuildFloorGlobe(furnitureRoot.transform, new Vector3(4.5f, y, 9.5f), brassMat, "Observatory_ArmillaryGlobe");
                        // Faux observation skylight windows on North wall
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(1.0f, y + 2.2f, 10.9f), Quaternion.Euler(0f, 180f, 0f), new Vector2(3.0f, 2.0f), walnutMat, brassMat, "Observatory_SkyWindow");
                        break;

                    case RoomType.Solarium:
                        // Central carved stone fountain
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(1f, y, -7f), new Vector3(2.2f, 1.2f, 2.2f), marbleIvoryMat, "Solarium_StoneFountain");
                        // Sun loungers & bistro table along East side
                        BuildSolidTable(furnitureRoot.transform, new Vector3(3.5f, y, -7f), new Vector3(1.2f, 0.70f, 1.0f), brassMat, "Solarium_LoungerTable");
                        BuildSunLounger(furnitureRoot.transform, new Vector3(3.5f, y, -5.6f), Quaternion.Euler(0f, 180f, 0f), walnutMat, velvetEmeraldMat, "Solarium_SunLounger_1");
                        BuildSunLounger(furnitureRoot.transform, new Vector3(3.5f, y, -8.4f), Quaternion.Euler(0f, 0f, 0f), walnutMat, velvetEmeraldMat, "Solarium_SunLounger_2");
                        // Stone planters in corners
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-2.8f, y, -10f), new Vector3(1.2f, 1.0f, 1.2f), marbleIvoryMat, "Solarium_Planter_SW");
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-2.8f, y, -4f), new Vector3(1.2f, 1.0f, 1.2f), marbleIvoryMat, "Solarium_Planter_NW");
                        // Faux terrace windows along South wall
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(-1.5f, y + 2.2f, -10.9f), Quaternion.Euler(0f, 0f, 0f), new Vector2(2.4f, 1.8f), marbleIvoryMat, brassMat, "Solarium_Window_S1");
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(3.5f, y + 2.2f, -10.9f), Quaternion.Euler(0f, 0f, 0f), new Vector2(2.4f, 1.8f), marbleIvoryMat, brassMat, "Solarium_Window_S2");
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
                        // Executive partner desk centered in room
                        BuildSolidTable(furnitureRoot.transform, new Vector3(-11f, y, -10.5f), new Vector3(2.8f, 0.85f, 1.5f), walnutMat, "Presidential_StudyDesk");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-11f, y, -9.4f), Quaternion.Euler(0f, 180f, 0f), walnutMat, greenFeltMat, "Presidential_ExecutiveChair");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-12.4f, y, -11.8f), Quaternion.Euler(0f, 20f, 0f), walnutMat, velvetGoldMat, "Presidential_VisitorChair_L");
                        BuildDetailedChair(furnitureRoot.transform, new Vector3(-9.6f, y, -11.8f), Quaternion.Euler(0f, -20f, 0f), walnutMat, velvetGoldMat, "Presidential_VisitorChair_R");
                        // Heavy floor safe in SW corner against West wall
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-17.2f, y, -12.5f), new Vector3(1.1f, 1.6f, 1.1f), marbleBlackMat, "Presidential_FloorSafe");
                        // Bookcase in NW corner against West wall
                        BuildSolidStorageUnit(furnitureRoot.transform, new Vector3(-17.2f, y, -8.5f), new Vector3(0.6f, 2.4f, 2.0f), walnutMat, "Presidential_Bookcase");
                        // Terrestrial globe near east corner
                        BuildFloorGlobe(furnitureRoot.transform, new Vector3(-6.5f, y, -12.0f), brassMat, "Presidential_Globe");
                        // Faux backlit windows on South exterior wall
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(-14.5f, y + 2.2f, -13.9f), Quaternion.Euler(0f, 0f, 0f), new Vector2(2.4f, 1.8f), walnutMat, brassMat, "Presidential_Window_S1");
                        BuildBacklitArchitecturalWindow(furnitureRoot.transform, new Vector3(-7.5f, y + 2.2f, -13.9f), Quaternion.Euler(0f, 0f, 0f), new Vector2(2.4f, 1.8f), walnutMat, brassMat, "Presidential_Window_S2");
                        break;

                    case RoomType.HousekeepingStorage:
                    case RoomType.LinenStorage:
                    case RoomType.ServiceAnnexFloor2:
                        // Shelves and carts placed flush against walls, leaving room interior completely clear
                        BuildSolidStorageUnit(furnitureRoot.transform,
                            new Vector3(room.Bounds.center.x, y, room.Bounds.center.z + room.Bounds.size.z * 0.40f),
                            new Vector3(2.4f, 2.4f, 0.7f), walnutMat, $"{room.RoomId}_Shelves");
                        BuildBellhopLuggageCart(furnitureRoot.transform,
                            new Vector3(room.Bounds.center.x - room.Bounds.size.x * 0.35f, y, room.Bounds.center.z),
                            brassMat, walnutMat, $"{room.RoomId}_LinenCart");
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
                    BuildDetailedChair(suiteRoot.transform, new Vector3(center.x + 5.5f, yFloor, center.z - size.z * 0.15f + 0.70f), Quaternion.Euler(0f, 180f, 0f), walnutMat, duvetMat, $"{room.RoomId}_DeskChair");
                }

                // Wardrobe Armoire flush against corner wall
                BuildSolidStorageUnit(suiteRoot.transform, new Vector3(center.x + size.x * 0.42f, yFloor, center.z - size.z * 0.35f),
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

                // Wardrobe Armoire flush against corner wall
                BuildSolidStorageUnit(suiteRoot.transform, new Vector3(center.x + 3.8f, yFloor, center.z - 3.2f),
                    new Vector3(1.3f, 2.3f, 0.60f), walnutMat, $"{room.RoomId}_Armoire");

                // En-Suite Bathroom Zone on West
                Vector3 showerPos = new Vector3(center.x - 2.2f, yFloor, center.z + 2.2f);
                Vector3 vanityPos = new Vector3(center.x - 0.6f, yFloor, center.z + 2.6f);
                Vector3 toiletPos = new Vector3(center.x - 2.2f, yFloor, center.z + 0.6f);
                Vector3 towelPos = new Vector3(center.x - 0.6f, yFloor, center.z + 0.6f);

                BuildEnSuiteBathroom(suiteRoot.transform, showerPos, vanityPos, toiletPos, towelPos, yFloor, mats, room.RoomId);
            }

            // Faux Backlit Architectural Window on exterior wall
            bool isNorth = center.z > 0;
            float winZ = isNorth ? (center.z + size.z * 0.5f - 0.10f) : (center.z - size.z * 0.5f + 0.10f);
            Quaternion winRot = isNorth ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            BuildBacklitArchitecturalWindow(suiteRoot.transform, new Vector3(center.x, yFloor + 2.2f, winZ), winRot,
                new Vector2(2.4f, 1.8f), walnutMat, brassMat, $"{room.RoomId}_Window");
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

            // East Lounge: Fireplace & Wingback Armchairs along North-East wall (Clear of Staircase at Z=-2.1m!)
            BuildSolidFireplace(lobbyRoot.transform, center + new Vector3(6.5f, 0f, 3.8f), mats, "Lobby_GrandFireplace");
            BuildDetailedChair(lobbyRoot.transform, center + new Vector3(4.8f, 0f, 3.0f),
                Quaternion.Euler(0f, -45f, 0f), walnutMat, carpetBurgundy, "Lobby_WingbackChair_South");
            BuildDetailedChair(lobbyRoot.transform, center + new Vector3(4.8f, 0f, 4.6f),
                Quaternion.Euler(0f, 45f, 0f), walnutMat, carpetBurgundy, "Lobby_WingbackChair_North");

            // Grandfather Clock in Lobby Corner (Inside lobby bounds Z in [-6, 6])
            BuildGrandfatherClock(lobbyRoot.transform, center + new Vector3(-6.5f, 0f, 4.5f), walnutMat, brassMat, whiteMat, "Lobby_GrandfatherClock");

            // Bellhop Luggage Trolley Cart in Lobby Entrance
            BuildBellhopLuggageCart(lobbyRoot.transform, center + new Vector3(5.5f, 0f, 1.2f), brassMat, walnutMat, "Lobby_LuggageTrolley");
        }

        private static void BuildDiningFurnishings(Transform parent, Vector3 center, Dictionary<string, Material> mats)
        {
            Material mahoganyMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));

            // North Dining Table - Sized to 2.6m x 1.8m, leaving wide 2.5m clear walking corridor to Kitchen Door at Z=6.0m
            BuildSolidTable(parent, center + new Vector3(-1.0f, 0f, 2.3f), new Vector3(1.8f, 0.85f, 2.6f), mahoganyMat, "Dining_BanquetTable_North");
            BuildDetailedChair(parent, center + new Vector3(-2.2f, 0f, 2.3f), Quaternion.Euler(0f, 90f, 0f), walnutMat, mahoganyMat, "Dining_Chair_North_W");
            BuildDetailedChair(parent, center + new Vector3(0.2f, 0f, 2.3f), Quaternion.Euler(0f, -90f, 0f), walnutMat, mahoganyMat, "Dining_Chair_North_E");
            BuildDetailedChair(parent, center + new Vector3(-1.0f, 0f, 3.8f), Quaternion.Euler(0f, 180f, 0f), walnutMat, mahoganyMat, "Dining_Chair_North_N");
            BuildDetailedChair(parent, center + new Vector3(-1.0f, 0f, 0.8f), Quaternion.Euler(0f, 0f, 0f), walnutMat, mahoganyMat, "Dining_Chair_North_S");

            // South Dining Table - Sized to 2.6m x 1.8m, leaving wide 2.5m clear walking corridor to Conservatory Door at Z=-6.0m
            BuildSolidTable(parent, center + new Vector3(-1.0f, 0f, -2.3f), new Vector3(1.8f, 0.85f, 2.6f), mahoganyMat, "Dining_BanquetTable_South");
            BuildDetailedChair(parent, center + new Vector3(-2.2f, 0f, -2.3f), Quaternion.Euler(0f, 90f, 0f), walnutMat, mahoganyMat, "Dining_Chair_South_W");
            BuildDetailedChair(parent, center + new Vector3(0.2f, 0f, -2.3f), Quaternion.Euler(0f, -90f, 0f), walnutMat, mahoganyMat, "Dining_Chair_South_E");
            BuildDetailedChair(parent, center + new Vector3(-1.0f, 0f, -0.8f), Quaternion.Euler(0f, 180f, 0f), walnutMat, mahoganyMat, "Dining_Chair_South_N");
            BuildDetailedChair(parent, center + new Vector3(-1.0f, 0f, -3.8f), Quaternion.Euler(0f, 0f, 0f), walnutMat, mahoganyMat, "Dining_Chair_South_S");

            // Sideboard Cabinet flush against West Wall
            BuildSolidStorageUnit(parent, center + new Vector3(-6.2f, 0f, 0f), new Vector3(1.0f, 1.6f, 3.2f), walnutMat, "Dining_SideboardCabinet");

            // Rolling Champagne / Dessert Trolley parked in North-East service corner (Clear of lobby doorway!)
            BuildSolidStorageUnit(parent, center + new Vector3(4.5f, 0f, 4.2f), new Vector3(1.0f, 0.9f, 1.6f), mahoganyMat, "Dining_DessertTrolley");
        }

        private static void BuildBarFurnishings(Transform parent, Vector3 center, Dictionary<string, Material> mats)
        {
            Material mahoganyMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));
            Material brassMat = GetMaterial(mats, "BrassGold", new Color(0.88f, 0.75f, 0.35f));

            // Main Bar Counter
            BuildSolidStorageUnit(parent, center + new Vector3(-1.5f, 0f, -3.5f), new Vector3(5.2f, 1.15f, 1.2f), mahoganyMat, "Bar_ServiceCounter");

            // 3 Art Deco Bar Stools in Front of Service Counter
            BuildBarStool(parent, center + new Vector3(-3.0f, 0f, -2.4f), walnutMat, brassMat, mahoganyMat, "Bar_BarStool_1");
            BuildBarStool(parent, center + new Vector3(-1.5f, 0f, -2.4f), walnutMat, brassMat, mahoganyMat, "Bar_BarStool_2");
            BuildBarStool(parent, center + new Vector3(0.0f, 0f, -2.4f), walnutMat, brassMat, mahoganyMat, "Bar_BarStool_3");

            // Back-Bar Liquor Shelf Unit
            BuildSolidStorageUnit(parent, center + new Vector3(-1.5f, 0f, -5.5f), new Vector3(5.0f, 2.4f, 0.6f), walnutMat, "Bar_BackShelves");

            // Cocktail Tables with Armchairs
            BuildSolidTable(parent, center + new Vector3(-3.0f, 0f, 0f), new Vector3(1.4f, 0.75f, 1.4f), walnutMat, "Bar_LoungeTable_A");
            BuildDetailedChair(parent, center + new Vector3(-3.0f, 0f, 1.1f), Quaternion.Euler(0f, 180f, 0f), walnutMat, mahoganyMat, "Bar_LoungeChair_A1");
            BuildDetailedChair(parent, center + new Vector3(-3.0f, 0f, -1.1f), Quaternion.Euler(0f, 0f, 0f), walnutMat, mahoganyMat, "Bar_LoungeChair_A2");

            BuildSolidTable(parent, center + new Vector3(1.0f, 0f, 0f), new Vector3(1.4f, 0.75f, 1.4f), walnutMat, "Bar_LoungeTable_B");
            BuildDetailedChair(parent, center + new Vector3(1.0f, 0f, 1.1f), Quaternion.Euler(0f, 180f, 0f), walnutMat, mahoganyMat, "Bar_LoungeChair_B1");
            BuildDetailedChair(parent, center + new Vector3(1.0f, 0f, -1.1f), Quaternion.Euler(0f, 0f, 0f), walnutMat, mahoganyMat, "Bar_LoungeChair_B2");

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

            // Solid Master Collider
            var col = bedRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, size.y * 0.35f, 0f);
            col.size = new Vector3(size.x, size.y * 0.7f, size.z);
            col.isTrigger = false;

            // 1. Bed Base Frame
            var bedFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bedFrame.name = "Bed_Base_Frame";
            bedFrame.transform.SetParent(bedRoot.transform);
            bedFrame.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            bedFrame.transform.localScale = new Vector3(size.x, 0.18f, size.z);
            if (bedFrame.GetComponent<Renderer>() != null && frameMat != null) bedFrame.GetComponent<Renderer>().sharedMaterial = frameMat;
            var fc = bedFrame.GetComponent<Collider>();
            if (fc != null) Object.DestroyImmediate(fc);

            // 2. Thick Mattress
            var mattress = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mattress.name = "Mattress";
            mattress.transform.SetParent(bedRoot.transform);
            mattress.transform.localPosition = new Vector3(0f, 0.44f, 0f);
            mattress.transform.localScale = new Vector3(size.x * 0.94f, 0.32f, size.z * 0.94f);
            if (mattress.GetComponent<Renderer>() != null && sheetMat != null) mattress.GetComponent<Renderer>().sharedMaterial = sheetMat;
            var mc = mattress.GetComponent<Collider>();
            if (mc != null) Object.DestroyImmediate(mc);

            // 3. Headboard
            var headboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headboard.name = "Headboard";
            headboard.transform.SetParent(bedRoot.transform);
            headboard.transform.localPosition = new Vector3(0f, size.y * 0.55f, size.z * 0.46f);
            headboard.transform.localScale = new Vector3(size.x * 1.02f, size.y * 0.85f, 0.12f);
            if (headboard.GetComponent<Renderer>() != null && frameMat != null) headboard.GetComponent<Renderer>().sharedMaterial = frameMat;
            var hc = headboard.GetComponent<Collider>();
            if (hc != null) Object.DestroyImmediate(hc);

            // 4. Footboard
            var footboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            footboard.name = "Footboard";
            footboard.transform.SetParent(bedRoot.transform);
            footboard.transform.localPosition = new Vector3(0f, 0.42f, -size.z * 0.46f);
            footboard.transform.localScale = new Vector3(size.x * 1.02f, 0.55f, 0.12f);
            if (footboard.GetComponent<Renderer>() != null && frameMat != null) footboard.GetComponent<Renderer>().sharedMaterial = frameMat;
            var foc = footboard.GetComponent<Collider>();
            if (foc != null) Object.DestroyImmediate(foc);

            // 5. Twin Bedside Pillows
            float px = size.x * 0.24f;
            for (int p = -1; p <= 1; p += 2)
            {
                var pillow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillow.name = $"Pillow_{(p < 0 ? "L" : "R")}";
                pillow.transform.SetParent(bedRoot.transform);
                pillow.transform.localPosition = new Vector3(p * px, 0.64f, size.z * 0.32f);
                pillow.transform.localScale = new Vector3(size.x * 0.38f, 0.12f, 0.45f);
                if (pillow.GetComponent<Renderer>() != null && sheetMat != null) pillow.GetComponent<Renderer>().sharedMaterial = sheetMat;
                var pc = pillow.GetComponent<Collider>();
                if (pc != null) Object.DestroyImmediate(pc);
            }
        }

        private static void BuildSolidPiano(Transform parent, Vector3 position, Dictionary<string, Material> mats)
        {
            Material woodMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material walnutMat = GetMaterial(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));
            Material whiteMat = GetMaterial(mats, "ShirtWhite", Color.white);
            Material blackMat = GetMaterial(mats, "MarbleBlack", new Color(0.12f, 0.12f, 0.14f));
            Material brassMat = GetMaterial(mats, "BrassGold", new Color(0.88f, 0.75f, 0.35f));

            var pianoRoot = new GameObject("MusicSalon_GrandPiano");
            pianoRoot.transform.SetParent(parent);
            pianoRoot.transform.position = position;

            var col = pianoRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.75f, 0f);
            col.size = new Vector3(2.4f, 1.5f, 2.6f);
            col.isTrigger = false;

            // 1. Grand Piano Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Piano_Body";
            body.transform.SetParent(pianoRoot.transform);
            body.transform.localPosition = new Vector3(0f, 0.75f, 0.15f);
            body.transform.localScale = new Vector3(2.2f, 0.35f, 2.1f);
            if (body.GetComponent<Renderer>() != null && blackMat != null) body.GetComponent<Renderer>().sharedMaterial = blackMat;
            var bc = body.GetComponent<Collider>();
            if (bc != null) Object.DestroyImmediate(bc);

            // 2. Keyboard Console
            var keys = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keys.name = "Piano_Keys";
            keys.transform.SetParent(pianoRoot.transform);
            keys.transform.localPosition = new Vector3(0f, 0.72f, -0.98f);
            keys.transform.localScale = new Vector3(1.75f, 0.08f, 0.35f);
            if (keys.GetComponent<Renderer>() != null && whiteMat != null) keys.GetComponent<Renderer>().sharedMaterial = whiteMat;
            var kc = keys.GetComponent<Collider>();
            if (kc != null) Object.DestroyImmediate(kc);

            // 3. Sheet Music Stand
            var musicStand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            musicStand.name = "Piano_MusicStand";
            musicStand.transform.SetParent(pianoRoot.transform);
            musicStand.transform.localPosition = new Vector3(0f, 1.05f, -0.75f);
            musicStand.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            musicStand.transform.localScale = new Vector3(0.90f, 0.32f, 0.04f);
            if (musicStand.GetComponent<Renderer>() != null && walnutMat != null) musicStand.GetComponent<Renderer>().sharedMaterial = walnutMat;
            var mc = musicStand.GetComponent<Collider>();
            if (mc != null) Object.DestroyImmediate(mc);

            // 4. Three Heavy Turned Piano Legs with Brass Casters
            Vector3[] pLegs = {
                new Vector3(-0.95f, 0.28f, -0.80f),
                new Vector3(0.95f, 0.28f, -0.80f),
                new Vector3(0f, 0.28f, 1.05f)
            };
            for (int i = 0; i < 3; i++)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = $"Piano_Leg_{i}";
                leg.transform.SetParent(pianoRoot.transform);
                leg.transform.localPosition = pLegs[i];
                leg.transform.localScale = new Vector3(0.16f, 0.28f, 0.16f);
                if (leg.GetComponent<Renderer>() != null && blackMat != null) leg.GetComponent<Renderer>().sharedMaterial = blackMat;
                var lc = leg.GetComponent<Collider>();
                if (lc != null) Object.DestroyImmediate(lc);

                var caster = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                caster.name = "Brass_Caster";
                caster.transform.SetParent(leg.transform);
                caster.transform.localPosition = new Vector3(0f, -0.95f, 0f);
                caster.transform.localScale = new Vector3(1.4f, 0.45f, 1.4f);
                if (caster.GetComponent<Renderer>() != null && brassMat != null) caster.GetComponent<Renderer>().sharedMaterial = brassMat;
                var cc = caster.GetComponent<Collider>();
                if (cc != null) Object.DestroyImmediate(cc);
            }

            // 5. Grand Piano Lyre (Brass Pedals)
            var lyre = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lyre.name = "Piano_LyrePedals";
            lyre.transform.SetParent(pianoRoot.transform);
            lyre.transform.localPosition = new Vector3(0f, 0.28f, -0.40f);
            lyre.transform.localScale = new Vector3(0.24f, 0.40f, 0.08f);
            if (lyre.GetComponent<Renderer>() != null && brassMat != null) lyre.GetComponent<Renderer>().sharedMaterial = brassMat;
            var lyc = lyre.GetComponent<Collider>();
            if (lyc != null) Object.DestroyImmediate(lyc);

            // 6. Matching Upholstered Piano Bench
            var bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "Piano_Bench";
            bench.transform.SetParent(pianoRoot.transform);
            bench.transform.localPosition = new Vector3(0f, 0.46f, -1.65f);
            bench.transform.localScale = new Vector3(1.10f, 0.14f, 0.45f);
            if (bench.GetComponent<Renderer>() != null && walnutMat != null) bench.GetComponent<Renderer>().sharedMaterial = walnutMat;
            var bnc = bench.GetComponent<Collider>();
            if (bnc != null) Object.DestroyImmediate(bnc);

            Vector3[] bLegs = {
                new Vector3(-0.46f, 0.20f, -1.80f),
                new Vector3(0.46f, 0.20f, -1.80f),
                new Vector3(-0.46f, 0.20f, -1.50f),
                new Vector3(0.46f, 0.20f, -1.50f)
            };
            for (int i = 0; i < 4; i++)
            {
                var bleg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bleg.name = $"Bench_Leg_{i}";
                bleg.transform.SetParent(pianoRoot.transform);
                bleg.transform.localPosition = bLegs[i];
                bleg.transform.localScale = new Vector3(0.06f, 0.20f, 0.06f);
                if (bleg.GetComponent<Renderer>() != null && walnutMat != null) bleg.GetComponent<Renderer>().sharedMaterial = walnutMat;
                var blc = bleg.GetComponent<Collider>();
                if (blc != null) Object.DestroyImmediate(blc);
            }
        }

        private static void BuildSolidBilliardsTable(Transform parent, Vector3 position, Dictionary<string, Material> mats)
        {
            Material woodMat = GetMaterial(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material feltMat = GetMaterial(mats, "GreenFelt", new Color(0.12f, 0.45f, 0.22f));
            Material brassMat = GetMaterial(mats, "BrassGold", new Color(0.88f, 0.75f, 0.35f));

            var tableRoot = new GameObject("Billiards_SlateTable");
            tableRoot.transform.SetParent(parent);
            tableRoot.transform.position = position;

            // Solid Master Collider
            var col = tableRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.45f, 0f);
            col.size = new Vector3(2.6f, 0.9f, 4.4f);
            col.isTrigger = false;

            // 1. Heavy Mahogany Cushion Frame
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Table_CushionRails";
            frame.transform.SetParent(tableRoot.transform);
            frame.transform.localPosition = new Vector3(0f, 0.78f, 0f);
            frame.transform.localScale = new Vector3(2.6f, 0.18f, 4.4f);
            if (frame.GetComponent<Renderer>() != null && woodMat != null) frame.GetComponent<Renderer>().sharedMaterial = woodMat;
            var fc = frame.GetComponent<Collider>();
            if (fc != null) Object.DestroyImmediate(fc);

            // 2. Recessed Green Felt Playing Bed
            var bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "GreenFelt_Bed";
            bed.transform.SetParent(tableRoot.transform);
            bed.transform.localPosition = new Vector3(0f, 0.83f, 0f);
            bed.transform.localScale = new Vector3(2.26f, 0.08f, 4.06f);
            if (bed.GetComponent<Renderer>() != null && feltMat != null) bed.GetComponent<Renderer>().sharedMaterial = feltMat;
            var bc = bed.GetComponent<Collider>();
            if (bc != null) Object.DestroyImmediate(bc);

            // 3. Six Heavy Fluted Mahogany Turned Legs with Brass Caps
            float lx = 1.15f;
            float[] lzs = { -1.85f, 0f, 1.85f };
            for (int side = -1; side <= 1; side += 2)
            {
                for (int zIdx = 0; zIdx < 3; zIdx++)
                {
                    var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    leg.name = $"Billiards_Leg_{(side < 0 ? "L" : "R")}_{zIdx}";
                    leg.transform.SetParent(tableRoot.transform);
                    leg.transform.localPosition = new Vector3(side * lx, 0.35f, lzs[zIdx]);
                    leg.transform.localScale = new Vector3(0.24f, 0.35f, 0.24f);
                    if (leg.GetComponent<Renderer>() != null && woodMat != null) leg.GetComponent<Renderer>().sharedMaterial = woodMat;
                    var lc = leg.GetComponent<Collider>();
                    if (lc != null) Object.DestroyImmediate(lc);

                    // Brass ferrule base
                    var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    cap.name = "Leg_Brass_Cap";
                    cap.transform.SetParent(leg.transform);
                    cap.transform.localPosition = new Vector3(0f, -0.85f, 0f);
                    cap.transform.localScale = new Vector3(1.15f, 0.18f, 1.15f);
                    if (cap.GetComponent<Renderer>() != null && brassMat != null) cap.GetComponent<Renderer>().sharedMaterial = brassMat;
                    var cc = cap.GetComponent<Collider>();
                    if (cc != null) Object.DestroyImmediate(cc);
                }
            }
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
            var tableRoot = new GameObject(name);
            tableRoot.transform.SetParent(parent);
            tableRoot.transform.position = position;

            // Solid Master Collider matching table bounding box for physics & character collision
            var col = tableRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, size.y * 0.5f, 0f);
            col.size = size;
            col.isTrigger = false;

            // 1. Tabletop Surface Slab (Thickness: 0.05m at the very top)
            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "Tabletop";
            top.transform.SetParent(tableRoot.transform);
            top.transform.localPosition = new Vector3(0f, size.y - 0.025f, 0f);
            top.transform.localScale = new Vector3(size.x, 0.05f, size.z);
            var tRend = top.GetComponent<Renderer>();
            if (tRend != null && mat != null) tRend.sharedMaterial = mat;
            var tCol = top.GetComponent<Collider>();
            if (tCol != null) Object.DestroyImmediate(tCol);

            // 2. Beveled Apron Skirt
            float apronH = 0.07f;
            var apron = GameObject.CreatePrimitive(PrimitiveType.Cube);
            apron.name = "Table_Apron";
            apron.transform.SetParent(tableRoot.transform);
            apron.transform.localPosition = new Vector3(0f, size.y - 0.05f - apronH * 0.5f, 0f);
            apron.transform.localScale = new Vector3(Mathf.Max(0.1f, size.x - 0.12f), apronH, Mathf.Max(0.1f, size.z - 0.12f));
            var aRend = apron.GetComponent<Renderer>();
            if (aRend != null && mat != null) aRend.sharedMaterial = mat;
            var aCol = apron.GetComponent<Collider>();
            if (aCol != null) Object.DestroyImmediate(aCol);

            // 3. Four Turned / Tapered Legs with Brass Ferrule Feet
            float legH = size.y - 0.05f;
            float legThick = Mathf.Min(0.08f, Mathf.Min(size.x, size.z) * 0.12f);
            float legInsetX = size.x * 0.5f - legThick * 0.9f;
            float legInsetZ = size.z * 0.5f - legThick * 0.9f;
            Vector3[] legPos = {
                new Vector3(-legInsetX, legH * 0.5f, -legInsetZ),
                new Vector3(legInsetX, legH * 0.5f, -legInsetZ),
                new Vector3(-legInsetX, legH * 0.5f, legInsetZ),
                new Vector3(legInsetX, legH * 0.5f, legInsetZ)
            };

            for (int i = 0; i < 4; i++)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = $"Table_Leg_{i}";
                leg.transform.SetParent(tableRoot.transform);
                leg.transform.localPosition = legPos[i];
                leg.transform.localScale = new Vector3(legThick, legH * 0.5f, legThick);
                var lRend = leg.GetComponent<Renderer>();
                if (lRend != null && mat != null) lRend.sharedMaterial = mat;
                var lCol = leg.GetComponent<Collider>();
                if (lCol != null) Object.DestroyImmediate(lCol);

                // Brass foot tip
                var foot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                foot.name = $"Table_Foot_{i}";
                foot.transform.SetParent(tableRoot.transform);
                foot.transform.localPosition = new Vector3(legPos[i].x, 0.03f, legPos[i].z);
                foot.transform.localScale = new Vector3(legThick * 1.15f, 0.03f, legThick * 1.15f);
                var fRend = foot.GetComponent<Renderer>();
                if (fRend != null && mat != null) fRend.sharedMaterial = mat;
                var fCol = foot.GetComponent<Collider>();
                if (fCol != null) Object.DestroyImmediate(fCol);
            }
        }

        private static void BuildSolidStorageUnit(Transform parent, Vector3 position, Vector3 size, Material mat, string name, Quaternion rotation = default)
        {
            var unitRoot = new GameObject(name);
            unitRoot.transform.SetParent(parent);
            unitRoot.transform.position = position;
            if (rotation != default) unitRoot.transform.rotation = rotation;

            // Solid Master Collider matching storage bounding box
            var col = unitRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, size.y * 0.5f, 0f);
            col.size = size;
            col.isTrigger = false;

            bool isBookshelf = name.Contains("Book") || name.Contains("Shelf") || name.Contains("Rack");
            bool isPlanter = name.Contains("Planter");
            bool isBench = name.Contains("Bench");
            bool isFountain = name.Contains("Fountain");
            bool isSofa = name.Contains("Sofa");

            if (isSofa)
            {
                // Art Deco Plush Upholstered Sofa
                var basePlinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                basePlinth.name = "Sofa_Base";
                basePlinth.transform.SetParent(unitRoot.transform);
                basePlinth.transform.localPosition = new Vector3(0f, 0.10f, 0f);
                basePlinth.transform.localScale = new Vector3(size.x, 0.20f, size.z);
                if (basePlinth.GetComponent<Renderer>() != null && mat != null) basePlinth.GetComponent<Renderer>().sharedMaterial = mat;
                var bpCol = basePlinth.GetComponent<Collider>();
                if (bpCol != null) Object.DestroyImmediate(bpCol);

                var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seat.name = "Sofa_SeatCushion";
                seat.transform.SetParent(unitRoot.transform);
                seat.transform.localPosition = new Vector3(0f, 0.32f, 0.04f);
                seat.transform.localScale = new Vector3(size.x * 0.88f, 0.24f, size.z * 0.82f);
                if (seat.GetComponent<Renderer>() != null && mat != null) seat.GetComponent<Renderer>().sharedMaterial = mat;
                var sCol = seat.GetComponent<Collider>();
                if (sCol != null) Object.DestroyImmediate(sCol);

                var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
                back.name = "Sofa_Backrest";
                back.transform.SetParent(unitRoot.transform);
                back.transform.localPosition = new Vector3(0f, size.y * 0.62f, -size.z * 0.38f);
                back.transform.localScale = new Vector3(size.x, size.y * 0.60f, size.z * 0.24f);
                if (back.GetComponent<Renderer>() != null && mat != null) back.GetComponent<Renderer>().sharedMaterial = mat;
                var bCol = back.GetComponent<Collider>();
                if (bCol != null) Object.DestroyImmediate(bCol);

                float armWidth = size.x * 0.12f;
                var armL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                armL.name = "Sofa_Arm_L";
                armL.transform.SetParent(unitRoot.transform);
                armL.transform.localPosition = new Vector3(-size.x * 0.5f + armWidth * 0.5f, size.y * 0.42f, 0f);
                armL.transform.localScale = new Vector3(armWidth, size.y * 0.50f, size.z);
                if (armL.GetComponent<Renderer>() != null && mat != null) armL.GetComponent<Renderer>().sharedMaterial = mat;
                var alCol = armL.GetComponent<Collider>();
                if (alCol != null) Object.DestroyImmediate(alCol);

                var armR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                armR.name = "Sofa_Arm_R";
                armR.transform.SetParent(unitRoot.transform);
                armR.transform.localPosition = new Vector3(size.x * 0.5f - armWidth * 0.5f, size.y * 0.42f, 0f);
                armR.transform.localScale = new Vector3(armWidth, size.y * 0.50f, size.z);
                if (armR.GetComponent<Renderer>() != null && mat != null) armR.GetComponent<Renderer>().sharedMaterial = mat;
                var arCol = armR.GetComponent<Collider>();
                if (arCol != null) Object.DestroyImmediate(arCol);
                return;
            }

            if (isPlanter)
            {
                var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stone.name = "Planter_Urn";
                stone.transform.SetParent(unitRoot.transform);
                stone.transform.localPosition = new Vector3(0f, size.y * 0.4f, 0f);
                stone.transform.localScale = new Vector3(size.x, size.y * 0.8f, size.z);
                if (stone.GetComponent<Renderer>() != null && mat != null) stone.GetComponent<Renderer>().sharedMaterial = mat;
                var stCol = stone.GetComponent<Collider>();
                if (stCol != null) Object.DestroyImmediate(stCol);

                var foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                foliage.name = "Planter_Foliage";
                foliage.transform.SetParent(unitRoot.transform);
                foliage.transform.localPosition = new Vector3(0f, size.y * 0.95f, 0f);
                foliage.transform.localScale = new Vector3(size.x * 1.05f, size.y * 0.55f, size.z * 1.05f);
                var fRend = foliage.GetComponent<Renderer>();
                if (fRend != null && mat != null) fRend.sharedMaterial = mat;
                var folCol = foliage.GetComponent<Collider>();
                if (folCol != null) Object.DestroyImmediate(folCol);
                return;
            }

            if (isBench)
            {
                float benchH = size.y;
                var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seat.name = "Bench_Seat";
                seat.transform.SetParent(unitRoot.transform);
                seat.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                seat.transform.localScale = new Vector3(size.x, 0.10f, size.z);
                if (seat.GetComponent<Renderer>() != null && mat != null) seat.GetComponent<Renderer>().sharedMaterial = mat;
                var sc = seat.GetComponent<Collider>();
                if (sc != null) Object.DestroyImmediate(sc);

                var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
                back.name = "Bench_Backrest";
                back.transform.SetParent(unitRoot.transform);
                back.transform.localPosition = new Vector3(0f, 0.45f + (benchH - 0.45f) * 0.5f, -size.z * 0.45f);
                back.transform.localScale = new Vector3(size.x, benchH - 0.45f, 0.10f);
                if (back.GetComponent<Renderer>() != null && mat != null) back.GetComponent<Renderer>().sharedMaterial = mat;
                var bc = back.GetComponent<Collider>();
                if (bc != null) Object.DestroyImmediate(bc);

                float lx = size.x * 0.45f;
                float lz = size.z * 0.40f;
                Vector3[] bLegs = { new Vector3(-lx, 0.22f, -lz), new Vector3(lx, 0.22f, -lz), new Vector3(-lx, 0.22f, lz), new Vector3(lx, 0.22f, lz) };
                for (int i = 0; i < 4; i++)
                {
                    var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    leg.name = $"Bench_Leg_{i}";
                    leg.transform.SetParent(unitRoot.transform);
                    leg.transform.localPosition = bLegs[i];
                    leg.transform.localScale = new Vector3(0.08f, 0.22f, 0.08f);
                    if (leg.GetComponent<Renderer>() != null && mat != null) leg.GetComponent<Renderer>().sharedMaterial = mat;
                    var lc = leg.GetComponent<Collider>();
                    if (lc != null) Object.DestroyImmediate(lc);
                }
                return;
            }

            if (isFountain)
            {
                var basin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                basin.name = "Fountain_Basin";
                basin.transform.SetParent(unitRoot.transform);
                basin.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                basin.transform.localScale = new Vector3(size.x, 0.4f, size.z);
                if (basin.GetComponent<Renderer>() != null && mat != null) basin.GetComponent<Renderer>().sharedMaterial = mat;
                var bCol = basin.GetComponent<Collider>();
                if (bCol != null) Object.DestroyImmediate(bCol);

                var spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                spire.name = "Fountain_Spire";
                spire.transform.SetParent(unitRoot.transform);
                spire.transform.localPosition = new Vector3(0f, size.y * 0.65f, 0f);
                spire.transform.localScale = new Vector3(0.35f, size.y * 0.35f, 0.35f);
                if (spire.GetComponent<Renderer>() != null && mat != null) spire.GetComponent<Renderer>().sharedMaterial = mat;
                var spCol = spire.GetComponent<Collider>();
                if (spCol != null) Object.DestroyImmediate(spCol);
                return;
            }

            // Default: Architectural Armoire / Credenza / Wardrobe / Bookshelf
            float plinthH = 0.10f;
            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "Unit_Plinth";
            plinth.transform.SetParent(unitRoot.transform);
            plinth.transform.localPosition = new Vector3(0f, plinthH * 0.5f, 0f);
            plinth.transform.localScale = new Vector3(size.x * 0.96f, plinthH, size.z * 0.96f);
            if (plinth.GetComponent<Renderer>() != null && mat != null) plinth.GetComponent<Renderer>().sharedMaterial = mat;
            var pCol = plinth.GetComponent<Collider>();
            if (pCol != null) Object.DestroyImmediate(pCol);

            float crownH = 0.08f;
            var crown = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crown.name = "Unit_Cornice";
            crown.transform.SetParent(unitRoot.transform);
            crown.transform.localPosition = new Vector3(0f, size.y - crownH * 0.5f, 0f);
            crown.transform.localScale = new Vector3(size.x, crownH, size.z);
            if (crown.GetComponent<Renderer>() != null && mat != null) crown.GetComponent<Renderer>().sharedMaterial = mat;
            var cCol = crown.GetComponent<Collider>();
            if (cCol != null) Object.DestroyImmediate(cCol);

            float carcassH = size.y - plinthH - crownH;
            var carcass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carcass.name = "Unit_Carcass";
            carcass.transform.SetParent(unitRoot.transform);
            carcass.transform.localPosition = new Vector3(0f, plinthH + carcassH * 0.5f, 0f);
            carcass.transform.localScale = new Vector3(size.x * 0.94f, carcassH, size.z * 0.94f);
            if (carcass.GetComponent<Renderer>() != null && mat != null) carcass.GetComponent<Renderer>().sharedMaterial = mat;
            var carCol = carcass.GetComponent<Collider>();
            if (carCol != null) Object.DestroyImmediate(carCol);

            if (isBookshelf)
            {
                int shelfCount = Mathf.Max(2, Mathf.RoundToInt(size.y / 0.7f));
                float shelfSpacing = carcassH / (shelfCount + 1);
                for (int s = 1; s <= shelfCount; s++)
                {
                    var shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shelf.name = $"Shelf_Divider_{s}";
                    shelf.transform.SetParent(unitRoot.transform);
                    shelf.transform.localPosition = new Vector3(0f, plinthH + s * shelfSpacing, size.z * 0.05f);
                    shelf.transform.localScale = new Vector3(size.x * 0.92f, 0.04f, size.z * 0.92f);
                    if (shelf.GetComponent<Renderer>() != null && mat != null) shelf.GetComponent<Renderer>().sharedMaterial = mat;
                    var shCol = shelf.GetComponent<Collider>();
                    if (shCol != null) Object.DestroyImmediate(shCol);
                }
            }
            else
            {
                float frontZ = (size.z * 0.94f) * 0.5f + 0.01f;
                float doorW = (size.x * 0.94f) * 0.46f;
                float doorH = carcassH * 0.88f;

                var doorL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                doorL.name = "Door_Panel_L";
                doorL.transform.SetParent(unitRoot.transform);
                doorL.transform.localPosition = new Vector3(-doorW * 0.52f, plinthH + carcassH * 0.5f, frontZ);
                doorL.transform.localScale = new Vector3(doorW, doorH, 0.02f);
                if (doorL.GetComponent<Renderer>() != null && mat != null) doorL.GetComponent<Renderer>().sharedMaterial = mat;
                var dlCol = doorL.GetComponent<Collider>();
                if (dlCol != null) Object.DestroyImmediate(dlCol);

                var doorR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                doorR.name = "Door_Panel_R";
                doorR.transform.SetParent(unitRoot.transform);
                doorR.transform.localPosition = new Vector3(doorW * 0.52f, plinthH + carcassH * 0.5f, frontZ);
                doorR.transform.localScale = new Vector3(doorW, doorH, 0.02f);
                if (doorR.GetComponent<Renderer>() != null && mat != null) doorR.GetComponent<Renderer>().sharedMaterial = mat;
                var drCol = doorR.GetComponent<Collider>();
                if (drCol != null) Object.DestroyImmediate(drCol);

                var handleL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                handleL.name = "Pull_Handle_L";
                handleL.transform.SetParent(unitRoot.transform);
                handleL.transform.localPosition = new Vector3(-0.06f, plinthH + carcassH * 0.5f, frontZ + 0.025f);
                handleL.transform.localScale = new Vector3(0.03f, 0.12f, 0.03f);
                if (handleL.GetComponent<Renderer>() != null && mat != null) handleL.GetComponent<Renderer>().sharedMaterial = mat;
                var hlCol = handleL.GetComponent<Collider>();
                if (hlCol != null) Object.DestroyImmediate(hlCol);

                var handleR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                handleR.name = "Pull_Handle_R";
                handleR.transform.SetParent(unitRoot.transform);
                handleR.transform.localPosition = new Vector3(0.06f, plinthH + carcassH * 0.5f, frontZ + 0.025f);
                handleR.transform.localScale = new Vector3(0.03f, 0.12f, 0.03f);
                if (handleR.GetComponent<Renderer>() != null && mat != null) handleR.GetComponent<Renderer>().sharedMaterial = mat;
                var hrCol = handleR.GetComponent<Collider>();
                if (hrCol != null) Object.DestroyImmediate(hrCol);
            }
        }

        private static void BuildBarStool(Transform parent, Vector3 localPos, Material woodMat, Material brassMat, Material cushionMat, string name)
        {
            var stool = new GameObject(name);
            stool.transform.SetParent(parent, false);
            stool.transform.localPosition = localPos;

            var col = stool.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.45f, 0f);
            col.size = new Vector3(0.5f, 0.9f, 0.5f);
            col.isTrigger = false;

            float legH = 0.70f;
            Vector3[] stoolLegs = {
                new Vector3(-0.16f, legH * 0.5f, -0.16f),
                new Vector3(0.16f, legH * 0.5f, -0.16f),
                new Vector3(-0.16f, legH * 0.5f, 0.16f),
                new Vector3(0.16f, legH * 0.5f, 0.16f)
            };
            for (int i = 0; i < 4; i++)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = $"Stool_Leg_{i}";
                leg.transform.SetParent(stool.transform, false);
                leg.transform.localPosition = stoolLegs[i];
                leg.transform.localScale = new Vector3(0.04f, legH * 0.5f, 0.04f);
                if (leg.GetComponent<Renderer>() != null && woodMat != null) leg.GetComponent<Renderer>().sharedMaterial = woodMat;
                var lc = leg.GetComponent<Collider>();
                if (lc != null) Object.DestroyImmediate(lc);
            }

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Stool_Footring";
            ring.transform.SetParent(stool.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            ring.transform.localScale = new Vector3(0.38f, 0.03f, 0.38f);
            if (ring.GetComponent<Renderer>() != null && brassMat != null) ring.GetComponent<Renderer>().sharedMaterial = brassMat;
            var rc = ring.GetComponent<Collider>();
            if (rc != null) Object.DestroyImmediate(rc);

            var seat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seat.name = "Stool_Seat";
            seat.transform.SetParent(stool.transform, false);
            seat.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            seat.transform.localScale = new Vector3(0.44f, 0.08f, 0.44f);
            if (seat.GetComponent<Renderer>() != null && cushionMat != null) seat.GetComponent<Renderer>().sharedMaterial = cushionMat;
            var sc = seat.GetComponent<Collider>();
            if (sc != null) Object.DestroyImmediate(sc);
        }

        private static void BuildSunLounger(Transform parent, Vector3 localPos, Quaternion localRot, Material frameMat, Material cushionMat, string name)
        {
            var lounger = new GameObject(name);
            lounger.transform.SetParent(parent, false);
            lounger.transform.localPosition = localPos;
            lounger.transform.localRotation = localRot;

            var col = lounger.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.35f, 0f);
            col.size = new Vector3(0.85f, 0.70f, 2.0f);
            col.isTrigger = false;

            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Lounger_Frame";
            frame.transform.SetParent(lounger.transform, false);
            frame.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            frame.transform.localScale = new Vector3(0.80f, 0.12f, 1.95f);
            if (frame.GetComponent<Renderer>() != null && frameMat != null) frame.GetComponent<Renderer>().sharedMaterial = frameMat;
            var fc = frame.GetComponent<Collider>();
            if (fc != null) Object.DestroyImmediate(fc);

            var cushion = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cushion.name = "Lounger_Cushion";
            cushion.transform.SetParent(lounger.transform, false);
            cushion.transform.localPosition = new Vector3(0f, 0.30f, -0.20f);
            cushion.transform.localScale = new Vector3(0.76f, 0.10f, 1.45f);
            if (cushion.GetComponent<Renderer>() != null && cushionMat != null) cushion.GetComponent<Renderer>().sharedMaterial = cushionMat;
            var cc = cushion.GetComponent<Collider>();
            if (cc != null) Object.DestroyImmediate(cc);

            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "Lounger_Backrest";
            back.transform.SetParent(lounger.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.50f, 0.68f);
            back.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            back.transform.localScale = new Vector3(0.76f, 0.10f, 0.65f);
            if (back.GetComponent<Renderer>() != null && cushionMat != null) back.GetComponent<Renderer>().sharedMaterial = cushionMat;
            var bc = back.GetComponent<Collider>();
            if (bc != null) Object.DestroyImmediate(bc);

            Vector3[] lLegs = {
                new Vector3(-0.36f, 0.10f, -0.90f),
                new Vector3(0.36f, 0.10f, -0.90f),
                new Vector3(-0.36f, 0.10f, 0.90f),
                new Vector3(0.36f, 0.10f, 0.90f)
            };
            for (int i = 0; i < 4; i++)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = $"Lounger_Leg_{i}";
                leg.transform.SetParent(lounger.transform, false);
                leg.transform.localPosition = lLegs[i];
                leg.transform.localScale = new Vector3(0.06f, 0.10f, 0.06f);
                if (leg.GetComponent<Renderer>() != null && frameMat != null) leg.GetComponent<Renderer>().sharedMaterial = frameMat;
                var lc = leg.GetComponent<Collider>();
                if (lc != null) Object.DestroyImmediate(lc);
            }
        }

        private static void BuildAstronomicalTelescope(Transform parent, Vector3 localPos, Material brassMat, Material woodMat, string name)
        {
            var scope = new GameObject(name);
            scope.transform.SetParent(parent, false);
            scope.transform.localPosition = localPos;

            var col = scope.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.1f, 0f);
            col.size = new Vector3(1.4f, 2.2f, 1.4f);
            col.isTrigger = false;

            Vector3[] tripodLegs = {
                new Vector3(-0.45f, 0.65f, -0.26f),
                new Vector3(0.45f, 0.65f, -0.26f),
                new Vector3(0f, 0.65f, 0.52f)
            };
            for (int i = 0; i < 3; i++)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = $"Tripod_Leg_{i}";
                leg.transform.SetParent(scope.transform, false);
                leg.transform.localPosition = tripodLegs[i];
                leg.transform.localRotation = Quaternion.Euler(tripodLegs[i].z > 0 ? -12f : 12f, 0f, tripodLegs[i].x < 0 ? 12f : (tripodLegs[i].x > 0 ? -12f : 0f));
                leg.transform.localScale = new Vector3(0.08f, 0.65f, 0.08f);
                if (leg.GetComponent<Renderer>() != null && woodMat != null) leg.GetComponent<Renderer>().sharedMaterial = woodMat;
                var lc = leg.GetComponent<Collider>();
                if (lc != null) Object.DestroyImmediate(lc);
            }

            var mount = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mount.name = "Telescope_Mount";
            mount.transform.SetParent(scope.transform, false);
            mount.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            mount.transform.localScale = new Vector3(0.22f, 0.15f, 0.22f);
            if (mount.GetComponent<Renderer>() != null && brassMat != null) mount.GetComponent<Renderer>().sharedMaterial = brassMat;
            var mc = mount.GetComponent<Collider>();
            if (mc != null) Object.DestroyImmediate(mc);

            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Telescope_Barrel";
            barrel.transform.SetParent(scope.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 1.65f, 0.25f);
            barrel.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.18f, 0.95f, 0.18f);
            if (barrel.GetComponent<Renderer>() != null && brassMat != null) barrel.GetComponent<Renderer>().sharedMaterial = brassMat;
            var bc = barrel.GetComponent<Collider>();
            if (bc != null) Object.DestroyImmediate(bc);

            var lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lens.name = "Telescope_ObjectiveHood";
            lens.transform.SetParent(barrel.transform, false);
            lens.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            lens.transform.localScale = new Vector3(1.25f, 0.22f, 1.25f);
            if (lens.GetComponent<Renderer>() != null && brassMat != null) lens.GetComponent<Renderer>().sharedMaterial = brassMat;
            var lenc = lens.GetComponent<Collider>();
            if (lenc != null) Object.DestroyImmediate(lenc);
        }
    }
}
