using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.World;
using TomorrowsCrime.Procedural.Core;
using TomorrowsCrime.Procedural.Hotel;

namespace TomorrowsCrime.Procedural.Objects
{
    /// <summary>
    /// Seeded room-aware object placement system for Meridian House.
    /// Distributes mandatory investigative clues, contextual functional items,
    /// and believable ambient clutter across room anchors.
    /// </summary>
    public static class RoomObjectPlacer
    {
        private static List<ObjectDefinition> _catalog;

        public static List<ObjectDefinition> GetCatalog()
        {
            if (_catalog != null) return _catalog;

            _catalog = new List<ObjectDefinition>
            {
                // Keys
                new ObjectDefinition("obj_key_room_101", "Room 101 Brass Key", ObjectCategory.Key, ObjectRole.Functional,
                    "Assets/_Project/Art/Models/Prop_Keycard_Detailed.obj", "BrassGold",
                    "Brass guest key stamped with '101 - Meridian House'.",
                    "Heavy brass hotel key for Room 101. Standard guest access.", "evidence_key_101")
                    .WithRooms(RoomType.Reception, RoomType.Lobby, RoomType.StaffOffice)
                    .WithAnchors(AnchorCategory.KeyStation, AnchorCategory.Desk, AnchorCategory.Tabletop)
                    .WithCollider(new Vector3(0.4f, 0.1f, 0.4f)),

                new ObjectDefinition("obj_key_room_102", "Room 102 Gold Keycard", ObjectCategory.Key, ObjectRole.Functional,
                    "Assets/_Project/Art/Models/Prop_Keycard_Detailed.obj", "BrassGold",
                    "A magnetic hotel keycard stamped with 'Room 102 - Meridian House'.",
                    "Standard electronic guest keycard for Room 102. Concierge schedule targets this key at 00:30.", "evidence_room102_keycard")
                    .WithRooms(RoomType.Lobby, RoomType.Reception, RoomType.StaffOffice)
                    .WithAnchors(AnchorCategory.Tabletop, AnchorCategory.Counter, AnchorCategory.Desk)
                    .WithCollider(new Vector3(0.4f, 0.1f, 0.4f)),

                new ObjectDefinition("obj_key_room_103", "Room 103 Keycard", ObjectCategory.Key, ObjectRole.Functional,
                    "Assets/_Project/Art/Models/Prop_Keycard_Detailed.obj", "BrassGold",
                    "Plastic-coated keycard stamped with 'Room 103'.",
                    "Guest access card for Room 103.", "evidence_key_103")
                    .WithRooms(RoomType.Reception, RoomType.Lobby, RoomType.GuestRoom103)
                    .WithAnchors(AnchorCategory.KeyStation, AnchorCategory.Desk, AnchorCategory.Nightstand)
                    .WithCollider(new Vector3(0.4f, 0.1f, 0.4f)),

                new ObjectDefinition("obj_key_staff_office", "Manager's Master Key", ObjectCategory.Key, ObjectRole.Functional,
                    "Assets/_Project/Art/Models/Prop_Keycard_Detailed.obj", "BrassGold",
                    "Heavy brass skeleton key labeled 'MANAGEMENT'.",
                    "Master key opening the Manager's Staff Office and executive records.", "evidence_key_office")
                    .WithRooms(RoomType.Reception, RoomType.UtilityRoom, RoomType.Kitchen)
                    .WithAnchors(AnchorCategory.Desk, AnchorCategory.Counter, AnchorCategory.Shelf)
                    .WithCollider(new Vector3(0.4f, 0.1f, 0.4f)),

                new ObjectDefinition("obj_key_room_104", "Room 104 Key", ObjectCategory.Key, ObjectRole.Functional,
                    "Assets/_Project/Art/Models/Prop_Keycard_Detailed.obj", "BrassGold",
                    "Polished brass key stamped 'Room 104'.",
                    "Key for Baroness Dupuis's guest suite.", "evidence_key_104")
                    .WithRooms(RoomType.Reception, RoomType.Lobby, RoomType.GuestRoom102)
                    .WithAnchors(AnchorCategory.KeyStation, AnchorCategory.Desk, AnchorCategory.Tabletop)
                    .WithCollider(new Vector3(0.4f, 0.1f, 0.4f)),

                new ObjectDefinition("obj_key_penthouse", "Crown Penthouse Master Key", ObjectCategory.Key, ObjectRole.Functional,
                    "Assets/_Project/Art/Models/Prop_Keycard_Detailed.obj", "BrassGold",
                    "Gilded master key stamped 'CROWN PENTHOUSE'.",
                    "Grants access to the top-floor Penthouse Grand Salon and Presidential Suite.", "evidence_key_penthouse")
                    .WithRooms(RoomType.Reception, RoomType.StaffOffice, RoomType.Floor3SkyFoyer)
                    .WithAnchors(AnchorCategory.KeyStation, AnchorCategory.Desk, AnchorCategory.Tabletop)
                    .WithCollider(new Vector3(0.4f, 0.1f, 0.4f)),

                new ObjectDefinition("obj_key_observatory", "Observatory Brass Key", ObjectCategory.Key, ObjectRole.Functional,
                    "Assets/_Project/Art/Models/Prop_Keycard_Detailed.obj", "BrassGold",
                    "Ancient brass skeleton key stamped 'OBSERVATORY'.",
                    "Unlocks the Clock Tower Observatory mechanism and telescope dome.", "evidence_key_observatory")
                    .WithRooms(RoomType.StaffOffice, RoomType.Library, RoomType.Floor3SkyFoyer)
                    .WithAnchors(AnchorCategory.Desk, AnchorCategory.Shelf, AnchorCategory.Tabletop)
                    .WithCollider(new Vector3(0.4f, 0.1f, 0.4f)),

                // Weapons & Potential Murder Tools
                new ObjectDefinition("obj_chef_cleaver", "Heavy Meat Cleaver", ObjectCategory.Utensil, ObjectRole.InvestigativeEvidence,
                    null, "BrassGold",
                    "A heavy French steel meat cleaver with a rosewood handle.",
                    "Professional chef's cleaver with a razor-sharp edge. Stored in the kitchen pantry.", "evidence_cleaver")
                    .WithRooms(RoomType.Kitchen, RoomType.DiningRoom, RoomType.UtilityRoom)
                    .WithAnchors(AnchorCategory.Counter, AnchorCategory.Shelf)
                    .WithCollider(new Vector3(0.5f, 0.2f, 0.3f)),

                new ObjectDefinition("obj_brass_candlestick", "Heavy Brass Candlestick", ObjectCategory.Valuable, ObjectRole.InvestigativeEvidence,
                    null, "BrassGold",
                    "A solid cast-brass Art Deco candlestick with candle wax remnants.",
                    "Extremely dense brass candlestick. Heavy enough to serve as a bludgeon.", "evidence_candlestick")
                    .WithRooms(RoomType.DiningRoom, RoomType.Lobby, RoomType.GuestRoom102, RoomType.UpperLanding)
                    .WithAnchors(AnchorCategory.Tabletop, AnchorCategory.Desk, AnchorCategory.Nightstand)
                    .WithCollider(new Vector3(0.3f, 0.6f, 0.3f)),

                new ObjectDefinition("obj_poison_vial", "Amber Tincture Bottle", ObjectCategory.Container, ObjectRole.InvestigativeEvidence,
                    null, "AmberGlow",
                    "A small dark amber apothecary bottle labeled 'Alkaloid Solution'.",
                    "Apothecary bottle with an eye-dropper cap. Residue smells faintly of bitter almonds.", "evidence_poison")
                    .WithRooms(RoomType.Kitchen, RoomType.HousekeepingStorage, RoomType.GuestRoom101, RoomType.StaffOffice)
                    .WithAnchors(AnchorCategory.Shelf, AnchorCategory.Nightstand, AnchorCategory.Desk)
                    .WithCollider(new Vector3(0.25f, 0.4f, 0.25f)),

                new ObjectDefinition("obj_maintenance_wrench", "Steel Pipe Wrench", ObjectCategory.Tool, ObjectRole.InvestigativeEvidence,
                    null, "MarbleBlack",
                    "A 14-inch drop-forged steel pipe wrench with grease stains.",
                    "Heavy utility wrench used for hotel plumbing and radiator maintenance.", "evidence_wrench")
                    .WithRooms(RoomType.UtilityRoom, RoomType.HousekeepingStorage, RoomType.Kitchen)
                    .WithAnchors(AnchorCategory.Counter, AnchorCategory.ServiceRack, AnchorCategory.Shelf)
                    .WithCollider(new Vector3(0.6f, 0.15f, 0.25f)),

                // Documents & Letters
                new ObjectDefinition("obj_duty_memo", "Concierge Night Shift Memo", ObjectCategory.Document, ObjectRole.InvestigativeEvidence,
                    null, "ShirtWhite",
                    "Concierge Night Shift duty memo on Meridian House letterhead.",
                    "Memo detailing scheduled lobby rounds: 00:00 midnight inspection, 00:30 retrieve Room 102 keycard, 00:35 inspect Suite 102.", "evidence_memo")
                    .WithRooms(RoomType.Reception, RoomType.Lobby, RoomType.StaffOffice)
                    .WithAnchors(AnchorCategory.Counter, AnchorCategory.Desk, AnchorCategory.Tabletop)
                    .WithCollider(new Vector3(0.4f, 0.05f, 0.5f)),

                new ObjectDefinition("obj_blackmail_letter", "Torn Handwritten Letter", ObjectCategory.Document, ObjectRole.InvestigativeEvidence,
                    null, "ShirtWhite",
                    "A folded slip of paper with aggressive cursive handwriting.",
                    "Reads: 'Midnight is your last chance to pay what you owe. Once the doors lock, there is no leaving Meridian House.'", "evidence_letter")
                    .WithRooms(RoomType.GuestRoom101, RoomType.GuestRoom102, RoomType.GuestRoom103, RoomType.StaffOffice)
                    .WithAnchors(AnchorCategory.Desk, AnchorCategory.Nightstand, AnchorCategory.Tabletop)
                    .WithCollider(new Vector3(0.35f, 0.05f, 0.35f)),

                new ObjectDefinition("obj_guest_register", "Meridian House Ledger", ObjectCategory.Document, ObjectRole.InvestigativeEvidence,
                    null, "WoodMahogany",
                    "Bound leather registry book recording tonight's registered guests.",
                    "Ledger confirming Room 101 occupied by Lord Harrington, Room 102 by Madame Valerie, and Room 103 by Dr. Sterling.", "evidence_ledger")
                    .WithRooms(RoomType.Reception, RoomType.StaffOffice)
                    .WithAnchors(AnchorCategory.Counter, AnchorCategory.Desk)
                    .WithCollider(new Vector3(0.5f, 0.1f, 0.6f)),

                // Ambient Clutter & Atmospheric Props
                new ObjectDefinition("obj_wine_bottle", "Empty Vintage Wine Bottle", ObjectCategory.Container, ObjectRole.Decorative,
                    null, "TieBurgundy",
                    "An opened green glass bottle of 1921 Chateau Latour.",
                    "Empty wine bottle, uncorked earlier in the evening.", null)
                    .WithRooms(RoomType.DiningRoom, RoomType.Kitchen, RoomType.GuestRoom102)
                    .WithAnchors(AnchorCategory.Tabletop, AnchorCategory.Counter)
                    .WithCollider(new Vector3(0.25f, 0.6f, 0.25f)),

                new ObjectDefinition("obj_crystal_glass", "Cut Crystal Tumbler", ObjectCategory.Utensil, ObjectRole.Decorative,
                    null, "ShirtWhite",
                    "Heavy lead crystal tumbler with amber liquor dregs.",
                    "Drink glass left on the table.", null)
                    .WithRooms(RoomType.DiningRoom, RoomType.Lobby, RoomType.GuestRoom101, RoomType.GuestRoom103)
                    .WithAnchors(AnchorCategory.Tabletop, AnchorCategory.Desk, AnchorCategory.Nightstand)
                    .WithCollider(new Vector3(0.2f, 0.25f, 0.2f)),

                new ObjectDefinition("obj_cleaning_bucket", "Housekeeping Wash Bucket", ObjectCategory.Container, ObjectRole.Decorative,
                    null, "BrassGold",
                    "Galvanized zinc wash bucket with scrubbing rags.",
                    "Standard cleaning bucket used by housekeeping staff.", null)
                    .WithRooms(RoomType.HousekeepingStorage, RoomType.UtilityRoom, RoomType.GroundCorridor, RoomType.LinenStorage, RoomType.ServiceAnnexFloor2)
                    .WithAnchors(AnchorCategory.FloorCorner, AnchorCategory.ServiceRack)
                    .WithCollider(new Vector3(0.6f, 0.5f, 0.6f)),

                new ObjectDefinition("obj_chess_set", "Carved Ivory & Ebony Chessboard", ObjectCategory.Valuable, ObjectRole.Decorative,
                    null, "WoodWalnut",
                    "A folding walnut chessboard with hand-carved pieces set in mid-game.",
                    "An ongoing game of chess. White's queen is pinning black's knight.", null)
                    .WithRooms(RoomType.CardRoom, RoomType.PresidentialStudy, RoomType.CrownPenthouse)
                    .WithAnchors(AnchorCategory.Tabletop, AnchorCategory.Desk)
                    .WithCollider(new Vector3(0.5f, 0.15f, 0.5f)),

                new ObjectDefinition("obj_sheet_music", "Handwritten Sonata Score", ObjectCategory.Document, ObjectRole.InvestigativeEvidence,
                    null, "ShirtWhite",
                    "Original manuscript sheet music stamped 'Conservatoire de Paris'.",
                    "Vocal score with handwritten annotations in the margins.", "evidence_sheet_music")
                    .WithRooms(RoomType.MusicSalon, RoomType.GuestRoom205)
                    .WithAnchors(AnchorCategory.Tabletop, AnchorCategory.Desk, AnchorCategory.Shelf)
                    .WithCollider(new Vector3(0.35f, 0.05f, 0.45f)),

                new ObjectDefinition("obj_brass_telescope", "Pocket Brass Telescope", ObjectCategory.Tool, ObjectRole.Decorative,
                    null, "BrassGold",
                    "A collapsible brass astronomical telescope with quartz lenses.",
                    "Precision nautical and celestial spotting glass.", null)
                    .WithRooms(RoomType.Observatory, RoomType.RoofGarden, RoomType.GuestRoom204)
                    .WithAnchors(AnchorCategory.Tabletop, AnchorCategory.Desk, AnchorCategory.Shelf)
                    .WithCollider(new Vector3(0.2f, 0.45f, 0.2f)),

                new ObjectDefinition("obj_cigar_box", "Cedar Cigar Humidor", ObjectCategory.Container, ObjectRole.Decorative,
                    null, "WoodMahogany",
                    "Inlaid Spanish cedar humidor filled with Havana cigars.",
                    "Luxury cigar humidor smelling of cured tobacco and cedar.", null)
                    .WithRooms(RoomType.CigarLounge, RoomType.CrownPenthouse, RoomType.BarLounge)
                    .WithAnchors(AnchorCategory.Tabletop, AnchorCategory.Counter, AnchorCategory.Desk)
                    .WithCollider(new Vector3(0.4f, 0.2f, 0.3f)),

                new ObjectDefinition("obj_antique_globe", "Gilded Terrestrial Globe", ObjectCategory.Valuable, ObjectRole.Decorative,
                    null, "BrassGold",
                    "A 12-inch terrestrial globe mounted on a brass meridian gimbal.",
                    "Detailed 19th-century cartographic globe.", null)
                    .WithRooms(RoomType.Library, RoomType.PresidentialStudy, RoomType.Floor3SkyFoyer)
                    .WithAnchors(AnchorCategory.Desk, AnchorCategory.Tabletop)
                    .WithCollider(new Vector3(0.4f, 0.6f, 0.4f)),

                // Rich Meaningful Hotel Props & Detective Clutter
                new ObjectDefinition("obj_vintage_typewriter", "Underwood Standard Typewriter", ObjectCategory.Tool, ObjectRole.Decorative,
                    null, "MarbleBlack",
                    "A 1920s Underwood No. 5 typewriter with a two-color ink ribbon.",
                    "Heavy mechanical typewriter loaded with clean cream bond paper.", null)
                    .WithRooms(RoomType.StaffOffice, RoomType.Reception, RoomType.Library, RoomType.GuestRoom101, RoomType.GuestRoom105, RoomType.PresidentialStudy)
                    .WithAnchors(AnchorCategory.Desk, AnchorCategory.Tabletop)
                    .WithCollider(new Vector3(0.45f, 0.25f, 0.40f)),

                new ObjectDefinition("obj_rotary_telephone", "Heavy Bakelite Rotary Telephone", ObjectCategory.Tool, ObjectRole.Decorative,
                    null, "MarbleBlack",
                    "A black bakelite pyramid telephone with a chrome rotary dial.",
                    "Hotel internal extension phone connected to the lobby switchboard.", null)
                    .WithRooms(RoomType.Reception, RoomType.StaffOffice, RoomType.GuestRoom101, RoomType.GuestRoom102, RoomType.GuestRoom103, RoomType.GuestRoom104, RoomType.GuestRoom105, RoomType.GuestRoom201, RoomType.GuestRoom202, RoomType.GuestRoom204, RoomType.CrownPenthouse)
                    .WithAnchors(AnchorCategory.Desk, AnchorCategory.Nightstand, AnchorCategory.Tabletop, AnchorCategory.Counter)
                    .WithCollider(new Vector3(0.35f, 0.25f, 0.35f)),

                new ObjectDefinition("obj_bedside_alarm_clock", "Wind-Up Brass Alarm Clock", ObjectCategory.Valuable, ObjectRole.Decorative,
                    null, "BrassGold",
                    "A twin-bell mechanical brass clock ticking softly.",
                    "Mechanical bedside clock stopped at 12:14 AM.", null)
                    .WithRooms(RoomType.GuestRoom101, RoomType.GuestRoom102, RoomType.GuestRoom103, RoomType.GuestRoom104, RoomType.GuestRoom105, RoomType.GuestRoom201, RoomType.GuestRoom202, RoomType.GuestRoom203, RoomType.GuestRoom204, RoomType.GuestRoom205, RoomType.VIPSuite301, RoomType.VIPSuite302, RoomType.PresidentialBedroom)
                    .WithAnchors(AnchorCategory.Nightstand, AnchorCategory.Desk)
                    .WithCollider(new Vector3(0.25f, 0.25f, 0.20f)),

                new ObjectDefinition("obj_perfume_atomizer", "Cut Crystal Perfume Atomizer", ObjectCategory.Valuable, ObjectRole.Decorative,
                    null, "ShirtWhite",
                    "An ornate crystal perfume flask with a braided silk pump bulb.",
                    "French Bulgarian rosewater perfume, highly favored by society ladies.", null)
                    .WithRooms(RoomType.GuestRoom102, RoomType.GuestRoom104, RoomType.GuestRoom202, RoomType.VIPSuite301, RoomType.Solarium)
                    .WithAnchors(AnchorCategory.Nightstand, AnchorCategory.Desk, AnchorCategory.Tabletop)
                    .WithCollider(new Vector3(0.20f, 0.30f, 0.20f)),

                new ObjectDefinition("obj_jewelry_casket", "Velvet-Lined Inlaid Jewelry Casket", ObjectCategory.Valuable, ObjectRole.InvestigativeEvidence,
                    null, "WoodMahogany",
                    "A rosewood jewelry box inlaid with mother-of-pearl.",
                    "Lined in burgundy silk. An empty velvet indentation reveals a missing diamond necklace.", "evidence_missing_jewels")
                    .WithRooms(RoomType.GuestRoom102, RoomType.GuestRoom104, RoomType.GuestRoom202, RoomType.VIPSuite301, RoomType.PresidentialBedroom)
                    .WithAnchors(AnchorCategory.Nightstand, AnchorCategory.Desk, AnchorCategory.Shelf)
                    .WithCollider(new Vector3(0.35f, 0.20f, 0.25f)),

                new ObjectDefinition("obj_silver_cloche", "Silver Room Service Food Cloche", ObjectCategory.Utensil, ObjectRole.Decorative,
                    null, "BrassGold",
                    "A domed polished silver dish cover with an ornate handle ring.",
                    "Room service platter cover covering cold oysters and roasted duck remnants.", null)
                    .WithRooms(RoomType.DiningRoom, RoomType.Kitchen, RoomType.GuestRoom102, RoomType.CrownPenthouse, RoomType.VIPSuite301)
                    .WithAnchors(AnchorCategory.Tabletop, AnchorCategory.Counter)
                    .WithCollider(new Vector3(0.40f, 0.25f, 0.40f)),

                new ObjectDefinition("obj_cocktail_shaker", "Art Deco Brass Cocktail Shaker", ObjectCategory.Utensil, ObjectRole.Decorative,
                    null, "BrassGold",
                    "A sleek three-piece geometric cocktail shaker with built-in strainer.",
                    "Professional bar shaker smelling of gin, vermouth, and bitters.", null)
                    .WithRooms(RoomType.BarLounge, RoomType.DiningRoom, RoomType.CrownPenthouse, RoomType.CardRoom)
                    .WithAnchors(AnchorCategory.Counter, AnchorCategory.Tabletop)
                    .WithCollider(new Vector3(0.22f, 0.35f, 0.22f)),

                new ObjectDefinition("obj_steamer_trunk", "Monogrammed Leather Steamer Trunk", ObjectCategory.Container, ObjectRole.Decorative,
                    null, "WoodWalnut",
                    "A brass-cornered vulcanized fiber travel trunk covered in ocean liner decals.",
                    "Heavy trunk labeled 'Cunard White Star Line - Southampton to Cherbourg'.", null)
                    .WithRooms(RoomType.LuggageStorage, RoomType.HousekeepingStorage, RoomType.GuestRoom101, RoomType.GuestRoom104, RoomType.GuestRoom201, RoomType.GuestRoom204, RoomType.VIPSuite302)
                    .WithAnchors(AnchorCategory.FloorCorner, AnchorCategory.ServiceRack)
                    .WithCollider(new Vector3(0.85f, 0.50f, 0.55f)),

                new ObjectDefinition("obj_banker_lamp", "Emerald Banker's Desk Lamp", ObjectCategory.Valuable, ObjectRole.Decorative,
                    null, "GreenFelt",
                    "A heavy brass desk lamp with an oblong emerald-green glass shade.",
                    "Classic banker's lamp casting a warm emerald hue over the blotter.", null)
                    .WithRooms(RoomType.StaffOffice, RoomType.Reception, RoomType.Library, RoomType.PresidentialStudy, RoomType.GuestRoom101, RoomType.GuestRoom103, RoomType.GuestRoom105)
                    .WithAnchors(AnchorCategory.Desk, AnchorCategory.Tabletop)
                    .WithCollider(new Vector3(0.30f, 0.38f, 0.25f)),

                new ObjectDefinition("obj_physician_bag", "Black Leather Doctor's Valise", ObjectCategory.Container, ObjectRole.InvestigativeEvidence,
                    null, "WoodWalnut",
                    "A Gladstone physician's bag with brass latch and initials 'S.S.'.",
                    "Doctor Sterling's medical satchel containing glass vials, morphine ampoules, and a nickel stethoscope.", "evidence_doctor_bag")
                    .WithRooms(RoomType.GuestRoom103, RoomType.Reception, RoomType.StaffOffice)
                    .WithAnchors(AnchorCategory.Desk, AnchorCategory.Nightstand, AnchorCategory.Tabletop)
                    .WithCollider(new Vector3(0.45f, 0.30f, 0.30f))
            };

            return _catalog;
        }

