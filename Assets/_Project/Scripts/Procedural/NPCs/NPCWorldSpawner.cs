using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TomorrowsCrime.NPC;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TomorrowsCrime.Procedural.NPCs
{
    /// <summary>
    /// Instantiates and populates the procedural NPC roster throughout Meridian House.
    /// Spawns distinct, stylized 3D character models with custom silhouettes, outfits,
    /// headwear, and signature accessories for all 10 roles.
    /// Sets up colliders, interaction controllers, waypoints, and dynamic 5-minute routines.
    /// </summary>
    public static class NPCWorldSpawner
    {
        public static GameObject SpawnRoster(Transform parent, List<NPCProfile> roster, Dictionary<string, Material> mats)
        {
            return SpawnRoster(parent, roster, null, mats);
        }

        public static GameObject SpawnRoster(Transform parent, List<NPCProfile> roster, Dictionary<string, NPCRoutinePlan> routines, Dictionary<string, Material> mats)
        {
            if (roster == null || roster.Count == 0) return null;

            var rosterRoot = new GameObject("Hotel_NPC_Roster");
            if (parent != null) rosterRoot.transform.SetParent(parent);

#if UNITY_EDITOR
            var pierrePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/NPC_Concierge_Pierre.obj");
#else
            GameObject pierrePrefab = null;
#endif

            foreach (var profile in roster)
            {
                var npcGo = new GameObject($"NPC_{profile.NpcId}_{profile.DisplayName.Replace(" ", "")}");
                npcGo.transform.SetParent(rosterRoot.transform);
                npcGo.transform.position = profile.BaselinePosition;
                npcGo.transform.rotation = Quaternion.Euler(profile.BaselineRotation);

                // Build Distinct 3D Visual Model
                GameObject visual = null;
                if (profile.Role == NPCRole.Concierge && pierrePrefab != null)
                {
                    visual = Object.Instantiate(pierrePrefab, npcGo.transform);
                    visual.name = "Model_Concierge_Pierre";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                    visual.transform.localScale = Vector3.one;

                    if (mats != null && mats.TryGetValue("UniformNavy", out var navyMat))
                    {
                        foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
                        {
                            r.sharedMaterial = navyMat;
                        }
                    }
                }
                else
                {
                    visual = CreateDistinctStylizedCharacter(npcGo.transform, profile, mats);
                }

                // Physical Capsule Collider for Movement Blocking & Raycasts
                var col = npcGo.AddComponent<CapsuleCollider>();
                col.radius = 0.40f;
                col.height = 1.95f;
                col.center = new Vector3(0f, 0.98f, 0f);
                col.isTrigger = false;

                // NavMesh Navigation Agent
                var agent = npcGo.AddComponent<NavMeshAgent>();
                agent.height = 1.95f;
                agent.radius = 0.40f;
                agent.speed = 3.0f;
                agent.stoppingDistance = 0.8f;

                // NPC Behavior Controller & Dialogue Integration
                var controller = npcGo.AddComponent<NPCController>();

                // Register Waypoint Transforms for Scheduled Routine Activities
                NPCRoutinePlan routinePlan = null;
                if (routines != null && routines.TryGetValue(profile.NpcId, out routinePlan) && routinePlan.Activities.Count > 0)
                {
                    var waypointsRoot = new GameObject($"Waypoints_{profile.DisplayName.Replace(" ", "")}");
                    waypointsRoot.transform.SetParent(rosterRoot.transform);
                    waypointsRoot.transform.position = Vector3.zero;

                    for (int i = 0; i < routinePlan.Activities.Count; i++)
                    {
                        var act = routinePlan.Activities[i];
                        string wpKey = $"{act.ActivityName}_{i}";
                        var wpGo = new GameObject($"WP_{wpKey}");
                        wpGo.transform.SetParent(waypointsRoot.transform);
                        wpGo.transform.position = act.Destination;
                        controller.SetWaypoint(wpKey, wpGo.transform);
                        controller.SetWaypoint(act.ActivityName, wpGo.transform);
                    }
                }

                controller.InitializeFromProfile(profile, routinePlan);
            }

            Debug.Log($"[NPCWorldSpawner] Successfully populated hotel with {roster.Count} distinct, living NPCs with 5-minute routines across all floors.");
            return rosterRoot;
        }

        /// <summary>
        /// Generates a rich, stylized Art Deco 3D character mesh with role-specific proportions,
        /// layered clothing, unique headwear, and signature handheld accessories.
        /// </summary>
        public static GameObject CreateDistinctStylizedCharacter(Transform parent, NPCProfile profile, Dictionary<string, Material> mats)
        {
            var charRoot = new GameObject($"Visual_{profile.DisplayName.Replace(" ", "")}");
            charRoot.transform.SetParent(parent);
            charRoot.transform.localPosition = Vector3.zero;
            charRoot.transform.localRotation = Quaternion.identity;

            // Resolve Palette
            Material skinMat = GetMat(mats, "MarbleIvory", new Color(0.94f, 0.82f, 0.72f));
            Material suitNavy = GetMat(mats, "UniformNavy", new Color(0.12f, 0.16f, 0.28f));
            Material shirtWhite = GetMat(mats, "ShirtWhite", new Color(0.96f, 0.96f, 0.98f));
            Material tieBurgundy = GetMat(mats, "TieBurgundy", new Color(0.55f, 0.12f, 0.16f));
            Material trousersCharcoal = GetMat(mats, "TrousersCharcoal", new Color(0.20f, 0.20f, 0.22f));
            Material velvetGold = GetMat(mats, "VelvetGold", new Color(0.85f, 0.70f, 0.25f));
            Material velvetEmerald = GetMat(mats, "VelvetEmerald", new Color(0.12f, 0.40f, 0.22f));
            Material woodMahogany = GetMat(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material woodWalnut = GetMat(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));
            Material brassGold = GetMat(mats, "BrassGold", new Color(0.88f, 0.75f, 0.35f));
            Material slateBlack = GetMat(mats, "FloorSlate", new Color(0.10f, 0.10f, 0.12f));

            bool isFemale = profile.Role == NPCRole.Receptionist || profile.Role == NPCRole.HousekeepingMaid ||
                            profile.Role == NPCRole.SocietyLady || profile.Role == NPCRole.JournalistGuest;

            // Height and Silhouette adjustments
            float heightScale = 1.0f;
            float shoulderWidth = 0.52f;
            float torsoDepth = 0.30f;

            if (profile.Role == NPCRole.HeadChef) { heightScale = 1.05f; shoulderWidth = 0.62f; torsoDepth = 0.38f; }
            else if (profile.Role == NPCRole.MaintenanceEngineer) { heightScale = 1.02f; shoulderWidth = 0.58f; torsoDepth = 0.34f; }
            else if (profile.Role == NPCRole.WealthyGuest) { heightScale = 1.06f; shoulderWidth = 0.54f; }
            else if (isFemale) { heightScale = 0.96f; shoulderWidth = 0.44f; torsoDepth = 0.26f; }

            // 1. Torso & Upper Clothing
            Material jacketMat = trousersCharcoal;
            if (profile.Role == NPCRole.Receptionist) jacketMat = suitNavy;
            else if (profile.Role == NPCRole.HeadChef) jacketMat = shirtWhite;
            else if (profile.Role == NPCRole.HousekeepingMaid) jacketMat = slateBlack;
            else if (profile.Role == NPCRole.MaintenanceEngineer) jacketMat = suitNavy; // Denim
            else if (profile.Role == NPCRole.HotelManager) jacketMat = slateBlack;
            else if (profile.Role == NPCRole.WealthyGuest) jacketMat = woodWalnut; // Tweed
            else if (profile.Role == NPCRole.SocietyLady) jacketMat = tieBurgundy; // Silk gown
            else if (profile.Role == NPCRole.DoctorGuest) jacketMat = woodMahogany; // Wool coat
            else if (profile.Role == NPCRole.JournalistGuest) jacketMat = velvetGold; // Trench coat

            var torso = CreatePart(charRoot.transform, PrimitiveType.Cube, "Torso",
                new Vector3(0f, 1.15f * heightScale, 0f),
                new Vector3(shoulderWidth, 0.65f * heightScale, torsoDepth), jacketMat);

            // Shirt / Vest Inlay
            if (profile.Role != NPCRole.SocietyLady && profile.Role != NPCRole.HeadChef)
            {
                CreatePart(torso.transform, PrimitiveType.Cube, "ShirtV",
                    new Vector3(0f, 0.15f, 0.51f),
                    new Vector3(0.22f, 0.45f, 0.05f), shirtWhite);

                // Tie / Ascot / Bowtie
                Material tieMat = (profile.Role == NPCRole.HotelManager || profile.Role == NPCRole.WealthyGuest) ? tieBurgundy : slateBlack;
                CreatePart(torso.transform, PrimitiveType.Cube, "Necktie",
                    new Vector3(0f, 0.05f, 0.54f),
                    new Vector3(0.08f, 0.38f, 0.04f), tieMat);
            }

            // 2. Neck & Head
            CreatePart(charRoot.transform, PrimitiveType.Cylinder, "Neck",
                new Vector3(0f, 1.50f * heightScale, 0f),
                new Vector3(0.14f, 0.10f * heightScale, 0.14f), skinMat);

            var head = CreatePart(charRoot.transform, PrimitiveType.Sphere, "Head",
                new Vector3(0f, 1.68f * heightScale, 0f),
                new Vector3(0.30f, 0.34f, 0.30f), skinMat);

            // Nose
            CreatePart(head.transform, PrimitiveType.Cube, "Nose",
                new Vector3(0f, 0f, 0.52f),
                new Vector3(0.06f, 0.09f, 0.10f), skinMat);

            // 3. Lower Body (Legs or Full Skirt)
            if (profile.Role == NPCRole.SocietyLady)
            {
                // Flowing Evening Gown Skirt
                CreatePart(charRoot.transform, PrimitiveType.Cylinder, "GownSkirt",
                    new Vector3(0f, 0.45f * heightScale, 0f),
                    new Vector3(0.50f, 0.48f * heightScale, 0.46f), jacketMat);
            }
            else if (profile.Role == NPCRole.HousekeepingMaid || profile.Role == NPCRole.Receptionist)
            {
                // A-Line Skirt
                CreatePart(charRoot.transform, PrimitiveType.Cylinder, "Skirt",
                    new Vector3(0f, 0.60f * heightScale, 0f),
                    new Vector3(0.48f, 0.30f * heightScale, 0.42f), jacketMat);

                // Frilled White Maid Apron Over Skirt
                if (profile.Role == NPCRole.HousekeepingMaid)
                {
                    CreatePart(charRoot.transform, PrimitiveType.Cube, "MaidApron",
                        new Vector3(0f, 0.72f * heightScale, 0.16f),
                        new Vector3(0.32f, 0.50f * heightScale, 0.04f), shirtWhite);
                }

                // Legs below skirt
                CreatePart(charRoot.transform, PrimitiveType.Cube, "LeftLeg",
                    new Vector3(-0.11f, 0.22f * heightScale, 0f),
                    new Vector3(0.14f, 0.45f * heightScale, 0.16f), slateBlack);
                CreatePart(charRoot.transform, PrimitiveType.Cube, "RightLeg",
                    new Vector3(0.11f, 0.22f * heightScale, 0f),
                    new Vector3(0.14f, 0.45f * heightScale, 0.16f), slateBlack);
            }
            else
            {
                // Tailored Trousers
                Material pantsMat = (profile.Role == NPCRole.HeadChef) ? slateBlack : jacketMat;
                CreatePart(charRoot.transform, PrimitiveType.Cube, "LeftLeg",
                    new Vector3(-0.14f * (shoulderWidth / 0.52f), 0.45f * heightScale, 0f),
                    new Vector3(0.18f, 0.85f * heightScale, 0.22f), pantsMat);
                CreatePart(charRoot.transform, PrimitiveType.Cube, "RightLeg",
                    new Vector3(0.14f * (shoulderWidth / 0.52f), 0.45f * heightScale, 0f),
                    new Vector3(0.18f, 0.85f * heightScale, 0.22f), pantsMat);

                // Polished Leather Shoes
                CreatePart(charRoot.transform, PrimitiveType.Cube, "Shoe_L",
                    new Vector3(-0.14f * (shoulderWidth / 0.52f), 0.05f, 0.04f),
                    new Vector3(0.19f, 0.10f, 0.28f), slateBlack);
                CreatePart(charRoot.transform, PrimitiveType.Cube, "Shoe_R",
                    new Vector3(0.14f * (shoulderWidth / 0.52f), 0.05f, 0.04f),
                    new Vector3(0.19f, 0.10f, 0.28f), slateBlack);
            }

            // 4. Arms
            float armSpacing = shoulderWidth * 0.56f;
            var leftArm = CreatePart(charRoot.transform, PrimitiveType.Cube, "Arm_L",
                new Vector3(-armSpacing, 1.08f * heightScale, 0f),
                new Vector3(0.14f, 0.65f * heightScale, 0.16f), jacketMat);
            var rightArm = CreatePart(charRoot.transform, PrimitiveType.Cube, "Arm_R",
                new Vector3(armSpacing, 1.08f * heightScale, 0f),
                new Vector3(0.14f, 0.65f * heightScale, 0.16f), jacketMat);

            // Hands
            CreatePart(leftArm.transform, PrimitiveType.Sphere, "Hand_L",
                new Vector3(0f, -0.52f, 0f),
                new Vector3(0.90f, 0.35f, 0.90f), skinMat);
            var rightHand = CreatePart(rightArm.transform, PrimitiveType.Sphere, "Hand_R",
                new Vector3(0f, -0.52f, 0f),
                new Vector3(0.90f, 0.35f, 0.90f), skinMat);

            // 5. Distinct Headwear, Hair & Accessories Per Role
            BuildRoleHeadwearAndProps(charRoot, head, rightHand, profile.Role, heightScale, mats);

            // Clean up child primitive colliders (the root CapsuleCollider handles physics)
            foreach (var c in charRoot.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(c);
            }

            return charRoot;
        }

        private static void BuildRoleHeadwearAndProps(GameObject charRoot, GameObject head, GameObject rightHand,
            NPCRole role, float heightScale, Dictionary<string, Material> mats)
        {
            Material shirtWhite = GetMat(mats, "ShirtWhite", Color.white);
            Material brassGold = GetMat(mats, "BrassGold", new Color(0.88f, 0.75f, 0.35f));
            Material woodMahogany = GetMat(mats, "WoodMahogany", new Color(0.35f, 0.18f, 0.12f));
            Material slateBlack = GetMat(mats, "FloorSlate", new Color(0.10f, 0.10f, 0.12f));
            Material tieBurgundy = GetMat(mats, "TieBurgundy", new Color(0.55f, 0.12f, 0.16f));
            Material velvetGold = GetMat(mats, "VelvetGold", new Color(0.85f, 0.70f, 0.25f));
            Material woodWalnut = GetMat(mats, "WoodWalnut", new Color(0.28f, 0.16f, 0.10f));
            Material suitNavy = GetMat(mats, "UniformNavy", new Color(0.12f, 0.16f, 0.28f));

            switch (role)
            {
                case NPCRole.Concierge:
                    // Distinct Art Deco Concierge Pillbox / Kepi Hat with Gold Braiding
                    var kepi = CreatePart(head.transform, PrimitiveType.Cylinder, "Concierge_Kepi",
                        new Vector3(0f, 0.44f, 0f), new Vector3(0.95f, 0.22f, 0.95f), suitNavy);
                    CreatePart(kepi.transform, PrimitiveType.Cylinder, "Kepi_GoldBraid",
                        new Vector3(0f, 0.15f, 0f), new Vector3(1.02f, 0.06f, 1.02f), brassGold);
                    CreatePart(kepi.transform, PrimitiveType.Cube, "Kepi_Visor",
                        new Vector3(0f, -0.1f, 0.52f), new Vector3(0.70f, 0.04f, 0.32f), slateBlack);

                    // Handheld Signature: Master Brass Keyring with skeleton keys
                    var keyring = CreatePart(rightHand.transform, PrimitiveType.Cylinder, "Keyring_Loop",
                        new Vector3(0.08f, -0.05f, 0.12f), new Vector3(0.14f, 0.03f, 0.14f), brassGold);
                    keyring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePart(keyring.transform, PrimitiveType.Cube, "Key_Stem1",
                        new Vector3(0f, 0.14f, 0f), new Vector3(0.03f, 0.20f, 0.03f), brassGold);
                    CreatePart(keyring.transform, PrimitiveType.Cube, "Key_Stem2",
                        new Vector3(0.08f, 0.12f, 0f), new Vector3(0.03f, 0.18f, 0.03f), brassGold);
                    break;

                case NPCRole.HeadChef:
                    // Tall Pleated Chef's Toque Blanche
                    var toqueBase = CreatePart(head.transform, PrimitiveType.Cylinder, "Toque_Band",
                        new Vector3(0f, 0.50f, 0f), new Vector3(0.95f, 0.20f, 0.95f), shirtWhite);
                    CreatePart(toqueBase.transform, PrimitiveType.Cylinder, "Toque_Puff",
                        new Vector3(0f, 1.4f, 0f), new Vector3(1.35f, 1.2f, 1.35f), shirtWhite);

                    // Handheld Signature: Heavy French Kitchen Cleaver
                    var cleaver = CreatePart(rightHand.transform, PrimitiveType.Cube, "Cleaver_Blade",
                        new Vector3(0f, -0.2f, 0.25f), new Vector3(0.04f, 0.32f, 0.20f), shirtWhite);
                    CreatePart(cleaver.transform, PrimitiveType.Cylinder, "Cleaver_Handle",
                        new Vector3(0f, -0.6f, -0.2f), new Vector3(0.8f, 0.6f, 0.8f), woodMahogany);
                    break;

                case NPCRole.HousekeepingMaid:
                    // Frilled Lace Maid Headpiece
                    CreatePart(head.transform, PrimitiveType.Cube, "Maid_LaceCap",
                        new Vector3(0f, 0.48f, -0.1f), new Vector3(0.85f, 0.14f, 0.40f), shirtWhite);

                    // Handheld Signature: Feather Duster
                    var dusterHandle = CreatePart(rightHand.transform, PrimitiveType.Cylinder, "Duster_Handle",
                        new Vector3(0f, 0.1f, 0.2f), new Vector3(0.06f, 0.35f, 0.06f), woodWalnut);
                    CreatePart(dusterHandle.transform, PrimitiveType.Sphere, "Duster_Feathers",
                        new Vector3(0f, 1.1f, 0f), new Vector3(3.2f, 1.8f, 3.2f), shirtWhite);
                    break;

                case NPCRole.MaintenanceEngineer:
                    // Tweed Newsboy Flat Cap
                    var cap = CreatePart(head.transform, PrimitiveType.Cylinder, "FlatCap_Crown",
                        new Vector3(0f, 0.44f, -0.05f), new Vector3(1.15f, 0.15f, 1.15f), slateBlack);
                    CreatePart(cap.transform, PrimitiveType.Cube, "FlatCap_Visor",
                        new Vector3(0f, -0.2f, 0.55f), new Vector3(0.75f, 0.05f, 0.35f), slateBlack);

                    // Handheld Signature: Heavy Pipe Wrench
                    var wrench = CreatePart(rightHand.transform, PrimitiveType.Cube, "PipeWrench_Shaft",
                        new Vector3(0f, 0.1f, 0.25f), new Vector3(0.06f, 0.45f, 0.08f), tieBurgundy);
                    CreatePart(wrench.transform, PrimitiveType.Cube, "Wrench_HookJaw",
                        new Vector3(0f, 0.55f, 0.5f), new Vector3(0.08f, 0.18f, 0.22f), slateBlack);
                    break;

                case NPCRole.HotelManager:
                    // Slicked Dark Hair & Wireframe Spectacles
                    CreatePart(head.transform, PrimitiveType.Cube, "Manager_Hair",
                        new Vector3(0f, 0.42f, -0.05f), new Vector3(0.95f, 0.18f, 0.95f), slateBlack);
                    CreatePart(head.transform, PrimitiveType.Cylinder, "Glasses_Rim_L",
                        new Vector3(-0.25f, 0.1f, 0.48f), new Vector3(0.20f, 0.02f, 0.20f), brassGold);
                    CreatePart(head.transform, PrimitiveType.Cylinder, "Glasses_Rim_R",
                        new Vector3(0.25f, 0.1f, 0.48f), new Vector3(0.20f, 0.02f, 0.20f), brassGold);

                    // Handheld Signature: Leather Executive Folio
                    CreatePart(rightHand.transform, PrimitiveType.Cube, "Executive_Folio",
                        new Vector3(0.1f, 0f, 0.2f), new Vector3(0.08f, 0.38f, 0.28f), woodMahogany);
                    break;

                case NPCRole.WealthyGuest: // Lord Harrington
                    // Aristocratic Silver-White Hair & Gold Monocle & Walking Cane
                    CreatePart(head.transform, PrimitiveType.Cube, "Silver_Hair",
                        new Vector3(0f, 0.44f, -0.05f), new Vector3(1.02f, 0.20f, 1.02f), shirtWhite);
                    CreatePart(head.transform, PrimitiveType.Cylinder, "Gold_Monocle",
                        new Vector3(0.25f, 0.1f, 0.48f), new Vector3(0.22f, 0.02f, 0.22f), brassGold);

                    // Handheld Signature: Brass Lion-Head Walking Cane
                    var cane = CreatePart(rightHand.transform, PrimitiveType.Cylinder, "WalkingCane_Shaft",
                        new Vector3(0.12f, -0.2f, 0.15f), new Vector3(0.05f, 0.65f, 0.05f), slateBlack);
                    CreatePart(cane.transform, PrimitiveType.Sphere, "Cane_LionHead",
                        new Vector3(0f, 1.05f, 0f), new Vector3(2.5f, 1.8f, 2.5f), brassGold);
                    break;

                case NPCRole.SocietyLady: // Madame Valerie
                    // Glamour Waves Hair, Feather Boa, Champagne Coupe
                    CreatePart(head.transform, PrimitiveType.Sphere, "Glamour_Curls",
                        new Vector3(0f, 0.35f, -0.1f), new Vector3(1.1f, 0.8f, 1.1f), woodMahogany);
                    CreatePart(charRoot.transform, PrimitiveType.Cylinder, "Feather_Boa",
                        new Vector3(0f, 1.40f * heightScale, 0f), new Vector3(0.55f, 0.12f, 0.40f), shirtWhite);

                    // Handheld Signature: Champagne Coupe Glass
                    var stem = CreatePart(rightHand.transform, PrimitiveType.Cylinder, "Coupe_Stem",
                        new Vector3(0f, 0.12f, 0.15f), new Vector3(0.03f, 0.18f, 0.03f), brassGold);
                    CreatePart(stem.transform, PrimitiveType.Cylinder, "Coupe_Bowl",
                        new Vector3(0f, 1.0f, 0f), new Vector3(3.5f, 0.4f, 3.5f), brassGold);
                    break;

                case NPCRole.DoctorGuest: // Dr. Sterling
                    // Parted Hair & Physician's Black Leather Gladstone Bag
                    CreatePart(head.transform, PrimitiveType.Cube, "Doc_Hair",
                        new Vector3(0f, 0.42f, -0.05f), new Vector3(0.96f, 0.18f, 0.96f), woodWalnut);

                    // Handheld Signature: Doctor's Bag
                    var docBag = CreatePart(rightHand.transform, PrimitiveType.Cube, "Doctor_Bag",
                        new Vector3(0.15f, -0.2f, 0.05f), new Vector3(0.18f, 0.30f, 0.38f), slateBlack);
                    CreatePart(docBag.transform, PrimitiveType.Cylinder, "Bag_BrassLatch",
                        new Vector3(0f, 0.52f, 0f), new Vector3(0.4f, 0.05f, 0.4f), brassGold);
                    break;

                case NPCRole.JournalistGuest: // Vivian Monroe
                    // 1930s Reporter Fedora with "PRESS" Card & Open Reporter's Notebook
                    var fedora = CreatePart(head.transform, PrimitiveType.Cylinder, "Fedora_Brim",
                        new Vector3(0f, 0.42f, 0.05f), new Vector3(1.30f, 0.05f, 1.30f), woodWalnut);
                    fedora.transform.localRotation = Quaternion.Euler(6f, 0f, -4f);
                    CreatePart(fedora.transform, PrimitiveType.Cylinder, "Fedora_Crown",
                        new Vector3(0f, 2.2f, 0f), new Vector3(0.75f, 2.0f, 0.75f), woodWalnut);
                    // Press Card in Hatband
                    CreatePart(fedora.transform, PrimitiveType.Cube, "Press_Card",
                        new Vector3(0.38f, 2.4f, 0.1f), new Vector3(0.04f, 0.35f, 0.22f), shirtWhite);

                    // Handheld Signature: Reporter's Notebook
                    CreatePart(rightHand.transform, PrimitiveType.Cube, "Reporter_Notebook",
                        new Vector3(0.08f, 0.05f, 0.18f), new Vector3(0.05f, 0.25f, 0.18f), velvetGold);
                    break;

                case NPCRole.Receptionist: // Celeste Moreau
                    // Victory Rolls Coiffure & Ledger Clipboard
                    CreatePart(head.transform, PrimitiveType.Sphere, "Victory_Rolls",
                        new Vector3(0f, 0.40f, -0.05f), new Vector3(1.05f, 0.75f, 1.05f), woodMahogany);
                    CreatePart(charRoot.transform, PrimitiveType.Cube, "SilkScarf",
                        new Vector3(0f, 1.44f * heightScale, 0.12f), new Vector3(0.18f, 0.12f, 0.08f), tieBurgundy);

                    // Handheld Signature: Hotel Reservation Clipboard
                    var clip = CreatePart(rightHand.transform, PrimitiveType.Cube, "Reservation_Clipboard",
                        new Vector3(0.08f, 0.05f, 0.18f), new Vector3(0.04f, 0.34f, 0.24f), woodWalnut);
                    CreatePart(clip.transform, PrimitiveType.Cube, "Ledger_Paper",
                        new Vector3(0.55f, 0f, 0f), new Vector3(0.05f, 0.85f, 0.85f), shirtWhite);
                    break;
            }
        }

        private static GameObject CreatePart(Transform parent, PrimitiveType type, string name,
            Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.transform.localRotation = Quaternion.identity;

            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            return go;
        }

        private static Material GetMat(Dictionary<string, Material> mats, string key, Color fallbackColor)
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

        private static GameObject CreateStylizedMannequin(Transform parent)
        {
            var root = new GameObject("MannequinVisual");
            root.transform.SetParent(parent);
            root.transform.localPosition = Vector3.zero;

            // Torso
            var torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(root.transform);
            torso.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            torso.transform.localScale = new Vector3(0.55f, 0.75f, 0.32f);

            // Head
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform);
            head.transform.localPosition = new Vector3(0f, 1.72f, 0f);
            head.transform.localScale = new Vector3(0.32f, 0.38f, 0.32f);

            // Legs
            var legs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legs.name = "Legs";
            legs.transform.SetParent(root.transform);
            legs.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            legs.transform.localScale = new Vector3(0.42f, 0.85f, 0.28f);

            // Clean up child primitive colliders
            foreach (var c in root.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(c);
            }

            return root;
        }
    }
}
