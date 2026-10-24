using System.Collections.Generic;
using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Events;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.Player;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.World;

namespace TomorrowsCrime.Debugging
{
    /// <summary>
    /// Real-time developer diagnostic overlay and contextual interaction HUD.
    /// Provides observability for:
    /// - 5-minute simulation clock and loop turnover
    /// - Live world state (key position, door status)
    /// - Chronological causal event history & dependency explanations
    /// - Cross-loop persistent investigation discoveries
    /// - First-person interaction prompt
    /// </summary>
    public class SimulationDebugUI : MonoBehaviour
    {
        [SerializeField] private bool showUI = true;

        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _timeStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _successStyle;
        private GUIStyle _failStyle;
        private GUIStyle _skipStyle;
        private GUIStyle _promptStyle;
        private Vector2 _scrollPosition;
        private bool _stylesInitialized;

        private PlayerInteraction _playerInteraction;

        private void Start()
        {
            _playerInteraction = FindAnyObjectByType<PlayerInteraction>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.BackQuote))
            {
                showUI = !showUI;
            }

            if (_playerInteraction == null)
            {
                _playerInteraction = FindAnyObjectByType<PlayerInteraction>();
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0.04f, 0.06f, 0.09f, 0.94f));
            bgTex.Apply();

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = bgTex },
                padding = new RectOffset(12, 12, 12, 12)
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.75f, 0.2f) }
            };

            _timeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.85f, 0.88f, 0.92f) },
                wordWrap = true
            };

            _successStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.3f, 0.9f, 0.4f) },
                wordWrap = true
            };

            _failStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.35f, 0.35f) },
                wordWrap = true
            };

            _skipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.75f, 0.25f) },
                wordWrap = true
            };

            _promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            // 1. Draw Center Interaction Prompt
            DrawInteractionPrompt();

            if (!showUI) return;

            var mgr = SimulationManager.Instance;
            var clock = mgr != null ? mgr.Clock : null;

            // 2. Draw Simulation, Causal Event History & World State Panel
            GUILayout.BeginArea(new Rect(14, 14, 420, 640), _boxStyle);

            GUILayout.Label("TOMORROW'S CRIME — SIMULATION & EVENT ENGINE", _headerStyle);
            GUILayout.Space(2);

            if (clock != null)
            {
                int loop = clock.LoopCount;
                string elapsed = clock.GetFormattedTime();
                string remaining = clock.GetFormattedRemainingTime();
                float progress = clock.NormalizedProgress * 100f;

                GUILayout.Label($"LOOP #{loop}  —  {elapsed} / 05:00", _timeStyle);
                GUILayout.Label($"Remaining: {remaining}  |  {progress:F0}% Elapsed", _labelStyle);
                GUILayout.Space(4);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(clock.IsPaused ? "▶ RESUME" : "❚❚ PAUSE", GUILayout.Height(24)))
                {
                    clock.TogglePause();
                }

                if (GUILayout.Button("↺ RESET LOOP [R]", GUILayout.Height(24)))
                {
                    mgr.ForceResetLoop();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                GUILayout.Label($"Speed Multiplier: {clock.TimeScale:F1}x", _labelStyle);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("1x [1]")) clock.SetTimeScale(1.0f);
                if (GUILayout.Button("2x [2]")) clock.SetTimeScale(2.0f);
                if (GUILayout.Button("5x [5]")) clock.SetTimeScale(5.0f);
                if (GUILayout.Button("10x")) clock.SetTimeScale(10.0f);
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Waiting for SimulationManager...", _labelStyle);
            }

            GUILayout.Space(6);
            GUILayout.Label("WORLD STATE", _headerStyle);

            string keyStateStr = "Unknown";
            if (WorldStateManager.Instance != null &&
                WorldStateManager.Instance.Registry.TryGetState<PropLocationState>("key_room_102", out var ks))
            {
                keyStateStr = ks switch
                {
                    PropLocationState.AtBaseline => "At Table (Lobby)",
                    PropLocationState.HeldByPlayer => "Carried by Player",
                    PropLocationState.DroppedElsewhere => "Moved / Relocated",
                    _ => ks.ToString()
                };
            }
            GUILayout.Label($"Room 102 Keycard: {keyStateStr}", _labelStyle);

            string doorStateStr = "Unknown";
            if (WorldStateManager.Instance != null &&
                WorldStateManager.Instance.Registry.TryGetState<DoorLockState>("door_room_102", out var ds))
            {
                doorStateStr = ds == DoorLockState.Locked ? "Locked" : "Unlocked";
            }
            GUILayout.Label($"Door 102: {doorStateStr}", _labelStyle);

            // Chronological Event Engine History
            GUILayout.Space(6);
            GUILayout.Label("CAUSAL EVENT HISTORY (CHRONOLOGICAL)", _headerStyle);

            var scheduler = CausalEventScheduler.Instance;
            var history = scheduler != null && scheduler.Engine != null ? scheduler.Engine.History : null;

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(110));
            if (history == null || history.Count == 0)
            {
                GUILayout.Label("No events processed yet in current loop. (Watch at 00:30)", _labelStyle);
            }
            else
            {
                foreach (var record in history)
                {
                    GUIStyle statusStyle = record.Status switch
                    {
                        EventStatus.Executed => _successStyle,
                        EventStatus.Failed => _failStyle,
                        EventStatus.Skipped or EventStatus.Cancelled => _skipStyle,
                        _ => _labelStyle
                    };

                    GUILayout.Label($"[{record.GetFormattedTime()}] {record.EventId} — {record.Status}", statusStyle);
                    GUILayout.Label($"  Reason: {record.Explanation}", _labelStyle);
                }
            }
            GUILayout.EndScrollView();

            // NPC Simulation Diagnostics
            GUILayout.Space(6);
            GUILayout.Label("SUSPECT / NPC SIMULATION", _headerStyle);
            var npc = FindAnyObjectByType<TomorrowsCrime.NPC.NPCController>();
            if (npc != null && npc.RuntimeState != null)
            {
                var rt = npc.RuntimeState;
                string npcName = npc.Definition != null ? npc.Definition.DisplayName : rt.CharacterId;
                GUIStyle npcStatusStyle = rt.BehaviorState switch
                {
                    TomorrowsCrime.NPC.NPCBehaviorState.PerformingAction => _successStyle,
                    TomorrowsCrime.NPC.NPCBehaviorState.Investigating => _failStyle,
                    TomorrowsCrime.NPC.NPCBehaviorState.Blocked => _failStyle,
                    TomorrowsCrime.NPC.NPCBehaviorState.Waiting => _skipStyle,
                    _ => _labelStyle
                };

                GUILayout.Label($"{npcName} [{rt.BehaviorState}] (Nav: {rt.NavigationStatus})", npcStatusStyle);
                if (rt.CurrentActivity != null)
                {
                    GUILayout.Label($"Activity: {rt.CurrentActivity.ActivityType} -> {rt.CurrentDestinationKey}", _labelStyle);
                }
                GUILayout.Label($"Status: {rt.LastDecisionReason}", _labelStyle);
            }
            else
            {
                GUILayout.Label("No active NPC in scene.", _labelStyle);
            }

            // Persistent Discoveries Section
            GUILayout.Space(6);
            var discoveries = InvestigationKnowledgeService.Instance.GetAllDiscoveries();
            GUILayout.Label($"PERSISTENT DISCOVERIES ({discoveries.Count})", _headerStyle);

            if (discoveries.Count == 0)
            {
                GUILayout.Label("No discoveries yet. (Witness the 00:30 event fail to learn clues)", _labelStyle);
            }
            else
            {
                foreach (var d in discoveries)
                {
                    GUILayout.Label($"• [{d.Id}] (Loop {d.DiscoveredInLoop}): {d.Description}", _labelStyle);
                }
            }

            GUILayout.Space(4);
            GUILayout.Label("WASD=Move | Mouse=Look | E=Interact | P=Pause | 1/2/5=Speed | R=Reset | F1=HUD", _labelStyle);

            GUILayout.EndArea();
        }

        private void DrawInteractionPrompt()
        {
            if (_playerInteraction != null && _playerInteraction.HasInteractable)
            {
                float w = 340f;
                float h = 36f;
                float x = (Screen.width - w) * 0.5f;
                float y = (Screen.height * 0.5f) + 40f;

                GUI.Box(new Rect(x, y, w, h), GUIContent.none, _boxStyle);
                GUI.Label(new Rect(x, y, w, h), _playerInteraction.CurrentPrompt, _promptStyle);
            }

            // Reticle center dot
            float dotSize = 4f;
            float rx = (Screen.width - dotSize) * 0.5f;
            float ry = (Screen.height - dotSize) * 0.5f;
            GUI.Box(new Rect(rx, ry, dotSize, dotSize), GUIContent.none);
        }
    }
}