        public static void PopulateRooms(Transform parent, List<RoomDefinition> rooms, SeededRandom rng,
            Dictionary<string, Material> mats, Dictionary<string, string> mandatoryPlacements = null)
        {
            var catalog = GetCatalog();
            var spawnedRoot = new GameObject("Hotel_PlacedObjects");
            if (parent != null) spawnedRoot.transform.SetParent(parent);

            // 1. Place Mandatory Scenario Evidence First
            if (mandatoryPlacements != null)
            {
                foreach (var kvp in mandatoryPlacements)
                {
                    string objectId = kvp.Key;
                    string targetRoomId = kvp.Value;

                    var def = catalog.Find(o => o.ObjectId == objectId);
                    var room = rooms.Find(r => r.RoomId == targetRoomId);

                    if (def != null && room != null)
                    {
                        // Find eligible unoccupied anchor in this room
                        RoomAnchor chosenAnchor = null;
                        foreach (var anchor in room.Anchors)
                        {
                            if (!anchor.IsOccupied && (def.EligibleAnchors.Count == 0 || def.EligibleAnchors.Contains(anchor.Category)))
                            {
                                chosenAnchor = anchor;
                                break;
                            }
                        }

                        // Fallback to any unoccupied anchor
                        if (chosenAnchor == null && room.Anchors.Count > 0)
                        {
                            foreach (var a in room.Anchors)
                            {
                                if (!a.IsOccupied) { chosenAnchor = a; break; }
                            }
                        }

                        if (chosenAnchor != null)
                        {
                            SpawnObjectAtAnchor(spawnedRoot.transform, def, chosenAnchor, mats);
                        }
                    }
                }
            }

            // 2. Seeded Contextual Clutter & Ambient Placement
            foreach (var room in rooms)
            {
                foreach (var anchor in room.Anchors)
                {
                    if (anchor.IsOccupied) continue;

                    // 15% chance of empty anchor so surfaces feel richly populated while retaining natural breathing room
                    if (rng.NextFloat() < 0.15f) continue;

                    // Find matching candidate objects from catalog
                    var candidates = catalog.FindAll(o =>
                        (o.EligibleRooms.Count == 0 || o.EligibleRooms.Contains(room.Type)) &&
                        (o.EligibleAnchors.Count == 0 || o.EligibleAnchors.Contains(anchor.Category)));

                    if (candidates.Count > 0)
                    {
                        var selectedObj = rng.NextItem(candidates);
                        SpawnObjectAtAnchor(spawnedRoot.transform, selectedObj, anchor, mats);
                    }
                }
            }
        }

