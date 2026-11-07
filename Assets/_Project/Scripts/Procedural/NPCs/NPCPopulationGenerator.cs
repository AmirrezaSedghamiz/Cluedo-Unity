using System.Collections.Generic;
using UnityEngine;
using TomorrowsCrime.Procedural.Core;
using TomorrowsCrime.Procedural.Hotel;

namespace TomorrowsCrime.Procedural.NPCs
{
    /// <summary>
    /// Procedural NPC population generator for Meridian House.
    /// Creates a 10-person character ecosystem with realistic hotel roles,
    /// personality profiles, room access permissions, and 5-minute scheduled routines.
    /// </summary>
    public static class NPCPopulationGenerator
    {
        public static List<NPCProfile> GenerateRoster(SeededRandom rng, List<RoomDefinition> rooms,
            string victimId = null, string culpritId = null, string motive = null)
        {
            var roster = new List<NPCProfile>();

            // 1. Staff Members
            var concierge = new NPCProfile("npc_concierge_pierre", "Concierge Pierre", NPCRole.Concierge, 42, "room_reception")
            {
                BaselinePosition = new Vector3(-1.2f, 0f, 11.6f),
                BaselineRotation = new Vector3(0f, 180f, 0f),
                ModelPrefabPath = "Assets/_Project/Art/Models/NPC_Concierge_Pierre.obj",
                MaterialKey = "UniformNavy",
                Personality = { Sociability = 0.6f, Caution = 0.7f, Curiosity = 0.5f, Honesty = 0.8f },
                PermittedRoomIds = { "room_lobby", "room_reception", "room_dining", "room_corridor_ground", "room_upper_corridor", "room_guest_102" }
            };
            roster.Add(concierge);

            var receptionist = new NPCProfile("npc_receptionist_celeste", "Celeste Moreau", NPCRole.Receptionist, 28, "room_reception")
            {
                BaselinePosition = new Vector3(1.2f, 0f, 11.6f),
                BaselineRotation = new Vector3(0f, 180f, 0f),
                MaterialKey = "UniformNavy",
                Personality = { Sociability = 0.7f, Caution = 0.5f, Curiosity = 0.7f, Honesty = 0.6f },
                PermittedRoomIds = { "room_reception", "room_lobby", "room_staff_office", "room_corridor_ground" }
            };
            roster.Add(receptionist);

            var chef = new NPCProfile("npc_chef_marcel", "Chef Marcel", NPCRole.HeadChef, 51, "room_kitchen")
            {
                BaselinePosition = new Vector3(-17.5f, 0f, 10.0f),
                BaselineRotation = new Vector3(0f, 270f, 0f),
                MaterialKey = "ShirtWhite",
                Personality = { Sociability = 0.4f, Caution = 0.5f, Curiosity = 0.3f, Irritability = 0.8f, Honesty = 0.7f },
                PermittedRoomIds = { "room_kitchen", "room_dining", "room_corridor_ground" }
            };
            roster.Add(chef);

            var maid = new NPCProfile("npc_maid_clara", "Clara Vance", NPCRole.HousekeepingMaid, 24, "room_housekeeping")
            {
                BaselinePosition = new Vector3(10.0f, 5f, -8.5f),
                BaselineRotation = new Vector3(0f, 90f, 0f),
                MaterialKey = "ShirtWhite",
                Personality = { Sociability = 0.5f, Caution = 0.8f, Curiosity = 0.8f, Honesty = 0.5f },
                PermittedRoomIds = { "room_housekeeping", "room_upper_corridor", "room_guest_101", "room_guest_102", "room_guest_103", "room_corridor_ground" }
            };
            roster.Add(maid);

            var engineer = new NPCProfile("npc_engineer_otto", "Otto Krause", NPCRole.MaintenanceEngineer, 47, "room_utility")
            {
                BaselinePosition = new Vector3(21.5f, 0f, -4.0f),
                BaselineRotation = new Vector3(0f, 90f, 0f),
                MaterialKey = "TrousersCharcoal",
                Personality = { Sociability = 0.2f, Caution = 0.6f, Curiosity = 0.5f, Irritability = 0.6f, Honesty = 0.8f },
                PermittedRoomIds = { "room_utility", "room_corridor_ground", "room_kitchen", "room_staff_office" }
            };
            roster.Add(engineer);

            var manager = new NPCProfile("npc_manager_adler", "Julian Adler", NPCRole.HotelManager, 55, "room_staff_office")
            {
                BaselinePosition = new Vector3(21.5f, 0f, 6.5f),
                BaselineRotation = new Vector3(0f, 180f, 0f),
                MaterialKey = "WoodMahogany",
                Personality = { Sociability = 0.5f, Caution = 0.9f, Curiosity = 0.4f, Honesty = 0.4f },
                PermittedRoomIds = { "room_staff_office", "room_reception", "room_lobby", "room_dining", "room_corridor_ground" }
            };
            roster.Add(manager);

            // 2. Hotel Guests
            var harrington = new NPCProfile("npc_lord_harrington", "Lord Harrington", NPCRole.WealthyGuest, 63, "room_guest_101")
            {
                BaselinePosition = new Vector3(2.5f, 5f, 8.5f),
                BaselineRotation = new Vector3(0f, 180f, 0f),
                MaterialKey = "WoodWalnut",
                Personality = { Sociability = 0.6f, Caution = 0.4f, Curiosity = 0.4f, Irritability = 0.7f, Honesty = 0.5f },
                PermittedRoomIds = { "room_guest_101", "room_upper_corridor", "room_dining", "room_lobby", "room_corridor_ground" }
            };
            roster.Add(harrington);

            var valerie = new NPCProfile("npc_madame_valerie", "Madame Valerie", NPCRole.SocietyLady, 36, "room_guest_102")
            {
                BaselinePosition = new Vector3(9.5f, 5f, 8.5f),
                BaselineRotation = new Vector3(0f, 180f, 0f),
                MaterialKey = "CarpetBurgundy",
                Personality = { Sociability = 0.8f, Caution = 0.6f, Curiosity = 0.6f, Honesty = 0.4f },
                PermittedRoomIds = { "room_guest_102", "room_upper_corridor", "room_dining", "room_lobby", "room_corridor_ground" }
            };
            roster.Add(valerie);

            var sterling = new NPCProfile("npc_dr_sterling", "Dr. Sterling", NPCRole.DoctorGuest, 49, "room_guest_103")
            {
                BaselinePosition = new Vector3(2.5f, 5f, -8.5f),
                BaselineRotation = new Vector3(0f, 0f, 0f),
                MaterialKey = "MarbleBlack",
                Personality = { Sociability = 0.4f, Caution = 0.8f, Curiosity = 0.7f, Honesty = 0.9f },
                PermittedRoomIds = { "room_guest_103", "room_upper_corridor", "room_lobby", "room_dining", "room_corridor_ground" }
            };
            roster.Add(sterling);

            var monroe = new NPCProfile("npc_journalist_monroe", "Vivian Monroe", NPCRole.JournalistGuest, 31, "room_lobby")
            {
                BaselinePosition = new Vector3(-3.2f, 0f, -2.5f),
                BaselineRotation = new Vector3(0f, 45f, 0f),
                MaterialKey = "TieBurgundy",
                Personality = { Sociability = 0.9f, Caution = 0.3f, Curiosity = 0.95f, Honesty = 0.5f },
                PermittedRoomIds = { "room_lobby", "room_dining", "room_corridor_ground", "room_reception", "room_upper_corridor" }
            };
            roster.Add(monroe);

            // Assign Victim & Culprit flags if specified by the scenario generator
            if (!string.IsNullOrEmpty(victimId))
            {
                var v = roster.Find(p => p.NpcId == victimId);
                if (v != null) v.IsVictim = true;
            }

            if (!string.IsNullOrEmpty(culpritId))
            {
                var c = roster.Find(p => p.NpcId == culpritId);
                if (c != null)
                {
                    c.IsCulprit = true;
                    c.MotiveDescription = motive ?? "Financial desperation and greed.";
                }
            }

            return roster;
        }

