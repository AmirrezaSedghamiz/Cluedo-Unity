using UnityEngine;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.UI.Core;
using TomorrowsCrime.UI.Styling;

namespace TomorrowsCrime.UI.Menu
{
    /// <summary>
    /// Pause Menu for Tomorrow's Crime.
    /// Safely pauses live simulation and provides restart loop, settings, and main menu options.
    /// Distinguishes restarting the current loop from resetting persistent player knowledge.
    /// </summary>
    public class PauseMenuView : MonoBehaviour
    {
        private void OnGUI()
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr == null || uiMgr.CurrentState != UIState.PauseMenu) return;

            // Semi-transparent atmospheric dark overlay
            UITheme.DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.06f, 0.07f, 0.08f, 0.85f));

            float menuWidth = 420f;
            float menuHeight = 390f;
            float x = (Screen.width - menuWidth) * 0.5f;
            float y = (Screen.height - menuHeight) * 0.5f;
            Rect menuRect = new Rect(x, y, menuWidth, menuHeight);

            UITheme.DrawSolidRect(menuRect, UITheme.SlateSurface);
            UITheme.DrawBorder(menuRect, UITheme.MutedBrass, 2f);

            GUILayout.BeginArea(new Rect(x + 24f, y + 24f, menuWidth - 48f, menuHeight - 48f));

            GUILayout.Label("INVESTIGATION PAUSED", UITheme.TitleStyle);
            GUILayout.Space(4f);
            GUILayout.Label("Meridian House simulation halted.", UITheme.BodyMutedStyle);
            GUILayout.Space(16f);

            if (GUILayout.Button("Resume Investigation [Esc]", UITheme.PrimaryButtonStyle, GUILayout.Height(38f)))
            {
                uiMgr.SetState(UIState.InGameHUD);
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Restart Current Loop [R]", UITheme.ButtonStyle, GUILayout.Height(36f)))
            {
                uiMgr.TriggerLoopResetFromMenu();
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Settings & Controls", UITheme.ButtonStyle, GUILayout.Height(36f)))
            {
                uiMgr.SetState(UIState.SettingsMenu);
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Main Menu", UITheme.ButtonStyle, GUILayout.Height(36f)))
            {
                uiMgr.SetState(UIState.MainMenu);
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Quit to Desktop", UITheme.ButtonStyle, GUILayout.Height(36f)))
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("<i>Note: Restarting a loop resets physical objects & NPC positions, but preserves all evidence and notes in your dossier.</i>", UITheme.BodyMutedStyle);

            GUILayout.EndArea();
        }
    }
}
