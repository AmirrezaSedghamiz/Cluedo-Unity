using System;
using UnityEngine;
using TomorrowsCrime.Procedural.Core;
using TomorrowsCrime.Procedural.Mystery;
using TomorrowsCrime.UI.Styling;

namespace TomorrowsCrime.Procedural.Debugging
{
    /// <summary>
    /// Developer-only generation inspector and seed debugging console.
    /// Toggled via F2. Displays procedural seed, scenario ground truth, NPC ecosystem,
    /// room inventory, object placements, and solvability validation diagnostics.
    /// </summary>
    public class GenerationInspectorUI : MonoBehaviour
    {
        private static GenerationInspectorUI _instance;
        public static GenerationInspectorUI Instance => _instance;

        [Header("State")]
        [SerializeField] private bool showUI = false;
        private string _seedInputField = "TC-1A2B3C4D";
        private int _selectedTab = 0;
        private Vector2 _scrollPos = Vector2.zero;

        private GeneratedInvestigation _currentInvestigation;
        public GeneratedInvestigation CurrentInvestigation => _currentInvestigation;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            // Initialize default baseline investigation if none set
            if (_currentInvestigation == null)
            {
                var seed = InvestigationSeed.FromCode(_seedInputField);
                _currentInvestigation = GenerationPipeline.GenerateInvestigation(seed);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                showUI = !showUI;
            }
        }

        public void SetInvestigation(GeneratedInvestigation investigation)
        {
            _currentInvestigation = investigation;
            if (investigation != null)
            {
                _seedInputField = investigation.Seed.SeedCode;
            }
        }

