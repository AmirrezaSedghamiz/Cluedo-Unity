using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.UI.Core;
using TomorrowsCrime.UI.Styling;

namespace TomorrowsCrime.UI.Notebook
{
    public enum NotebookSection
    {
        EvidenceLibrary,
        LoopTimeline,
        DeductionBoard,
        HypothesisManager,
        FieldNotes,
        AccusationBoard
    }

    /// <summary>
    /// Streamlined, modern Investigation Workspace 2.0 for Tomorrow's Crime.
    /// Senior Game Designer & Programmer overhaul:
    /// - High signal-to-noise ratio: Eliminated walls of explanatory text and duplicate buttons.
    /// - Dynamic responsive layout: Expansive full-width canvas for Deduction and Accusation boards,
    ///   docked inspector for Evidence Archive.
    /// - Intuitive 3-step deductive connection tool replacing overflowing button grids.
    /// - Sleek 2x2 Cluedo Accusation Case Board with instant visual feedback.
    /// - Fast keyboard navigation and telegraphic case-dossier cards.
    /// </summary>
    public class InvestigationWorkspaceView : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private NotebookSection activeSection = NotebookSection.EvidenceLibrary;

        // Navigation & Filter state
        private string _evidenceCategoryFilter = "All";
        private string _selectedEvidenceId;
        private Vector2 _evidenceScroll;
        private Vector2 _timelineScroll;
        private Vector2 _hypothesesScroll;
        private Vector2 _boardScroll;
        private Vector2 _notesScroll;

        // Loop comparison state
        private bool _isComparisonMode = false;
        private int _comparisonLoopA = 1;
        private int _comparisonLoopB = 2;

        // Deduction board authoring state
        private int _sourceClueIndex = 0;
        private int _targetClueIndex = 1;
        private EvidenceRelationshipType _newRelType = EvidenceRelationshipType.Supports;
        private string _newRelExplanation = "";

        // Hypothesis authoring state
        private string _newHypoTitle = "";
        private string _newHypoExplanation = "";
        private readonly HashSet<string> _selectedSupportingEvidence = new();
        private bool _isEditingHypothesis = false;
        private string _editingHypoId = "";

        // Accusation state
        private string _accusedSuspect = "Concierge Pierre";
        private string _chosenWeapon = "Heavy Meat Cleaver";
        private string _chosenRoom = "Room 102 VIP Suite";
        private string _chosenMotive = "Blackmail Silence";
        private Vector2 _accusationScroll = Vector2.zero;

        public void SelectAccusationSuspect(string suspectName)
        {
            _accusedSuspect = suspectName;
            activeSection = NotebookSection.AccusationBoard;
        }

