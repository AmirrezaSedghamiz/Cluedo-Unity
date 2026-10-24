using UnityEngine;
using TomorrowsCrime.Simulation;

namespace TomorrowsCrime.Debugging
{
    /// <summary>
    /// Real-time developer diagnostic overlay.
    /// Uses IMGUI to provide guaranteed, fail-safe rendering without Canvas
    /// or EventSystem dependencies.
    /// </summary>
    public class SimulationDebugUI : MonoBehaviour
    {
        [SerializeField] private bool showUI = true;

        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _timeStyle;
        private GUIStyle _labelStyle;
        private bool _stylesInitialized;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.BackQuote))
            {
                showUI = !showUI;
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0.04f, 0.06f, 0.09f, 0.90f));
            bgTex.Apply();

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = bgTex },
                padding = new RectOffset(14, 14, 14, 14)
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.75f, 0.2f) }
            };

            _timeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.8f, 0.85f, 0.9f) }
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!showUI) return;
            InitStyles();

            var mgr = SimulationManager.Instance;
            var clock = mgr != null ? mgr.Clock : null;

            GUILayout.BeginArea(new Rect(16, 16, 320, 260), _boxStyle);

            GUILayout.Label("TOMORROW'S CRIME — SIMULATION HUD", _headerStyle);
            GUILayout.Space(4);

            if (clock != null)
            {
                int loop = clock.LoopCount;
                string elapsed = clock.GetFormattedTime();
                string remaining = clock.GetFormattedRemainingTime();
                float progress = clock.NormalizedProgress * 100f;

                GUILayout.Label($"LOOP #{loop}  —  {elapsed} / 05:00", _timeStyle);
                GUILayout.Label($"Remaining: {remaining}  |  {progress:F0}% Elapsed", _labelStyle);
                GUILayout.Space(6);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(clock.IsPaused ? "▶ RESUME" : "❚❚ PAUSE", GUILayout.Height(26)))
                {
                    clock.TogglePause();
                }

                if (GUILayout.Button("↺ RESET LOOP [R]", GUILayout.Height(26)))
                {
                    mgr.ForceResetLoop();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(6);
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
            GUILayout.Label("WASD: Move | Mouse: Look | Shift: Sprint\nP: Pause | 1/2/5: Speed | R: Reset | F1: Toggle HUD", _labelStyle);

            GUILayout.EndArea();
        }
    }
}
