using UnityEngine;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.UI.Core;
using TomorrowsCrime.UI.Styling;

namespace TomorrowsCrime.UI.Menu
{
    /// <summary>
    /// Pause Menu for Tomorrow's Crime.
    /// Rebuilt for Step 7: Modern Cluedo Art Direction (Meridian House Aesthetic).
    /// Safely halts live simulation and provides restart loop, settings, and main menu controls.
    /// </summary>
    public class PauseMenuView : MonoBehaviour
    {
        private void OnGUI()
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr == null || uiMgr.CurrentState != UIState.PauseMenu) return;

            // Semi-transparent atmospheric dark overlay
            UITheme.DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.06f, 0.09f, 0.88f));

            float menuWidth = Mathf.Min(480f, Screen.width - 60f);
            float menuHeight = 440f;
            float x = (Screen.width - menuWidth) * 0.5f;
            float y = (Screen.height - menuHeight) * 0.5f;
            Rect menuRect = new Rect(x, y, menuWidth, menuHeight);

            UITheme.DrawSolidRect(menuRect, UITheme.ObsidianSurface);
            UITheme.DrawOrnateFrame(menuRect, UITheme.PolishedBrass, UITheme.BrassHighlight, 2.5f);

            // Top Header Plaque
            Rect headerRect = new Rect(x + 12f, y + 12f, menuWidth - 24f, 54f);
            UITheme.DrawGoldHeader(headerRect, "INVESTIGATION HALTED", "MERIDIAN HOUSE SIMULATION SUSPENDED");

            float contentY = y + 80f;
            float btnWidth = menuWidth - 60f;
            float btnX = x + 30f;

            Rect btnResume = new Rect(btnX, contentY, btnWidth, 44f);
            if (UITheme.DrawLuxeButton(btnResume, "RESUME INVESTIGATION [ESC]", isPrimary: true))
            {
                uiMgr.SetState(UIState.InGameHUD);
            }

            contentY += 54f;
            Rect btnRestart = new Rect(btnX, contentY, btnWidth, 40f);
            if (UITheme.DrawLuxeButton(btnRestart, "RESTART CURRENT 5-MIN LOOP [R]", isPrimary: false))
            {
                uiMgr.TriggerLoopResetFromMenu();
            }

            contentY += 48f;
            Rect btnSettings = new Rect(btnX, contentY, btnWidth, 40f);
            if (UITheme.DrawLuxeButton(btnSettings, "SETTINGS & PREFERENCES", isPrimary: false))
            {
                uiMgr.SetState(UIState.SettingsMenu);
            }

            contentY += 48f;
            Rect btnMenu = new Rect(btnX, contentY, btnWidth, 40f);
            if (UITheme.DrawLuxeButton(btnMenu, "RETURN TO TITLE MENU", isPrimary: false))
            {
                uiMgr.SetState(UIState.MainMenu);
            }

            contentY += 48f;
            Rect btnExit = new Rect(btnX, contentY, btnWidth, 40f);
            if (UITheme.DrawLuxeButton(btnExit, "QUIT TO DESKTOP", isPrimary: false))
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }

            // Persistence Guarantee Notice
            Rect noticeRect = new Rect(x + 20f, y + menuHeight - 34f, menuWidth - 40f, 24f);
            var noticeStyle = new GUIStyle(UITheme.BodyMutedStyle)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = UITheme.TextMuted }
            };
            GUI.Label(noticeRect, "<i>Loop resets restore physical positions while permanently preserving all case discoveries.</i>", noticeStyle);
        }
    }
}
