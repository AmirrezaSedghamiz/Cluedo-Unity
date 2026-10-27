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
                    .WithAnchors(AnchorCategory.Desk, AnchorCategory.Nightstand, AnchorCategory.FloorCorner)
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
                    .WithAnchors(AnchorCategory.Tabletop, AnchorCategory.Desk, AnchorCategory.FloorCorner)
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
                    .WithCollider(new Vector3(0.4f, 0.6f, 0.4f))
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

                    // 40% chance of empty search for believable detective environment
                    if (rng.NextFloat() < 0.40f) continue;

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

        private static GameObject SpawnObjectAtAnchor(Transform parent, ObjectDefinition def, RoomAnchor anchor, Dictionary<string, Material> mats)
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
                // Fallback primitive
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Primitive";
                visual.transform.SetParent(objGo.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = def.ColliderSize;
            }

            // Apply Material
            if (!string.IsNullOrEmpty(def.MaterialKey) && mats.TryGetValue(def.MaterialKey, out var mat))
            {
                var rends = visual.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends) r.sharedMaterial = mat;
            }

            // Collider Setup
            var col = objGo.GetComponent<BoxCollider>() ?? objGo.AddComponent<BoxCollider>();
            col.size = def.ColliderSize;
            col.center = new Vector3(0f, def.ColliderSize.y * 0.5f, 0f);
            col.isTrigger = false;

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
    }
}
