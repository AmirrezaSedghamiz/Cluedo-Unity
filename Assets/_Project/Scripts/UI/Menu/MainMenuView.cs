using UnityEngine;
using TomorrowsCrime.UI.Core;
using TomorrowsCrime.UI.Styling;

namespace TomorrowsCrime.UI.Menu
{
    /// <summary>
    /// Atmospheric Main Menu view for Tomorrow's Crime.
    /// Provides Start Investigation, Settings, and Quit controls.
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        private void OnGUI()
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr == null || uiMgr.CurrentState != UIState.MainMenu) return;

            // Full screen backdrop
            UITheme.DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), UITheme.CharcoalBackground);

            // Centered Title Block
            float menuWidth = 440f;
            float menuHeight = 360f;
            float x = (Screen.width - menuWidth) * 0.5f;
            float y = (Screen.height - menuHeight) * 0.5f;
            Rect menuRect = new Rect(x, y, menuWidth, menuHeight);

            UITheme.DrawSolidRect(menuRect, UITheme.SlateSurface);
            UITheme.DrawBorder(menuRect, UITheme.MutedBrass, 2f);

            GUILayout.BeginArea(new Rect(x + 30f, y + 30f, menuWidth - 60f, menuHeight - 60f));

            GUILayout.Label("TOMORROW'S CRIME", UITheme.TitleStyle);
            GUILayout.Space(2f);
            GUILayout.Label("<color=#6D9C98>MERIDIAN HOUSE — A TIME-LOOP MYSTERY</color>", UITheme.SubHeaderStyle);
            GUILayout.Space(4f);
            GUILayout.Label("Midnight repeats every five minutes. The hotel resets, but your discoveries, deductive connections, and field notes endure.", UITheme.BodyMutedStyle);

            GUILayout.Space(24f);

            if (GUILayout.Button("Begin Investigation", UITheme.PrimaryButtonStyle, GUILayout.Height(42f)))
            {
                uiMgr.SetState(UIState.InGameHUD);
            }

            GUILayout.Space(10f);

            if (GUILayout.Button("Settings & Controls", UITheme.ButtonStyle, GUILayout.Height(36f)))
            {
                uiMgr.SetState(UIState.SettingsMenu);
            }

            GUILayout.Space(10f);

            if (GUILayout.Button("Exit Incident", UITheme.ButtonStyle, GUILayout.Height(36f)))
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("<size=10><color=#9BA0A6>Version 0.6.0 LTS • Unity 6 Engine</color></size>", UITheme.BodyMutedStyle);

            GUILayout.EndArea();
        }
    }
}