        public static GameObject SpawnObjectAtAnchor(Transform parent, ObjectDefinition def, RoomAnchor anchor, Dictionary<string, Material> mats)
        {
            anchor.IsOccupied = true;
            anchor.OccupyingObjectId = def.ObjectId;

            var objGo = new GameObject($"Prop_{def.ObjectId}");
            objGo.transform.SetParent(parent);
            objGo.transform.position = anchor.LocalPosition + def.LocalOffset;
            objGo.transform.rotation = Quaternion.Euler(anchor.LocalRotation);

            // Visual mesh
            GameObject visual = null;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(def.ModelPrefabPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(def.ModelPrefabPath);
                if (prefab != null)
                {
                    visual = UnityEngine.Object.Instantiate(prefab, objGo.transform);
                    visual.name = "Model";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                }
            }
#endif

            if (visual == null)
            {
                visual = CreateBespokePropVisual(def, objGo, mats);
            }
            else
            {
                // Apply Material to instantiated prefab
                if (!string.IsNullOrEmpty(def.MaterialKey) && mats.TryGetValue(def.MaterialKey, out var mat))
                {
                    var rends = visual.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in rends) r.sharedMaterial = mat;
                }
            }

            // Collider Setup
            var col = objGo.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = objGo.AddComponent<BoxCollider>();
            }
            col.size = def.ColliderSize;
            col.center = new Vector3(0f, def.ColliderSize.y * 0.5f, 0f);
            col.isTrigger = false;

