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
    /// Comprehensive Investigation Workspace 2.0 for Tomorrow's Crime.
    /// Professional three-part layout: Navigation Column, Main Workspace, and Contextual Detail Panel.
    /// Features:
    /// - Evidence Library with category filtering and detailed clue inspection.
    /// - Loop Timeline with cross-loop comparison mode.
    /// - Visual Deduction Board for connecting discovered evidence with causal relationships.
    /// - Hypothesis Manager with supporting evidence links.
    /// - Persistent Field Notes scratchpad.
    /// </summary>
    public class InvestigationWorkspaceView : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private NotebookSection activeSection = NotebookSection.EvidenceLibrary;

        // Navigation state
        private string _evidenceCategoryFilter = "All";
        private string _selectedEvidenceId;
        private string _selectedHypothesisId;
        private Vector2 _evidenceScroll;
        private Vector2 _timelineScroll;
        private Vector2 _hypothesesScroll;
        private Vector2 _boardScroll;
        private Vector2 _notesScroll;

        // Loop comparison state
        private bool _isComparisonMode = false;
        private int _comparisonLoopA = 1;
        private int _comparisonLoopB = 2;

        // Deduction board connection authoring state
        private string _sourceConnectEvidenceId;
        private string _targetConnectEvidenceId;
        private EvidenceRelationshipType _newRelType = EvidenceRelationshipType.Supports;
        private string _newRelExplanation = "";

        // Hypothesis authoring state
        private string _newHypoTitle = "";
        private string _newHypoExplanation = "";
        private readonly HashSet<string> _selectedSupportingEvidence = new();
        private bool _isEditingHypothesis = false;
        private string _editingHypoId = "";

        // Accusation authoring state
        private string _accusedSuspect = "Concierge Pierre";
        private string _chosenWeapon = "Heavy Meat Cleaver";
        private string _chosenRoom = "Room 102 Suite";
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
            UITheme.DrawSolidRect(fullScreen, UITheme.CharcoalBackground);

            // Top Header Bar
            DrawTopBar();

            // Three-column layout dimensions
            float topBarHeight = 56f;
            float leftColWidth = 240f;
            float rightColWidth = Mathf.Max(320f, Screen.width * 0.28f);
            float centerWidth = Screen.width - leftColWidth - rightColWidth;
            float mainHeight = Screen.height - topBarHeight;

            Rect leftRect = new Rect(0, topBarHeight, leftColWidth, mainHeight);
            Rect centerRect = new Rect(leftColWidth, topBarHeight, centerWidth, mainHeight);
            Rect rightRect = new Rect(leftColWidth + centerWidth, topBarHeight, rightColWidth, mainHeight);

            // Left Navigation Column
            DrawNavigationColumn(leftRect);

            // Center Workspace
            DrawCenterWorkspace(centerRect);

            // Right Detail Panel
            DrawRightDetailPanel(rightRect);
        }

        private void DrawTopBar()
        {
            Rect barRect = new Rect(0, 0, Screen.width, 56f);
            UITheme.DrawSolidRect(barRect, UITheme.ObsidianSurface);
            UITheme.DrawOrnateFrame(barRect, UITheme.PolishedBrass, UITheme.BrassHighlight, 1.5f);

            // Title with Gold Embossed Case Dossier
            Rect titleRect = new Rect(24f, 12f, 440f, 32f);
            GUI.Label(titleRect, "MERIDIAN HOUSE <size=13><color=#D4AF37>— CONFIDENTIAL DOSSIER</color></size>", UITheme.TitleStyle);

            // Clock & Loop Status on Top Right
            var simMgr = SimulationManager.Instance;
            int loop = simMgr != null && simMgr.Clock != null ? simMgr.Clock.LoopCount : 1;
            float elapsed = simMgr != null && simMgr.Clock != null ? simMgr.Clock.ElapsedSeconds : 0f;
            int min = (int)(elapsed / 60f);
            int sec = (int)(elapsed % 60f);

            Rect statusRect = new Rect(Screen.width - 440f, 16f, 270f, 24f);
            var statusStyle = new GUIStyle(UITheme.BodyStyle)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 12
            };
            GUI.Label(statusRect, $"ACTIVE INCIDENT: <color=#D4AF37>LOOP {loop:D2}</color> @ <color=#E5A93C>{min:00}:{sec:00}</color>", statusStyle);

            // Close Button
            Rect closeBtnRect = new Rect(Screen.width - 150f, 10f, 130f, 36f);
            if (UITheme.DrawLuxeButton(closeBtnRect, "RETURN [ESC]", isPrimary: true))
            {
                UIManager.Instance.SetState(UIState.InGameHUD);
            }
        }

        private void DrawNavigationColumn(Rect rect)
        {
            UITheme.DrawSolidRect(rect, UITheme.ObsidianSurface);
            UITheme.DrawOrnateFrame(rect, UITheme.PolishedBrass, UITheme.BrassDark, 1.5f);

            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 16f, rect.width - 24f, rect.height - 32f));

            GUILayout.BeginHorizontal();
            Rect sealRect = GUILayoutUtility.GetRect(28f, 28f, GUILayout.Width(28f), GUILayout.Height(28f));
            UITheme.DrawWaxSeal(sealRect);
            GUILayout.Space(6f);
            GUILayout.Label("CASE DOSSIER", UITheme.SubHeaderStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(12f);

            DrawSectionTab("I. Evidence Archive", NotebookSection.EvidenceLibrary);
            DrawSectionTab("II. Loop Timeline", NotebookSection.LoopTimeline);
            DrawSectionTab("III. Deduction Board", NotebookSection.DeductionBoard);
            DrawSectionTab("IV. Case Hypotheses", NotebookSection.HypothesisManager);
            DrawSectionTab("V. Field Scratchpad", NotebookSection.FieldNotes);
            GUILayout.Space(8f);
            DrawSectionTab("VI. SOLVE CASE", NotebookSection.AccusationBoard);

            GUILayout.FlexibleSpace();

            // Knowledge Statistics Card
            var knowledge = InvestigationKnowledgeService.Instance;
            int clues = knowledge != null ? knowledge.EvidenceCount : 0;
            int obs = knowledge != null ? knowledge.ObservationCount : 0;
            int hypos = knowledge != null ? knowledge.HypothesisCount : 0;
            int rels = knowledge != null ? knowledge.RelationshipCount : 0;

            GUILayout.BeginVertical(UITheme.CardStyle);
            GUILayout.Label("INVESTIGATION METRICS", UITheme.SubHeaderStyle);
            GUILayout.Space(4f);
            GUILayout.Label($"Cataloged Clues: <color=#D4AF37>{clues}</color>", UITheme.BodyStyle);
            GUILayout.Label($"Witnessed Events: <color=#D4AF37>{obs}</color>", UITheme.BodyStyle);
            GUILayout.Label($"Active Hypotheses: <color=#D4AF37>{hypos}</color>", UITheme.BodyStyle);
            GUILayout.Label($"Deductive Links: <color=#D4AF37>{rels}</color>", UITheme.BodyStyle);
            GUILayout.EndVertical();

            GUILayout.EndArea();
        }

        private void DrawSectionTab(string label, NotebookSection section)
        {
            bool isActive = activeSection == section;
            var style = isActive ? UITheme.ActiveTabButtonStyle : UITheme.TabButtonStyle;

            if (GUILayout.Button(label, style, GUILayout.Height(38f)))
            {
                if (activeSection != section)
                {
                    activeSection = section;
                    TomorrowsCrime.Audio.AudioManager.Instance?.PlayPaperRustle();
                }
                if (UIManager.Instance != null) UIManager.Instance.IsTypingInField = false;
            }
            GUILayout.Space(4f);
        }

        private void DrawCenterWorkspace(Rect rect)
        {
            UITheme.DrawSolidRect(rect, UITheme.MeridianMidnight);
            UITheme.DrawOrnateFrame(rect, UITheme.PolishedBrass, UITheme.BrassDark, 1.5f);

            Rect contentRect = new Rect(rect.x + 16f, rect.y + 16f, rect.width - 32f, rect.height - 32f);

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

        // --- SECTION 1: EVIDENCE LIBRARY ---
        private void DrawEvidenceLibrary(Rect rect)
        {
            GUILayout.BeginArea(rect);

            GUILayout.Label("DISCOVERED EVIDENCE CATALOG", UITheme.HeaderStyle);
            GUILayout.Space(6f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", UITheme.BodyMutedStyle, GUILayout.Width(45f));
            string[] filters = { "All", "Keys & Access", "Documents", "Locations" };
            foreach (var f in filters)
            {
                var style = _evidenceCategoryFilter == f ? UITheme.PrimaryButtonStyle : UITheme.ButtonStyle;
                if (GUILayout.Button(f, style, GUILayout.Height(24f), GUILayout.MinWidth(85f)))
                {
                    _evidenceCategoryFilter = f;
                }
                GUILayout.Space(4f);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(12f);

            var knowledge = InvestigationKnowledgeService.Instance;
            var evidenceList = knowledge != null ? knowledge.GetAllDiscoveredEvidence() : Array.Empty<DiscoveredEvidenceRecord>();

            var filtered = evidenceList
                .Where(e => _evidenceCategoryFilter == "All" || e.Definition.Category == _evidenceCategoryFilter)
                .ToList();

            if (filtered.Count == 0)
            {
                DrawEmptyState(
                    "NO EVIDENCE MATCHES CURRENT FILTER",
                    "Explore Meridian House in first-person mode to locate physical clues, inspect door locks, and read documents.\nPress [E] to inspect objects."
                );
            }
            else
            {
                _evidenceScroll = GUILayout.BeginScrollView(_evidenceScroll);
                foreach (var record in filtered)
                {
                    bool isSelected = _selectedEvidenceId == record.Definition.EvidenceId;
                    var cardStyle = isSelected ? UITheme.CardSelectedStyle : UITheme.CardStyle;

                    GUILayout.BeginVertical(cardStyle);
                    GUILayout.BeginHorizontal();

                    GUILayout.Label($"<b>{record.Definition.DisplayName}</b>", UITheme.SubHeaderStyle);
                    GUILayout.FlexibleSpace();

                    // Category Badge
                    GUILayout.Box(record.Definition.Category, UITheme.BadgeStyle, GUILayout.Width(90f), GUILayout.Height(18f));

                    GUILayout.EndHorizontal();

                    GUILayout.Space(4f);
                    GUILayout.Label(record.Definition.ShortDescription, UITheme.BodyStyle);

                    GUILayout.Space(6f);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Discovered in Loop {record.DiscoveredInLoop} @ {record.GetFormattedTimestamp()}", UITheme.BodyMutedStyle);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(isSelected ? "Selected" : "Examine Clue", UITheme.ButtonStyle, GUILayout.Width(110f), GUILayout.Height(24f)))
                    {
                        _selectedEvidenceId = record.Definition.EvidenceId;
                    }

                    if (GUILayout.Button("Full Inspection", UITheme.PrimaryButtonStyle, GUILayout.Width(120f), GUILayout.Height(24f)))
                    {
                        _selectedEvidenceId = record.Definition.EvidenceId;
                        UIManager.Instance.ShowEvidenceModal(record.Definition);
                    }

                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    GUILayout.Space(6f);
                }
                GUILayout.EndScrollView();
            }

            GUILayout.EndArea();
        }

        // --- SECTION 2: TIMELINE & LOOP COMPARISON ---
        private void DrawLoopTimeline(Rect rect)
        {
            GUILayout.BeginArea(rect);

            GUILayout.BeginHorizontal();
            GUILayout.Label("OBSERVATION TIMELINE", UITheme.HeaderStyle);
            GUILayout.FlexibleSpace();

            string toggleLabel = _isComparisonMode ? "Switch to Chronological View" : "Compare Loops Side-by-Side";
            if (GUILayout.Button(toggleLabel, UITheme.PrimaryButtonStyle, GUILayout.Height(26f)))
            {
                _isComparisonMode = !_isComparisonMode;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            var knowledge = InvestigationKnowledgeService.Instance;
            var allObservations = knowledge != null ? knowledge.GetAllObservations() : Array.Empty<PlayerObservationEntry>();

            if (allObservations.Count == 0)
            {
                DrawEmptyState(
                    "NO EVENTS WITNESSED YET",
                    "Events you personally observe inside Meridian House (such as suspect actions, schedule failures, or door interactions) will be automatically recorded here."
                );
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
            // Group by loop
            var grouped = observations.GroupBy(o => o.LoopIndex).OrderBy(g => g.Key);

            _timelineScroll = GUILayout.BeginScrollView(_timelineScroll);

            foreach (var group in grouped)
            {
                GUILayout.BeginVertical(UITheme.CardStyle);
                GUILayout.Label($"<b>TIME LOOP {group.Key:D2}</b>", UITheme.SubHeaderStyle);
                GUILayout.Space(4f);

                foreach (var obs in group.OrderBy(o => o.SimTime))
                {
                    GUILayout.BeginHorizontal(UITheme.CardSelectedStyle);

                    // Timestamp
                    GUILayout.Label($"<color=#B69A61><b>{obs.GetFormattedTimestamp()}</b></color>", UITheme.BodyStyle, GUILayout.Width(60f));

                    // Method Badge
                    string badge = obs.Method == ObservationMethod.DirectInspection ? "INSPECTION" : "WITNESSED";
                    GUILayout.Box(badge, UITheme.BadgeStyle, GUILayout.Width(85f), GUILayout.Height(18f));

                    // Description
                    GUILayout.Label(obs.Description, UITheme.BodyStyle);

                    GUILayout.EndHorizontal();
                    GUILayout.Space(3f);
                }

                GUILayout.EndVertical();
                GUILayout.Space(10f);
            }

            GUILayout.EndScrollView();
        }

        private void DrawLoopComparisonView(InvestigationKnowledgeService knowledge)
        {
            GUILayout.BeginHorizontal(UITheme.CardStyle);
            GUILayout.Label("Select Loops to Compare:", UITheme.BodyStyle);

            GUILayout.Label("Loop A:", UITheme.BodyMutedStyle);
            if (int.TryParse(GUILayout.TextField(_comparisonLoopA.ToString(), GUILayout.Width(40f)), out int la)) _comparisonLoopA = Mathf.Max(1, la);

            GUILayout.Label("Loop B:", UITheme.BodyMutedStyle);
            if (int.TryParse(GUILayout.TextField(_comparisonLoopB.ToString(), GUILayout.Width(40f)), out int lb)) _comparisonLoopB = Mathf.Max(1, lb);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            var comparison = knowledge.CompareLoops(_comparisonLoopA, _comparisonLoopB);

            if (comparison.Count == 0)
            {
                DrawEmptyState("NO COMPARABLE OBSERVATIONS", $"No overlapping observations recorded between Loop {_comparisonLoopA} and Loop {_comparisonLoopB}.");
                return;
            }

            _timelineScroll = GUILayout.BeginScrollView(_timelineScroll);
            foreach (var item in comparison)
            {
                var cardColor = item.HasDivergence ? UITheme.RestrainedAmber : UITheme.SlateSurface;
                GUILayout.BeginVertical(UITheme.CardStyle);

                GUILayout.BeginHorizontal();
                GUILayout.Label($"<b>Observation ID:</b> {item.ObservationId}", UITheme.SubHeaderStyle);
                GUILayout.FlexibleSpace();
                if (item.HasDivergence)
                {
                    GUILayout.Box("DIVERGENT OUTCOME", UITheme.BadgeStyle, GUILayout.Width(130f), GUILayout.Height(18f));
                }
                else
                {
                    GUILayout.Box("IDENTICAL", UITheme.BadgeStyle, GUILayout.Width(80f), GUILayout.Height(18f));
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(4f);
                GUILayout.Label($"<b>Summary:</b> {item.ComparisonSummary}", UITheme.BodyStyle);

                GUILayout.Space(4f);
                GUILayout.BeginHorizontal();

                // Loop A Side
                GUILayout.BeginVertical(UITheme.CardSelectedStyle, GUILayout.Width(260f));
                GUILayout.Label($"<color=#B69A61>Loop {_comparisonLoopA}</color>", UITheme.SubHeaderStyle);
                if (item.RecordA != null)
                {
                    GUILayout.Label($"Time: {item.RecordA.GetFormattedTimestamp()}", UITheme.BodyMutedStyle);
                    GUILayout.Label(item.RecordA.Description, UITheme.BodyStyle);
                }
                else
                {
                    GUILayout.Label("<i>No record witnessed.</i>", UITheme.BodyMutedStyle);
                }
                GUILayout.EndVertical();

                // Loop B Side
                GUILayout.BeginVertical(UITheme.CardSelectedStyle, GUILayout.Width(260f));
                GUILayout.Label($"<color=#6D9C98>Loop {_comparisonLoopB}</color>", UITheme.SubHeaderStyle);
                if (item.RecordB != null)
                {
                    GUILayout.Label($"Time: {item.RecordB.GetFormattedTimestamp()}", UITheme.BodyMutedStyle);
                    GUILayout.Label(item.RecordB.Description, UITheme.BodyStyle);
                }
                else
                {
                    GUILayout.Label("<i>No record witnessed.</i>", UITheme.BodyMutedStyle);
                }
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.Space(6f);
            }
            GUILayout.EndScrollView();
        }

        // --- SECTION 3: DEDUCTION BOARD ---
        private void DrawDeductionBoard(Rect rect)
        {
            GUILayout.BeginArea(rect);

            GUILayout.BeginHorizontal();
            GUILayout.Label("DEDUCTION BOARD & EVIDENCE LINKS", UITheme.HeaderStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            var knowledge = InvestigationKnowledgeService.Instance;
            var discovered = knowledge != null ? knowledge.GetAllDiscoveredEvidence() : Array.Empty<DiscoveredEvidenceRecord>();
            var relationships = knowledge != null ? knowledge.GetAllRelationships() : Array.Empty<EvidenceRelationship>();

            if (discovered.Count < 2)
            {
                DrawEmptyState(
                    "INSUFFICIENT EVIDENCE FOR DEDUCTION",
                    "You must discover at least two pieces of evidence to form deductive links on the board.\nKeep investigating Meridian House to unlock connection capabilities."
                );
                GUILayout.EndArea();
                return;
            }

            // Connection Authoring Tool
            GUILayout.BeginVertical(UITheme.CardStyle);
            GUILayout.Label("ESTABLISH DEDUCTIVE RELATIONSHIP", UITheme.SubHeaderStyle);
            GUILayout.Space(4f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Source Clue:", UITheme.BodyMutedStyle, GUILayout.Width(80f));
            if (string.IsNullOrEmpty(_sourceConnectEvidenceId) && discovered.Count > 0)
            {
                _sourceConnectEvidenceId = discovered[0].Definition.EvidenceId;
            }

            // Dropdown substitute: buttons for discovered evidence
            foreach (var d in discovered)
            {
                var btnStyle = _sourceConnectEvidenceId == d.Definition.EvidenceId ? UITheme.PrimaryButtonStyle : UITheme.ButtonStyle;
                if (GUILayout.Button(d.Definition.DisplayName, btnStyle, GUILayout.Height(24f)))
                {
                    _sourceConnectEvidenceId = d.Definition.EvidenceId;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Target Clue:", UITheme.BodyMutedStyle, GUILayout.Width(80f));
            if (string.IsNullOrEmpty(_targetConnectEvidenceId) && discovered.Count > 1)
            {
                _targetConnectEvidenceId = discovered[1].Definition.EvidenceId;
            }

            foreach (var d in discovered)
            {
                var btnStyle = _targetConnectEvidenceId == d.Definition.EvidenceId ? UITheme.PrimaryButtonStyle : UITheme.ButtonStyle;
                if (GUILayout.Button(d.Definition.DisplayName, btnStyle, GUILayout.Height(24f)))
                {
                    _targetConnectEvidenceId = d.Definition.EvidenceId;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Type:", UITheme.BodyMutedStyle, GUILayout.Width(80f));
            foreach (EvidenceRelationshipType t in Enum.GetValues(typeof(EvidenceRelationshipType)))
            {
                var btnStyle = _newRelType == t ? UITheme.PrimaryButtonStyle : UITheme.ButtonStyle;
                if (GUILayout.Button(t.ToString(), btnStyle, GUILayout.Height(22f)))
                {
                    _newRelType = t;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Deduction:", UITheme.BodyMutedStyle, GUILayout.Width(80f));
            GUI.SetNextControlName("RelExplanationField");
            _newRelExplanation = GUILayout.TextField(_newRelExplanation);
            if (GUI.GetNameOfFocusedControl() == "RelExplanationField")
            {
                UIManager.Instance.IsTypingInField = true;
            }

            if (GUILayout.Button("Connect Clues", UITheme.PrimaryButtonStyle, GUILayout.Width(120f)))
            {
                if (_sourceConnectEvidenceId != _targetConnectEvidenceId)
                {
                    int loop = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                        ? SimulationManager.Instance.Clock.LoopCount
                        : 1;
                    knowledge.AddRelationship(
                        _sourceConnectEvidenceId,
                        _targetConnectEvidenceId,
                        _newRelType,
                        _newRelExplanation,
                        isPlayerCreated: true,
                        createdInLoop: loop
                    );
                    _newRelExplanation = "";
                    UIManager.Instance.IsTypingInField = false;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.Space(10f);
            GUILayout.Label("ACTIVE RELATIONSHIPS ON BOARD", UITheme.SubHeaderStyle);

            _boardScroll = GUILayout.BeginScrollView(_boardScroll);

            if (relationships.Count == 0)
            {
                GUILayout.Label("<i>No relationships connected yet. Form a connection above.</i>", UITheme.BodyMutedStyle);
            }
            else
            {
                foreach (var rel in relationships)
                {
                    var sourceDef = knowledge.GetEvidenceRecord(rel.SourceEvidenceId)?.Definition;
                    var targetDef = knowledge.GetEvidenceRecord(rel.TargetEvidenceId)?.Definition;

                    string sName = sourceDef != null ? sourceDef.DisplayName : rel.SourceEvidenceId;
                    string tName = targetDef != null ? targetDef.DisplayName : rel.TargetEvidenceId;

                    GUILayout.BeginVertical(UITheme.CardStyle);
                    GUILayout.BeginHorizontal();

                    GUILayout.Label($"<b>{sName}</b>  <color=#6D9C98>--[{rel.RelationshipType}]--> </color> <b>{tName}</b>", UITheme.BodyStyle);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Remove Link", UITheme.ButtonStyle, GUILayout.Width(100f), GUILayout.Height(22f)))
                    {
                        knowledge.RemoveRelationship(rel.RelationshipId);
                        break;
                    }
                    GUILayout.EndHorizontal();

                    if (!string.IsNullOrEmpty(rel.Explanation))
                    {
                        GUILayout.Space(2f);
                        GUILayout.Label($"<i>Reason: {rel.Explanation}</i>", UITheme.BodyMutedStyle);
                    }
                    GUILayout.EndVertical();
                    GUILayout.Space(4f);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // --- SECTION 4: HYPOTHESIS MANAGER ---
        private void DrawHypothesisManager(Rect rect)
        {
            GUILayout.BeginArea(rect);

            GUILayout.BeginHorizontal();
            GUILayout.Label("HYPOTHESIS WORKSPACE", UITheme.HeaderStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            var knowledge = InvestigationKnowledgeService.Instance;
            var hypotheses = knowledge != null ? knowledge.GetAllHypotheses() : Array.Empty<HypothesisEntry>();
            var allEvidence = knowledge != null ? knowledge.GetAllDiscoveredEvidence() : Array.Empty<DiscoveredEvidenceRecord>();

            // Authoring Box
            GUILayout.BeginVertical(UITheme.CardStyle);
            GUILayout.Label(_isEditingHypothesis ? "EDIT HYPOTHESIS" : "FORMULATE NEW HYPOTHESIS", UITheme.SubHeaderStyle);
            GUILayout.Space(4f);

            GUILayout.Label("Theory Title:", UITheme.BodyMutedStyle);
            GUI.SetNextControlName("HypoTitleField");
            _newHypoTitle = GUILayout.TextField(_newHypoTitle);

            GUILayout.Space(4f);
            GUILayout.Label("Deduction & Explanation:", UITheme.BodyMutedStyle);
            GUI.SetNextControlName("HypoExplanationField");
            _newHypoExplanation = GUILayout.TextArea(_newHypoExplanation, GUILayout.Height(50f));

            if (GUI.GetNameOfFocusedControl() == "HypoTitleField" || GUI.GetNameOfFocusedControl() == "HypoExplanationField")
            {
                UIManager.Instance.IsTypingInField = true;
            }

            GUILayout.Space(4f);
            GUILayout.Label("Attach Supporting Evidence:", UITheme.BodyMutedStyle);

            GUILayout.BeginHorizontal();
            foreach (var ev in allEvidence)
            {
                bool isAttached = _selectedSupportingEvidence.Contains(ev.Definition.EvidenceId);
                bool toggle = GUILayout.Toggle(isAttached, ev.Definition.DisplayName, GUILayout.Width(160f));
                if (toggle != isAttached)
                {
                    if (toggle) _selectedSupportingEvidence.Add(ev.Definition.EvidenceId);
                    else _selectedSupportingEvidence.Remove(ev.Definition.EvidenceId);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            string btnText = _isEditingHypothesis ? "Save Changes" : "Record Hypothesis";
            if (GUILayout.Button(btnText, UITheme.PrimaryButtonStyle, GUILayout.Width(150f)))
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
                    _selectedSupportingEvidence.Clear();
                    UIManager.Instance.IsTypingInField = false;
                }
            }

            if (_isEditingHypothesis)
            {
                if (GUILayout.Button("Cancel", UITheme.ButtonStyle, GUILayout.Width(80f)))
                {
                    _isEditingHypothesis = false;
                    _editingHypoId = "";
                    _newHypoTitle = "";
                    _newHypoExplanation = "";
                    _selectedSupportingEvidence.Clear();
                    UIManager.Instance.IsTypingInField = false;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.Space(10f);
            GUILayout.Label("ACTIVE HYPOTHESES", UITheme.SubHeaderStyle);

            _hypothesesScroll = GUILayout.BeginScrollView(_hypothesesScroll);
            if (hypotheses.Count == 0)
            {
                GUILayout.Label("<i>No hypotheses recorded yet. Formulate a hypothesis above.</i>", UITheme.BodyMutedStyle);
            }
            else
            {
                foreach (var h in hypotheses)
                {
                    GUILayout.BeginVertical(UITheme.CardStyle);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"<b>{h.Title}</b>", UITheme.SubHeaderStyle);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Edit", UITheme.ButtonStyle, GUILayout.Width(60f), GUILayout.Height(22f)))
                    {
                        _isEditingHypothesis = true;
                        _editingHypoId = h.HypothesisId;
                        _newHypoTitle = h.Title;
                        _newHypoExplanation = h.Explanation;
                        _selectedSupportingEvidence.Clear();
                        foreach (var id in h.SupportingEvidenceIds) _selectedSupportingEvidence.Add(id);
                    }

                    if (GUILayout.Button("Delete", UITheme.ButtonStyle, GUILayout.Width(60f), GUILayout.Height(22f)))
                    {
                        knowledge.DeleteHypothesis(h.HypothesisId);
                        break;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(3f);
                    GUILayout.Label(h.Explanation, UITheme.BodyStyle);

                    if (h.SupportingEvidenceIds.Count > 0)
                    {
                        GUILayout.Space(4f);
                        GUILayout.Label($"Supporting Evidence: <color=#6D9C98>{string.Join(", ", h.SupportingEvidenceIds)}</color>", UITheme.BodyMutedStyle);
                    }

                    GUILayout.EndVertical();
                    GUILayout.Space(4f);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        // --- SECTION 5: FIELD NOTES ---
        private void DrawFieldNotes(Rect rect)
        {
            GUILayout.BeginArea(rect);

            GUILayout.BeginHorizontal();
            GUILayout.Label("DETECTIVE FIELD NOTES", UITheme.HeaderStyle);
            GUILayout.FlexibleSpace();

            var knowledge = InvestigationKnowledgeService.Instance;
            int length = knowledge != null && knowledge.GeneralNotes != null ? knowledge.GeneralNotes.Length : 0;
            GUILayout.Label($"Characters: {length} / 4000", UITheme.BodyMutedStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("Freeform scratchpad for recording cross-loop patterns, suspect anomalies, and timing deductions. Persists permanently across time loops.", UITheme.BodyMutedStyle);
            GUILayout.Space(8f);

            if (knowledge != null)
            {
                GUI.SetNextControlName("FieldNotesTextArea");
                _notesScroll = GUILayout.BeginScrollView(_notesScroll);

                string updatedNotes = GUILayout.TextArea(knowledge.GeneralNotes, GUILayout.MinHeight(rect.height - 100f));
                if (updatedNotes != knowledge.GeneralNotes)
                {
                    knowledge.GeneralNotes = updatedNotes;
                }

                if (GUI.GetNameOfFocusedControl() == "FieldNotesTextArea")
                {
                    UIManager.Instance.IsTypingInField = true;
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndArea();
        }

        // --- RIGHT DETAIL PANEL ---
        private void DrawRightDetailPanel(Rect rect)
        {
            UITheme.DrawSolidRect(rect, UITheme.ObsidianSurface);
            UITheme.DrawOrnateFrame(rect, UITheme.PolishedBrass, UITheme.BrassHighlight, 1.5f);

            GUILayout.BeginArea(new Rect(rect.x + 16f, rect.y + 16f, rect.width - 32f, rect.height - 32f));

            GUILayout.Label("DOSSIER INSPECTOR", UITheme.HeaderStyle);
            GUILayout.Space(10f);

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
                GUILayout.Box(def.Category, UITheme.BadgeStyle, GUILayout.Width(100f), GUILayout.Height(18f));
                GUILayout.Space(6f);
                GUILayout.Label($"Discovered: Loop {selectedRecord.DiscoveredInLoop} @ {selectedRecord.GetFormattedTimestamp()}", UITheme.BodyMutedStyle);
                GUILayout.EndVertical();

                GUILayout.Space(10f);
                GUILayout.Label("ANALYSIS & TEXT:", UITheme.SubHeaderStyle);
                GUILayout.Space(4f);
                GUILayout.Label(def.DetailedInspectionText, UITheme.BodyStyle);

                GUILayout.Space(12f);
                if (!string.IsNullOrEmpty(def.RelatedEventId))
                {
                    GUILayout.Label("LINKED CAUSAL EVENT:", UITheme.SubHeaderStyle);
                    GUILayout.Label($"Event Routine: <color=#6D9C98>{def.RelatedEventId}</color>", UITheme.BodyStyle);
                    GUILayout.Space(10f);
                }

                // Related Evidence Links
                var rels = knowledge.GetRelationshipsForEvidence(def.EvidenceId);
                if (rels.Count > 0)
                {
                    GUILayout.Label("DEDUCTIVE CONNECTIONS:", UITheme.SubHeaderStyle);
                    foreach (var r in rels)
                    {
                        string other = r.SourceEvidenceId == def.EvidenceId ? r.TargetEvidenceId : r.SourceEvidenceId;
                        GUILayout.Label($"• [{r.RelationshipType}] with <color=#B69A61>{other}</color>", UITheme.BodyMutedStyle);
                    }
                    GUILayout.Space(10f);
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Open Full Inspection Modal", UITheme.PrimaryButtonStyle, GUILayout.Height(32f)))
                {
                    UIManager.Instance.ShowEvidenceModal(def);
                }
            }
            else
            {
                GUILayout.BeginVertical(UITheme.CardStyle);
                GUILayout.Label("NO CLUE SELECTED", UITheme.SubHeaderStyle);
                GUILayout.Space(6f);
                GUILayout.Label("Select an evidence item from the Evidence Library or Deduction Board to inspect its full metadata, analysis, and links.", UITheme.BodyMutedStyle);
                GUILayout.EndVertical();
            }

            GUILayout.EndArea();
        }

        private void DrawEmptyState(string title, string description)
        {
            GUILayout.BeginVertical(UITheme.CardStyle);
            GUILayout.Space(20f);
            GUILayout.Label($"<b>{title}</b>", UITheme.SubHeaderStyle);
            GUILayout.Space(8f);
            GUILayout.Label(description, UITheme.BodyStyle);
            GUILayout.Space(20f);
            GUILayout.EndVertical();
        }

        // --- SECTION 6: ACCUSATION / SOLVE CASE ---
        private void DrawAccusationBoard(Rect rect)
        {
            GUILayout.BeginArea(rect);

            GUILayout.Label("FORMAL CASE DEDUCTION & ACCUSATION", UITheme.HeaderStyle);
            GUILayout.Space(4f);
            GUILayout.Label("Synthesize your investigation. Identify the perpetrator, murder weapon, crime scene, and motive.", UITheme.BodyMutedStyle);
            GUILayout.Space(8f);

            var accusationService = CaseAccusationService.Instance;
            var suspects = accusationService != null ? accusationService.GetKnownSuspects() : new List<string> { "Concierge Pierre", "Celeste Moreau", "Chef Marcel" };
            var weapons = accusationService != null ? accusationService.GetKnownWeapons() : new List<string> { "Heavy Meat Cleaver", "Heavy Brass Candlestick", "Poison Vial" };
            var rooms = accusationService != null ? accusationService.GetKnownCrimeScenes() : new List<string> { "Room 102 Suite", "Kitchen & Pantry", "Staff Office" };
            var motives = accusationService != null ? accusationService.GetKnownMotives() : new List<string> { "Blackmail Silence", "Financial Embezzlement", "Revenge & Spite" };

            _accusationScroll = GUILayout.BeginScrollView(_accusationScroll, GUILayout.Height(rect.height - 130f));

            // Card 1: Accused Culprit
            GUILayout.BeginVertical(UITheme.CardStyle);
            GUILayout.Label($"<b>1. ACCUSED PERPETRATOR:</b> <color=#{ColorUtility.ToHtmlStringRGB(UITheme.BrassHighlight)}>{_accusedSuspect}</color>", UITheme.SubHeaderStyle);
            GUILayout.Space(6f);
            DrawOptionGrid(suspects, _accusedSuspect, s => _accusedSuspect = s, columns: 2);
            GUILayout.EndVertical();

            GUILayout.Space(8f);

            // Card 2: Murder Weapon
            GUILayout.BeginVertical(UITheme.CardStyle);
            GUILayout.Label($"<b>2. MURDER WEAPON:</b> <color=#{ColorUtility.ToHtmlStringRGB(UITheme.BrassHighlight)}>{_chosenWeapon}</color>", UITheme.SubHeaderStyle);
            GUILayout.Space(6f);
            DrawOptionGrid(weapons, _chosenWeapon, w => _chosenWeapon = w, columns: 2);
            GUILayout.EndVertical();

            GUILayout.Space(8f);

            // Card 3: Crime Scene Room
            GUILayout.BeginVertical(UITheme.CardStyle);
            GUILayout.Label($"<b>3. CRIME SCENE LOCATION:</b> <color=#{ColorUtility.ToHtmlStringRGB(UITheme.BrassHighlight)}>{_chosenRoom}</color>", UITheme.SubHeaderStyle);
            GUILayout.Space(6f);
            DrawOptionGrid(rooms, _chosenRoom, rm => _chosenRoom = rm, columns: 2);
            GUILayout.EndVertical();

            GUILayout.Space(8f);

            // Card 4: Motive
            GUILayout.BeginVertical(UITheme.CardStyle);
            GUILayout.Label($"<b>4. UNDERLYING MOTIVE:</b> <color=#{ColorUtility.ToHtmlStringRGB(UITheme.BrassHighlight)}>{_chosenMotive}</color>", UITheme.SubHeaderStyle);
            GUILayout.Space(6f);
            DrawOptionGrid(motives, _chosenMotive, m => _chosenMotive = m, columns: 2);
            GUILayout.EndVertical();

            GUILayout.EndScrollView();

            GUILayout.Space(10f);

            // Big Climax Submit Button
            if (UITheme.DrawLuxeButton(new Rect(0, rect.height - 48f, rect.width, 44f), "CONFRONT SUSPECT & SUBMIT FINAL DEDUCTION", isPrimary: true))
            {
                if (CaseAccusationService.Instance != null)
                {
                    var result = CaseAccusationService.Instance.FormulateAccusation(_accusedSuspect, _chosenWeapon, _chosenRoom, _chosenMotive);
                    UIManager.Instance?.ShowCaseResolutionModal(result);
                }
            }

            GUILayout.EndArea();
        }

        private void DrawOptionGrid(List<string> options, string selectedOption, System.Action<string> onSelect, int columns = 2)
        {
            if (options == null || options.Count == 0) return;

            for (int i = 0; i < options.Count; i += columns)
            {
                GUILayout.BeginHorizontal();
                for (int j = 0; j < columns && (i + j) < options.Count; j++)
                {
                    string opt = options[i + j];
                    bool isSel = selectedOption == opt;
                    var bStyle = isSel ? UITheme.PrimaryButtonStyle : UITheme.ButtonStyle;
                    if (GUILayout.Button(opt, bStyle, GUILayout.Height(32f)))
                    {
                        onSelect(opt);
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(2f);
            }
        }
    }
}