        private void OnGUI()
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr == null || uiMgr.CurrentState != UIState.InvestigationNotebook) return;

            // Full screen backdrop
            Rect fullScreen = new Rect(0, 0, Screen.width, Screen.height);
            UITheme.DrawSolidRect(fullScreen, UITheme.MeridianMidnight);

            // Top Header Bar (50px)
            float topBarHeight = 50f;
            DrawTopBar(topBarHeight);

            // Left Navigation Bar (180px)
            float navWidth = 180f;
            float mainHeight = Screen.height - topBarHeight;
            Rect navRect = new Rect(0, topBarHeight, navWidth, mainHeight);
            DrawNavigationColumn(navRect);

            // Dynamic Workspace Layout:
            // Evidence Library gets a docked 280px Right Inspector;
            // Deduction Board, Accusation Board, Timeline, and Notes get the FULL available width!
            bool showRightInspector = (activeSection == NotebookSection.EvidenceLibrary);
            float rightInspectorWidth = showRightInspector ? Mathf.Min(300f, Screen.width * 0.28f) : 0f;
            float centerWidth = Screen.width - navWidth - rightInspectorWidth;

            Rect centerRect = new Rect(navWidth, topBarHeight, centerWidth, mainHeight);
            DrawCenterWorkspace(centerRect);

            if (showRightInspector)
            {
                Rect rightRect = new Rect(navWidth + centerWidth, topBarHeight, rightInspectorWidth, mainHeight);
                DrawRightDetailPanel(rightRect);
            }
        }

        private void DrawTopBar(float height)
        {
            Rect barRect = new Rect(0, 0, Screen.width, height);
            UITheme.DrawSolidRect(barRect, UITheme.ObsidianSurface);
            UITheme.DrawBorder(barRect, new Color(UITheme.PolishedBrass.r, UITheme.PolishedBrass.g, UITheme.PolishedBrass.b, 0.7f), 1f);

            // Left Title
            Rect titleRect = new Rect(16f, 12f, 380f, 26f);
            var titleStyle = new GUIStyle(UITheme.TitleStyle)
            {
                fontSize = 15,
                normal = { textColor = UITheme.BrassHighlight }
            };
            GUI.Label(titleRect, "MERIDIAN HOUSE <size=12><color=#D4AF37>• CASE DOSSIER</color></size>", titleStyle);

            // Clock & Loop Status
            var simMgr = SimulationManager.Instance;
            int loop = simMgr != null && simMgr.Clock != null ? simMgr.Clock.LoopCount : 1;
            float remaining = simMgr != null && simMgr.Clock != null ? simMgr.Clock.RemainingSeconds : 300f;
            int remMin = (int)(remaining / 60f);
            int remSec = (int)(remaining % 60f);

            Rect statusRect = new Rect(Screen.width - 340f, 14f, 210f, 22f);
            var statusStyle = new GUIStyle(UITheme.BodyStyle)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(statusRect, $"LOOP {loop:D2}  •  <color=#F5D973>{remMin:D2}:{remSec:D2}</color> REMAINING", statusStyle);

            // Return Button
            Rect closeBtnRect = new Rect(Screen.width - 115f, 9f, 100f, 32f);
            if (UITheme.DrawLuxeButton(closeBtnRect, "RETURN [ESC]", isPrimary: true))
            {
                UIManager.Instance.SetState(UIState.InGameHUD);
            }
        }

        private void DrawNavigationColumn(Rect rect)
        {
            UITheme.DrawSolidRect(rect, UITheme.ObsidianSurface);
            UITheme.DrawBorder(rect, new Color(UITheme.PolishedBrass.r, UITheme.PolishedBrass.g, UITheme.PolishedBrass.b, 0.4f), 1f);

            var knowledge = InvestigationKnowledgeService.Instance;
            int clueCount = knowledge != null ? knowledge.EvidenceCount : 0;
            int obsCount = knowledge != null ? knowledge.ObservationCount : 0;
            int hypoCount = knowledge != null ? knowledge.HypothesisCount : 0;
            int relCount = knowledge != null ? knowledge.RelationshipCount : 0;

            GUILayout.BeginArea(new Rect(rect.x + 8f, rect.y + 12f, rect.width - 16f, rect.height - 24f));

            GUILayout.Label("SECTIONS", UITheme.SubHeaderStyle);
            GUILayout.Space(6f);

            DrawSectionTab($"I. Clues ({clueCount})", NotebookSection.EvidenceLibrary);
            DrawSectionTab($"II. Timeline ({obsCount})", NotebookSection.LoopTimeline);
            DrawSectionTab($"III. Deductions ({relCount})", NotebookSection.DeductionBoard);
            DrawSectionTab($"IV. Hypotheses ({hypoCount})", NotebookSection.HypothesisManager);
            DrawSectionTab("V. Field Notes", NotebookSection.FieldNotes);

            GUILayout.Space(12f);
            DrawSectionTab("★ SOLVE CASE", NotebookSection.AccusationBoard, isHighlight: true);

            GUILayout.EndArea();
        }

        private void DrawSectionTab(string label, NotebookSection section, bool isHighlight = false)
        {
            bool isActive = activeSection == section;
            Color textColor = isActive ? UITheme.MeridianMidnight : (isHighlight ? UITheme.StatusAmber : UITheme.TextIvory);
            Color bgColor = isActive ? (isHighlight ? UITheme.StatusAmber : UITheme.PolishedBrass) : UITheme.ObsidianSurfaceLight;

            Rect btnRect = GUILayoutUtility.GetRect(new GUIContent(label), GUIStyle.none, GUILayout.Height(34f));
            UITheme.DrawSolidRect(btnRect, bgColor);
            UITheme.DrawBorder(btnRect, isActive ? UITheme.BrassHighlight : new Color(UITheme.PolishedBrass.r, UITheme.PolishedBrass.g, UITheme.PolishedBrass.b, 0.35f), 1f);

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                fontStyle = (isActive || isHighlight) ? FontStyle.Bold : FontStyle.Normal,
                padding = new RectOffset(10, 4, 0, 0),
                normal = { textColor = textColor }
            };
            GUI.Label(btnRect, label, style);

            if (GUI.Button(btnRect, GUIContent.none, GUIStyle.none))
            {
                if (activeSection != section)
                {
                    activeSection = section;
                    TomorrowsCrime.Audio.AudioManager.Instance?.PlayPaperRustle();
                }
                if (UIManager.Instance != null) UIManager.Instance.IsTypingInField = false;
            }

            GUILayout.Space(3f);
        }

        private void DrawCenterWorkspace(Rect rect)
        {
            UITheme.DrawSolidRect(rect, UITheme.MeridianMidnight);
            UITheme.DrawBorder(rect, new Color(UITheme.PolishedBrass.r, UITheme.PolishedBrass.g, UITheme.PolishedBrass.b, 0.45f), 1f);

            Rect contentRect = new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, rect.height - 24f);

            switch (activeSection)
            {
                case NotebookSection.EvidenceLibrary:
                    DrawEvidenceLibrary(contentRect);
                    break;
                case NotebookSection.LoopTimeline:
                    DrawLoopTimeline(contentRect);
                    break;
                case NotebookSection.DeductionBoard:
                    DrawDeductionBoard(contentRect);
                    break;
                case NotebookSection.HypothesisManager:
                    DrawHypothesisManager(contentRect);
                    break;
                case NotebookSection.FieldNotes:
                    DrawFieldNotes(contentRect);
                    break;
                case NotebookSection.AccusationBoard:
                    DrawAccusationBoard(contentRect);
                    break;
            }
        }

        // ==========================================
        // SECTION 1: EVIDENCE LIBRARY
        // ==========================================
        private void DrawEvidenceLibrary(Rect rect)
        {
            GUILayout.BeginArea(rect);

            // Filter bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("CATEGORY:", UITheme.BodyMutedStyle, GUILayout.Width(65f));
            string[] filters = { "All", "Key", "Utensil", "Tool", "Document", "Valuable", "Container" };
            foreach (var f in filters)
            {
                bool isSel = _evidenceCategoryFilter == f;
                var style = isSel ? UITheme.PrimaryButtonStyle : UITheme.ButtonStyle;
                if (GUILayout.Button(f, style, GUILayout.Height(22f), GUILayout.MinWidth(55f)))
                {
                    _evidenceCategoryFilter = f;
                }
                GUILayout.Space(2f);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            var knowledge = InvestigationKnowledgeService.Instance;
            var evidenceList = knowledge != null ? knowledge.GetAllDiscoveredEvidence() : Array.Empty<DiscoveredEvidenceRecord>();

            var filtered = evidenceList
                .Where(e => _evidenceCategoryFilter == "All" || string.Equals(e.Definition.Category, _evidenceCategoryFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filtered.Count == 0)
            {
                DrawEmptyState("NO CLUES CATALOGED", "Investigate hotel rooms in first-person mode. Press [E] on objects to inspect and catalog clues.");
            }
            else
            {
                if (string.IsNullOrEmpty(_selectedEvidenceId) && filtered.Count > 0)
                {
                    _selectedEvidenceId = filtered[0].Definition.EvidenceId;
                }

                _evidenceScroll = GUILayout.BeginScrollView(_evidenceScroll);
                foreach (var record in filtered)
                {
                    bool isSelected = _selectedEvidenceId == record.Definition.EvidenceId;
                    var cardStyle = isSelected ? UITheme.CardSelectedStyle : UITheme.CardStyle;

                    GUILayout.BeginVertical(cardStyle);
                    GUILayout.BeginHorizontal();

                    GUILayout.Label($"<b>{record.Definition.DisplayName}</b>", UITheme.SubHeaderStyle);
                    GUILayout.FlexibleSpace();

                    // Category Pill
                    GUILayout.Box(record.Definition.Category.ToUpper(), UITheme.BadgeStyle, GUILayout.Height(18f));
                    GUILayout.EndHorizontal();

                    GUILayout.Space(2f);
                    GUILayout.Label(record.Definition.ShortDescription, UITheme.BodyStyle);

                    GUILayout.Space(4f);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Loop {record.DiscoveredInLoop:D2} • {record.GetFormattedTimestamp()}", UITheme.BodyMutedStyle);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(isSelected ? "Selected" : "Examine", isSelected ? UITheme.PrimaryButtonStyle : UITheme.ButtonStyle, GUILayout.Width(80f), GUILayout.Height(22f)))
                    {
                        _selectedEvidenceId = record.Definition.EvidenceId;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                    GUILayout.Space(4f);
                }
                GUILayout.EndScrollView();
            }

            GUILayout.EndArea();
        }

        // ==========================================
        // SECTION 2: TIMELINE & LOOP COMPARISON
        // ==========================================
        private void DrawLoopTimeline(Rect rect)
        {
            GUILayout.BeginArea(rect);

            GUILayout.BeginHorizontal();
            GUILayout.Label("OBSERVATION TIMELINE", UITheme.HeaderStyle);
            GUILayout.FlexibleSpace();

            string toggleLabel = _isComparisonMode ? "Chronological View" : "Compare Loops Side-by-Side";
            if (GUILayout.Button(toggleLabel, UITheme.PrimaryButtonStyle, GUILayout.Height(24f)))
            {
                _isComparisonMode = !_isComparisonMode;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            var knowledge = InvestigationKnowledgeService.Instance;
            var allObservations = knowledge != null ? knowledge.GetAllObservations() : Array.Empty<PlayerObservationEntry>();

            if (allObservations.Count == 0)
            {
                DrawEmptyState("NO WITNESSED EVENTS", "Actions you observe inside Meridian House (suspect movements, door interactions) are automatically recorded here.");
                GUILayout.EndArea();
                return;
            }

            if (_isComparisonMode)
            {
                DrawLoopComparisonView(knowledge);
            }
            else
            {
                DrawChronologicalTimeline(allObservations);
            }

            GUILayout.EndArea();
        }

        private void DrawChronologicalTimeline(IReadOnlyList<PlayerObservationEntry> observations)
        {
            var grouped = observations.GroupBy(o => o.LoopIndex).OrderBy(g => g.Key);
            _timelineScroll = GUILayout.BeginScrollView(_timelineScroll);

            foreach (var group in grouped)
            {
                GUILayout.BeginVertical(UITheme.CardStyle);
                GUILayout.Label($"<b>LOOP {group.Key:D2}</b>", UITheme.SubHeaderStyle);
                GUILayout.Space(4f);

                foreach (var obs in group.OrderBy(o => o.SimTime))
                {
                    GUILayout.BeginHorizontal(UITheme.CardSelectedStyle);
                    GUILayout.Label($"<b>{obs.GetFormattedTimestamp()}</b>", UITheme.BodyStyle, GUILayout.Width(50f));
                    string badge = obs.Method == ObservationMethod.DirectInspection ? "INSPECTED" : "WITNESSED";
                    GUILayout.Box(badge, UITheme.BadgeStyle, GUILayout.Width(80f), GUILayout.Height(18f));
                    GUILayout.Space(6f);
                    GUILayout.Label(obs.Description, UITheme.BodyStyle);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(2f);
                }

                GUILayout.EndVertical();
                GUILayout.Space(6f);
            }

            GUILayout.EndScrollView();
        }

        private void DrawLoopComparisonView(InvestigationKnowledgeService knowledge)
        {
            GUILayout.BeginHorizontal(UITheme.CardStyle);
            GUILayout.Label("Compare Loops:", UITheme.BodyStyle, GUILayout.Width(100f));
            GUILayout.Label("A:", UITheme.BodyMutedStyle, GUILayout.Width(18f));
            if (int.TryParse(GUILayout.TextField(_comparisonLoopA.ToString(), GUILayout.Width(35f)), out int la)) _comparisonLoopA = Mathf.Max(1, la);
            GUILayout.Space(8f);
            GUILayout.Label("B:", UITheme.BodyMutedStyle, GUILayout.Width(18f));
            if (int.TryParse(GUILayout.TextField(_comparisonLoopB.ToString(), GUILayout.Width(35f)), out int lb)) _comparisonLoopB = Mathf.Max(1, lb);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            var comparison = knowledge.CompareLoops(_comparisonLoopA, _comparisonLoopB);

            if (comparison.Count == 0)
            {
                DrawEmptyState("NO COMPARABLE OBSERVATIONS", $"No overlapping observations recorded between Loop {_comparisonLoopA} and Loop {_comparisonLoopB}.");
                return;
            }

            _timelineScroll = GUILayout.BeginScrollView(_timelineScroll);
            foreach (var item in comparison)
            {
                GUILayout.BeginVertical(UITheme.CardStyle);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<b>{item.ObservationId}</b>: {item.ComparisonSummary}", UITheme.SubHeaderStyle);
                GUILayout.FlexibleSpace();
                string tag = item.HasDivergence ? "DIVERGENT" : "IDENTICAL";
                GUILayout.Box(tag, UITheme.BadgeStyle, GUILayout.Height(18f));
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.Space(4f);
            }
            GUILayout.EndScrollView();
        }

        // ==========================================
        // SECTION 3: DEDUCTION BOARD 2.0 (EXPANSIVE)
        // ==========================================
        private void DrawDeductionBoard(Rect rect)
        {
            GUILayout.BeginArea(rect);

            var knowledge = InvestigationKnowledgeService.Instance;
            var discovered = knowledge != null ? knowledge.GetAllDiscoveredEvidence() : Array.Empty<DiscoveredEvidenceRecord>();
            var relationships = knowledge != null ? knowledge.GetAllRelationships() : Array.Empty<EvidenceRelationship>();

            if (discovered.Count < 2)
            {
                DrawEmptyState("INSUFFICIENT CLUES FOR DEDUCTION", "Discover at least two pieces of evidence to form deductive links on this board.");
                GUILayout.EndArea();
                return;
            }

            // Connection Authoring Tool (Clean Dropdown / Cycle Tool)
            GUILayout.BeginVertical(UITheme.CardStyle);
            GUILayout.Label("ESTABLISH DEDUCTIVE LINK", UITheme.SubHeaderStyle);
            GUILayout.Space(4f);

            // Clamp Indices
            _sourceClueIndex = Mathf.Clamp(_sourceClueIndex, 0, discovered.Count - 1);
            _targetClueIndex = Mathf.Clamp(_targetClueIndex, 0, discovered.Count - 1);
            if (_sourceClueIndex == _targetClueIndex)
            {
                _targetClueIndex = (_sourceClueIndex + 1) % discovered.Count;
            }

            var sourceClue = discovered[_sourceClueIndex].Definition;
            var targetClue = discovered[_targetClueIndex].Definition;

            GUILayout.BeginHorizontal();

            // Source Selector
            GUILayout.BeginVertical(GUILayout.Width(rect.width * 0.38f));
            GUILayout.Label("Source Clue:", UITheme.BodyMutedStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀", UITheme.ButtonStyle, GUILayout.Width(26f), GUILayout.Height(24f)))
            {
                _sourceClueIndex = (_sourceClueIndex - 1 + discovered.Count) % discovered.Count;
            }
            GUILayout.Box(sourceClue.DisplayName, UITheme.CardSelectedStyle, GUILayout.Height(24f));
            if (GUILayout.Button("▶", UITheme.ButtonStyle, GUILayout.Width(26f), GUILayout.Height(24f)))
            {
                _sourceClueIndex = (_sourceClueIndex + 1) % discovered.Count;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            // Relationship Type Selector
            GUILayout.BeginVertical(GUILayout.Width(rect.width * 0.20f));
            GUILayout.Label("Relationship:", UITheme.BodyMutedStyle);
            if (GUILayout.Button(_newRelType.ToString().ToUpper(), UITheme.PrimaryButtonStyle, GUILayout.Height(24f)))
            {
                // Cycle through enum values
                var values = (EvidenceRelationshipType[])Enum.GetValues(typeof(EvidenceRelationshipType));
                int next = ((int)_newRelType + 1) % values.Length;
                _newRelType = values[next];
            }
            GUILayout.EndVertical();

            // Target Selector
            GUILayout.BeginVertical(GUILayout.Width(rect.width * 0.38f));
            GUILayout.Label("Target Clue:", UITheme.BodyMutedStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀", UITheme.ButtonStyle, GUILayout.Width(26f), GUILayout.Height(24f)))
            {
                _targetClueIndex = (_targetClueIndex - 1 + discovered.Count) % discovered.Count;
            }
            GUILayout.Box(targetClue.DisplayName, UITheme.CardSelectedStyle, GUILayout.Height(24f));
            if (GUILayout.Button("▶", UITheme.ButtonStyle, GUILayout.Width(26f), GUILayout.Height(24f)))
            {
                _targetClueIndex = (_targetClueIndex + 1) % discovered.Count;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // Deduction Note & Action Button
            GUILayout.BeginHorizontal();
            GUILayout.Label("Note:", UITheme.BodyMutedStyle, GUILayout.Width(45f));
            GUI.SetNextControlName("RelExplanationField");
            _newRelExplanation = GUILayout.TextField(_newRelExplanation);
            if (GUI.GetNameOfFocusedControl() == "RelExplanationField")
            {
                UIManager.Instance.IsTypingInField = true;
            }

            if (GUILayout.Button("Link Clues", UITheme.PrimaryButtonStyle, GUILayout.Width(100f), GUILayout.Height(22f)))
            {
                if (sourceClue.EvidenceId != targetClue.EvidenceId)
                {
                    int loop = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                        ? SimulationManager.Instance.Clock.LoopCount : 1;
                    knowledge.AddRelationship(sourceClue.EvidenceId, targetClue.EvidenceId, _newRelType, _newRelExplanation, isPlayerCreated: true, createdInLoop: loop);
                    _newRelExplanation = "";
                    UIManager.Instance.IsTypingInField = false;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8f);
            GUILayout.Label("ACTIVE CONNECTIONS", UITheme.SubHeaderStyle);

            _boardScroll = GUILayout.BeginScrollView(_boardScroll);
            if (relationships.Count == 0)
            {
                GUILayout.Label("<i>No connections formed yet. Use the tool above to link clues.</i>", UITheme.BodyMutedStyle);
            }
            else
            {
                foreach (var rel in relationships)
                {
                    var sDef = knowledge.GetEvidenceRecord(rel.SourceEvidenceId)?.Definition;
                    var tDef = knowledge.GetEvidenceRecord(rel.TargetEvidenceId)?.Definition;
                    string sName = sDef != null ? sDef.DisplayName : rel.SourceEvidenceId;
                    string tName = tDef != null ? tDef.DisplayName : rel.TargetEvidenceId;

                    GUILayout.BeginVertical(UITheme.CardStyle);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"<b>{sName}</b> <color=#F5D973>--[{rel.RelationshipType}]--> </color><b>{tName}</b>", UITheme.BodyStyle);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Remove", UITheme.ButtonStyle, GUILayout.Width(65f), GUILayout.Height(20f)))
                    {
                        knowledge.RemoveRelationship(rel.RelationshipId);
                        break;
                    }
                    GUILayout.EndHorizontal();

                    if (!string.IsNullOrEmpty(rel.Explanation))
                    {
                        GUILayout.Label($"<i>Reason: {rel.Explanation}</i>", UITheme.BodyMutedStyle);
                    }
                    GUILayout.EndVertical();
                    GUILayout.Space(3f);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        // ==========================================
        // SECTION 4: HYPOTHESIS MANAGER
        // ==========================================
        private void DrawHypothesisManager(Rect rect)
        {
            GUILayout.BeginArea(rect);

            var knowledge = InvestigationKnowledgeService.Instance;
            var hypotheses = knowledge != null ? knowledge.GetAllHypotheses() : Array.Empty<HypothesisEntry>();

            // Formulate Box
            GUILayout.BeginVertical(UITheme.CardStyle);
            GUILayout.Label(_isEditingHypothesis ? "EDIT THEORY" : "RECORD WORKING HYPOTHESIS", UITheme.SubHeaderStyle);
            GUILayout.Space(4f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Title:", UITheme.BodyMutedStyle, GUILayout.Width(45f));
            GUI.SetNextControlName("HypoTitleField");
            _newHypoTitle = GUILayout.TextField(_newHypoTitle);
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Notes:", UITheme.BodyMutedStyle, GUILayout.Width(45f));
            GUI.SetNextControlName("HypoExplanationField");
            _newHypoExplanation = GUILayout.TextField(_newHypoExplanation);

            if (GUI.GetNameOfFocusedControl() == "HypoTitleField" || GUI.GetNameOfFocusedControl() == "HypoExplanationField")
            {
                UIManager.Instance.IsTypingInField = true;
            }

            string btnText = _isEditingHypothesis ? "Save" : "Add";
            if (GUILayout.Button(btnText, UITheme.PrimaryButtonStyle, GUILayout.Width(80f)))
            {
                if (!string.IsNullOrWhiteSpace(_newHypoTitle))
                {
                    int loop = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                        ? SimulationManager.Instance.Clock.LoopCount : 1;
                    float simTime = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                        ? SimulationManager.Instance.Clock.ElapsedSeconds : 0f;

                    if (_isEditingHypothesis)
                    {
                        knowledge.UpdateHypothesis(_editingHypoId, _newHypoTitle, _newHypoExplanation, _selectedSupportingEvidence);
                        _isEditingHypothesis = false;
                        _editingHypoId = "";
                    }
                    else
                    {
                        knowledge.CreateHypothesis(_newHypoTitle, _newHypoExplanation, loop, simTime, _selectedSupportingEvidence);
                    }

                    _newHypoTitle = "";
                    _newHypoExplanation = "";
                    UIManager.Instance.IsTypingInField = false;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8f);
            GUILayout.Label("WORKING HYPOTHESES", UITheme.SubHeaderStyle);

            _hypothesesScroll = GUILayout.BeginScrollView(_hypothesesScroll);
            if (hypotheses.Count == 0)
            {
                GUILayout.Label("<i>No hypotheses recorded yet. Formulate a working theory above.</i>", UITheme.BodyMutedStyle);
            }
            else
            {
                foreach (var h in hypotheses)
                {
                    GUILayout.BeginVertical(UITheme.CardStyle);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"<b>{h.Title}</b>", UITheme.SubHeaderStyle);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Delete", UITheme.ButtonStyle, GUILayout.Width(60f), GUILayout.Height(20f)))
                    {
                        knowledge.DeleteHypothesis(h.HypothesisId);
                        break;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Label(h.Explanation, UITheme.BodyStyle);
                    GUILayout.EndVertical();
                    GUILayout.Space(4f);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        // ==========================================
        // SECTION 5: FIELD NOTES
        // ==========================================
        private void DrawFieldNotes(Rect rect)
        {
            GUILayout.BeginArea(rect);

            var knowledge = InvestigationKnowledgeService.Instance;
            GUILayout.Label("DETECTIVE FIELD NOTES (CROSS-LOOP PERSISTENT)", UITheme.SubHeaderStyle);
            GUILayout.Space(6f);

            if (knowledge != null)
            {
                GUI.SetNextControlName("FieldNotesTextArea");
                _notesScroll = GUILayout.BeginScrollView(_notesScroll);
                string updated = GUILayout.TextArea(knowledge.GeneralNotes ?? "", GUILayout.MinHeight(rect.height - 40f));
                if (updated != knowledge.GeneralNotes)
                {
                    knowledge.GeneralNotes = updated;
                }
                if (GUI.GetNameOfFocusedControl() == "FieldNotesTextArea")
                {
                    UIManager.Instance.IsTypingInField = true;
                }
                GUILayout.EndScrollView();
            }

            GUILayout.EndArea();
        }

        // ==========================================
        // SECTION 6: ACCUSATION BOARD 2.0 (2x2 GRID)
        // ==========================================
        private void DrawAccusationBoard(Rect rect)
        {
            GUILayout.BeginArea(rect);

            GUILayout.Label("SOLVE CASE • FORMAL ACCUSATION", UITheme.HeaderStyle);
            GUILayout.Space(2f);
            GUILayout.Label("Synthesize your clues. Accuse the perpetrator with the matching weapon, crime scene, and motive.", UITheme.BodyMutedStyle);
            GUILayout.Space(8f);

            var accusationService = CaseAccusationService.Instance;
            var suspects = accusationService != null ? accusationService.GetKnownSuspects() : new List<string> { "Concierge Pierre", "Chef Marcel", "Lord Harrington", "Dr. Alistair Sterling" };
            var weapons = accusationService != null ? accusationService.GetKnownWeapons() : new List<string> { "Heavy Meat Cleaver", "Heavy Brass Candlestick", "Amber Alkaloid Poison Vial", "Steel Pipe Wrench" };
            var rooms = accusationService != null ? accusationService.GetKnownCrimeScenes() : new List<string> { "Room 102 VIP Suite", "Room 101 Guest Suite", "Executive Staff Office", "Maintenance Utility Room" };
            var motives = accusationService != null ? accusationService.GetKnownMotives() : new List<string> { "Blackmail Silence", "Financial Embezzlement", "Family Inheritance", "Stolen Industrial Patents" };

            // 2x2 Clean Case Board Layout
            float boxWidth = (rect.width - 12f) * 0.5f;
            float boxHeight = (rect.height - 100f) * 0.5f;

            // Row 1: Who & What
            GUILayout.BeginHorizontal();
            DrawPillarCard("1. ACCUSED PERPETRATOR", _accusedSuspect, suspects, s => _accusedSuspect = s, boxWidth, boxHeight);
            GUILayout.Space(12f);
            DrawPillarCard("2. MURDER WEAPON", _chosenWeapon, weapons, w => _chosenWeapon = w, boxWidth, boxHeight);
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            // Row 2: Where & Why
            GUILayout.BeginHorizontal();
            DrawPillarCard("3. CRIME SCENE LOCATION", _chosenRoom, rooms, r => _chosenRoom = r, boxWidth, boxHeight);
            GUILayout.Space(12f);
            DrawPillarCard("4. UNDERLYING MOTIVE", _chosenMotive, motives, m => _chosenMotive = m, boxWidth, boxHeight);
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            // Final Submit Button
            if (UITheme.DrawLuxeButton(new Rect(0, rect.height - 38f, rect.width, 36f), "CONFRONT PERPETRATOR & BREAK TIME LOOP", isPrimary: true))
            {
                if (CaseAccusationService.Instance != null)
                {
                    var result = CaseAccusationService.Instance.FormulateAccusation(_accusedSuspect, _chosenWeapon, _chosenRoom, _chosenMotive);
                    UIManager.Instance?.ShowCaseResolutionModal(result);
                }
            }

            GUILayout.EndArea();
        }

        private void DrawPillarCard(string title, string currentSelection, List<string> options, Action<string> onSelect, float width, float height)
        {
            GUILayout.BeginVertical(UITheme.CardStyle, GUILayout.Width(width), GUILayout.Height(height));
            GUILayout.Label($"<b>{title}</b>", UITheme.SubHeaderStyle);
            GUILayout.Space(2f);
            GUILayout.Label($"Selected: <color=#{ColorUtility.ToHtmlStringRGB(UITheme.BrassHighlight)}><b>{currentSelection}</b></color>", UITheme.BodyStyle);
            GUILayout.Space(4f);

            // Compact chips
            int cols = 2;
            for (int i = 0; i < options.Count; i += cols)
            {
                GUILayout.BeginHorizontal();
                for (int j = 0; j < cols && (i + j) < options.Count; j++)
                {
                    string opt = options[i + j];
                    bool isSel = currentSelection == opt;
                    var style = isSel ? UITheme.PrimaryButtonStyle : UITheme.ButtonStyle;
                    if (GUILayout.Button(opt, style, GUILayout.Height(24f)))
                    {
                        onSelect(opt);
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(2f);
            }

            GUILayout.EndVertical();
        }

        // ==========================================
        // RIGHT DETAIL PANEL (Dossier Inspector)
        // ==========================================
        private void DrawRightDetailPanel(Rect rect)
        {
            UITheme.DrawSolidRect(rect, UITheme.ObsidianSurface);
            UITheme.DrawBorder(rect, new Color(UITheme.PolishedBrass.r, UITheme.PolishedBrass.g, UITheme.PolishedBrass.b, 0.45f), 1f);

            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, rect.height - 24f));
            GUILayout.Label("CLUE DOSSIER", UITheme.SubHeaderStyle);
            GUILayout.Space(6f);

            var knowledge = InvestigationKnowledgeService.Instance;
            DiscoveredEvidenceRecord selectedRecord = null;
            if (!string.IsNullOrEmpty(_selectedEvidenceId) && knowledge != null)
            {
                selectedRecord = knowledge.GetEvidenceRecord(_selectedEvidenceId);
            }

            if (selectedRecord != null)
            {
                var def = selectedRecord.Definition;

                GUILayout.BeginVertical(UITheme.CardSelectedStyle);
                GUILayout.Label($"<b>{def.DisplayName}</b>", UITheme.SubHeaderStyle);
                GUILayout.Box(def.Category.ToUpper(), UITheme.BadgeStyle, GUILayout.Height(18f));
                GUILayout.Space(2f);
                GUILayout.Label($"Discovered: Loop {selectedRecord.DiscoveredInLoop:D2} • {selectedRecord.GetFormattedTimestamp()}", UITheme.BodyMutedStyle);
                GUILayout.EndVertical();

                GUILayout.Space(8f);
                GUILayout.Label("FORENSIC OBSERVATION:", UITheme.BodyMutedStyle);
                GUILayout.Label(def.DetailedInspectionText, UITheme.BodyStyle);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Examine 3D Clue", UITheme.PrimaryButtonStyle, GUILayout.Height(30f)))
                {
                    UIManager.Instance.ShowEvidenceModal(def);
                }
            }
            else
            {
                GUILayout.BeginVertical(UITheme.CardStyle);
                GUILayout.Label("Select a clue on the left to examine forensic analysis and details.", UITheme.BodyMutedStyle);
                GUILayout.EndVertical();
            }

            GUILayout.EndArea();
        }

        private void DrawEmptyState(string title, string description)
        {
            GUILayout.BeginVertical(UITheme.CardStyle);
            GUILayout.Space(14f);
            GUILayout.Label($"<b>{title}</b>", UITheme.SubHeaderStyle);
            GUILayout.Space(4f);
            GUILayout.Label(description, UITheme.BodyMutedStyle);
            GUILayout.Space(14f);
            GUILayout.EndVertical();
        }
    }
}
