using System.Collections.Generic;
using UnityEngine;
using TomorrowsCrime.Procedural.Core;

namespace TomorrowsCrime.Procedural.Hotel
{
    /// <summary>
    /// Master architectural template and pseudorandom environment generator for Meridian House.
    /// Produces a massive, 4-floor hotel with 10 distinct, individually themed rooms on each floor
    /// (40 rooms total) with explicit physical bounds, doorway connections, stairways, typed placement anchors,
    /// and seed-driven spatial jitter and visual themes.
    /// </summary>
    public static class HotelLayoutTemplate
    {
        public static List<RoomDefinition> CreateMeridianHouseLayout(SeededRandom rng = null)
        {
            var rooms = new List<RoomDefinition>();

            // Theme palette pools for pseudorandom variation
            string[] floorMats = { "FloorCheckerMarble", "WoodMahogany", "WoodWalnut", "CarpetBurgundy", "CarpetSapphire", "FloorSlate" };
            string[] wallMats = { "WallpaperEmerald", "WallpaperNavy", "WallpaperBordeaux", "WallpaperArtDecoDamask", "WoodMahogany", "AgedParchment" };
            string[] wainscotMats = { "WoodMahogany", "WoodWalnut", "MarbleBlack" };
            Color[] lightColors = {
                new Color(1.0f, 0.88f, 0.65f), // 2700K warm tungsten
                new Color(1.0f, 0.75f, 0.45f), // 2200K amber gaslight
                new Color(0.95f, 0.90f, 0.82f), // 3200K soft ivory
                new Color(0.65f, 0.80f, 1.0f),  // 5500K moonlight
                new Color(0.60f, 0.95f, 0.70f)  // Emerald glow
            };

            // ==========================================
            // FLOOR 0: GROUND FLOOR (Y = 0.0 to 4.5)
            // ==========================================

            // 1. Grand Lobby
            var lobby = CreateRoom(rng, "room_lobby", "Grand Marble Lobby", RoomType.Lobby, 0,
                new Vector3(0f, 2.25f, 0f), new Vector3(18f, 4.5f, 14f),
                "FloorCheckerMarble", "WallpaperEmerald", "WoodMahogany", new Color(1.0f, 0.88f, 0.65f), 3.2f, 18f);
            lobby.AddAnchor("anchor_lobby_center_table", AnchorCategory.Tabletop, new Vector3(0f, 0f, -3f));
            lobby.AddAnchor("anchor_lobby_lounge_west", AnchorCategory.FloorCorner, new Vector3(-4f, 0f, -2f));
            lobby.AddAnchor("anchor_lobby_lounge_east", AnchorCategory.FloorCorner, new Vector3(4f, 0f, -2f));
            lobby.AddAnchor("anchor_lobby_display", AnchorCategory.Tabletop, new Vector3(0f, 0f, 3f));
            lobby.AddConnection(new DoorwayConnection("conn_lobby_reception", "room_lobby", "room_reception", new Vector3(0f, 0f, 7f), Vector3.zero, false, null, "Front Reception Archway"));
            lobby.AddConnection(new DoorwayConnection("conn_lobby_dining", "room_lobby", "room_dining", new Vector3(-9f, 0f, 0f), new Vector3(0f, 90f, 0f), false, null, "Dining Hall Double Door"));
            lobby.AddConnection(new DoorwayConnection("conn_lobby_corridor", "room_lobby", "room_corridor_ground", new Vector3(9f, 0f, 0f), new Vector3(0f, 90f, 0f), false, null, "East Wing Stair Archway"));
            lobby.AddConnection(new DoorwayConnection("conn_lobby_bar", "room_lobby", "room_bar_lounge", new Vector3(-4f, 0f, -7f), Vector3.zero, false, null, "Bar Lounge Entrance"));
            lobby.AddConnection(new DoorwayConnection("conn_lobby_luggage", "room_lobby", "room_luggage_storage", new Vector3(4f, 0f, -7f), Vector3.zero, false, null, "Luggage Room Door"));
            rooms.Add(lobby);

            // 2. Reception
            var reception = CreateRoom(rng, "room_reception", "Front Reception & Key Station", RoomType.Reception, 0,
                new Vector3(0f, 2.25f, 10.5f), new Vector3(12f, 4.5f, 6f),
                "FloorCheckerMarble", "WallpaperEmerald", "WoodMahogany", new Color(0.6f, 0.95f, 0.7f), 2.2f, 10f);
            reception.AddAnchor("anchor_reception_counter", AnchorCategory.Counter, new Vector3(0f, 0f, 9.0f));
            reception.AddAnchor("anchor_reception_desk", AnchorCategory.Desk, new Vector3(0f, 0f, 10.5f));
            reception.AddAnchor("anchor_reception_key_rack", AnchorCategory.KeyStation, new Vector3(2.5f, 1.2f, 11.5f));
            reception.AddAnchor("anchor_reception_safe", AnchorCategory.Desk, new Vector3(-4f, 0f, 11.0f));
            rooms.Add(reception);

            // 3. Dining Hall
            var dining = CreateRoom(rng, "room_dining", "Grand Dining Hall", RoomType.DiningRoom, 0,
                new Vector3(-16f, 2.25f, 0f), new Vector3(12f, 4.5f, 14f),
                "WoodMahogany", "WallpaperBordeaux", "WoodMahogany", new Color(1.0f, 0.85f, 0.60f), 2.8f, 16f);
            dining.AddAnchor("anchor_dining_table_north", AnchorCategory.Tabletop, new Vector3(-16f, 0f, 3f));
            dining.AddAnchor("anchor_dining_table_south", AnchorCategory.Tabletop, new Vector3(-16f, 0f, -3f));
            dining.AddAnchor("anchor_dining_sideboard", AnchorCategory.Counter, new Vector3(-21f, 0f, 0f));
            dining.AddConnection(new DoorwayConnection("conn_dining_kitchen", "room_dining", "room_kitchen", new Vector3(-16f, 0f, 7f), Vector3.zero, false, null, "Kitchen Service Door"));
            dining.AddConnection(new DoorwayConnection("conn_dining_conservatory", "room_dining", "room_conservatory", new Vector3(-16f, 0f, -7f), Vector3.zero, false, null, "Conservatory Glass Door"));
            rooms.Add(dining);

            // 4. Kitchen
            var kitchen = CreateRoom(rng, "room_kitchen", "Hotel Kitchen & Scullery", RoomType.Kitchen, 0,
                new Vector3(-16f, 2.25f, 10.5f), new Vector3(12f, 4.5f, 6f),
                "FloorSlate", "AgedParchment", "WoodMahogany", new Color(0.95f, 0.95f, 0.95f), 2.5f, 12f);
            kitchen.AddAnchor("anchor_kitchen_prep_counter", AnchorCategory.Counter, new Vector3(-16f, 0f, 10.0f));
            kitchen.AddAnchor("anchor_kitchen_stove", AnchorCategory.Counter, new Vector3(-20f, 0f, 11.0f));
            kitchen.AddAnchor("anchor_kitchen_pantry", AnchorCategory.Shelf, new Vector3(-12f, 0f, 12.0f));
            rooms.Add(kitchen);

            // 5. Bar & Lounge
            var barLounge = CreateRoom(rng, "room_bar_lounge", "The Golden Quill Bar & Lounge", RoomType.BarLounge, 0,
                new Vector3(-6f, 2.25f, -13f), new Vector3(10f, 4.5f, 8f),
                "WoodWalnut", "WallpaperNavy", "WoodMahogany", new Color(1.0f, 0.72f, 0.40f), 2.0f, 12f);
            barLounge.AddAnchor("anchor_bar_counter", AnchorCategory.Counter, new Vector3(-6f, 0f, -14f));
            barLounge.AddAnchor("anchor_bar_table", AnchorCategory.Tabletop, new Vector3(-8f, 0f, -11f));
            rooms.Add(barLounge);

            // 6. Winter Garden Conservatory
            var conservatory = CreateRoom(rng, "room_conservatory", "Winter Garden Conservatory", RoomType.Conservatory, 0,
                new Vector3(-16f, 2.25f, -10.5f), new Vector3(12f, 4.5f, 6f),
                "FloorCheckerMarble", "VelvetEmerald", "BrassGold", new Color(0.70f, 0.85f, 1.0f), 2.2f, 12f);
            conservatory.AddAnchor("anchor_conservatory_bench", AnchorCategory.FloorCorner, new Vector3(-16f, 0f, -11f));
            conservatory.AddAnchor("anchor_conservatory_planter", AnchorCategory.FloorCorner, new Vector3(-19f, 0f, -11f));
            rooms.Add(conservatory);

            // 7. Ground Service Corridor & Grand Staircase Hub
            var corridorGround = CreateRoom(rng, "room_corridor_ground", "East Service Corridor & Grand Staircase", RoomType.GroundCorridor, 0,
                new Vector3(13f, 2.25f, 0f), new Vector3(8f, 4.5f, 20f),
                "WoodMahogany", "WallpaperEmerald", "WoodMahogany", new Color(1.0f, 0.88f, 0.65f), 2.6f, 16f);
            corridorGround.AddAnchor("anchor_corridor_notice_board", AnchorCategory.Wall, new Vector3(16.5f, 1.5f, 0f));
            corridorGround.AddConnection(new DoorwayConnection("conn_corridor_office", "room_corridor_ground", "room_staff_office", new Vector3(17f, 0f, 6f), new Vector3(0f, 90f, 0f), true, "key_staff_office", "Manager's Office Door"));
            corridorGround.AddConnection(new DoorwayConnection("conn_corridor_utility", "room_corridor_ground", "room_utility", new Vector3(17f, 0f, -6f), new Vector3(0f, 90f, 0f), true, "key_utility_room", "Utility Room Door"));
            rooms.Add(corridorGround);

            // 8. Staff / Manager's Office
            var office = CreateRoom(rng, "room_staff_office", "Manager's Executive Office", RoomType.StaffOffice, 0,
                new Vector3(22f, 2.25f, 7f), new Vector3(8f, 4.5f, 8f),
                "WoodWalnut", "WallpaperArtDecoDamask", "WoodMahogany", new Color(0.6f, 0.95f, 0.7f), 2.0f, 10f);
            office.AddAnchor("anchor_office_desk", AnchorCategory.Desk, new Vector3(22f, 0f, 8f));
            office.AddAnchor("anchor_office_cabinet", AnchorCategory.Shelf, new Vector3(25f, 0f, 8f));
            office.AddAnchor("anchor_office_safe", AnchorCategory.FloorCorner, new Vector3(25f, 0f, 4f));
            rooms.Add(office);

            // 9. Utility & Boiler Room
            var utility = CreateRoom(rng, "room_utility", "Maintenance & Boiler Utility Room", RoomType.UtilityRoom, 0,
                new Vector3(22f, 2.25f, -7f), new Vector3(8f, 4.5f, 8f),
                "FloorSlate", "AgedParchment", "WoodMahogany", new Color(1.0f, 0.70f, 0.40f), 2.0f, 10f);
            utility.AddAnchor("anchor_utility_workbench", AnchorCategory.Counter, new Vector3(22f, 0f, -7f));
            utility.AddAnchor("anchor_utility_breaker", AnchorCategory.Wall, new Vector3(25.5f, 1.5f, -7f));
            utility.AddAnchor("anchor_utility_shelf", AnchorCategory.Shelf, new Vector3(22f, 0f, -10.0f));
            rooms.Add(utility);

            // 10. Luggage Storage
            var luggage = CreateRoom(rng, "room_luggage_storage", "Luggage & Bellhop Storage", RoomType.LuggageStorage, 0,
                new Vector3(6f, 2.25f, -13f), new Vector3(10f, 4.5f, 8f),
                "WoodMahogany", "AgedParchment", "WoodMahogany", new Color(0.95f, 0.88f, 0.75f), 1.8f, 10f);
            luggage.AddAnchor("anchor_luggage_rack", AnchorCategory.ServiceRack, new Vector3(6f, 0f, -14f));
            luggage.AddAnchor("anchor_luggage_trunks", AnchorCategory.FloorCorner, new Vector3(8f, 0f, -11f));
            rooms.Add(luggage);

            // ==========================================
            // FLOOR 1: FIRST FLOOR (Y = 5.0 to 9.5)
            // ==========================================

            // 11. Upper Landing / Floor 1 Gallery
            var upperCorridor = CreateRoom(rng, "room_upper_corridor", "Floor 1 Gallery & Stair Landing", RoomType.UpperLanding, 1,
                new Vector3(3f, 7.25f, 0f), new Vector3(14f, 4.5f, 20f),
                "CarpetBurgundy", "WallpaperEmerald", "WoodMahogany", new Color(1.0f, 0.88f, 0.65f), 2.8f, 16f);
            upperCorridor.AddAnchor("anchor_upper_hall_table", AnchorCategory.Tabletop, new Vector3(3f, 5f, 0f));
            upperCorridor.AddAnchor("anchor_upper_hall_sconce", AnchorCategory.Wall, new Vector3(3f, 6.5f, -9f));
            upperCorridor.AddConnection(new DoorwayConnection("conn_upper_library", "room_upper_corridor", "room_library", new Vector3(-4f, 5f, 0f), new Vector3(0f, 90f, 0f), false, null, "Library Archway"));
            upperCorridor.AddConnection(new DoorwayConnection("conn_upper_room101", "room_upper_corridor", "room_guest_101", new Vector3(-8f, 5f, 6f), new Vector3(0f, 90f, 0f), true, "key_room_101", "Room 101 Door"));
            upperCorridor.AddConnection(new DoorwayConnection("conn_upper_room102", "room_upper_corridor", "room_guest_102", new Vector3(10f, 5f, 6f), new Vector3(0f, 90f, 0f), true, "key_room_102", "Room 102 VIP Suite Door"));
            upperCorridor.AddConnection(new DoorwayConnection("conn_upper_room103", "room_upper_corridor", "room_guest_103", new Vector3(-8f, 5f, -6f), new Vector3(0f, 90f, 0f), true, "key_room_103", "Room 103 Door"));
            upperCorridor.AddConnection(new DoorwayConnection("conn_upper_housekeeping", "room_upper_corridor", "room_housekeeping", new Vector3(10f, 5f, -6f), new Vector3(0f, 90f, 0f), true, "key_housekeeping", "Housekeeping Storage Door"));
            upperCorridor.AddConnection(new DoorwayConnection("conn_upper_card", "room_upper_corridor", "room_card_room", new Vector3(-4f, 5f, -7f), Vector3.zero, false, null, "Card Room Door"));
            upperCorridor.AddConnection(new DoorwayConnection("conn_upper_linen", "room_upper_corridor", "room_linen_storage", new Vector3(4f, 5f, -7f), Vector3.zero, false, null, "Linen Annex Door"));
            rooms.Add(upperCorridor);

            // 12. Library
            var library = CreateRoom(rng, "room_library", "Meridian Grand Library & Archive", RoomType.Library, 1,
                new Vector3(-16f, 7.25f, 0f), new Vector3(12f, 4.5f, 14f),
                "CarpetSapphire", "AgedParchment", "WoodMahogany", new Color(0.6f, 0.95f, 0.7f), 2.5f, 16f);
            library.AddAnchor("anchor_library_reading_desk", AnchorCategory.Desk, new Vector3(-16f, 5f, 0f));
            library.AddAnchor("anchor_library_bookshelf_north", AnchorCategory.Shelf, new Vector3(-16f, 5f, 6f));
            library.AddAnchor("anchor_library_bookshelf_south", AnchorCategory.Shelf, new Vector3(-16f, 5f, -6f));
            rooms.Add(library);

            // 13. Card Room
            var cardRoom = CreateRoom(rng, "room_card_room", "Gentleman's Card & Chess Room", RoomType.CardRoom, 1,
                new Vector3(-6f, 7.25f, -13f), new Vector3(10f, 4.5f, 8f),
                "GreenFelt", "WallpaperArtDecoDamask", "WoodWalnut", new Color(1.0f, 0.85f, 0.55f), 2.2f, 12f);
            cardRoom.AddAnchor("anchor_card_table", AnchorCategory.Tabletop, new Vector3(-6f, 5f, -13f));
            rooms.Add(cardRoom);

            // 14. Linen Storage
            var linen = CreateRoom(rng, "room_linen_storage", "Floor 1 Housekeeping & Linen Annex", RoomType.LinenStorage, 1,
                new Vector3(6f, 7.25f, -13f), new Vector3(10f, 4.5f, 8f),
                "WoodWalnut", "AgedParchment", "WoodMahogany", new Color(0.95f, 0.90f, 0.80f), 1.8f, 10f);
            linen.AddAnchor("anchor_linen_rack", AnchorCategory.ServiceRack, new Vector3(6f, 5f, -14f));
            rooms.Add(linen);

            // 15. Guest Room 101
            var room101 = CreateRoom(rng, "room_guest_101", "Guest Suite 101 - Lord Harrington", RoomType.GuestRoom101, 1,
                new Vector3(-16f, 7.25f, 10.5f), new Vector3(12f, 4.5f, 6f),
                "CarpetBurgundy", "WallpaperNavy", "WoodMahogany", new Color(1.0f, 0.85f, 0.65f), 2.2f, 12f);
            room101.AddAnchor("anchor_room101_desk", AnchorCategory.Desk, new Vector3(-18f, 5f, 10f));
            room101.AddAnchor("anchor_room101_nightstand", AnchorCategory.Nightstand, new Vector3(-13f, 5f, 12f));
            room101.AddAnchor("anchor_room101_wardrobe", AnchorCategory.Shelf, new Vector3(-19f, 5f, 12f));
            rooms.Add(room101);

            // 16. Guest Room 102 (VIP Suite / Murder Scene)
            var room102 = CreateRoom(rng, "room_guest_102", "VIP Suite 102 - Madame Valerie (Crime Scene)", RoomType.GuestRoom102, 1,
                new Vector3(16f, 7.25f, 7f), new Vector3(10f, 4.5f, 8f),
                "CarpetBurgundy", "WallpaperEmerald", "WoodMahogany", new Color(1.0f, 0.80f, 0.55f), 2.6f, 14f);
            room102.AddAnchor("anchor_room102_desk", AnchorCategory.Desk, new Vector3(19f, 5f, 5f));
            room102.AddAnchor("anchor_room102_nightstand", AnchorCategory.Nightstand, new Vector3(13f, 5f, 9f));
            room102.AddAnchor("anchor_room102_wardrobe", AnchorCategory.Shelf, new Vector3(19f, 5f, 9f));
            room102.AddAnchor("anchor_room102_balcony", AnchorCategory.FloorCorner, new Vector3(16f, 5f, 10.5f));
            room102.AddConnection(new DoorwayConnection("conn_room102_room104", "room_guest_102", "room_guest_104", new Vector3(21f, 5f, 7f), new Vector3(0f, 90f, 0f), true, "key_room_104", "Interconnecting Suite Door"));
            rooms.Add(room102);

            // 17. Guest Room 103
            var room103 = CreateRoom(rng, "room_guest_103", "Guest Suite 103 - Dr. Sterling", RoomType.GuestRoom103, 1,
                new Vector3(-16f, 7.25f, -10.5f), new Vector3(12f, 4.5f, 6f),
                "CarpetSapphire", "WallpaperBordeaux", "WoodMahogany", new Color(0.95f, 0.88f, 0.70f), 2.0f, 12f);
            room103.AddAnchor("anchor_room103_desk", AnchorCategory.Desk, new Vector3(-18f, 5f, -10f));
            room103.AddAnchor("anchor_room103_nightstand", AnchorCategory.Nightstand, new Vector3(-13f, 5f, -12f));
            room103.AddAnchor("anchor_room103_wardrobe", AnchorCategory.Shelf, new Vector3(-19f, 5f, -12f));
            rooms.Add(room103);

            // 18. Guest Room 104
            var room104 = CreateRoom(rng, "room_guest_104", "Guest Suite 104 - Baroness Dupuis", RoomType.GuestRoom104, 1,
                new Vector3(25f, 7.25f, 7f), new Vector3(8f, 4.5f, 8f),
                "CarpetBurgundy", "VelvetGold", "WoodMahogany", new Color(1.0f, 0.82f, 0.50f), 2.2f, 10f);
            room104.AddAnchor("anchor_room104_vanity", AnchorCategory.Desk, new Vector3(25f, 5f, 8f));
            rooms.Add(room104);

            // 19. Guest Room 105
            var room105 = CreateRoom(rng, "room_guest_105", "Guest Suite 105 - Inspector Cross", RoomType.GuestRoom105, 1,
                new Vector3(25f, 7.25f, -7f), new Vector3(8f, 4.5f, 8f),
                "WoodWalnut", "WallpaperNavy", "WoodMahogany", new Color(0.90f, 0.90f, 0.95f), 2.0f, 10f);
            room105.AddAnchor("anchor_room105_desk", AnchorCategory.Desk, new Vector3(25f, 5f, -7f));
            rooms.Add(room105);

            // 20. Housekeeping Storage
            var housekeeping = CreateRoom(rng, "room_housekeeping", "Housekeeping Storage & Linen Depot", RoomType.HousekeepingStorage, 1,
                new Vector3(16f, 7.25f, -7f), new Vector3(10f, 4.5f, 8f),
                "WoodMahogany", "AgedParchment", "WoodMahogany", new Color(1.0f, 0.85f, 0.65f), 2.0f, 10f);
            housekeeping.AddAnchor("anchor_housekeeping_rack", AnchorCategory.ServiceRack, new Vector3(16f, 5f, -9f));
            housekeeping.AddAnchor("anchor_housekeeping_cart", AnchorCategory.FloorCorner, new Vector3(13f, 5f, -5f));
            housekeeping.AddAnchor("anchor_housekeeping_shelf", AnchorCategory.Shelf, new Vector3(19f, 5f, -7f));
            housekeeping.AddConnection(new DoorwayConnection("conn_housekeeping_room105", "room_housekeeping", "room_guest_105", new Vector3(21f, 5f, -7f), new Vector3(0f, 90f, 0f), true, "key_room_105", "Service Door to 105"));
            rooms.Add(housekeeping);

            // ==========================================
            // FLOOR 2: SECOND FLOOR (Y = 10.0 to 14.5)
            // ==========================================

            // 21. Floor 2 Promenade
            var floor2Promenade = CreateRoom(rng, "room_floor2_promenade", "Floor 2 Promenade & Grand Landing", RoomType.Floor2Promenade, 2,
                new Vector3(3f, 12.25f, 0f), new Vector3(14f, 4.5f, 20f),
                "CarpetSapphire", "WallpaperArtDecoDamask", "WoodMahogany", new Color(1.0f, 0.88f, 0.65f), 2.8f, 16f);
            floor2Promenade.AddAnchor("anchor_floor2_table", AnchorCategory.Tabletop, new Vector3(3f, 10f, 0f));
            floor2Promenade.AddConnection(new DoorwayConnection("conn_floor2_salon", "room_floor2_promenade", "room_music_salon", new Vector3(0f, 10f, 7f), Vector3.zero, false, null, "Music Salon Double Door"));
            floor2Promenade.AddConnection(new DoorwayConnection("conn_floor2_billiards", "room_floor2_promenade", "room_billiards", new Vector3(-4f, 10f, 0f), new Vector3(0f, 90f, 0f), false, null, "Billiards Hall Archway"));
            floor2Promenade.AddConnection(new DoorwayConnection("conn_floor2_cigar", "room_floor2_promenade", "room_cigar_lounge", new Vector3(-4f, 10f, -7f), Vector3.zero, false, null, "Cigar Lounge Door"));
            floor2Promenade.AddConnection(new DoorwayConnection("conn_floor2_service", "room_floor2_promenade", "room_service_floor2", new Vector3(4f, 10f, -7f), Vector3.zero, false, null, "Floor 2 Service Annex Door"));
            floor2Promenade.AddConnection(new DoorwayConnection("conn_floor2_room201", "room_floor2_promenade", "room_guest_201", new Vector3(-8f, 10f, 6f), new Vector3(0f, 90f, 0f), true, "key_room_201", "Deluxe Suite 201 Door"));
            floor2Promenade.AddConnection(new DoorwayConnection("conn_floor2_room202", "room_floor2_promenade", "room_guest_202", new Vector3(10f, 10f, 6f), new Vector3(0f, 90f, 0f), true, "key_room_202", "Deluxe Suite 202 Door"));
            floor2Promenade.AddConnection(new DoorwayConnection("conn_floor2_room203", "room_floor2_promenade", "room_guest_203", new Vector3(-8f, 10f, -6f), new Vector3(0f, 90f, 0f), true, "key_room_203", "Deluxe Suite 203 Door"));
            floor2Promenade.AddConnection(new DoorwayConnection("conn_floor2_room204", "room_floor2_promenade", "room_guest_204", new Vector3(10f, 10f, -6f), new Vector3(0f, 90f, 0f), true, "key_room_204", "Deluxe Suite 204 Door"));
            rooms.Add(floor2Promenade);

            // 22. Music Salon
            var musicSalon = CreateRoom(rng, "room_music_salon", "Meridian Grand Music Salon", RoomType.MusicSalon, 2,
                new Vector3(0f, 12.25f, 10.5f), new Vector3(12f, 4.5f, 6f),
                "WoodMahogany", "WallpaperBordeaux", "WoodWalnut", new Color(1.0f, 0.82f, 0.55f), 2.5f, 14f);
            musicSalon.AddAnchor("anchor_salon_piano", AnchorCategory.FloorCorner, new Vector3(0f, 10f, 11f));
            musicSalon.AddAnchor("anchor_salon_music_stand", AnchorCategory.Tabletop, new Vector3(2f, 10f, 11f));
            rooms.Add(musicSalon);

            // 23. Billiards Room
            var billiards = CreateRoom(rng, "room_billiards", "Billiards & Game Hall", RoomType.BilliardsRoom, 2,
                new Vector3(-16f, 12.25f, 0f), new Vector3(12f, 4.5f, 14f),
                "WoodWalnut", "WallpaperNavy", "WoodMahogany", new Color(0.6f, 0.95f, 0.7f), 2.8f, 16f);
            billiards.AddAnchor("anchor_billiards_table", AnchorCategory.Tabletop, new Vector3(-16f, 10f, 0f));
            billiards.AddAnchor("anchor_billiards_cue_rack", AnchorCategory.Wall, new Vector3(-21f, 11.5f, 0f));
            rooms.Add(billiards);

            // 24. Cigar Lounge
            var cigar = CreateRoom(rng, "room_cigar_lounge", "Velvet Cigar & Cognac Den", RoomType.CigarLounge, 2,
                new Vector3(-6f, 12.25f, -13f), new Vector3(10f, 4.5f, 8f),
                "WoodMahogany", "WallpaperArtDecoDamask", "MarbleBlack", new Color(1.0f, 0.70f, 0.40f), 2.0f, 12f);
            cigar.AddAnchor("anchor_cigar_fireplace", AnchorCategory.FloorCorner, new Vector3(-6f, 10f, -15f));
            rooms.Add(cigar);

            // 25. Service Annex Floor 2
            var service2 = CreateRoom(rng, "room_service_floor2", "Floor 2 Service Annex & Dumbwaiter", RoomType.ServiceAnnexFloor2, 2,
                new Vector3(6f, 12.25f, -13f), new Vector3(10f, 4.5f, 8f),
                "FloorSlate", "AgedParchment", "WoodMahogany", new Color(0.95f, 0.90f, 0.80f), 1.8f, 10f);
            service2.AddAnchor("anchor_service2_dumbwaiter", AnchorCategory.Counter, new Vector3(6f, 10f, -14f));
            rooms.Add(service2);

            // 26. Deluxe Suite 201
            var suite201 = CreateRoom(rng, "room_guest_201", "Deluxe Suite 201 - Ambassador Vance", RoomType.GuestRoom201, 2,
                new Vector3(-16f, 12.25f, 10.5f), new Vector3(12f, 4.5f, 6f),
                "CarpetSapphire", "WallpaperEmerald", "WoodMahogany", new Color(1.0f, 0.85f, 0.65f), 2.2f, 12f);
            suite201.AddAnchor("anchor_suite201_desk", AnchorCategory.Desk, new Vector3(-18f, 10f, 10f));
            rooms.Add(suite201);

            // 27. Deluxe Suite 202
            var suite202 = CreateRoom(rng, "room_guest_202", "Deluxe Suite 202 - Countess Rostov", RoomType.GuestRoom202, 2,
                new Vector3(16f, 12.25f, 7f), new Vector3(10f, 4.5f, 8f),
                "CarpetBurgundy", "VelvetGold", "WoodWalnut", new Color(1.0f, 0.80f, 0.50f), 2.4f, 12f);
            suite202.AddAnchor("anchor_suite202_vanity", AnchorCategory.Desk, new Vector3(19f, 10f, 7f));
            suite202.AddConnection(new DoorwayConnection("conn_suite202_suite204", "room_guest_202", "room_guest_204", new Vector3(21f, 10f, 7f), new Vector3(0f, 90f, 0f), true, "key_room_204", "Connecting Door to 204"));
            rooms.Add(suite202);

            // 28. Deluxe Suite 203
            var suite203 = CreateRoom(rng, "room_guest_203", "Deluxe Suite 203 - Professor Aldous", RoomType.GuestRoom203, 2,
                new Vector3(-16f, 12.25f, -10.5f), new Vector3(12f, 4.5f, 6f),
                "CarpetBurgundy", "AgedParchment", "WoodMahogany", new Color(0.95f, 0.88f, 0.70f), 2.0f, 12f);
            suite203.AddAnchor("anchor_suite203_cabinet", AnchorCategory.Shelf, new Vector3(-19f, 10f, -11f));
            rooms.Add(suite203);

            // 29. Deluxe Suite 204
            var suite204 = CreateRoom(rng, "room_guest_204", "Deluxe Suite 204 - Captain Blackwood", RoomType.GuestRoom204, 2,
                new Vector3(25f, 12.25f, 7f), new Vector3(8f, 4.5f, 8f),
                "WoodWalnut", "WallpaperNavy", "WoodMahogany", new Color(1.0f, 0.85f, 0.60f), 2.0f, 10f);
            suite204.AddAnchor("anchor_suite204_chart_table", AnchorCategory.Desk, new Vector3(25f, 10f, 8f));
            rooms.Add(suite204);

            // 30. Deluxe Suite 205
            var suite205 = CreateRoom(rng, "room_guest_205", "Deluxe Suite 205 - Opera Singer Delacroix", RoomType.GuestRoom205, 2,
                new Vector3(25f, 12.25f, -7f), new Vector3(8f, 4.5f, 8f),
                "CarpetSapphire", "WallpaperBordeaux", "WoodMahogany", new Color(1.0f, 0.75f, 0.55f), 2.2f, 10f);
            suite205.AddAnchor("anchor_suite205_wardrobe", AnchorCategory.Shelf, new Vector3(25f, 10f, -7f));
            rooms.Add(suite205);

            // ==========================================
            // FLOOR 3: THIRD FLOOR (Y = 15.0 to 19.5)
            // ==========================================

            // 31. Floor 3 Sky Foyer
            var skyFoyer = CreateRoom(rng, "room_floor3_sky_foyer", "Floor 3 Sky Foyer & Grand Landing", RoomType.Floor3SkyFoyer, 3,
                new Vector3(3f, 17.25f, 0f), new Vector3(14f, 4.5f, 20f),
                "FloorCheckerMarble", "WallpaperArtDecoDamask", "BrassGold", new Color(0.85f, 0.90f, 1.0f), 3.0f, 18f);
            skyFoyer.AddAnchor("anchor_sky_foyer_pedestal", AnchorCategory.Tabletop, new Vector3(3f, 15f, 0f));
            skyFoyer.AddConnection(new DoorwayConnection("conn_sky_observatory", "room_floor3_sky_foyer", "room_observatory", new Vector3(0f, 15f, 7f), Vector3.zero, true, "key_observatory", "Clock Tower Observatory Door"));
            skyFoyer.AddConnection(new DoorwayConnection("conn_sky_penthouse", "room_floor3_sky_foyer", "room_crown_penthouse", new Vector3(-4f, 15f, 0f), new Vector3(0f, 90f, 0f), true, "key_penthouse", "Crown Penthouse Gilded Double Door"));
            skyFoyer.AddConnection(new DoorwayConnection("conn_sky_solarium", "room_floor3_sky_foyer", "room_solarium", new Vector3(-4f, 15f, -7f), Vector3.zero, false, null, "Rooftop Solarium French Doors"));
            skyFoyer.AddConnection(new DoorwayConnection("conn_sky_roof_garden", "room_floor3_sky_foyer", "room_roof_garden", new Vector3(4f, 15f, -7f), Vector3.zero, false, null, "Roof Garden Lookout Door"));
            skyFoyer.AddConnection(new DoorwayConnection("conn_sky_vip301", "room_floor3_sky_foyer", "room_vip_301", new Vector3(10f, 15f, 6f), new Vector3(0f, 90f, 0f), true, "key_vip_301", "Royal Penthouse 301 Door"));
            skyFoyer.AddConnection(new DoorwayConnection("conn_sky_vip302", "room_floor3_sky_foyer", "room_vip_302", new Vector3(10f, 15f, -6f), new Vector3(0f, 90f, 0f), true, "key_vip_302", "Royal Penthouse 302 Door"));
            rooms.Add(skyFoyer);

            // 32. Crown Penthouse Salon
            var penthouse = CreateRoom(rng, "room_crown_penthouse", "Crown Penthouse Grand Salon", RoomType.CrownPenthouse, 3,
                new Vector3(-16f, 17.25f, 0f), new Vector3(12f, 4.5f, 14f),
                "CarpetBurgundy", "VelvetGold", "MarbleBlack", new Color(1.0f, 0.85f, 0.50f), 3.0f, 16f);
            penthouse.AddAnchor("anchor_penthouse_bar", AnchorCategory.Counter, new Vector3(-16f, 15f, 0f));
            penthouse.AddAnchor("anchor_penthouse_fireplace", AnchorCategory.FloorCorner, new Vector3(-21f, 15f, 0f));
            penthouse.AddConnection(new DoorwayConnection("conn_penthouse_bed", "room_crown_penthouse", "room_presidential_bed", new Vector3(-16f, 15f, 7f), Vector3.zero, false, null, "Presidential Bedroom Door"));
            penthouse.AddConnection(new DoorwayConnection("conn_penthouse_study", "room_crown_penthouse", "room_presidential_study", new Vector3(-16f, 15f, -7f), Vector3.zero, false, null, "Presidential Study Door"));
            rooms.Add(penthouse);

            // 33. Presidential Bedroom
            var presBed = CreateRoom(rng, "room_presidential_bed", "Presidential Master Bedroom", RoomType.PresidentialBedroom, 3,
                new Vector3(-16f, 17.25f, 10.5f), new Vector3(12f, 4.5f, 6f),
                "CarpetSapphire", "WallpaperEmerald", "WoodMahogany", new Color(1.0f, 0.82f, 0.60f), 2.2f, 12f);
            presBed.AddAnchor("anchor_presbed_wardrobe", AnchorCategory.Shelf, new Vector3(-19f, 15f, 12f));
            rooms.Add(presBed);

            // 34. Presidential Study
            var presStudy = CreateRoom(rng, "room_presidential_study", "Presidential Private Study", RoomType.PresidentialStudy, 3,
                new Vector3(-16f, 17.25f, -10.5f), new Vector3(12f, 4.5f, 6f),
                "WoodWalnut", "WallpaperArtDecoDamask", "WoodWalnut", new Color(0.6f, 0.95f, 0.7f), 2.2f, 12f);
            presStudy.AddAnchor("anchor_presstudy_desk", AnchorCategory.Desk, new Vector3(-16f, 15f, -11f));
            rooms.Add(presStudy);

            // 35. Clock Tower Observatory
            var observatory = CreateRoom(rng, "room_observatory", "Clock Tower Observatory", RoomType.Observatory, 3,
                new Vector3(0f, 17.25f, 10.5f), new Vector3(12f, 4.5f, 6f),
                "FloorSlate", "WallpaperNavy", "BrassGold", new Color(0.65f, 0.80f, 1.0f), 2.5f, 14f);
            observatory.AddAnchor("anchor_observatory_telescope", AnchorCategory.FloorCorner, new Vector3(0f, 15f, 11f));
            rooms.Add(observatory);

            // 36. Solarium Terrace
            var solarium = CreateRoom(rng, "room_solarium", "Rooftop Solarium & Terrace", RoomType.Solarium, 3,
                new Vector3(-6f, 17.25f, -13f), new Vector3(10f, 4.5f, 8f),
                "FloorCheckerMarble", "VelvetEmerald", "BrassGold", new Color(0.80f, 0.90f, 1.0f), 2.5f, 12f);
            solarium.AddAnchor("anchor_solarium_fountain", AnchorCategory.FloorCorner, new Vector3(-6f, 15f, -14f));
            rooms.Add(solarium);

            // 37. Royal Penthouse 301
            var vip301 = CreateRoom(rng, "room_vip_301", "Royal Penthouse Suite 301", RoomType.VIPSuite301, 3,
                new Vector3(16f, 17.25f, 7f), new Vector3(10f, 4.5f, 8f),
                "CarpetBurgundy", "VelvetGold", "WoodMahogany", new Color(1.0f, 0.85f, 0.55f), 2.4f, 12f);
            vip301.AddAnchor("anchor_vip301_bed", AnchorCategory.FloorCorner, new Vector3(16f, 15f, 9f));
            vip301.AddConnection(new DoorwayConnection("conn_vip301_motor", "room_vip_301", "room_motor_room", new Vector3(21f, 15f, 7f), new Vector3(0f, 90f, 0f), true, "key_motor_room", "Access Door to Motor Room"));
            rooms.Add(vip301);

            // 38. Royal Penthouse 302
            var vip302 = CreateRoom(rng, "room_vip_302", "Royal Penthouse Suite 302", RoomType.VIPSuite302, 3,
                new Vector3(16f, 17.25f, -7f), new Vector3(10f, 4.5f, 8f),
                "CarpetSapphire", "WallpaperArtDecoDamask", "WoodWalnut", new Color(1.0f, 0.88f, 0.65f), 2.4f, 12f);
            vip302.AddAnchor("anchor_vip302_bed", AnchorCategory.FloorCorner, new Vector3(16f, 15f, -9f));
            rooms.Add(vip302);

            // 39. Elevator Motor Room
            var motorRoom = CreateRoom(rng, "room_motor_room", "Elevator & Clockwork Motor Room", RoomType.MotorRoom, 3,
                new Vector3(25f, 17.25f, 7f), new Vector3(8f, 4.5f, 8f),
                "FloorSlate", "AgedParchment", "MarbleBlack", new Color(1.0f, 0.65f, 0.35f), 2.0f, 10f);
            motorRoom.AddAnchor("anchor_motor_switchboard", AnchorCategory.Wall, new Vector3(25f, 16.5f, 8f));
            rooms.Add(motorRoom);

            // 40. Roof Garden Lookout
            var roofGarden = CreateRoom(rng, "room_roof_garden", "Meridian Roof Lookout & Garden", RoomType.RoofGarden, 3,
                new Vector3(25f, 17.25f, -7f), new Vector3(8f, 4.5f, 8f),
                "FloorSlate", "VelvetEmerald", "BrassGold", new Color(0.65f, 0.80f, 1.0f), 2.2f, 10f);
            roofGarden.AddAnchor("anchor_roof_telescope", AnchorCategory.FloorCorner, new Vector3(25f, 15f, -7f));
            rooms.Add(roofGarden);

            return rooms;
        }

