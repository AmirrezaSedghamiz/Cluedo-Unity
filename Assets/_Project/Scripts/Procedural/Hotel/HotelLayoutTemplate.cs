using System.Collections.Generic;
using UnityEngine;
using TomorrowsCrime.Procedural.Core;

namespace TomorrowsCrime.Procedural.Hotel
{
    /// <summary>
    /// Master architectural template for Meridian House.
    /// Produces a massive, 4-floor hotel with 10 distinct, individually themed rooms on each floor
    /// (40 rooms total) with seamless physical boundaries, shared architectural partitions,
    /// central spine corridors on all floors, perfectly aligned elevator/stair shafts,
    /// and seed-driven material palettes and lighting atmospheres.
    /// </summary>
    public static class HotelLayoutTemplate
    {
        public static List<RoomDefinition> CreateMeridianHouseLayout(SeededRandom rng = null)
        {
            var rooms = new List<RoomDefinition>();

            // Palette options for seed-based theme variety
            string[] floorMats = { "FloorCheckerMarble", "WoodMahogany", "WoodWalnut", "CarpetBurgundy", "CarpetSapphire", "FloorSlate" };
            string[] wallMats = { "WallpaperEmerald", "WallpaperNavy", "WallpaperBordeaux", "WallpaperArtDecoDamask", "WoodMahogany", "AgedParchment" };

            // ==========================================
            // FLOOR 0: GROUND PUBLIC FLOOR (Y = 0.0 to 4.5)
            // ==========================================

            // 1. Grand Marble Lobby (Central Foyer)
            // Bounds: X [-8, 8], Z [-6, 6] -> Center (0, 2.25, 0), Size (16, 4.5, 12)
            var lobby = CreateRoom(rng, "room_lobby", "Grand Marble Lobby", RoomType.Lobby, 0,
                new Vector3(0f, 2.25f, 0f), new Vector3(16f, 4.5f, 12f),
                "FloorCheckerMarble", "WallpaperEmerald", "WoodMahogany", new Color(1.0f, 0.88f, 0.65f), 3.2f, 18f);
            lobby.AddAnchor("anchor_lobby_center_table", AnchorCategory.Tabletop, new Vector3(0f, 0.85f, 0f));
            lobby.AddAnchor("anchor_lobby_lounge_west", AnchorCategory.FloorCorner, new Vector3(-4f, 0.02f, -2f));
            lobby.AddAnchor("anchor_lobby_lounge_east", AnchorCategory.FloorCorner, new Vector3(4f, 0.02f, -2f));
            lobby.AddAnchor("anchor_lobby_display", AnchorCategory.Tabletop, new Vector3(0f, 0.85f, 3f));
            lobby.AddConnection(new DoorwayConnection("conn_lobby_reception", "room_lobby", "room_reception", new Vector3(0f, 0f, 6f), Vector3.zero, false, null, "Front Reception Archway"));
            lobby.AddConnection(new DoorwayConnection("conn_lobby_dining", "room_lobby", "room_dining", new Vector3(-8f, 0f, 0f), new Vector3(0f, 90f, 0f), false, null, "Dining Hall Grand Archway"));
            lobby.AddConnection(new DoorwayConnection("conn_lobby_corridor", "room_lobby", "room_corridor_ground", new Vector3(8f, 0f, 0f), new Vector3(0f, 90f, 0f), false, null, "East Wing Transport Archway"));
            lobby.AddConnection(new DoorwayConnection("conn_lobby_staircase_arch", "room_lobby", "room_corridor_ground", new Vector3(8f, 0f, -2f), new Vector3(0f, 90f, 0f), false, null, "Grand Staircase Open Archway"));
            lobby.AddConnection(new DoorwayConnection("conn_lobby_bar", "room_lobby", "room_bar_lounge", new Vector3(-3f, 0f, -6f), new Vector3(0f, 180f, 0f), false, null, "Bar Lounge Entrance"));
            lobby.AddConnection(new DoorwayConnection("conn_lobby_luggage", "room_lobby", "room_luggage_storage", new Vector3(5f, 0f, -6f), new Vector3(0f, 180f, 0f), false, null, "Luggage Room Door"));
            rooms.Add(lobby);

            // 2. Front Reception & Key Desk
            // Bounds: X [-8, 8], Z [6, 14] -> Center (0, 2.25, 10), Size (16, 4.5, 8)
            var reception = CreateRoom(rng, "room_reception", "Front Reception & Key Station", RoomType.Reception, 0,
                new Vector3(0f, 2.25f, 10f), new Vector3(16f, 4.5f, 8f),
                "FloorCheckerMarble", "WallpaperEmerald", "WoodMahogany", new Color(0.6f, 0.95f, 0.7f), 2.4f, 12f);
            reception.AddAnchor("anchor_reception_counter", AnchorCategory.Counter, new Vector3(0f, 1.15f, 9.8f));
            reception.AddAnchor("anchor_reception_desk", AnchorCategory.Desk, new Vector3(0f, 0.82f, 11.6f));
            reception.AddAnchor("anchor_reception_key_rack", AnchorCategory.KeyStation, new Vector3(2.5f, 1.5f, 12.2f));
            reception.AddAnchor("anchor_reception_safe", AnchorCategory.Desk, new Vector3(-3.2f, 1.4f, 12.0f));
            rooms.Add(reception);

            // 3. Grand Dining Hall
            // Bounds: X [-22, -8], Z [-6, 6] -> Center (-15, 2.25, 0), Size (14, 4.5, 12)
            var dining = CreateRoom(rng, "room_dining", "Grand Dining Hall", RoomType.DiningRoom, 0,
                new Vector3(-15f, 2.25f, 0f), new Vector3(14f, 4.5f, 12f),
                "WoodMahogany", "WallpaperBordeaux", "WoodMahogany", new Color(1.0f, 0.85f, 0.60f), 2.8f, 16f);
            dining.AddAnchor("anchor_dining_table_north", AnchorCategory.Tabletop, new Vector3(-16f, 0.85f, 3.0f));
            dining.AddAnchor("anchor_dining_table_south", AnchorCategory.Tabletop, new Vector3(-16f, 0.85f, -3.0f));
            dining.AddAnchor("anchor_dining_sideboard", AnchorCategory.Counter, new Vector3(-20.5f, 1.15f, 0f));
            dining.AddConnection(new DoorwayConnection("conn_dining_kitchen", "room_dining", "room_kitchen", new Vector3(-15f, 0f, 6f), Vector3.zero, false, null, "Kitchen Service Door"));
            dining.AddConnection(new DoorwayConnection("conn_dining_conservatory", "room_dining", "room_conservatory", new Vector3(-15f, 0f, -6f), new Vector3(0f, 180f, 0f), false, null, "Conservatory Glass Door"));
            rooms.Add(dining);

            // 4. Kitchen & Scullery
            // Bounds: X [-22, -8], Z [6, 14] -> Center (-15, 2.25, 10), Size (14, 4.5, 8)
            var kitchen = CreateRoom(rng, "room_kitchen", "Hotel Kitchen & Scullery", RoomType.Kitchen, 0,
                new Vector3(-15f, 2.25f, 10f), new Vector3(14f, 4.5f, 8f),
                "FloorSlate", "AgedParchment", "WoodMahogany", new Color(0.95f, 0.95f, 0.95f), 2.5f, 12f);
            kitchen.AddAnchor("anchor_kitchen_prep_counter", AnchorCategory.Counter, new Vector3(-15f, 0.95f, 10.0f));
            kitchen.AddAnchor("anchor_kitchen_stove", AnchorCategory.Counter, new Vector3(-18.5f, 0.95f, 11.5f));
            kitchen.AddAnchor("anchor_kitchen_pantry", AnchorCategory.Shelf, new Vector3(-11.5f, 1.25f, 12.0f));
            rooms.Add(kitchen);

            // 5. Winter Garden Conservatory
            // Bounds: X [-22, -8], Z [-14, -6] -> Center (-15, 2.25, -10), Size (14, 4.5, 8)
            var conservatory = CreateRoom(rng, "room_conservatory", "Winter Garden Conservatory", RoomType.Conservatory, 0,
                new Vector3(-15f, 2.25f, -10f), new Vector3(14f, 4.5f, 8f),
                "FloorCheckerMarble", "VelvetEmerald", "BrassGold", new Color(0.70f, 0.85f, 1.0f), 2.2f, 12f);
            conservatory.AddAnchor("anchor_conservatory_bench", AnchorCategory.FloorCorner, new Vector3(-13f, 0.45f, -11f));
            conservatory.AddAnchor("anchor_conservatory_planter", AnchorCategory.FloorCorner, new Vector3(-19f, 1.0f, -11f));
            rooms.Add(conservatory);

            // 6. The Golden Quill Bar & Lounge
            // Bounds: X [-8, 2], Z [-14, -6] -> Center (-3, 2.25, -10), Size (10, 4.5, 8)
            var barLounge = CreateRoom(rng, "room_bar_lounge", "The Golden Quill Bar & Lounge", RoomType.BarLounge, 0,
                new Vector3(-3f, 2.25f, -10f), new Vector3(10f, 4.5f, 8f),
                "WoodWalnut", "WallpaperNavy", "WoodMahogany", new Color(1.0f, 0.72f, 0.40f), 2.2f, 12f);
            barLounge.AddAnchor("anchor_bar_counter", AnchorCategory.Counter, new Vector3(-4.5f, 1.15f, -13.5f));
            barLounge.AddAnchor("anchor_bar_table", AnchorCategory.Tabletop, new Vector3(-6f, 0.75f, -10f));
            rooms.Add(barLounge);

            // 7. Luggage Storage
            // Bounds: X [2, 8], Z [-14, -6] -> Center (5, 2.25, -10), Size (6, 4.5, 8)
            var luggage = CreateRoom(rng, "room_luggage_storage", "Luggage & Bellhop Storage", RoomType.LuggageStorage, 0,
                new Vector3(5f, 2.25f, -10f), new Vector3(6f, 4.5f, 8f),
                "WoodMahogany", "AgedParchment", "WoodMahogany", new Color(0.95f, 0.88f, 0.75f), 1.8f, 10f);
            luggage.AddAnchor("anchor_luggage_rack", AnchorCategory.ServiceRack, new Vector3(6f, 1.25f, -14f));
            luggage.AddAnchor("anchor_luggage_trunks", AnchorCategory.FloorCorner, new Vector3(6f, 0.02f, -9f));
            rooms.Add(luggage);

            // 8. East Grand Hallway & Vertical Transport Hub (Houses Elevator at (14, 0, 0) and Stairs at (8-11, 0, -2))
            // Bounds: X [8, 16], Z [-6, 6] -> Center (12, 2.25, 0), Size (8, 4.5, 12)
            var corridorGround = CreateRoom(rng, "room_corridor_ground", "East Grand Hallway & Transport Hub", RoomType.GroundCorridor, 0,
                new Vector3(12f, 2.25f, 0f), new Vector3(8f, 4.5f, 12f),
                "WoodMahogany", "WallpaperEmerald", "WoodMahogany", new Color(1.0f, 0.88f, 0.65f), 2.8f, 16f);
            corridorGround.AddAnchor("anchor_corridor_notice_board", AnchorCategory.Wall, new Vector3(15.5f, 1.6f, 0f));
            corridorGround.AddConnection(new DoorwayConnection("conn_corridor_office", "room_corridor_ground", "room_staff_office", new Vector3(16f, 0f, 3f), new Vector3(0f, 90f, 0f), true, "key_staff_office", "Manager's Office Door"));
            corridorGround.AddConnection(new DoorwayConnection("conn_corridor_utility", "room_corridor_ground", "room_utility", new Vector3(16f, 0f, -3f), new Vector3(0f, 90f, 0f), true, "key_utility_room", "Utility Room Door"));
            corridorGround.AddConnection(new DoorwayConnection("conn_lobby_corridor", "room_corridor_ground", "room_lobby", new Vector3(8f, 0f, 0f), new Vector3(0f, 90f, 0f), false, null, "East Wing Transport Archway"));
            corridorGround.AddConnection(new DoorwayConnection("conn_lobby_staircase_arch", "room_corridor_ground", "room_lobby", new Vector3(8f, 0f, -2f), new Vector3(0f, 90f, 0f), false, null, "Grand Staircase Open Archway"));
            rooms.Add(corridorGround);

            // 9. Manager's Executive Office
            // Bounds: X [16, 26], Z [0, 8] -> Center (21, 2.25, 4), Size (10, 4.5, 8)
            var office = CreateRoom(rng, "room_staff_office", "Manager's Executive Office", RoomType.StaffOffice, 0,
                new Vector3(21f, 2.25f, 4f), new Vector3(10f, 4.5f, 8f),
                "WoodWalnut", "WallpaperArtDecoDamask", "WoodMahogany", new Color(0.6f, 0.95f, 0.7f), 2.2f, 12f);
            office.AddAnchor("anchor_office_desk", AnchorCategory.Desk, new Vector3(22f, 0.85f, 4f));
            office.AddAnchor("anchor_office_cabinet", AnchorCategory.Shelf, new Vector3(24.5f, 1.25f, 6.5f));
            office.AddAnchor("anchor_office_safe", AnchorCategory.FloorCorner, new Vector3(24.5f, 0.90f, 1.5f));
            rooms.Add(office);

            // 10. Maintenance & Boiler Utility Room
            // Bounds: X [16, 26], Z [-8, 0] -> Center (21, 2.25, -4), Size (10, 4.5, 8)
            var utility = CreateRoom(rng, "room_utility", "Maintenance & Boiler Utility Room", RoomType.UtilityRoom, 0,
                new Vector3(21f, 2.25f, -4f), new Vector3(10f, 4.5f, 8f),
                "FloorSlate", "AgedParchment", "WoodMahogany", new Color(1.0f, 0.70f, 0.40f), 2.0f, 10f);
            utility.AddAnchor("anchor_utility_workbench", AnchorCategory.Counter, new Vector3(21f, 0.90f, -4f));
            utility.AddAnchor("anchor_utility_breaker", AnchorCategory.Wall, new Vector3(24.5f, 1.6f, -4f));
            utility.AddAnchor("anchor_utility_shelf", AnchorCategory.Shelf, new Vector3(21f, 1.25f, -6.5f));
            rooms.Add(utility);

            // ==========================================
            // FLOOR 1: GUEST SUITES & LIBRARY (Y = 5.0 to 9.5)
            // ==========================================

            // 11. Floor 1 Grand Gallery Corridor (Central Spine)
            // Bounds: X [-4, 16], Z [-3, 3] -> Center (6, 7.25, 0), Size (20, 4.5, 6)
            var upperCorridor = CreateRoom(rng, "room_upper_corridor", "Floor 1 Grand Gallery Corridor", RoomType.UpperLanding, 1,
                new Vector3(6f, 7.25f, 0f), new Vector3(20f, 4.5f, 6f),
                "CarpetBurgundy", "WallpaperEmerald", "WoodMahogany", new Color(1.0f, 0.88f, 0.65f), 2.8f, 16f);
            upperCorridor.AddAnchor("anchor_upper_hall_table", AnchorCategory.Tabletop, new Vector3(4f, 5.85f, 1.5f));
            upperCorridor.AddAnchor("anchor_upper_hall_sconce", AnchorCategory.Wall, new Vector3(0f, 6.6f, 2.85f));
            upperCorridor.AddConnection(new DoorwayConnection("conn_upper_library", "room_upper_corridor", "room_library", new Vector3(-4f, 5f, 0f), new Vector3(0f, 90f, 0f), false, null, "Grand Library Archway"));
            upperCorridor.AddConnection(new DoorwayConnection("conn_upper_room101", "room_upper_corridor", "room_guest_101", new Vector3(1f, 5f, 3f), Vector3.zero, true, "key_room_101", "Room 101 Door"));
            upperCorridor.AddConnection(new DoorwayConnection("conn_upper_room102", "room_upper_corridor", "room_guest_102", new Vector3(10f, 5f, 3f), Vector3.zero, true, "key_room_102", "Room 102 VIP Suite Door"));
            upperCorridor.AddConnection(new DoorwayConnection("conn_upper_room103", "room_upper_corridor", "room_guest_103", new Vector3(1f, 5f, -3f), new Vector3(0f, 180f, 0f), true, "key_room_103", "Room 103 Door"));
            upperCorridor.AddConnection(new DoorwayConnection("conn_upper_housekeeping", "room_upper_corridor", "room_housekeeping", new Vector3(14.5f, 5f, -3f), new Vector3(0f, 180f, 0f), true, "key_housekeeping", "Housekeeping Storage Door"));
            rooms.Add(upperCorridor);

            // 12. Grand Library & Archive
            // Bounds: X [-18, -4], Z [-7, 7] -> Center (-11, 7.25, 0), Size (14, 4.5, 14)
            var library = CreateRoom(rng, "room_library", "Meridian Grand Library & Archive", RoomType.Library, 1,
                new Vector3(-11f, 7.25f, 0f), new Vector3(14f, 4.5f, 14f),
                "CarpetSapphire", "AgedParchment", "WoodMahogany", new Color(0.6f, 0.95f, 0.7f), 2.8f, 16f);
            library.AddAnchor("anchor_library_reading_desk", AnchorCategory.Desk, new Vector3(-16f, 5.85f, 0f));
            library.AddAnchor("anchor_library_bookshelf_north", AnchorCategory.Shelf, new Vector3(-16f, 6.25f, 5.5f));
            library.AddAnchor("anchor_library_bookshelf_south", AnchorCategory.Shelf, new Vector3(-16f, 6.25f, -5.5f));
            library.AddConnection(new DoorwayConnection("conn_upper_card", "room_library", "room_card_room", new Vector3(-11f, 5f, 7f), Vector3.zero, false, null, "Card Room Door"));
            library.AddConnection(new DoorwayConnection("conn_upper_linen", "room_library", "room_linen_storage", new Vector3(-11f, 5f, -7f), new Vector3(0f, 180f, 0f), false, null, "Linen Annex Door"));
            rooms.Add(library);

            // 13. Gentleman's Card Room
            // Bounds: X [-18, -4], Z [7, 14] -> Center (-11, 7.25, 10.5), Size (14, 4.5, 7)
            var cardRoom = CreateRoom(rng, "room_card_room", "Gentleman's Card & Chess Room", RoomType.CardRoom, 1,
                new Vector3(-11f, 7.25f, 10.5f), new Vector3(14f, 4.5f, 7f),
                "GreenFelt", "WallpaperArtDecoDamask", "WoodWalnut", new Color(1.0f, 0.85f, 0.55f), 2.2f, 12f);
            cardRoom.AddAnchor("anchor_card_table", AnchorCategory.Tabletop, new Vector3(-11f, 5.82f, 10.5f));
            rooms.Add(cardRoom);

            // 14. Linen & Towel Depot
            // Bounds: X [-18, -4], Z [-14, -7] -> Center (-11, 7.25, -10.5), Size (14, 4.5, 7)
            var linen = CreateRoom(rng, "room_linen_storage", "Floor 1 Linen & Towel Depot", RoomType.LinenStorage, 1,
                new Vector3(-11f, 7.25f, -10.5f), new Vector3(14f, 4.5f, 7f),
                "WoodWalnut", "AgedParchment", "WoodMahogany", new Color(0.95f, 0.90f, 0.80f), 1.8f, 10f);
            linen.AddAnchor("anchor_linen_rack", AnchorCategory.ServiceRack, new Vector3(-11f, 6.25f, -10.5f));
            rooms.Add(linen);

            // 15. Guest Room 101 (Lord Harrington)
            // Bounds: X [-4, 6], Z [3, 11] -> Center (1, 7.25, 7), Size (10, 4.5, 8)
            var room101 = CreateRoom(rng, "room_guest_101", "Guest Suite 101 - Lord Harrington", RoomType.GuestRoom101, 1,
                new Vector3(1f, 7.25f, 7f), new Vector3(10f, 4.5f, 8f),
                "CarpetBurgundy", "WallpaperNavy", "WoodMahogany", new Color(1.0f, 0.85f, 0.65f), 2.2f, 12f);
            room101.AddAnchor("anchor_room101_desk", AnchorCategory.Desk, new Vector3(3.4f, 5.85f, 9.2f));
            room101.AddAnchor("anchor_room101_nightstand", AnchorCategory.Nightstand, new Vector3(4.0f, 5.68f, 6.5f));
            room101.AddAnchor("anchor_room101_wardrobe", AnchorCategory.Shelf, new Vector3(3.8f, 6.25f, 4.2f));
            rooms.Add(room101);

            // 16. Guest Room 102 (VIP Suite / Murder Scene)
            // Bounds: X [6, 16], Z [3, 11] -> Center (11, 7.25, 7), Size (10, 4.5, 8)
            var room102 = CreateRoom(rng, "room_guest_102", "VIP Suite 102 - Madame Valerie (Crime Scene)", RoomType.GuestRoom102, 1,
                new Vector3(11f, 7.25f, 7f), new Vector3(10f, 4.5f, 8f),
                "CarpetBurgundy", "WallpaperEmerald", "WoodMahogany", new Color(1.0f, 0.80f, 0.55f), 2.6f, 14f);
            room102.AddAnchor("anchor_room102_desk", AnchorCategory.Desk, new Vector3(13.4f, 5.85f, 9.2f));
            room102.AddAnchor("anchor_room102_nightstand", AnchorCategory.Nightstand, new Vector3(14.0f, 5.68f, 6.5f));
            room102.AddAnchor("anchor_room102_wardrobe", AnchorCategory.Shelf, new Vector3(13.8f, 6.25f, 4.2f));
            room102.AddAnchor("anchor_room102_balcony", AnchorCategory.FloorCorner, new Vector3(11f, 5.02f, 10f));
            room102.AddConnection(new DoorwayConnection("conn_room102_room104", "room_guest_102", "room_guest_104", new Vector3(16f, 5f, 7f), new Vector3(0f, 90f, 0f), true, "key_room_104", "Interconnecting Suite Door"));
            rooms.Add(room102);

            // 17. Guest Room 104 (Baroness Dupuis)
            // Bounds: X [16, 26], Z [3, 11] -> Center (21, 7.25, 7), Size (10, 4.5, 8)
            var room104 = CreateRoom(rng, "room_guest_104", "Guest Suite 104 - Baroness Dupuis", RoomType.GuestRoom104, 1,
                new Vector3(21f, 7.25f, 7f), new Vector3(10f, 4.5f, 8f),
                "CarpetBurgundy", "VelvetGold", "WoodMahogany", new Color(1.0f, 0.82f, 0.50f), 2.2f, 10f);
            room104.AddAnchor("anchor_room104_vanity", AnchorCategory.Desk, new Vector3(23.4f, 5.85f, 9.2f));
            room104.AddAnchor("anchor_room104_nightstand", AnchorCategory.Nightstand, new Vector3(24.0f, 5.68f, 6.5f));
            room104.AddAnchor("anchor_room104_luggage", AnchorCategory.FloorCorner, new Vector3(19f, 5.02f, 5.5f));
            rooms.Add(room104);

            // 18. Guest Room 103 (Dr. Sterling)
            // Bounds: X [-4, 6], Z [-11, -3] -> Center (1, 7.25, -7), Size (10, 4.5, 8)
            var room103 = CreateRoom(rng, "room_guest_103", "Guest Suite 103 - Dr. Sterling", RoomType.GuestRoom103, 1,
                new Vector3(1f, 7.25f, -7f), new Vector3(10f, 4.5f, 8f),
                "CarpetSapphire", "WallpaperBordeaux", "WoodMahogany", new Color(0.95f, 0.88f, 0.70f), 2.0f, 12f);
            room103.AddAnchor("anchor_room103_desk", AnchorCategory.Desk, new Vector3(3.4f, 5.85f, -4.8f));
            room103.AddAnchor("anchor_room103_nightstand", AnchorCategory.Nightstand, new Vector3(4.0f, 5.68f, -7.5f));
            room103.AddAnchor("anchor_room103_wardrobe", AnchorCategory.Shelf, new Vector3(3.8f, 6.25f, -9.8f));
            rooms.Add(room103);

            // 19. Housekeeping Storage
            // Bounds: X [6, 16], Z [-11, -3] -> Center (11, 7.25, -7), Size (10, 4.5, 8)
            var housekeeping = CreateRoom(rng, "room_housekeeping", "Housekeeping Storage & Linen Depot", RoomType.HousekeepingStorage, 1,
                new Vector3(11f, 7.25f, -7f), new Vector3(10f, 4.5f, 8f),
                "WoodMahogany", "AgedParchment", "WoodMahogany", new Color(1.0f, 0.85f, 0.65f), 2.0f, 10f);
            housekeeping.AddAnchor("anchor_housekeeping_rack", AnchorCategory.ServiceRack, new Vector3(11f, 6.25f, -9f));
            housekeeping.AddAnchor("anchor_housekeeping_cart", AnchorCategory.FloorCorner, new Vector3(8f, 5.02f, -6f));
            housekeeping.AddAnchor("anchor_housekeeping_shelf", AnchorCategory.Shelf, new Vector3(14f, 6.25f, -7f));
            housekeeping.AddConnection(new DoorwayConnection("conn_housekeeping_room105", "room_housekeeping", "room_guest_105", new Vector3(16f, 5f, -7f), new Vector3(0f, 90f, 0f), true, "key_room_105", "Service Door to 105"));
            rooms.Add(housekeeping);

            // 20. Guest Room 105 (Inspector Cross)
            // Bounds: X [16, 26], Z [-11, -3] -> Center (21, 7.25, -7), Size (10, 4.5, 8)
            var room105 = CreateRoom(rng, "room_guest_105", "Guest Suite 105 - Inspector Cross", RoomType.GuestRoom105, 1,
                new Vector3(21f, 7.25f, -7f), new Vector3(10f, 4.5f, 8f),
                "WoodWalnut", "WallpaperNavy", "WoodMahogany", new Color(0.90f, 0.90f, 0.95f), 2.0f, 10f);
            room105.AddAnchor("anchor_room105_desk", AnchorCategory.Desk, new Vector3(23.4f, 5.85f, -4.8f));
            room105.AddAnchor("anchor_room105_nightstand", AnchorCategory.Nightstand, new Vector3(24.0f, 5.68f, -7.5f));
            room105.AddAnchor("anchor_room105_shelf", AnchorCategory.Shelf, new Vector3(19f, 6.25f, -9f));
            rooms.Add(room105);

            // ==========================================
            // FLOOR 2: DELUXE SUITES & GRAND SALON (Y = 10.0 to 14.5)
            // ==========================================

            // 21. Floor 2 Gallery Promenade (Central Spine)
            // Bounds: X [-4, 16], Z [-3, 3] -> Center (6, 12.25, 0), Size (20, 4.5, 6)
            var floor2Promenade = CreateRoom(rng, "room_floor2_promenade", "Floor 2 Gallery Promenade", RoomType.Floor2Promenade, 2,
                new Vector3(6f, 12.25f, 0f), new Vector3(20f, 4.5f, 6f),
                "CarpetSapphire", "WallpaperArtDecoDamask", "WoodMahogany", new Color(1.0f, 0.88f, 0.65f), 2.8f, 16f);
            floor2Promenade.AddAnchor("anchor_floor2_table", AnchorCategory.Tabletop, new Vector3(4f, 10.85f, 1.5f));
            floor2Promenade.AddConnection(new DoorwayConnection("conn_floor2_billiards", "room_floor2_promenade", "room_billiards", new Vector3(-4f, 10f, 0f), new Vector3(0f, 90f, 0f), false, null, "Billiards Hall Grand Archway"));
            floor2Promenade.AddConnection(new DoorwayConnection("conn_floor2_room201", "room_floor2_promenade", "room_guest_201", new Vector3(1f, 10f, 3f), Vector3.zero, true, "key_room_201", "Deluxe Suite 201 Door"));
            floor2Promenade.AddConnection(new DoorwayConnection("conn_floor2_room202", "room_floor2_promenade", "room_guest_202", new Vector3(10f, 10f, 3f), Vector3.zero, true, "key_room_202", "Deluxe Suite 202 Door"));
            floor2Promenade.AddConnection(new DoorwayConnection("conn_floor2_room203", "room_floor2_promenade", "room_guest_203", new Vector3(1f, 10f, -3f), new Vector3(0f, 180f, 0f), true, "key_room_203", "Deluxe Suite 203 Door"));
            floor2Promenade.AddConnection(new DoorwayConnection("conn_floor2_service", "room_floor2_promenade", "room_service_floor2", new Vector3(14.5f, 10f, -3f), new Vector3(0f, 180f, 0f), false, null, "Floor 2 Service Annex Door"));
            rooms.Add(floor2Promenade);

            // 22. Billiards Hall & Game Room
            // Bounds: X [-18, -4], Z [-7, 7] -> Center (-11, 12.25, 0), Size (14, 4.5, 14)
            var billiards = CreateRoom(rng, "room_billiards", "Billiards Hall & Game Room", RoomType.BilliardsRoom, 2,
                new Vector3(-11f, 12.25f, 0f), new Vector3(14f, 4.5f, 14f),
                "WoodMahogany", "WallpaperEmerald", "WoodMahogany", new Color(1.0f, 0.85f, 0.60f), 2.6f, 14f);
            billiards.AddAnchor("anchor_billiards_table", AnchorCategory.Tabletop, new Vector3(-16f, 10.85f, 0f));
            billiards.AddAnchor("anchor_billiards_cue_rack", AnchorCategory.Wall, new Vector3(-21f, 11.5f, 0f));
            billiards.AddConnection(new DoorwayConnection("conn_floor2_salon", "room_billiards", "room_music_salon", new Vector3(-11f, 10f, 7f), Vector3.zero, false, null, "Music Salon Double Door"));
            billiards.AddConnection(new DoorwayConnection("conn_floor2_cigar", "room_billiards", "room_cigar_lounge", new Vector3(-11f, 10f, -7f), new Vector3(0f, 180f, 0f), false, null, "Cigar Lounge Door"));
            rooms.Add(billiards);

            // 23. Music Salon
            // Bounds: X [-18, -4], Z [7, 14] -> Center (-11, 12.25, 10.5), Size (14, 4.5, 7)
            var musicSalon = CreateRoom(rng, "room_music_salon", "Meridian Grand Music Salon", RoomType.MusicSalon, 2,
                new Vector3(-11f, 12.25f, 10.5f), new Vector3(14f, 4.5f, 7f),
                "WoodMahogany", "WallpaperBordeaux", "WoodWalnut", new Color(1.0f, 0.82f, 0.55f), 2.5f, 14f);
            musicSalon.AddAnchor("anchor_salon_piano", AnchorCategory.FloorCorner, new Vector3(0f, 11.05f, 11f));
            musicSalon.AddAnchor("anchor_salon_music_stand", AnchorCategory.Tabletop, new Vector3(-2f, 10.85f, 11f));
            rooms.Add(musicSalon);

            // 24. Cigar & Cognac Lounge
            // Bounds: X [-18, -4], Z [-14, -7] -> Center (-11, 12.25, -10.5), Size (14, 4.5, 7)
            var cigar = CreateRoom(rng, "room_cigar_lounge", "Cigar & Cognac Lounge", RoomType.CigarLounge, 2,
                new Vector3(-11f, 12.25f, -10.5f), new Vector3(14f, 4.5f, 7f),
                "WoodWalnut", "WallpaperNavy", "WoodMahogany", new Color(1.0f, 0.70f, 0.40f), 2.0f, 12f);
            cigar.AddAnchor("anchor_cigar_humidor", AnchorCategory.Counter, new Vector3(-15f, 11.15f, -11f));
            cigar.AddAnchor("anchor_cigar_chair", AnchorCategory.FloorCorner, new Vector3(-6f, 10.02f, -12f));
            rooms.Add(cigar);

            // 25. Deluxe Suite 201 (Count Rostova)
            // Bounds: X [-4, 6], Z [3, 11] -> Center (1, 12.25, 7), Size (10, 4.5, 8)
            var suite201 = CreateRoom(rng, "room_guest_201", "Deluxe Suite 201 - Count Rostova", RoomType.GuestRoom201, 2,
                new Vector3(1f, 12.25f, 7f), new Vector3(10f, 4.5f, 8f),
                "CarpetBurgundy", "WallpaperEmerald", "WoodMahogany", new Color(1.0f, 0.82f, 0.60f), 2.2f, 12f);
            suite201.AddAnchor("anchor_suite201_bed", AnchorCategory.FloorCorner, new Vector3(2.8f, 10.02f, 6.5f));
            suite201.AddAnchor("anchor_suite201_desk", AnchorCategory.Desk, new Vector3(3.4f, 10.85f, 9.2f));
            suite201.AddAnchor("anchor_suite201_nightstand", AnchorCategory.Nightstand, new Vector3(4.0f, 10.68f, 6.5f));
            rooms.Add(suite201);

            // 26. Deluxe Suite 202 (Lady Vivienne)
            // Bounds: X [6, 16], Z [3, 11] -> Center (11, 12.25, 7), Size (10, 4.5, 8)
            var suite202 = CreateRoom(rng, "room_guest_202", "Deluxe Suite 202 - Lady Vivienne", RoomType.GuestRoom202, 2,
                new Vector3(11f, 12.25f, 7f), new Vector3(10f, 4.5f, 8f),
                "CarpetSapphire", "VelvetGold", "WoodMahogany", new Color(1.0f, 0.85f, 0.65f), 2.4f, 12f);
            suite202.AddAnchor("anchor_suite202_desk", AnchorCategory.Desk, new Vector3(13.4f, 10.85f, 9.2f));
            suite202.AddAnchor("anchor_suite202_nightstand", AnchorCategory.Nightstand, new Vector3(14.0f, 10.68f, 6.5f));
            suite202.AddAnchor("anchor_suite202_vanity", AnchorCategory.Tabletop, new Vector3(13.5f, 10.85f, 9.2f));
            suite202.AddConnection(new DoorwayConnection("conn_suite202_suite204", "room_guest_202", "room_guest_204", new Vector3(16f, 10f, 7f), new Vector3(0f, 90f, 0f), true, "key_room_204", "Connecting Door to 204"));
            rooms.Add(suite202);

            // 27. Deluxe Suite 204 (Captain Blackwood)
            // Bounds: X [16, 26], Z [3, 11] -> Center (21, 12.25, 7), Size (10, 4.5, 8)
            var suite204 = CreateRoom(rng, "room_guest_204", "Deluxe Suite 204 - Captain Blackwood", RoomType.GuestRoom204, 2,
                new Vector3(21f, 12.25f, 7f), new Vector3(10f, 4.5f, 8f),
                "WoodWalnut", "WallpaperNavy", "WoodMahogany", new Color(1.0f, 0.85f, 0.60f), 2.0f, 10f);
            suite204.AddAnchor("anchor_suite204_chart_table", AnchorCategory.Desk, new Vector3(23.4f, 10.85f, 9.2f));
            suite204.AddAnchor("anchor_suite204_nightstand", AnchorCategory.Nightstand, new Vector3(24.0f, 10.68f, 6.5f));
            suite204.AddAnchor("anchor_suite204_luggage", AnchorCategory.FloorCorner, new Vector3(19f, 10.02f, 5.5f));
            rooms.Add(suite204);

            // 28. Deluxe Suite 203 (Professor Aldous)
            // Bounds: X [-4, 6], Z [-11, -3] -> Center (1, 12.25, -7), Size (10, 4.5, 8)
            var suite203 = CreateRoom(rng, "room_guest_203", "Deluxe Suite 203 - Professor Aldous", RoomType.GuestRoom203, 2,
                new Vector3(1f, 12.25f, -7f), new Vector3(10f, 4.5f, 8f),
                "CarpetBurgundy", "AgedParchment", "WoodMahogany", new Color(0.95f, 0.88f, 0.70f), 2.0f, 12f);
            suite203.AddAnchor("anchor_suite203_cabinet", AnchorCategory.Shelf, new Vector3(3.8f, 11.25f, -9.8f));
            suite203.AddAnchor("anchor_suite203_desk", AnchorCategory.Desk, new Vector3(3.4f, 10.85f, -4.8f));
            suite203.AddAnchor("anchor_suite203_nightstand", AnchorCategory.Nightstand, new Vector3(4.0f, 10.68f, -7.5f));
            rooms.Add(suite203);

            // 29. Floor 2 Service Annex
            // Bounds: X [6, 16], Z [-11, -3] -> Center (11, 12.25, -7), Size (10, 4.5, 8)
            var serviceFloor2 = CreateRoom(rng, "room_service_floor2", "Floor 2 Service Annex", RoomType.ServiceAnnexFloor2, 2,
                new Vector3(11f, 12.25f, -7f), new Vector3(10f, 4.5f, 8f),
                "WoodMahogany", "AgedParchment", "WoodMahogany", new Color(0.95f, 0.90f, 0.80f), 1.8f, 10f);
            serviceFloor2.AddAnchor("anchor_service2_cart", AnchorCategory.FloorCorner, new Vector3(11f, 10.02f, -9f));
            serviceFloor2.AddConnection(new DoorwayConnection("conn_service_suite205", "room_service_floor2", "room_guest_205", new Vector3(16f, 10f, -7f), new Vector3(0f, 90f, 0f), true, "key_room_205", "Private Door to 205"));
            rooms.Add(serviceFloor2);

            // 30. Deluxe Suite 205 (Opera Singer Delacroix)
            // Bounds: X [16, 26], Z [-11, -3] -> Center (21, 12.25, -7), Size (10, 4.5, 8)
            var suite205 = CreateRoom(rng, "room_guest_205", "Deluxe Suite 205 - Opera Singer Delacroix", RoomType.GuestRoom205, 2,
                new Vector3(21f, 12.25f, -7f), new Vector3(10f, 4.5f, 8f),
                "CarpetSapphire", "WallpaperBordeaux", "WoodMahogany", new Color(1.0f, 0.75f, 0.55f), 2.2f, 10f);
            suite205.AddAnchor("anchor_suite205_wardrobe", AnchorCategory.Shelf, new Vector3(23.8f, 11.25f, -9.8f));
            suite205.AddAnchor("anchor_suite205_desk", AnchorCategory.Desk, new Vector3(23.4f, 10.85f, -4.8f));
            suite205.AddAnchor("anchor_suite205_nightstand", AnchorCategory.Nightstand, new Vector3(24.0f, 10.68f, -7.5f));
            rooms.Add(suite205);

            // ==========================================
            // FLOOR 3: SKY PENTHOUSE & OBSERVATORY (Y = 15.0 to 19.5)
            // ==========================================

            // 31. Floor 3 Sky Foyer (Central Spine)
            // Bounds: X [-4, 16], Z [-3, 3] -> Center (6, 17.25, 0), Size (20, 4.5, 6)
            var skyFoyer = CreateRoom(rng, "room_floor3_sky_foyer", "Floor 3 Sky Foyer & Grand Landing", RoomType.Floor3SkyFoyer, 3,
                new Vector3(6f, 17.25f, 0f), new Vector3(20f, 4.5f, 6f),
                "FloorCheckerMarble", "WallpaperArtDecoDamask", "BrassGold", new Color(0.85f, 0.90f, 1.0f), 3.0f, 18f);
            skyFoyer.AddAnchor("anchor_sky_foyer_pedestal", AnchorCategory.Tabletop, new Vector3(4f, 15.85f, 1.5f));
            skyFoyer.AddConnection(new DoorwayConnection("conn_sky_penthouse", "room_floor3_sky_foyer", "room_crown_penthouse", new Vector3(-4f, 15f, 0f), new Vector3(0f, 90f, 0f), true, "key_penthouse", "Crown Penthouse Gilded Double Door"));
            skyFoyer.AddConnection(new DoorwayConnection("conn_sky_observatory", "room_floor3_sky_foyer", "room_observatory", new Vector3(1f, 15f, 3f), Vector3.zero, true, "key_observatory", "Clock Tower Observatory Door"));
            skyFoyer.AddConnection(new DoorwayConnection("conn_sky_vip301", "room_floor3_sky_foyer", "room_vip_301", new Vector3(10f, 15f, 3f), Vector3.zero, true, "key_vip_301", "Royal Penthouse 301 Door"));
            skyFoyer.AddConnection(new DoorwayConnection("conn_sky_solarium", "room_floor3_sky_foyer", "room_solarium", new Vector3(1f, 15f, -3f), new Vector3(0f, 180f, 0f), false, null, "Rooftop Solarium French Doors"));
            skyFoyer.AddConnection(new DoorwayConnection("conn_sky_vip302", "room_floor3_sky_foyer", "room_vip_302", new Vector3(14.5f, 15f, -3f), new Vector3(0f, 180f, 0f), true, "key_vip_302", "Royal Penthouse 302 Door"));
            rooms.Add(skyFoyer);

            // 32. Crown Penthouse Grand Salon
            // Bounds: X [-18, -4], Z [-7, 7] -> Center (-11, 17.25, 0), Size (14, 4.5, 14)
            var penthouse = CreateRoom(rng, "room_crown_penthouse", "Crown Penthouse Grand Salon", RoomType.CrownPenthouse, 3,
                new Vector3(-11f, 17.25f, 0f), new Vector3(14f, 4.5f, 14f),
                "CarpetBurgundy", "VelvetGold", "MarbleBlack", new Color(1.0f, 0.85f, 0.50f), 3.0f, 16f);
            penthouse.AddAnchor("anchor_penthouse_bar", AnchorCategory.Counter, new Vector3(-16f, 16.15f, 0f));
            penthouse.AddAnchor("anchor_penthouse_fireplace", AnchorCategory.FloorCorner, new Vector3(-21f, 15.02f, 0f));
            penthouse.AddConnection(new DoorwayConnection("conn_penthouse_bed", "room_crown_penthouse", "room_presidential_bed", new Vector3(-11f, 15f, 7f), Vector3.zero, false, null, "Presidential Bedroom Door"));
            penthouse.AddConnection(new DoorwayConnection("conn_penthouse_study", "room_crown_penthouse", "room_presidential_study", new Vector3(-11f, 15f, -7f), new Vector3(0f, 180f, 0f), false, null, "Presidential Study Door"));
            rooms.Add(penthouse);

            // 33. Presidential Master Bedroom
            // Bounds: X [-18, -4], Z [7, 14] -> Center (-11, 17.25, 10.5), Size (14, 4.5, 7)
            var presBed = CreateRoom(rng, "room_presidential_bed", "Presidential Master Bedroom", RoomType.PresidentialBedroom, 3,
                new Vector3(-11f, 17.25f, 10.5f), new Vector3(14f, 4.5f, 7f),
                "CarpetSapphire", "WallpaperEmerald", "WoodMahogany", new Color(1.0f, 0.82f, 0.60f), 2.2f, 12f);
            presBed.AddAnchor("anchor_presbed_wardrobe", AnchorCategory.Shelf, new Vector3(-8f, 16.25f, 11f));
            presBed.AddAnchor("anchor_presbed_nightstand", AnchorCategory.Nightstand, new Vector3(-8.5f, 15.68f, 10.5f));
            presBed.AddAnchor("anchor_presbed_vanity", AnchorCategory.Tabletop, new Vector3(-5.5f, 15.85f, 12f));
            rooms.Add(presBed);

            // 34. Presidential Private Study
            // Bounds: X [-18, -4], Z [-14, -7] -> Center (-11, 17.25, -10.5), Size (14, 4.5, 7)
            var presStudy = CreateRoom(rng, "room_presidential_study", "Presidential Private Study", RoomType.PresidentialStudy, 3,
                new Vector3(-11f, 17.25f, -10.5f), new Vector3(14f, 4.5f, 7f),
                "WoodWalnut", "WallpaperArtDecoDamask", "WoodWalnut", new Color(0.6f, 0.95f, 0.7f), 2.2f, 12f);
            presStudy.AddAnchor("anchor_presstudy_desk", AnchorCategory.Desk, new Vector3(-16f, 15.85f, -11f));
            rooms.Add(presStudy);

            // 35. Clock Tower Observatory
            // Bounds: X [-4, 6], Z [3, 11] -> Center (1, 17.25, 7), Size (10, 4.5, 8)
            var observatory = CreateRoom(rng, "room_observatory", "Clock Tower Observatory", RoomType.Observatory, 3,
                new Vector3(1f, 17.25f, 7f), new Vector3(10f, 4.5f, 8f),
                "FloorSlate", "WallpaperNavy", "BrassGold", new Color(0.65f, 0.80f, 1.0f), 2.5f, 14f);
            observatory.AddAnchor("anchor_observatory_telescope", AnchorCategory.FloorCorner, new Vector3(0f, 15.02f, 11f));
            rooms.Add(observatory);

            // 36. Royal Penthouse Suite 301
            // Bounds: X [6, 16], Z [3, 11] -> Center (11, 17.25, 7), Size (10, 4.5, 8)
            var vip301 = CreateRoom(rng, "room_vip_301", "Royal Penthouse Suite 301", RoomType.VIPSuite301, 3,
                new Vector3(11f, 17.25f, 7f), new Vector3(10f, 4.5f, 8f),
                "CarpetBurgundy", "VelvetGold", "WoodMahogany", new Color(1.0f, 0.85f, 0.55f), 2.4f, 12f);
            vip301.AddAnchor("anchor_vip301_bed", AnchorCategory.FloorCorner, new Vector3(12.8f, 15.02f, 6.5f));
            vip301.AddAnchor("anchor_vip301_desk", AnchorCategory.Desk, new Vector3(13.4f, 15.85f, 9.2f));
            vip301.AddAnchor("anchor_vip301_nightstand", AnchorCategory.Nightstand, new Vector3(14.0f, 15.68f, 6.5f));
            vip301.AddConnection(new DoorwayConnection("conn_vip301_motor", "room_vip_301", "room_motor_room", new Vector3(16f, 15f, 7f), new Vector3(0f, 90f, 0f), true, "key_motor_room", "Access Door to Motor Room"));
            rooms.Add(vip301);

            // 37. Elevator & Clockwork Motor Room
            // Bounds: X [16, 26], Z [3, 11] -> Center (21, 17.25, 7), Size (10, 4.5, 8)
            var motorRoom = CreateRoom(rng, "room_motor_room", "Elevator & Clockwork Motor Room", RoomType.MotorRoom, 3,
                new Vector3(21f, 17.25f, 7f), new Vector3(10f, 4.5f, 8f),
                "FloorSlate", "AgedParchment", "MarbleBlack", new Color(1.0f, 0.65f, 0.35f), 2.0f, 10f);
            motorRoom.AddAnchor("anchor_motor_switchboard", AnchorCategory.Wall, new Vector3(21f, 16.5f, 8f));
            rooms.Add(motorRoom);

            // 38. Rooftop Solarium & Terrace
            // Bounds: X [-4, 6], Z [-11, -3] -> Center (1, 17.25, -7), Size (10, 4.5, 8)
            var solarium = CreateRoom(rng, "room_solarium", "Rooftop Solarium & Terrace", RoomType.Solarium, 3,
                new Vector3(1f, 17.25f, -7f), new Vector3(10f, 4.5f, 8f),
                "FloorCheckerMarble", "VelvetEmerald", "BrassGold", new Color(0.80f, 0.90f, 1.0f), 2.5f, 12f);
            solarium.AddAnchor("anchor_solarium_fountain", AnchorCategory.FloorCorner, new Vector3(-6f, 15.02f, -14f));
            rooms.Add(solarium);

            // 39. Royal Penthouse Suite 302
            // Bounds: X [6, 16], Z [-11, -3] -> Center (11, 17.25, -7), Size (10, 4.5, 8)
            var vip302 = CreateRoom(rng, "room_vip_302", "Royal Penthouse Suite 302", RoomType.VIPSuite302, 3,
                new Vector3(11f, 17.25f, -7f), new Vector3(10f, 4.5f, 8f),
                "CarpetSapphire", "WallpaperArtDecoDamask", "WoodWalnut", new Color(1.0f, 0.88f, 0.65f), 2.4f, 12f);
            vip302.AddAnchor("anchor_vip302_bed", AnchorCategory.FloorCorner, new Vector3(12.8f, 15.02f, -6.5f));
            vip302.AddAnchor("anchor_vip302_desk", AnchorCategory.Desk, new Vector3(13.4f, 15.85f, -4.8f));
            vip302.AddAnchor("anchor_vip302_nightstand", AnchorCategory.Nightstand, new Vector3(14.0f, 15.68f, -7.5f));
            vip302.AddConnection(new DoorwayConnection("conn_vip302_roof", "room_vip_302", "room_roof_garden", new Vector3(16f, 15f, -7f), new Vector3(0f, 90f, 0f), false, null, "Roof Garden Lookout Door"));
            rooms.Add(vip302);

            // 40. Roof Garden Lookout
            // Bounds: X [16, 26], Z [-11, -3] -> Center (21, 17.25, -7), Size (10, 4.5, 8)
            var roofGarden = CreateRoom(rng, "room_roof_garden", "Meridian Roof Lookout & Garden", RoomType.RoofGarden, 3,
                new Vector3(21f, 17.25f, -7f), new Vector3(10f, 4.5f, 8f),
                "FloorSlate", "VelvetEmerald", "BrassGold", new Color(0.65f, 0.80f, 1.0f), 2.2f, 10f);
            roofGarden.AddAnchor("anchor_roof_telescope", AnchorCategory.FloorCorner, new Vector3(21f, 15.02f, -7f));
            rooms.Add(roofGarden);

            return rooms;
        }