            // Attach 3D Physics prop integration for pickup, throw, and collision audio
            var rb = objGo.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = objGo.AddComponent<Rigidbody>();
                rb.mass = def.Category == ObjectCategory.Tool || def.Category == ObjectCategory.Valuable ? 3.5f : 0.8f;
                rb.linearDamping = 0.5f;
                rb.angularDamping = 0.5f;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            if (objGo.GetComponent<TomorrowsCrime.PhysicsEngine.PhysicsProp>() == null)
            {
                var pp = objGo.AddComponent<TomorrowsCrime.PhysicsEngine.PhysicsProp>();
                pp.PropDisplayName = def.DisplayName;
                pp.MaterialType = def.Category == ObjectCategory.Valuable || def.Category == ObjectCategory.Tool ?
                    TomorrowsCrime.PhysicsEngine.PhysicsMaterialType.Metal :
                    TomorrowsCrime.PhysicsEngine.PhysicsMaterialType.Wood;
            }

            // Evidence Inspection or Key Component
            if (!string.IsNullOrEmpty(def.LinkedEvidenceId))
            {
                if (def.Category == ObjectCategory.Key)
                {
                    var keyComp = objGo.AddComponent<InteractableKey>();
                    // Create or load evidence definition asset
                    keyComp.KeyEvidence = GetOrCreateEvidenceDefinition(def);
                }
                else
                {
                    var inspectable = objGo.AddComponent<InspectableEvidence>();
                    inspectable.Definition = GetOrCreateEvidenceDefinition(def);
                }
            }