        public static Dictionary<string, NPCRoutinePlan> GenerateRoutines(List<NPCProfile> roster, SeededRandom rng)
        {
            var plans = new Dictionary<string, NPCRoutinePlan>();

            foreach (var npc in roster)
            {
                var plan = new NPCRoutinePlan(npc.NpcId);

                switch (npc.Role)
                {
                    case NPCRole.Concierge:
                        plan.AddActivity("WaitAtPost", "room_reception", new Vector3(-1.2f, 0f, 11.6f), 0f, 70f, null, "Guarding reception counter and key cubbies.");
                        plan.AddActivity("PatrolLobby", "room_lobby", new Vector3(0f, 0f, 2.0f), 70f, 140f, null, "Inspecting central lobby and coat rack.");
                        plan.AddActivity("CheckTransport", "room_corridor_ground", new Vector3(10.0f, 0f, 0f), 140f, 210f, null, "Inspecting east transport hub and elevator station.");
                        plan.AddActivity("StationAtLobby", "room_reception", new Vector3(-1.2f, 0f, 11.6f), 210f, 300f, null, "Stationed behind reception desk overseeing front hall.");
                        break;

                    case NPCRole.Receptionist:
                        plan.AddActivity("DutyAtDesk", "room_reception", new Vector3(1.2f, 0f, 11.6f), 0f, 90f, null, "Managing guest ledger at reception desk.");
                        plan.AddActivity("ConsultManager", "room_staff_office", new Vector3(19.0f, 0f, 3.5f), 90f, 170f, null, "Delivering registration records to the manager's office.");
                        plan.AddActivity("VisitBar", "room_bar_lounge", new Vector3(-3.0f, 0f, -9.0f), 170f, 230f, null, "Fetching water and checking on bar patrons.");
                        plan.AddActivity("DutyAtDesk", "room_reception", new Vector3(1.2f, 0f, 11.6f), 230f, 300f, null, "Stationed behind front counter sorting mail.");
                        break;

                    case NPCRole.HeadChef:
                        plan.AddActivity("PrepKitchen", "room_kitchen", new Vector3(-16.0f, 0f, 10.0f), 0f, 80f, null, "Prepping midnight broth at commercial cooking range.");
                        plan.AddActivity("CheckPantry", "room_kitchen", new Vector3(-12.0f, 0f, 11.5f), 80f, 160f, null, "Inventorying spice shelf and pantry stores.");
                        plan.AddActivity("ServeDining", "room_dining", new Vector3(-14.0f, 0f, 1.5f), 160f, 230f, null, "Checking dining banquet tables and clearing service ware.");
                        plan.AddActivity("PrepKitchen", "room_kitchen", new Vector3(-16.0f, 0f, 10.0f), 230f, 300f, null, "Sharpening knives and prepping morning breakfast menu.");
                        break;

                    case NPCRole.HousekeepingMaid:
                        plan.AddActivity("OrganizeLinens", "room_housekeeping", new Vector3(10.0f, 5f, -7.0f), 0f, 75f, null, "Folding clean towels on housekeeping service rack.");
                        plan.AddActivity("TurnDown101", "room_guest_101", new Vector3(2.0f, 5f, 7.0f), 75f, 150f, null, "Turn-down service and fluffing pillows in Suite 101.");
                        plan.AddActivity("DustLibrary", "room_library", new Vector3(-8.0f, 5f, 2.0f), 150f, 225f, null, "Dusting reading tables in the Grand Library archive.");
                        plan.AddActivity("CorridorSweep", "room_upper_corridor", new Vector3(4.0f, 5f, 0f), 225f, 300f, null, "Sweeping carpet on the second-floor gallery.");
                        break;

                    case NPCRole.MaintenanceEngineer:
                        plan.AddActivity("MonitorBoiler", "room_utility", new Vector3(21.0f, 0f, -5.0f), 0f, 75f, null, "Checking boiler pressure in utility room.");
                        plan.AddActivity("CheckBreakers", "room_utility", new Vector3(23.5f, 0f, -3.0f), 75f, 150f, null, "Inspecting electrical breaker panel.");
                        plan.AddActivity("InspectLiftGears", "room_corridor_ground", new Vector3(11.0f, 0f, 1.0f), 150f, 225f, null, "Checking freight lift gears and elevator pulleys.");
                        plan.AddActivity("WorkbenchWork", "room_utility", new Vector3(21.0f, 0f, -2.5f), 225f, 300f, null, "Filing replacement brass fittings at utility workbench.");
                        break;

                    case NPCRole.HotelManager:
                        plan.AddActivity("ReviewAccounts", "room_staff_office", new Vector3(21.5f, 0f, 4.5f), 0f, 100f, null, "Auditing secret financial safe and invoices behind executive desk.");
                        plan.AddActivity("InspectDining", "room_dining", new Vector3(-13.5f, 0f, -2.0f), 100f, 170f, null, "Inspecting dining hall presentation and wine inventory.");
                        plan.AddActivity("LobbyAppearance", "room_lobby", new Vector3(2.0f, 0f, 1.5f), 170f, 235f, null, "Observing midnight operations in the main lobby by the fireplace.");
                        plan.AddActivity("LockOfficeSafe", "room_staff_office", new Vector3(23.5f, 0f, 2.0f), 235f, 300f, null, "Locking office safe and filing confidential papers.");
                        break;

                    case NPCRole.WealthyGuest:
                        plan.AddActivity("InRoomWriting", "room_guest_101", new Vector3(1.5f, 5f, 8.5f), 0f, 85f, null, "Drafting will and business telegram at writing desk in Suite 101.");
                        plan.AddActivity("LibraryReading", "room_library", new Vector3(-10.0f, 5f, 0f), 85f, 160f, null, "Studying historical genealogies in Grand Library armchair.");
                        plan.AddActivity("ChessContemplation", "room_card_room", new Vector3(-10.0f, 5f, 9.5f), 160f, 230f, null, "Contemplating a chess problem in the card room.");
                        plan.AddActivity("RetireToRoom", "room_guest_101", new Vector3(1.5f, 5f, 8.5f), 230f, 300f, null, "Resting in Suite 101 and checking pocket watch.");
                        break;

                    case NPCRole.SocietyLady:
                        plan.AddActivity("VanityPreparation", "room_guest_102", new Vector3(12.0f, 5f, 7.5f), 0f, 80f, null, "Fixing jewelry and applying French perfume at Suite 102 vanity.");
                        plan.AddActivity("PoetryBrowsing", "room_library", new Vector3(-6.0f, 5f, 1.0f), 80f, 155f, null, "Browsing rare poetry volumes in the Grand Library archway.");
                        plan.AddActivity("BalconyView", "room_guest_102", new Vector3(11.0f, 5f, 9.5f), 155f, 225f, null, "Looking out into the foggy midnight air on Suite 102 balcony.");
                        plan.AddActivity("SuiteRelaxation", "room_guest_102", new Vector3(10.0f, 5f, 6.5f), 225f, 300f, null, "Sipping champagne at Suite 102 salon table.");
                        break;

                    case NPCRole.DoctorGuest:
                        plan.AddActivity("ReadingJournal", "room_guest_103", new Vector3(2.0f, 5f, -7.5f), 0f, 90f, null, "Reading medical toxicology treatise under Suite 103 bedside lamp.");
                        plan.AddActivity("ResearchArchive", "room_library", new Vector3(-12.0f, 5f, -3.0f), 90f, 165f, null, "Consulting botanical pharmacology records in the library archive.");
                        plan.AddActivity("GalleryWalk", "room_upper_corridor", new Vector3(5.0f, 5f, 0f), 165f, 230f, null, "Pacing the upper gallery corridor for midnight exercise.");
                        plan.AddActivity("ReturnToRoom", "room_guest_103", new Vector3(2.0f, 5f, -7.5f), 230f, 300f, null, "Packing apothecary vials into physician's Gladstone bag.");
                        break;

                    case NPCRole.JournalistGuest:
                        plan.AddActivity("LobbyEavesdrop", "room_lobby", new Vector3(-4.0f, 0f, -2.5f), 0f, 80f, null, "Sitting in lobby lounge pretending to read newspaper while eavesdropping.");
                        plan.AddActivity("CorridorStalk", "room_corridor_ground", new Vector3(10.0f, 0f, 2.0f), 80f, 150f, null, "Loitering near manager's office taking shorthand notes.");
                        plan.AddActivity("BarInquiries", "room_bar_lounge", new Vector3(-5.5f, 0f, -9.5f), 150f, 220f, null, "Sipping gin at the Golden Quill bar and questioning patrons.");
                        plan.AddActivity("LobbyWatch", "room_lobby", new Vector3(3.0f, 0f, -2.0f), 220f, 300f, null, "Watching reception desk from fireplace armchair for suspicious exchanges.");
                        break;
                }

                plans[npc.NpcId] = plan;
            }

            return plans;
        }
    }
}
