using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TomorrowsCrime.Player;
using TomorrowsCrime.Simulation;

namespace TomorrowsCrime.Investigation
{
    /// <summary>
    /// Interactive Detective Notebook UI.
    /// Toggleable via Tab or N key.
    /// 
    /// Contains three sections:
    /// 1. Evidence: Discovered evidence catalog and detailed clue inspector.
    /// 2. Timeline: Factual player-observed event history across loops.
    /// 3. Hypotheses & Notes: Player-authored theories and freeform deduction notes.
    /// </summary>
    public class NotebookUIController : MonoBehaviour
    {
        public enum NotebookTab
        {
            Evidence,
            Timeline,
            HypothesesAndNotes
        }

        [Header("Configuration")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
        [SerializeField] private KeyCode alternateToggleKey = KeyCode.N;

        [Header("State")]
        [SerializeField] private bool isOpen;

        public bool IsOpen => isOpen;

        private NotebookTab _currentTab = NotebookTab.Evidence;
        private PlayerController _playerController;

        // UI state: Evidence
        private string _selectedEvidenceId;
        private Vector2 _evidenceListScroll;
        private Vector2 _evidenceDetailScroll;

        // UI state: Timeline
        private Vector2 _timelineScroll;

        // UI state: Hypotheses & Notes
        private Vector2 _hypothesesScroll;
        private bool _isCreatingHypothesis;
        private string _editingHypothesisId;
        private string _inputTitle = string.Empty;
        private string _inputExplanation = string.Empty;
        private readonly HashSet<string> _selectedSupportingEvidence = new();

        // GUI Styles
        private GUIStyle _windowStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _tabButtonStyle;
        private GUIStyle _activeTabButtonStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _detailBoxStyle;
        private bool _stylesInitialized;

        public event Action<bool> OnNotebookToggle;

        private void Awake()
        {
            _playerController = FindAnyObjectByType<PlayerController>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(alternateToggleKey))
            {
                SetOpen(!isOpen);
            }
        }

        public void SetOpen(bool open)
        {
            isOpen = open;

            if (_playerController == null)
            {
                _playerController = FindAnyObjectByType<PlayerController>();
            }

            if (_playerController != null)
            {
                _playerController.SetInputBlocked(isOpen);
            }

            OnNotebookToggle?.Invoke(isOpen);

            // If opening, load current notes
            if (isOpen)
            {
                _isCreatingHypothesis = false;
                _editingHypothesisId = null;
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0.08f, 0.10f, 0.14f, 0.98f));
            bgTex.Apply();

            var detailTex = new Texture2D(1, 1);
            detailTex.SetPixel(0, 0, new Color(0.12f, 0.15f, 0.20f, 0.90f));
            detailTex.Apply();