        private void OnGUI()
        {
            if (!showUI) return;

            float width = 720f;
            float height = 580f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            Rect panelRect = new Rect(x, y, width, height);
            UITheme.DrawSolidRect(panelRect, UITheme.MeridianMidnight);
            UITheme.DrawOrnateFrame(panelRect, UITheme.PolishedBrass, UITheme.BrassHighlight, 2f);

            GUILayout.BeginArea(new Rect(x + 14f, y + 14f, width - 28f, height - 28f));

            // Header
            GUILayout.BeginHorizontal();
            GUILayout.Label("DEVELOPER GENERATION INSPECTOR [F2]", UITheme.HeaderStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close [X]", UITheme.ButtonStyle, GUILayout.Width(80f), GUILayout.Height(24f)))
            {
                showUI = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // Seed Controls Bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("Seed Code:", UITheme.BodyMutedStyle, GUILayout.Width(75f));
            _seedInputField = GUILayout.TextField(_seedInputField, GUILayout.Width(130f), GUILayout.Height(24f));
            GUILayout.Space(6f);

            if (GUILayout.Button("Load Seed", UITheme.PrimaryButtonStyle, GUILayout.Width(95f), GUILayout.Height(24f)))
            {
                var seed = InvestigationSeed.FromCode(_seedInputField);
                _currentInvestigation = GenerationPipeline.GenerateInvestigation(seed);
            }

            if (GUILayout.Button("New Random", UITheme.ButtonStyle, GUILayout.Width(100f), GUILayout.Height(24f)))
            {
                var seed = InvestigationSeed.GenerateRandom();
                _seedInputField = seed.SeedCode;
                _currentInvestigation = GenerationPipeline.GenerateInvestigation(seed);
            }

            GUILayout.FlexibleSpace();
            string statusStr = _currentInvestigation?.Validation?.IsValid == true
                ? "<color=#2E7D32>VALID MYSTERY</color>"
                : "<color=#C62828>INVALID</color>";
            GUILayout.Label($"Status: {statusStr}", UITheme.SubHeaderStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            // Tabs
            string[] tabs = { "Ground Truth", "NPC Ecosystem", "Hotel Layout", "Object Placements", "Validation" };
            _selectedTab = GUILayout.Toolbar(_selectedTab, tabs, GUILayout.Height(28f));

            GUILayout.Space(10f);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            if (_currentInvestigation == null)
            {
                GUILayout.Label("No investigation generated.", UITheme.BodyMutedStyle);
            }
            else
            {
                switch (_selectedTab)
                {
                    case 0: DrawGroundTruthTab(); break;
                    case 1: DrawNpcTab(); break;
                    case 2: DrawLayoutTab(); break;
                    case 3: DrawObjectsTab(); break;
                    case 4: DrawValidationTab(); break;
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawGroundTruthTab()
        {
            var gt = _currentInvestigation.GroundTruth;
            if (gt == null) return;

            GUILayout.Label($"CASE: {gt.CaseTitle.ToUpper()}", UITheme.SubHeaderStyle);
            GUILayout.Label($"Victim: <color=#F5D973>{gt.VictimName}</color> (ID: {gt.VictimId})", UITheme.BodyStyle);
            GUILayout.Label($"Culprit: <color=#E65100>{gt.CulpritName}</color> (ID: {gt.CulpritId})", UITheme.BodyStyle);
            GUILayout.Label($"Motive: {gt.Motive}", UITheme.BodyStyle);
            GUILayout.Label($"Method: {gt.Method} (Weapon: {gt.MurderWeaponObjectId})", UITheme.BodyStyle);
            GUILayout.Label($"Crime Scene: {gt.CrimeSceneRoomId} @ {gt.MurderTimestamp:F0}s into loop (12:{gt.MurderTimestamp/60f:00}:{gt.MurderTimestamp%60f:00})", UITheme.BodyStyle);
            GUILayout.Label($"Required Key: {gt.RequiredAccessKeyId}", UITheme.BodyStyle);

            GUILayout.Space(10f);
            GUILayout.Label("CRIME TIMELINE:", UITheme.SubHeaderStyle);
            foreach (var evt in gt.CrimeTimeline)
            {
                GUILayout.Label($"• [{evt.SimTimestamp:F0}s] {evt.ActorId} -> {evt.ActionType}: {evt.NarrativeDescription}", UITheme.BodyMutedStyle);
            }

            GUILayout.Space(10f);
            GUILayout.Label("VERIFIED SUSPECT ALIBIS:", UITheme.SubHeaderStyle);
            foreach (var kvp in gt.SuspectAlibis)
            {
                GUILayout.Label($"• {kvp.Key}: {kvp.Value}", UITheme.BodyMutedStyle);
            }
        }

        private void DrawNpcTab()
        {
            var roster = _currentInvestigation.Roster;
            if (roster == null) return;

            GUILayout.Label($"TOTAL ACTIVE NPCS: {roster.Count}", UITheme.SubHeaderStyle);
            GUILayout.Space(4f);

            foreach (var npc in roster)
            {
                string tag = npc.IsVictim ? " [VICTIM]" : (npc.IsCulprit ? " [CULPRIT]" : "");
                GUILayout.Label($"• <b>{npc.DisplayName}</b> ({npc.Role}, Age {npc.Age}){tag}", UITheme.BodyStyle);
                GUILayout.Label($"  Base: {npc.AssignedRoomId} | Pos: {npc.BaselinePosition} | Caution: {npc.Personality.Caution:F2}, Honesty: {npc.Personality.Honesty:F2}", UITheme.BodyMutedStyle);

                if (_currentInvestigation.Routines.TryGetValue(npc.NpcId, out var plan))
                {
                    GUILayout.Label($"  Routine Activities: {plan.Activities.Count} scheduled blocks across 300s.", UITheme.BodyMutedStyle);
                }
                GUILayout.Space(4f);
            }
        }

        private void DrawLayoutTab()
        {
            var rooms = _currentInvestigation.Rooms;
            if (rooms == null) return;

            GUILayout.Label($"HOTEL ARCHITECTURE ({rooms.Count} ROOMS ACROSS 2 FLOORS):", UITheme.SubHeaderStyle);
            GUILayout.Space(4f);

            foreach (var r in rooms)
            {
                string floorStr = r.FloorIndex == 0 ? "Ground Floor" : "Second Floor";
                GUILayout.Label($"• <b>{r.DisplayName}</b> (ID: {r.RoomId}) — {floorStr}", UITheme.BodyStyle);
                GUILayout.Label($"  Bounds: {r.Bounds.size.x}m x {r.Bounds.size.z}m | Doors: {r.Connections.Count} | Anchors: {r.Anchors.Count}", UITheme.BodyMutedStyle);
            }
        }

        private void DrawObjectsTab()
        {
            var gt = _currentInvestigation.GroundTruth;
            if (gt == null) return;

            GUILayout.Label("MANDATORY EVIDENCE PLACEMENTS:", UITheme.SubHeaderStyle);
            foreach (var kvp in gt.MandatoryObjectPlacements)
            {
                GUILayout.Label($"• Object: <color=#D4AF37>{kvp.Key}</color> -> Target Room: <b>{kvp.Value}</b>", UITheme.BodyStyle);
            }

            GUILayout.Space(8f);
            GUILayout.Label("REQUIRED EVIDENCE THREADS:", UITheme.SubHeaderStyle);
            foreach (var link in gt.RequiredEvidenceLinks)
            {
                GUILayout.Label($"• [{link.ClueType}] in {link.TargetRoomId}: {link.Explanation}", UITheme.BodyMutedStyle);
            }
        }

        private void DrawValidationTab()
        {
            var val = _currentInvestigation.Validation;
            if (val == null) return;

            GUILayout.Label($"GENERATION ATTEMPTS: {_currentInvestigation.AttemptCount}", UITheme.SubHeaderStyle);
            GUILayout.Label($"OVERALL VALIDATION: {(val.IsValid ? "<color=#2E7D32>PASSED (SOLVABLE)</color>" : "<color=#C62828>FAILED</color>")}", UITheme.BodyStyle);

            GUILayout.Space(8f);
            if (val.FailureReasons.Count > 0)
            {
                GUILayout.Label("FAILURE REASONS:", UITheme.SubHeaderStyle);
                foreach (var reason in val.FailureReasons)
                {
                    GUILayout.Label($"• <color=#C62828>{reason}</color>", UITheme.BodyMutedStyle);
                }
            }
            else
            {
                GUILayout.Label("• Criterion 1: Culprit and victim identities are valid and distinct.", UITheme.BodyMutedStyle);
                GUILayout.Label("• Criterion 2: Crime timeline has valid chronological ordering within 300s.", UITheme.BodyMutedStyle);
                GUILayout.Label("• Criterion 3: Crime scene and weapon objects exist in layout.", UITheme.BodyMutedStyle);
                GUILayout.Label("• Criterion 4: Required evidence threads are mapped and reachable.", UITheme.BodyMutedStyle);
                GUILayout.Label("• Criterion 5: Innocent suspects have verified alibis for deduction.", UITheme.BodyMutedStyle);
                GUILayout.Label("• Criterion 6: Access key requirements are defined and satisfiable.", UITheme.BodyMutedStyle);
                GUILayout.Label("• Criterion 7: Motive is coherent and non-contradictory.", UITheme.BodyMutedStyle);
                GUILayout.Label("• Criterion 8: Mandatory object placements target valid room definitions.", UITheme.BodyMutedStyle);
            }
        }
    }
}
