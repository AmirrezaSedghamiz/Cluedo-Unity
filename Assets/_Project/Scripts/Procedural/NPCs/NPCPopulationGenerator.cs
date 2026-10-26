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
                BaselinePosition = new Vector3(0f, 0f, 9.5f),
                BaselineRotation = new Vector3(0f, 180f, 0f),
                ModelPrefabPath = "Assets/_Project/Art/Models/NPC_Concierge_Pierre.obj",
                MaterialKey = "UniformNavy",
                Personality = { Sociability = 0.6f, Caution = 0.7f, Curiosity = 0.5f, Honesty = 0.8f },
                PermittedRoomIds = { "room_lobby", "room_reception", "room_dining", "room_corridor_ground", "room_upper_corridor", "room_guest_102" }
            };
            roster.Add(concierge);

            var receptionist = new NPCProfile("npc_receptionist_celeste", "Celeste Moreau", NPCRole.Receptionist, 28, "room_reception")
            {
                BaselinePosition = new Vector3(0f, 0f, 10.5f),
                BaselineRotation = new Vector3(0f, 180f, 0f),
                MaterialKey = "UniformNavy",
                Personality = { Sociability = 0.7f, Caution = 0.5f, Curiosity = 0.7f, Honesty = 0.6f },
                PermittedRoomIds = { "room_reception", "room_lobby", "room_staff_office", "room_corridor_ground" }
            };
            roster.Add(receptionist);

            var chef = new NPCProfile("npc_chef_marcel", "Chef Marcel", NPCRole.HeadChef, 51, "room_kitchen")
            {
                BaselinePosition = new Vector3(-15f, 0f, 9.5f),
                BaselineRotation = new Vector3(0f, 180f, 0f),
                MaterialKey = "ShirtWhite",
                Personality = { Sociability = 0.4f, Caution = 0.5f, Curiosity = 0.3f, Irritability = 0.8f, Honesty = 0.7f },
                PermittedRoomIds = { "room_kitchen", "room_dining", "room_corridor_ground" }
            };
            roster.Add(chef);

            var maid = new NPCProfile("npc_maid_clara", "Clara Vance", NPCRole.HousekeepingMaid, 24, "room_housekeeping")
            {
                BaselinePosition = new Vector3(12f, 5f, -4f),
                BaselineRotation = new Vector3(0f, -90f, 0f),
                MaterialKey = "ShirtWhite",
                Personality = { Sociability = 0.5f, Caution = 0.8f, Curiosity = 0.8f, Honesty = 0.5f },
                PermittedRoomIds = { "room_housekeeping", "room_upper_corridor", "room_guest_101", "room_guest_102", "room_guest_103", "room_corridor_ground" }
            };
            roster.Add(maid);

            var engineer = new NPCProfile("npc_engineer_otto", "Otto Krause", NPCRole.MaintenanceEngineer, 47, "room_utility")
            {
                BaselinePosition = new Vector3(21f, 0f, -6f),
                BaselineRotation = new Vector3(0f, 90f, 0f),
                MaterialKey = "TrousersCharcoal",
                Personality = { Sociability = 0.2f, Caution = 0.6f, Curiosity = 0.5f, Irritability = 0.6f, Honesty = 0.8f },
                PermittedRoomIds = { "room_utility", "room_corridor_ground", "room_kitchen", "room_staff_office" }
            };
            roster.Add(engineer);

            var manager = new NPCProfile("npc_manager_adler", "Julian Adler", NPCRole.HotelManager, 55, "room_staff_office")
            {
                BaselinePosition = new Vector3(21f, 0f, 7f),
                BaselineRotation = new Vector3(0f, 180f, 0f),
                MaterialKey = "WoodMahogany",
                Personality = { Sociability = 0.5f, Caution = 0.9f, Curiosity = 0.4f, Honesty = 0.4f },
                PermittedRoomIds = { "room_staff_office", "room_reception", "room_lobby", "room_dining", "room_corridor_ground" }
            };
            roster.Add(manager);

            // 2. Hotel Guests
            var harrington = new NPCProfile("npc_lord_harrington", "Lord Harrington", NPCRole.WealthyGuest, 63, "room_guest_101")
            {
                BaselinePosition = new Vector3(-12f, 5f, 4f),
                BaselineRotation = new Vector3(0f, 90f, 0f),
                MaterialKey = "WoodWalnut",
                Personality = { Sociability = 0.6f, Caution = 0.4f, Curiosity = 0.4f, Irritability = 0.7f, Honesty = 0.5f },
                PermittedRoomIds = { "room_guest_101", "room_upper_corridor", "room_dining", "room_lobby", "room_corridor_ground" }
            };
            roster.Add(harrington);

            var valerie = new NPCProfile("npc_madame_valerie", "Madame Valerie", NPCRole.SocietyLady, 36, "room_guest_102")
            {
                BaselinePosition = new Vector3(12f, 5f, 8f),
                BaselineRotation = new Vector3(0f, -90f, 0f),
                MaterialKey = "CarpetBurgundy",
                Personality = { Sociability = 0.8f, Caution = 0.6f, Curiosity = 0.6f, Honesty = 0.4f },
                PermittedRoomIds = { "room_guest_102", "room_upper_corridor", "room_dining", "room_lobby", "room_corridor_ground" }
            };
            roster.Add(valerie);

            var sterling = new NPCProfile("npc_dr_sterling", "Dr. Sterling", NPCRole.DoctorGuest, 49, "room_guest_103")
            {
                BaselinePosition = new Vector3(-12f, 5f, -4f),
                BaselineRotation = new Vector3(0f, 90f, 0f),
                MaterialKey = "MarbleBlack",
                Personality = { Sociability = 0.4f, Caution = 0.8f, Curiosity = 0.7f, Honesty = 0.9f },
                PermittedRoomIds = { "room_guest_103", "room_upper_corridor", "room_lobby", "room_dining", "room_corridor_ground" }
            };
            roster.Add(sterling);

            var monroe = new NPCProfile("npc_journalist_monroe", "Vivian Monroe", NPCRole.JournalistGuest, 31, "room_lobby")
            {
                BaselinePosition = new Vector3(-4f, 0f, -2f),
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
                        // 00:00 - 00:30: Wait at post in reception
                        plan.AddActivity("WaitAtPost", "room_reception", new Vector3(0f, 0f, 9.5f), 0f, 30f, null, "Guarding reception counter and key cubbies.");
                        // 00:30 - 00:45: Patrol to lobby center table to check keys
                        plan.AddActivity("RetrieveKey", "room_lobby", new Vector3(0f, 0f, -2f), 30f, 45f, "obj_key_room_102", "Inspecting central lobby table for spare Room 102 keycard.");
                        // 00:45 - 01:15: Climb stairs to upper gallery and inspect VIP suite 102
                        plan.AddActivity("InspectSuite", "room_guest_102", new Vector3(10f, 5f, 6f), 45f, 75f, "obj_key_room_102", "Inspecting Room 102 exterior deadbolt lock.");
                        // 01:15 - 05:00: Return to Lobby Desk
                        plan.AddActivity("StationAtLobby", "room_lobby", new Vector3(0f, 0f, 4.5f), 75f, 300f, null, "Stationed at reception desk overseeing lobby entrance.");
                        break;

                    case NPCRole.Receptionist:
                        plan.AddActivity("DutyAtDesk", "room_reception", new Vector3(0f, 0f, 10.5f), 0f, 120f, null, "Managing guest ledger at reception desk.");
                        plan.AddActivity("ConsultManager", "room_staff_office", new Vector3(21f, 0f, 6f), 120f, 180f, null, "Delivering registration records to the manager's office.");
                        plan.AddActivity("DutyAtDesk", "room_reception", new Vector3(0f, 0f, 10.5f), 180f, 300f, null, "Stationed behind front counter.");
                        break;

                    case NPCRole.HeadChef:
                        plan.AddActivity("PrepKitchen", "room_kitchen", new Vector3(-15f, 0f, 9.5f), 0f, 90f, null, "Prepping midnight broth at prep counter.");
                        plan.AddActivity("CheckPantry", "room_kitchen", new Vector3(-11f, 0f, 11f), 90f, 150f, null, "Inventorying spice shelf and lockbox.");
                        plan.AddActivity("ServeDining", "room_dining", new Vector3(-15f, 0f, 0f), 150f, 300f, null, "Checking dining tables and clearing glasses.");
                        break;

                    case NPCRole.HousekeepingMaid:
                        plan.AddActivity("OrganizeLinens", "room_housekeeping", new Vector3(15f, 5f, -8f), 0f, 60f, null, "Folding clean towels on housekeeping service rack.");
                        plan.AddActivity("TurnDown101", "room_guest_101", new Vector3(-9f, 5f, 6f), 60f, 140f, null, "Turn-down service in Room 101.");
                        plan.AddActivity("CorridorSweep", "room_upper_corridor", new Vector3(3f, 5f, 0f), 140f, 300f, null, "Sweeping carpet on the second-floor gallery.");
                        break;

                    case NPCRole.MaintenanceEngineer:
                        plan.AddActivity("MonitorBoiler", "room_utility", new Vector3(21f, 0f, -6f), 0f, 80f, null, "Checking boiler pressure in utility room.");
                        plan.AddActivity("CheckBreakers", "room_utility", new Vector3(24.5f, 1.5f, -6f), 80f, 160f, null, "Inspecting electrical breaker panel.");
                        plan.AddActivity("PatrolGrounds", "room_corridor_ground", new Vector3(13f, 0f, 0f), 160f, 300f, null, "Walking service corridor checking radiator valves.");
                        break;

                    case NPCRole.HotelManager:
                        plan.AddActivity("ReviewAccounts", "room_staff_office", new Vector3(21f, 0f, 7f), 0f, 150f, null, "Auditing financial safe and invoices.");
                        plan.AddActivity("InspectDining", "room_dining", new Vector3(-15f, 0f, 0f), 150f, 220f, null, "Inspecting dining hall presentation.");
                        plan.AddActivity("LobbyAppearance", "room_lobby", new Vector3(0f, 0f, 2f), 220f, 300f, null, "Observing midnight operations in the main lobby.");
                        break;

                    case NPCRole.WealthyGuest:
                        plan.AddActivity("InRoomWriting", "room_guest_101", new Vector3(-12f, 5f, 4f), 0f, 90f, null, "Drafting business telegram at writing desk.");
                        plan.AddActivity("LateNightDrink", "room_dining", new Vector3(-15f, 0f, 3f), 90f, 200f, null, "Sipping brandy at dining table.");
                        plan.AddActivity("RetireToRoom", "room_guest_101", new Vector3(-9f, 5f, 8f), 200f, 300f, null, "Resting in suite 101.");
                        break;

                    case NPCRole.SocietyLady:
                        plan.AddActivity("LoungeCocktail", "room_dining", new Vector3(-15f, 0f, -3f), 0f, 80f, null, "Sitting in dining hall holding wine glass.");
                        plan.AddActivity("PrivateRetreat", "room_guest_102", new Vector3(12f, 5f, 8f), 80f, 240f, null, "Retreating to VIP Suite 102.");
                        plan.AddActivity("BalconyView", "room_guest_102", new Vector3(15f, 5f, 9.5f), 240f, 300f, null, "Looking out balcony window.");
                        break;

                    case NPCRole.DoctorGuest:
                        plan.AddActivity("ReadingJournal", "room_guest_103", new Vector3(-12f, 5f, -4f), 0f, 100f, null, "Reading medical journal under bedside lamp.");
                        plan.AddActivity("NightAirLobby", "room_lobby", new Vector3(4f, 0f, -2f), 100f, 210f, null, "Sitting in lobby armchair near the fireplace.");
                        plan.AddActivity("ReturnToRoom", "room_guest_103", new Vector3(-9f, 5f, -6f), 210f, 300f, null, "Resting in Room 103.");
                        break;

                    case NPCRole.JournalistGuest:
                        plan.AddActivity("LobbyEavesdrop", "room_lobby", new Vector3(-4f, 0f, -2f), 0f, 110f, null, "Sitting in armchair observing concierge and guests.");
                        plan.AddActivity("CorridorStalk", "room_corridor_ground", new Vector3(15f, 0f, 4f), 110f, 200f, null, "Loitering near manager's office taking notes.");
                        plan.AddActivity("UpperGalleryPatrol", "room_upper_corridor", new Vector3(3f, 5f, 0f), 200f, 300f, null, "Pacing second-floor gallery overlooking suites.");
                        break;
                }

                plans[npc.NpcId] = plan;
            }

            return plans;
        }
    }
}