        private static RoomDefinition CreateRoom(SeededRandom rng, string roomId, string displayName, RoomType type, int floor,
            Vector3 center, Vector3 size, string defaultFloorMat, string defaultWallMat, string defaultWainscotMat,
            Color defaultLightColor, float defaultIntensity, float defaultRange)
        {
            Vector3 finalCenter = center;
            Vector3 finalSize = size;

            // Apply bounded pseudorandom jitter if RNG stream provided
            if (rng != null)
            {
                // Jitter size within +/- 1.0m to create distinct room proportions per seed
                float jitterX = rng.NextFloat(-0.8f, 0.8f);
                float jitterZ = rng.NextFloat(-0.8f, 0.8f);
                finalSize = new Vector3(Mathf.Max(6.0f, size.x + jitterX), size.y, Mathf.Max(5.0f, size.z + jitterZ));

                // Jitter center slightly within +/- 0.4m
                float offsetX = rng.NextFloat(-0.35f, 0.35f);
                float offsetZ = rng.NextFloat(-0.35f, 0.35f);
                finalCenter = new Vector3(center.x + offsetX, center.y, center.z + offsetZ);
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
                // Slight lighting intensity variance
                room.Styling.LightIntensity = defaultIntensity * rng.NextFloat(0.85f, 1.25f);
            }

            return room;
        }
    }
}
