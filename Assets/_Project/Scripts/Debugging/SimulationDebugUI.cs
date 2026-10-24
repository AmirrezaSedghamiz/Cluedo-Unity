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
    /// - Causal scenario evaluation (precondition pass/fail reasons)
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
        private GUIStyle _promptStyle;
        private bool _stylesInitialized;

        private PlayerInteraction _playerInteraction;

        private void Start()
        {
            _playerInteraction = FindFirstObjectByType<PlayerInteraction>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.BackQuote))
            {
                showUI = !showUI;
            }

            if (_playerInteraction == null)
            {
                _playerInteraction = FindFirstObjectByType<PlayerInteraction>();
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0.04f, 0.06f, 0.09f, 0.92f));
            bgTex.Apply();

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = bgTex },
                padding = new RectOffset(12, 12, 12, 12)
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.75f, 0.2f) }
            };

            _timeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
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

            // 2. Draw Simulation & Causal World State Panel
            GUILayout.BeginArea(new Rect(14, 14, 380, 520), _boxStyle);

            GUILayout.Label("TOMORROW'S CRIME — DIAGNOSTICS", _headerStyle);
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

            GUILayout.Space(8);
            GUILayout.Label("WORLD STATE & CAUSAL SCENARIO", _headerStyle);

            // Key status
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

            // Door status
            string doorStateStr = "Unknown";
            if (WorldStateManager.Instance != null &&
                WorldStateManager.Instance.Registry.TryGetState<DoorLockState>("door_room_102", out var ds))
            {
                doorStateStr = ds == DoorLockState.Locked ? "Locked" : "Unlocked";
            }
            GUILayout.Label($"Door 102: {doorStateStr}", _labelStyle);

            // Scenario status
            var scenario = FindFirstObjectByType<KeyScenarioController>();
            if (scenario != null)
            {
                GUILayout.Space(4);
                string statusText = scenario.Status switch
                {
                    ScenarioExecutionStatus.Pending => $"Pending (Scheduled at 00:30)",
                    ScenarioExecutionStatus.Succeeded => "SUCCEEDED",
                    ScenarioExecutionStatus.Failed => "FAILED",
                    _ => scenario.Status.ToString()
                };

                GUIStyle statusStyle = scenario.Status == ScenarioExecutionStatus.Succeeded
                    ? _successStyle
                    : (scenario.Status == ScenarioExecutionStatus.Failed ? _failStyle : _labelStyle);

                GUILayout.Label($"Concierge Scenario: {statusText}", statusStyle);
                GUILayout.Label($"Reason: {scenario.LastResultReason}", _labelStyle);
            }

            // Persistent Discoveries Section
            GUILayout.Space(8);
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

            GUILayout.Space(6);
            GUILayout.Label("Controls: WASD=Move | Mouse=Look | E=Interact\nP=Pause | 1/2/5=Speed | R=Reset | F1=Toggle HUD", _labelStyle);

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