        private static RoomDefinition CreateRoom(SeededRandom rng, string roomId, string displayName, RoomType type, int floor,
            Vector3 center, Vector3 size, string defaultFloorMat, string defaultWallMat, string defaultWainscotMat,
            Color defaultLightColor, float defaultIntensity, float defaultRange)
        {
            Vector3 finalCenter = center;
            Vector3 finalSize = size;

            if (rng != null)
            {
                // For exterior wing rooms, apply subtle seeded depth/width variation to the exterior face
                // without touching any interior partition seams or corridor doorways
                if (type == RoomType.GuestRoom104 || type == RoomType.GuestRoom204 || type == RoomType.MotorRoom ||
                    type == RoomType.StaffOffice || type == RoomType.UtilityRoom)
                {
                    float dx = rng.NextFloat(-0.6f, 0.6f);
                    finalCenter = new Vector3(center.x + dx * 0.5f, center.y, center.z);
                    finalSize = new Vector3(size.x + dx, size.y, size.z);
                }
            }

            var room = new RoomDefinition(roomId, displayName, type, floor, finalCenter, finalSize);

            // Configure Styling
            room.Styling.FloorMatKey = defaultFloorMat;
            room.Styling.WallpaperMatKey = defaultWallMat;
            room.Styling.WainscotMatKey = defaultWainscotMat;
            room.Styling.TrimMatKey = "BrassGold";
            room.Styling.CeilingMatKey = defaultFloorMat.Contains("Marble") ? "WoodMahogany" : "WoodWalnut";
            room.Styling.LightColor = defaultLightColor;
            room.Styling.LightIntensity = defaultIntensity;
            room.Styling.LightRange = defaultRange;

            // Apply seeded theme variations if RNG provided
            if (rng != null)
            {
                // Slight lighting intensity variance (85% to 115%)
                room.Styling.LightIntensity = defaultIntensity * rng.NextFloat(0.85f, 1.15f);
            }

            return room;
        }
    }
}
