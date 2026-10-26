using System.Collections.Generic;
using UnityEngine;

namespace TomorrowsCrime.Procedural.Hotel
{
    /// <summary>
    /// Master architectural template for Meridian House.
    /// Provides 12 interconnected, distinct rooms across 2 floors with explicit
    /// physical bounds, doorways, stairways, and typed placement anchors.
    /// </summary>
    public static class HotelLayoutTemplate
    {
        public static List<RoomDefinition> CreateMeridianHouseLayout()
        {
            var rooms = new List<RoomDefinition>();

            // ==========================================
            // FLOOR 0: GROUND FLOOR (Y = 0.0 to 4.5)
            // ==========================================

            // 1. Lobby
            var lobby = new RoomDefinition("room_lobby", "Grand Lobby", RoomType.Lobby, 0,
                new Vector3(0f, 2.25f, 0f), new Vector3(18f, 4.5f, 14f));
            lobby.AddAnchor("anchor_lobby_center_table", AnchorCategory.Tabletop, new Vector3(0f, 0f, -2f));
            lobby.AddAnchor("anchor_lobby_lounge_west", AnchorCategory.FloorCorner, new Vector3(-4f, 0f, -2f));
            lobby.AddAnchor("anchor_lobby_lounge_east", AnchorCategory.FloorCorner, new Vector3(4f, 0f, -2f));
            lobby.AddAnchor("anchor_lobby_display", AnchorCategory.Tabletop, new Vector3(0f, 0f, 3f));
            lobby.AddConnection(new DoorwayConnection("conn_lobby_dining", "room_lobby", "room_dining", new Vector3(-9f, 0f, 0f), new Vector3(0f, 90f, 0f), false, null, "Dining Room Double Door"));
            lobby.AddConnection(new DoorwayConnection("conn_lobby_corridor", "room_lobby", "room_corridor_ground", new Vector3(9f, 0f, 0f), new Vector3(0f, 90f, 0f), false, null, "East Wing Archway"));
            lobby.AddConnection(new DoorwayConnection("conn_lobby_reception", "room_lobby", "room_reception", new Vector3(0f, 0f, 7f), Vector3.zero, false, null, "Reception Archway"));
            rooms.Add(lobby);

            // 2. Reception
            var reception = new RoomDefinition("room_reception", "Front Reception", RoomType.Reception, 0,
                new Vector3(0f, 2.25f, 9.5f), new Vector3(12f, 4.5f, 5f));
            reception.AddAnchor("anchor_reception_counter", AnchorCategory.Counter, new Vector3(0f, 0f, 9.0f));
            reception.AddAnchor("anchor_reception_desk", AnchorCategory.Desk, new Vector3(0f, 0f, 10.5f));
            reception.AddAnchor("anchor_reception_key_rack", AnchorCategory.KeyStation, new Vector3(2.5f, 1.2f, 11.5f));
            reception.AddAnchor("anchor_reception_safe", AnchorCategory.Desk, new Vector3(-4f, 0f, 11.0f));
            rooms.Add(reception);

            // 3. Dining Room
            var dining = new RoomDefinition("room_dining", "Dining Hall", RoomType.DiningRoom, 0,
                new Vector3(-15f, 2.25f, 0f), new Vector3(12f, 4.5f, 14f));
            dining.AddAnchor("anchor_dining_table_north", AnchorCategory.Tabletop, new Vector3(-15f, 0f, 3f));
            dining.AddAnchor("anchor_dining_table_south", AnchorCategory.Tabletop, new Vector3(-15f, 0f, -3f));
            dining.AddAnchor("anchor_dining_sideboard", AnchorCategory.Counter, new Vector3(-20f, 0f, 0f));
            dining.AddConnection(new DoorwayConnection("conn_dining_kitchen", "room_dining", "room_kitchen", new Vector3(-15f, 0f, 7f), Vector3.zero, false, null, "Kitchen Service Door"));
            rooms.Add(dining);

            // 4. Kitchen
            var kitchen = new RoomDefinition("room_kitchen", "Hotel Kitchen", RoomType.Kitchen, 0,
                new Vector3(-15f, 2.25f, 9.5f), new Vector3(12f, 4.5f, 5f));
            kitchen.AddAnchor("anchor_kitchen_prep_counter", AnchorCategory.Counter, new Vector3(-15f, 0f, 9.5f));
            kitchen.AddAnchor("anchor_kitchen_stove", AnchorCategory.Counter, new Vector3(-19f, 0f, 10.0f));
            kitchen.AddAnchor("anchor_kitchen_pantry", AnchorCategory.Shelf, new Vector3(-11f, 0f, 11.0f));
            rooms.Add(kitchen);

            // 5. Ground Corridor & Staircase
            var corridorGround = new RoomDefinition("room_corridor_ground", "East Service Corridor & Staircase", RoomType.GroundCorridor, 0,
                new Vector3(13f, 2.25f, 0f), new Vector3(8f, 4.5f, 20f));
            corridorGround.AddAnchor("anchor_corridor_notice_board", AnchorCategory.Wall, new Vector3(16.5f, 1.5f, 0f));
            corridorGround.AddConnection(new DoorwayConnection("conn_corridor_office", "room_corridor_ground", "room_staff_office", new Vector3(17f, 0f, 6f), new Vector3(0f, 90f, 0f), true, "key_staff_office", "Manager's Office Door"));
            corridorGround.AddConnection(new DoorwayConnection("conn_corridor_utility", "room_corridor_ground", "room_utility", new Vector3(17f, 0f, -6f), new Vector3(0f, 90f, 0f), true, "key_utility_room", "Utility Room Door"));
            rooms.Add(corridorGround);

            // 6. Staff / Manager's Office
            var office = new RoomDefinition("room_staff_office", "Manager's Office", RoomType.StaffOffice, 0,
                new Vector3(21f, 2.25f, 6f), new Vector3(8f, 4.5f, 8f));
            office.AddAnchor("anchor_office_desk", AnchorCategory.Desk, new Vector3(21f, 0f, 7f));
            office.AddAnchor("anchor_office_cabinet", AnchorCategory.Shelf, new Vector3(24f, 0f, 8f));
            office.AddAnchor("anchor_office_safe", AnchorCategory.FloorCorner, new Vector3(24f, 0f, 3f));
            rooms.Add(office);

            // 7. Utility & Maintenance Room
            var utility = new RoomDefinition("room_utility", "Maintenance & Utility Room", RoomType.UtilityRoom, 0,
                new Vector3(21f, 2.25f, -6f), new Vector3(8f, 4.5f, 8f));
            utility.AddAnchor("anchor_utility_workbench", AnchorCategory.Counter, new Vector3(21f, 0f, -6f));
            utility.AddAnchor("anchor_utility_breaker", AnchorCategory.Wall, new Vector3(24.5f, 1.5f, -6f));
            utility.AddAnchor("anchor_utility_shelf", AnchorCategory.Shelf, new Vector3(21f, 0f, -9.5f));
            rooms.Add(utility);

            // ==========================================
            // FLOOR 1: UPPER FLOOR (Y = 5.0 to 9.5)
            // ==========================================

            // 8. Upper Landing & Corridor
            var upperCorridor = new RoomDefinition("room_upper_corridor", "Second Floor Gallery & Landing", RoomType.UpperLanding, 1,
                new Vector3(3f, 7.25f, 0f), new Vector3(14f, 4.5f, 20f));
            upperCorridor.AddAnchor("anchor_upper_hall_table", AnchorCategory.Tabletop, new Vector3(3f, 5f, 0f));
            upperCorridor.AddAnchor("anchor_upper_hall_sconce", AnchorCategory.Wall, new Vector3(3f, 6.5f, -9f));
            upperCorridor.AddConnection(new DoorwayConnection("conn_upper_room101", "room_upper_corridor", "room_guest_101", new Vector3(-4f, 5f, 6f), new Vector3(0f, 90f, 0f), true, "key_room_101", "Room 101 Door"));
            upperCorridor.AddConnection(new DoorwayConnection("conn_upper_room102", "room_upper_corridor", "room_guest_102", new Vector3(10f, 5f, 6f), new Vector3(0f, 90f, 0f), true, "key_room_102", "Room 102 VIP Suite Door"));
            upperCorridor.AddConnection(new DoorwayConnection("conn_upper_room103", "room_upper_corridor", "room_guest_103", new Vector3(-4f, 5f, -6f), new Vector3(0f, 90f, 0f), true, "key_room_103", "Room 103 Door"));
            upperCorridor.AddConnection(new DoorwayConnection("conn_upper_housekeeping", "room_upper_corridor", "room_housekeeping", new Vector3(10f, 5f, -6f), new Vector3(0f, 90f, 0f), true, "key_housekeeping", "Housekeeping Storage Door"));
            rooms.Add(upperCorridor);

            // 9. Guest Room 101
            var room101 = new RoomDefinition("room_guest_101", "Guest Suite 101", RoomType.GuestRoom101, 1,
                new Vector3(-9f, 7.25f, 6f), new Vector3(10f, 4.5f, 8f));
            room101.AddAnchor("anchor_room101_desk", AnchorCategory.Desk, new Vector3(-12f, 5f, 4f));
            room101.AddAnchor("anchor_room101_nightstand", AnchorCategory.Nightstand, new Vector3(-6f, 5f, 8f));
            room101.AddAnchor("anchor_room101_wardrobe", AnchorCategory.Shelf, new Vector3(-12f, 5f, 8f));
            rooms.Add(room101);

            // 10. Guest Room 102 (VIP Suite / Murder Scene)
            var room102 = new RoomDefinition("room_guest_102", "VIP Suite 102", RoomType.GuestRoom102, 1,
                new Vector3(15f, 7.25f, 6f), new Vector3(10f, 4.5f, 8f));
            room102.AddAnchor("anchor_room102_desk", AnchorCategory.Desk, new Vector3(18f, 5f, 4f));
            room102.AddAnchor("anchor_room102_nightstand", AnchorCategory.Nightstand, new Vector3(12f, 5f, 8f));
            room102.AddAnchor("anchor_room102_wardrobe", AnchorCategory.Shelf, new Vector3(18f, 5f, 8f));
            room102.AddAnchor("anchor_room102_balcony", AnchorCategory.FloorCorner, new Vector3(15f, 5f, 9.5f));
            rooms.Add(room102);

            // 11. Guest Room 103
            var room103 = new RoomDefinition("room_guest_103", "Guest Room 103", RoomType.GuestRoom103, 1,
                new Vector3(-9f, 7.25f, -6f), new Vector3(10f, 4.5f, 8f));
            room103.AddAnchor("anchor_room103_desk", AnchorCategory.Desk, new Vector3(-12f, 5f, -4f));
            room103.AddAnchor("anchor_room103_nightstand", AnchorCategory.Nightstand, new Vector3(-6f, 5f, -8f));
            room103.AddAnchor("anchor_room103_wardrobe", AnchorCategory.Shelf, new Vector3(-12f, 5f, -8f));
            rooms.Add(room103);

            // 12. Housekeeping Storage
            var housekeeping = new RoomDefinition("room_housekeeping", "Housekeeping Storage", RoomType.HousekeepingStorage, 1,
                new Vector3(15f, 7.25f, -6f), new Vector3(10f, 4.5f, 8f));
            housekeeping.AddAnchor("anchor_housekeeping_rack", AnchorCategory.ServiceRack, new Vector3(15f, 5f, -8f));
            housekeeping.AddAnchor("anchor_housekeeping_cart", AnchorCategory.FloorCorner, new Vector3(12f, 5f, -4f));
            housekeeping.AddAnchor("anchor_housekeeping_shelf", AnchorCategory.Shelf, new Vector3(18f, 5f, -6f));
            rooms.Add(housekeeping);

            return rooms;
        }
    }
}