            _windowStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = bgTex },
                padding = new RectOffset(16, 16, 16, 16)
            };

            _detailBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = detailTex },
                padding = new RectOffset(12, 12, 12, 12)
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.78f, 0.25f) }
            };

            _tabButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _activeTabButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.78f, 0.25f) }
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.7f, 0.8f, 0.9f) }
            };

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.85f, 0.88f, 0.92f) },
                wordWrap = true
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!isOpen) return;

            InitStyles();

            // Notebook dimensions
            float width = Mathf.Min(860f, Screen.width - 60f);
            float height = Mathf.Min(620f, Screen.height - 60f);
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            GUILayout.BeginArea(new Rect(x, y, width, height), _windowStyle);

            // Top Header & Navigation
            DrawTopHeader();

            GUILayout.Space(8);

            // Tab Content
            switch (_currentTab)
            {
                case NotebookTab.Evidence:
                    DrawEvidenceTab(width);
                    break;
                case NotebookTab.Timeline:
                    DrawTimelineTab();
                    break;
                case NotebookTab.HypothesesAndNotes:
                    DrawHypothesesAndNotesTab();
                    break;
            }

            GUILayout.EndArea();
        }

        private void DrawTopHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("DETECTIVE NOTEBOOK — MERIDIAN HOUSE", _headerStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("✕ CLOSE [Tab / Esc]", GUILayout.Width(140), GUILayout.Height(26)))
            {
                SetOpen(false);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Tabs Selector
            GUILayout.BeginHorizontal();

            int evidenceCount = InvestigationKnowledgeService.Instance.EvidenceCount;
            int obsCount = InvestigationKnowledgeService.Instance.ObservationCount;
            int hypCount = InvestigationKnowledgeService.Instance.HypothesisCount;

            if (GUILayout.Button($"EVIDENCE ({evidenceCount})", _currentTab == NotebookTab.Evidence ? _activeTabButtonStyle : _tabButtonStyle, GUILayout.Height(30)))
            {
                _currentTab = NotebookTab.Evidence;
            }

            if (GUILayout.Button($"OBSERVATION TIMELINE ({obsCount})", _currentTab == NotebookTab.Timeline ? _activeTabButtonStyle : _tabButtonStyle, GUILayout.Height(30)))
            {
                _currentTab = NotebookTab.Timeline;
            }

            if (GUILayout.Button($"HYPOTHESES & NOTES ({hypCount})", _currentTab == NotebookTab.HypothesesAndNotes ? _activeTabButtonStyle : _tabButtonStyle, GUILayout.Height(30)))
            {
                _currentTab = NotebookTab.HypothesesAndNotes;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawEvidenceTab(float totalWidth)
        {
            var evidenceList = InvestigationKnowledgeService.Instance.GetAllDiscoveredEvidence();

            if (evidenceList.Count == 0)
            {
                GUILayout.Space(20);
                GUILayout.Label("No evidence discovered yet.", _titleStyle);
                GUILayout.Label("Explore the hotel lobby, inspect objects, and examine suspicious locations to discover clues.", _bodyStyle);
                return;
            }

            // Auto-select first if none selected
            if (string.IsNullOrEmpty(_selectedEvidenceId) && evidenceList.Count > 0)
            {
                _selectedEvidenceId = evidenceList[0].EvidenceId;
            }

            float leftWidth = (totalWidth - 48f) * 0.4f;
            float rightWidth = (totalWidth - 48f) * 0.6f;

            GUILayout.BeginHorizontal();

            // Left Panel: Discovered Evidence List
            GUILayout.BeginVertical(GUILayout.Width(leftWidth));
            GUILayout.Label("DISCOVERED CLUES", _subHeaderStyle);

            _evidenceListScroll = GUILayout.BeginScrollView(_evidenceListScroll, GUILayout.Height(460));
            foreach (var ev in evidenceList)
            {
                bool isSelected = ev.EvidenceId == _selectedEvidenceId;
                GUIStyle btnStyle = isSelected ? _activeTabButtonStyle : _tabButtonStyle;

                if (GUILayout.Button($"[{ev.Category}]\n{ev.DisplayName}", btnStyle, GUILayout.Height(44)))
                {
                    _selectedEvidenceId = ev.EvidenceId;
                }
                GUILayout.Space(2);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(12);

            // Right Panel: Detailed Clue View
            GUILayout.BeginVertical(_detailBoxStyle, GUILayout.Width(rightWidth), GUILayout.Height(485));

            var selected = InvestigationKnowledgeService.Instance.GetEvidenceRecord(_selectedEvidenceId);
            if (selected != null)
            {
                GUILayout.Label(selected.DisplayName, _headerStyle);
                GUILayout.Label($"Category: {selected.Category}  |  Discovered in Loop #{selected.DiscoveredInLoop} at {selected.GetFormattedTimestamp()}", _subHeaderStyle);
                GUILayout.Space(8);

                GUILayout.Label("Brief Description:", _subHeaderStyle);
                GUILayout.Label(selected.ShortDescription, _bodyStyle);
                GUILayout.Space(8);

                GUILayout.Label("Detailed Inspection Analysis:", _subHeaderStyle);
                _evidenceDetailScroll = GUILayout.BeginScrollView(_evidenceDetailScroll);
                GUILayout.Label(selected.DetailedInspectionText, _bodyStyle);
                GUILayout.EndScrollView();

                if (selected.Definition.RelatedEvidenceIds.Count > 0)
                {
                    GUILayout.Space(6);
                    GUILayout.Label("Cross-References:", _subHeaderStyle);
                    foreach (var refId in selected.Definition.RelatedEvidenceIds)
                    {
                        if (InvestigationKnowledgeService.Instance.HasDiscoveredEvidence(refId))
                        {
                            var related = InvestigationKnowledgeService.Instance.GetEvidenceRecord(refId);
                            GUILayout.Label($"• Related Clue: {related.DisplayName}", _bodyStyle);
                        }
                    }
                }
            }
            else
            {
                GUILayout.Label("Select an evidence item from the list to inspect details.", _bodyStyle);
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawTimelineTab()
        {
            var observations = InvestigationKnowledgeService.Instance.GetAllObservations();

            GUILayout.Label("WITNESSED EVENT TIMELINE", _subHeaderStyle);
            GUILayout.Label("Chronological log of events you have personally observed across simulation loops.", _bodyStyle);
            GUILayout.Space(6);

            if (observations.Count == 0)
            {
                GUILayout.Label("No personal observations recorded yet.\nPosition yourself to witness suspect routines and environmental reactions during the 5-minute loop.", _bodyStyle);
                return;
            }

            _timelineScroll = GUILayout.BeginScrollView(_timelineScroll, GUILayout.Height(460));

            // Group by loop index for cross-loop comparison
            var grouped = observations.GroupBy(o => o.LoopIndex).OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                GUILayout.Label($"=== LOOP #{group.Key} ===", _headerStyle);
                GUILayout.Space(2);

                foreach (var obs in group.OrderBy(o => o.SimTime))
                {
                    GUILayout.BeginVertical(_detailBoxStyle);
                    GUILayout.Label($"[{obs.GetFormattedTimestamp()}] — {obs.Description}", _titleStyle);
                    if (!string.IsNullOrEmpty(obs.RelatedEvidenceId))
                    {
                        GUILayout.Label($"Connected Clue: {obs.RelatedEvidenceId}", _subHeaderStyle);
                    }
                    GUILayout.EndVertical();
                    GUILayout.Space(4);
                }

                GUILayout.Space(8);
            }

            GUILayout.EndScrollView();
        }

        private void DrawHypothesesAndNotesTab()
        {
            GUILayout.BeginHorizontal();

            // Left side: General Notes (35%)
            GUILayout.BeginVertical(GUILayout.Width(280));
            GUILayout.Label("INVESTIGATION NOTES", _subHeaderStyle);
            GUILayout.Label("Freeform notes preserved across all loops:", _bodyStyle);

            var notes = InvestigationKnowledgeService.Instance.GeneralNotes;
            string updatedNotes = GUILayout.TextArea(notes, GUILayout.Height(430));
            if (updatedNotes != notes)
            {
                InvestigationKnowledgeService.Instance.GeneralNotes = updatedNotes;
            }
            GUILayout.EndVertical();

            GUILayout.Space(12);

            // Right side: Hypotheses formulation (65%)
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("THEORIES & HYPOTHESES", _subHeaderStyle);
            GUILayout.FlexibleSpace();

            if (!_isCreatingHypothesis && GUILayout.Button("+ NEW HYPOTHESIS", GUILayout.Width(150), GUILayout.Height(24)))
            {
                _isCreatingHypothesis = true;
                _editingHypothesisId = null;
                _inputTitle = "Suspect Opportunity Theory";
                _inputExplanation = string.Empty;
                _selectedSupportingEvidence.Clear();
            }
            GUILayout.EndHorizontal();

            if (_isCreatingHypothesis)
            {
                DrawHypothesisForm();
            }
            else
            {
                DrawHypothesesList();
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawHypothesisForm()
        {
            GUILayout.BeginVertical(_detailBoxStyle, GUILayout.Height(450));
            GUILayout.Label(_editingHypothesisId == null ? "FORMULATE NEW HYPOTHESIS" : "EDIT HYPOTHESIS", _titleStyle);
            GUILayout.Space(4);

            GUILayout.Label("Hypothesis Title:", _subHeaderStyle);
            _inputTitle = GUILayout.TextField(_inputTitle, GUILayout.Height(24));
            GUILayout.Space(4);

            GUILayout.Label("Deductive Reasoning & Explanation:", _subHeaderStyle);
            _inputExplanation = GUILayout.TextArea(_inputExplanation, GUILayout.Height(90));
            GUILayout.Space(6);

            GUILayout.Label("Supporting Discovered Evidence:", _subHeaderStyle);
            var discovered = InvestigationKnowledgeService.Instance.GetAllDiscoveredEvidence();

            if (discovered.Count == 0)
            {
                GUILayout.Label("(No evidence discovered yet to link to this hypothesis)", _bodyStyle);
            }
            else
            {
                foreach (var ev in discovered)
                {
                    bool isChecked = _selectedSupportingEvidence.Contains(ev.EvidenceId);
                    bool newChecked = GUILayout.Toggle(isChecked, $" {ev.DisplayName} ({ev.Category})");
                    if (newChecked && !isChecked) _selectedSupportingEvidence.Add(ev.EvidenceId);
                    else if (!newChecked && isChecked) _selectedSupportingEvidence.Remove(ev.EvidenceId);
                }
            }

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("SAVE HYPOTHESIS", GUILayout.Height(28)))
            {
                SaveHypothesis();
            }

            if (GUILayout.Button("CANCEL", GUILayout.Height(28)))
            {
                _isCreatingHypothesis = false;
                _editingHypothesisId = null;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void SaveHypothesis()
        {
            if (string.IsNullOrWhiteSpace(_inputTitle)) return;

            var service = InvestigationKnowledgeService.Instance;

            if (_editingHypothesisId != null)
            {
                service.UpdateHypothesis(_editingHypothesisId, _inputTitle, _inputExplanation, _selectedSupportingEvidence);
            }
            else
            {
                int loop = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                    ? SimulationManager.Instance.Clock.LoopCount
                    : 1;

                float simTime = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                    ? SimulationManager.Instance.Clock.ElapsedSeconds
                    : 0f;

                service.CreateHypothesis(_inputTitle, _inputExplanation, loop, simTime, _selectedSupportingEvidence);
            }

            _isCreatingHypothesis = false;
            _editingHypothesisId = null;
        }

        private void DrawHypothesesList()
        {
            var hypotheses = InvestigationKnowledgeService.Instance.GetAllHypotheses();

            if (hypotheses.Count == 0)
            {
                GUILayout.Space(10);
                GUILayout.Label("No hypotheses formulated yet.\nClick '+ New Hypothesis' above to record theories about the murder or suspect motives.", _bodyStyle);
                return;
            }

            _hypothesesScroll = GUILayout.BeginScrollView(_hypothesesScroll, GUILayout.Height(450));

            foreach (var hyp in hypotheses)
            {
                GUILayout.BeginVertical(_detailBoxStyle);

                GUILayout.BeginHorizontal();
                GUILayout.Label(hyp.Title, _titleStyle);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Edit", GUILayout.Width(60)))
                {
                    _isCreatingHypothesis = true;
                    _editingHypothesisId = hyp.HypothesisId;
                    _inputTitle = hyp.Title;
                    _inputExplanation = hyp.Explanation;
                    _selectedSupportingEvidence.Clear();
                    foreach (var id in hyp.SupportingEvidenceIds) _selectedSupportingEvidence.Add(id);
                }

                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    InvestigationKnowledgeService.Instance.DeleteHypothesis(hyp.HypothesisId);
                    break;
                }
                GUILayout.EndHorizontal();

                GUILayout.Label($"Formulated in Loop #{hyp.CreatedInLoop}", _subHeaderStyle);
                GUILayout.Space(2);
                GUILayout.Label(hyp.Explanation, _bodyStyle);

                if (hyp.SupportingEvidenceIds.Count > 0)
                {
                    GUILayout.Space(4);
                    GUILayout.Label("Supporting Evidence:", _subHeaderStyle);
                    foreach (var id in hyp.SupportingEvidenceIds)
                    {
                        var ev = InvestigationKnowledgeService.Instance.GetEvidenceRecord(id);
                        string name = ev != null ? ev.DisplayName : id;
                        GUILayout.Label($"• {name}", _bodyStyle);
                    }
                }

                GUILayout.EndVertical();
                GUILayout.Space(6);
            }

            GUILayout.EndScrollView();
        }
    }
}