            return objGo;
        }

        private static EvidenceDefinition GetOrCreateEvidenceDefinition(ObjectDefinition def)
        {
#if UNITY_EDITOR
            string folder = "Assets/_Project/Data/Evidence";
            string assetPath = $"{folder}/{def.LinkedEvidenceId}.asset";

            var asset = AssetDatabase.LoadAssetAtPath<EvidenceDefinition>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<EvidenceDefinition>();
                asset.Initialize(
                    def.LinkedEvidenceId,
                    def.DisplayName,
                    def.ShortDescription ?? def.DisplayName,
                    def.DetailedDescription ?? def.ShortDescription ?? def.DisplayName,
                    def.Category.ToString()
                );
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
            }

            return asset;
#else
            var asset = ScriptableObject.CreateInstance<EvidenceDefinition>();
            asset.Initialize(
                def.LinkedEvidenceId,
                def.DisplayName,
                def.ShortDescription ?? def.DisplayName,
                def.DetailedDescription ?? def.ShortDescription ?? def.DisplayName,
                def.Category.ToString()
            );
            return asset;
#endif
        }

        private static GameObject CreateBespokePropVisual(ObjectDefinition def, GameObject objGo, Dictionary<string, Material> mats)
        {
            var visual = new GameObject("VisualModel");
            visual.transform.SetParent(objGo.transform, false);

            Material brassMat = null, mahoganyMat = null, walnutMat = null, paperMat = null, steelMat = null, amberMat = null, glassMat = null;
            if (mats != null)
            {
                mats.TryGetValue("BrassGold", out brassMat);
                mats.TryGetValue("WoodMahogany", out mahoganyMat);
                mats.TryGetValue("WoodWalnut", out walnutMat);
                mats.TryGetValue("ShirtWhite", out paperMat);
                mats.TryGetValue("FloorSlate", out steelMat);
                mats.TryGetValue("AmberGlow", out amberMat);
                mats.TryGetValue("UniformNavy", out glassMat);
            }

            string id = def.ObjectId.ToLower();

            if (id.Contains("cleaver"))
            {
                // French Meat Cleaver: Steel Blade + Rosewood Handle
                var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = "Cleaver_Blade";
                blade.transform.SetParent(visual.transform, false);
                blade.transform.localPosition = new Vector3(0f, 0.10f, 0.08f);
                blade.transform.localScale = new Vector3(0.03f, 0.18f, 0.28f);
                if (steelMat != null) blade.GetComponent<Renderer>().sharedMaterial = steelMat;
                DestroyCollider(blade);

                var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                handle.name = "Cleaver_Handle";
                handle.transform.SetParent(visual.transform, false);
                handle.transform.localPosition = new Vector3(0f, 0.05f, -0.12f);
                handle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                handle.transform.localScale = new Vector3(0.045f, 0.10f, 0.045f);
                if (mahoganyMat != null) handle.GetComponent<Renderer>().sharedMaterial = mahoganyMat;
                DestroyCollider(handle);
            }
            else if (id.Contains("candlestick"))
            {
                // Heavy Brass Candlestick: Turned Brass Base + Stem + Wax Candle + Wick
                var basePlate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                basePlate.name = "Candle_Base";
                basePlate.transform.SetParent(visual.transform, false);
                basePlate.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                basePlate.transform.localScale = new Vector3(0.24f, 0.02f, 0.24f);
                if (brassMat != null) basePlate.GetComponent<Renderer>().sharedMaterial = brassMat;
                DestroyCollider(basePlate);

                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "Candle_Stem";
                stem.transform.SetParent(visual.transform, false);
                stem.transform.localPosition = new Vector3(0f, 0.16f, 0f);
                stem.transform.localScale = new Vector3(0.065f, 0.14f, 0.065f);
                if (brassMat != null) stem.GetComponent<Renderer>().sharedMaterial = brassMat;
                DestroyCollider(stem);

                var candle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                candle.name = "Wax_Candle";
                candle.transform.SetParent(visual.transform, false);
                candle.transform.localPosition = new Vector3(0f, 0.36f, 0f);
                candle.transform.localScale = new Vector3(0.055f, 0.10f, 0.055f);
                if (paperMat != null) candle.GetComponent<Renderer>().sharedMaterial = paperMat;
                DestroyCollider(candle);

                var wick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wick.name = "Candle_Wick";
                wick.transform.SetParent(visual.transform, false);
                wick.transform.localPosition = new Vector3(0f, 0.47f, 0f);
                wick.transform.localScale = new Vector3(0.01f, 0.015f, 0.01f);
                if (steelMat != null) wick.GetComponent<Renderer>().sharedMaterial = steelMat;
                DestroyCollider(wick);
            }
            else if (id.Contains("poison") || id.Contains("vial") || id.Contains("bottle"))
            {
                // Apothecary Bottle / Wine Bottle: Bottle Body + Neck + Cork + Label
                var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                body.name = "Bottle_Body";
                body.transform.SetParent(visual.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                body.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
                var botMat = id.Contains("poison") ? (amberMat ?? brassMat) : (glassMat ?? mahoganyMat);
                if (botMat != null) body.GetComponent<Renderer>().sharedMaterial = botMat;
                DestroyCollider(body);

                var neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                neck.name = "Bottle_Neck";
                neck.transform.SetParent(visual.transform, false);
                neck.transform.localPosition = new Vector3(0f, 0.28f, 0f);
                neck.transform.localScale = new Vector3(0.045f, 0.06f, 0.045f);
                if (botMat != null) neck.GetComponent<Renderer>().sharedMaterial = botMat;
                DestroyCollider(neck);

                var cork = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cork.name = "Bottle_Cork";
                cork.transform.SetParent(visual.transform, false);
                cork.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                cork.transform.localScale = new Vector3(0.048f, 0.02f, 0.048f);
                if (walnutMat != null) cork.GetComponent<Renderer>().sharedMaterial = walnutMat;
                DestroyCollider(cork);

                var label = GameObject.CreatePrimitive(PrimitiveType.Cube);
                label.name = "Poison_Label";
                label.transform.SetParent(visual.transform, false);
                label.transform.localPosition = new Vector3(0f, 0.12f, 0.062f);
                label.transform.localScale = new Vector3(0.08f, 0.09f, 0.01f);
                if (paperMat != null) label.GetComponent<Renderer>().sharedMaterial = paperMat;
                DestroyCollider(label);
            }
            else if (id.Contains("wrench"))
            {
                // Heavy Steel Pipe Wrench: Handle + Threaded Collar + L-Jaw
                var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                handle.name = "Wrench_Handle";
                handle.transform.SetParent(visual.transform, false);
                handle.transform.localPosition = new Vector3(0f, 0.03f, -0.06f);
                handle.transform.localScale = new Vector3(0.04f, 0.025f, 0.34f);
                if (steelMat != null) handle.GetComponent<Renderer>().sharedMaterial = steelMat;
                DestroyCollider(handle);

                var jaw = GameObject.CreatePrimitive(PrimitiveType.Cube);
                jaw.name = "Wrench_Jaw";
                jaw.transform.SetParent(visual.transform, false);
                jaw.transform.localPosition = new Vector3(0f, 0.06f, 0.15f);
                jaw.transform.localScale = new Vector3(0.055f, 0.08f, 0.12f);
                if (steelMat != null) jaw.GetComponent<Renderer>().sharedMaterial = steelMat;
                DestroyCollider(jaw);

                var collar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                collar.name = "Wrench_KnurledCollar";
                collar.transform.SetParent(visual.transform, false);
                collar.transform.localPosition = new Vector3(0f, 0.04f, 0.07f);
                collar.transform.localScale = new Vector3(0.055f, 0.03f, 0.055f);
                if (brassMat != null) collar.GetComponent<Renderer>().sharedMaterial = brassMat;
                DestroyCollider(collar);
            }
            else if (id.Contains("letter") || id.Contains("memo") || id.Contains("sheet_music"))
            {
                // Parchment Stationery Document + Brass Fountain Pen
                var paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
                paper.name = "Paper_Sheet";
                paper.transform.SetParent(visual.transform, false);
                paper.transform.localPosition = new Vector3(0f, 0.005f, 0f);
                paper.transform.localScale = new Vector3(0.30f, 0.008f, 0.40f);
                if (paperMat != null) paper.GetComponent<Renderer>().sharedMaterial = paperMat;
                DestroyCollider(paper);

                var pen = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pen.name = "Fountain_Pen";
                pen.transform.SetParent(visual.transform, false);
                pen.transform.localPosition = new Vector3(0.12f, 0.015f, 0.08f);
                pen.transform.localRotation = Quaternion.Euler(0f, 25f, 90f);
                pen.transform.localScale = new Vector3(0.012f, 0.10f, 0.012f);
                if (brassMat != null) pen.GetComponent<Renderer>().sharedMaterial = brassMat;
                DestroyCollider(pen);
            }
            else if (id.Contains("register") || id.Contains("ledger"))
            {
                // Leather-Bound Ledger Book
                var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cover.name = "Ledger_Cover";
                cover.transform.SetParent(visual.transform, false);
                cover.transform.localPosition = new Vector3(0f, 0.035f, 0f);
                cover.transform.localScale = new Vector3(0.38f, 0.07f, 0.48f);
                if (mahoganyMat != null) cover.GetComponent<Renderer>().sharedMaterial = mahoganyMat;
                DestroyCollider(cover);

                var pages = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pages.name = "Ledger_Pages";
                pages.transform.SetParent(visual.transform, false);
                pages.transform.localPosition = new Vector3(0.01f, 0.035f, 0f);
                pages.transform.localScale = new Vector3(0.35f, 0.058f, 0.45f);
                if (paperMat != null) pages.GetComponent<Renderer>().sharedMaterial = paperMat;
                DestroyCollider(pages);
            }
            else if (id.Contains("glass") || id.Contains("tumbler"))
            {
                // Cut Crystal Tumbler with Amber Liquid
                var glass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                glass.name = "Tumbler_Glass";
                glass.transform.SetParent(visual.transform, false);
                glass.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                glass.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f);
                if (paperMat != null) glass.GetComponent<Renderer>().sharedMaterial = paperMat;
                DestroyCollider(glass);

                var liquor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                liquor.name = "Tumbler_AmberLiquor";
                liquor.transform.SetParent(visual.transform, false);
                liquor.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                liquor.transform.localScale = new Vector3(0.10f, 0.04f, 0.10f);
                if (amberMat != null) liquor.GetComponent<Renderer>().sharedMaterial = amberMat;
                DestroyCollider(liquor);
            }
            else if (id.Contains("cigar") || id.Contains("humidor"))
            {
                // Spanish Cedar Cigar Humidor Box
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = "Humidor_Box";
                box.transform.SetParent(visual.transform, false);
                box.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                box.transform.localScale = new Vector3(0.34f, 0.15f, 0.24f);
                if (walnutMat != null) box.GetComponent<Renderer>().sharedMaterial = walnutMat;
                DestroyCollider(box);

                var latch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                latch.name = "Brass_Latch";
                latch.transform.SetParent(visual.transform, false);
                latch.transform.localPosition = new Vector3(0f, 0.08f, -0.125f);
                latch.transform.localScale = new Vector3(0.04f, 0.04f, 0.015f);
                if (brassMat != null) latch.GetComponent<Renderer>().sharedMaterial = brassMat;
                DestroyCollider(latch);
            }
            else if (id.Contains("bucket"))
            {
                // Galvanized Wash Bucket
                var bucket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bucket.name = "Wash_Bucket";
                bucket.transform.SetParent(visual.transform, false);
                bucket.transform.localPosition = new Vector3(0f, 0.16f, 0f);
                bucket.transform.localScale = new Vector3(0.30f, 0.16f, 0.30f);
                if (steelMat != null) bucket.GetComponent<Renderer>().sharedMaterial = steelMat;
                DestroyCollider(bucket);
            }
            else
            {
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.name = "Prop_Geometry";
                fallback.transform.SetParent(visual.transform, false);
                fallback.transform.localPosition = new Vector3(0f, def.ColliderSize.y * 0.5f, 0f);
                fallback.transform.localScale = def.ColliderSize;
                if (!string.IsNullOrEmpty(def.MaterialKey) && mats != null && mats.TryGetValue(def.MaterialKey, out var m))
                {
                    fallback.GetComponent<Renderer>().sharedMaterial = m;
                }
                DestroyCollider(fallback);
            }

            return visual;
        }

        private static void DestroyCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);
        }
    }
}
