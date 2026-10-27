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
    /// Instantiates and populates the procedural NPC roster throughout the hotel.
    /// Positions staff and guests at their designated posts across all 4 floors,
    /// sets up colliders, interaction controllers, and PBR character materials.
    /// </summary>
    public static class NPCWorldSpawner
    {
        public static GameObject SpawnRoster(Transform parent, List<NPCProfile> roster, Dictionary<string, Material> mats)
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

                // Visual Model
                GameObject visual = null;
                if (pierrePrefab != null)
                {
                    visual = Object.Instantiate(pierrePrefab, npcGo.transform);
                    visual.name = "Model";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                    visual.transform.localScale = Vector3.one;
                }
                else
                {
                    // Stylized Art Deco Mannequin Fallback
                    visual = CreateStylizedMannequin(npcGo.transform);
                }

                // Apply Costume Material
                if (mats != null && !string.IsNullOrEmpty(profile.MaterialKey) && mats.TryGetValue(profile.MaterialKey, out var charMat))
                {
                    var renderers = visual.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in renderers)
                    {
                        r.sharedMaterial = charMat;
                    }
                }

                // Physical Collider for Raycast & Movement Blocking
                var col = npcGo.AddComponent<CapsuleCollider>();
                col.radius = 0.45f;
                col.height = 2.0f;
                col.center = new Vector3(0f, 1.0f, 0f);
                col.isTrigger = false;

                // NavMesh Navigation Agent
                var agent = npcGo.AddComponent<NavMeshAgent>();
                agent.height = 2.0f;
                agent.radius = 0.4f;
                agent.speed = 3.0f;
                agent.stoppingDistance = 1.2f;

                // NPC Behavior Controller & Dialogue Integration
                var controller = npcGo.AddComponent<NPCController>();
                controller.InitializeFromProfile(profile);
            }

            Debug.Log($"[NPCWorldSpawner] Successfully populated hotel with {roster.Count} living NPCs across all floors.");
            return rosterRoot;
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

            // Clean up child primitive colliders (main collider is on root)
            foreach (var c in root.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(c);
            }

            return root;
        }
    }
}
