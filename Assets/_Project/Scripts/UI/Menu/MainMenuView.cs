using UnityEngine;
using TomorrowsCrime.UI.Core;
using TomorrowsCrime.UI.Styling;

namespace TomorrowsCrime.UI.Menu
{
    /// <summary>
    /// Theatrical, atmospheric Main Menu view for Tomorrow's Crime.
    /// Rebuilt for Step 7: Modern Cluedo Art Direction (Meridian House Aesthetic).
    /// Features Art Deco gold framing, embossed monogram seal, letter-spaced typography,
    /// and polished luxury interactive buttons.
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        private void OnGUI()
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr == null || uiMgr.CurrentState != UIState.MainMenu) return;

            // Fullscreen Meridian Midnight Backdrop
            Rect fullScreen = new Rect(0, 0, Screen.width, Screen.height);
            UITheme.DrawSolidRect(fullScreen, UITheme.MeridianMidnight);

            // Subtle Art Deco perimeter border around screen
            UITheme.DrawOrnateFrame(new Rect(16f, 16f, Screen.width - 32f, Screen.height - 32f),
                new Color(UITheme.PolishedBrass.r, UITheme.PolishedBrass.g, UITheme.PolishedBrass.b, 0.4f),
                UITheme.PolishedBrass, 1.5f);

            // Centered Main Title Card
            float menuWidth = Mathf.Min(560f, Screen.width - 60f);
            float menuHeight = 520f;
            float x = (Screen.width - menuWidth) * 0.5f;
            float y = (Screen.height - menuHeight) * 0.5f;
            Rect menuRect = new Rect(x, y, menuWidth, menuHeight);

            // Obsidian Card Surface
            UITheme.DrawSolidRect(menuRect, UITheme.ObsidianSurface);
            UITheme.DrawOrnateFrame(menuRect, UITheme.PolishedBrass, UITheme.BrassHighlight, 2.5f);

            // Top Gold Wax Seal Monogram
            float sealSize = 72f;
            Rect sealRect = new Rect(x + (menuWidth - sealSize) * 0.5f, y + 24f, sealSize, sealSize);
            UITheme.DrawWaxSeal(sealRect);

            // Title & Subtitle Block
            float contentY = y + 104f;
            Rect titleRect = new Rect(x + 20f, contentY, menuWidth - 40f, 36f);
            var titleStyle = new GUIStyle(UITheme.TitleStyle)
            {
                fontSize = 28,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = UITheme.BrassHighlight }
            };
            GUI.Label(titleRect, "TOMORROW'S CRIME", titleStyle);

            contentY += 38f;
            Rect subTitleRect = new Rect(x + 20f, contentY, menuWidth - 40f, 22f);
            var subStyle = new GUIStyle(UITheme.SubHeaderStyle)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = UITheme.PeacockTeal }
            };
            GUI.Label(subTitleRect, "MERIDIAN HOUSE • A TIME-LOOP MYSTERY", subStyle);

            contentY += 28f;
            // Gold separator rule
            UITheme.DrawSolidRect(new Rect(x + 40f, contentY, menuWidth - 80f, 1.5f), UITheme.PolishedBrass);

            contentY += 16f;
            Rect narrativeRect = new Rect(x + 36f, contentY, menuWidth - 72f, 54f);
            var narrStyle = new GUIStyle(UITheme.BodyStyle)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = UITheme.TextIvory }
            };
            GUI.Label(narrativeRect, "Midnight repeats every five minutes. The hotel resets, but your discoveries, deductive connections, and field notes endure.", narrStyle);

            contentY += 66f;
            // Primary & Secondary Buttons
            float btnWidth = menuWidth - 80f;
            float btnX = x + 40f;

            var session = FindAnyObjectByType<TomorrowsCrime.Procedural.Core.GameSessionManager>();
            string seedCode = session != null ? session.ActiveSeedCode : "TC-1A2B3C4D";
            int solvedCount = TomorrowsCrime.Investigation.CaseAccusationService.CasesSolvedCount;

            Rect btnStart = new Rect(btnX, contentY, btnWidth, 44f);
            if (UITheme.DrawLuxeButton(btnStart, "BEGIN INVESTIGATION", isPrimary: true))
            {
                if (session != null && session.CurrentInvestigation == null)
                {
                    session.StartNewProceduralCase();
                }
                uiMgr.SetState(UIState.InGameHUD);
            }

            contentY += 50f;
            Rect btnNewCase = new Rect(btnX, contentY, btnWidth, 38f);
            if (UITheme.DrawLuxeButton(btnNewCase, $"START NEW CASE [RANDOM SEED]", isPrimary: false))
            {
                if (session != null)
                {
                    session.StartNewProceduralCase();
                }
                uiMgr.SetState(UIState.InGameHUD);
            }

            contentY += 46f;
            Rect btnSettings = new Rect(btnX, contentY, btnWidth, 38f);
            if (UITheme.DrawLuxeButton(btnSettings, "SETTINGS & PREFERENCES", isPrimary: false))
            {
                uiMgr.SetState(UIState.SettingsMenu);
            }

            contentY += 46f;
            Rect btnExit = new Rect(btnX, contentY, btnWidth, 38f);
            if (UITheme.DrawLuxeButton(btnExit, "EXIT INCIDENT", isPrimary: false))
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }

            // Footer Case & Seed Status
            Rect footerRect = new Rect(x, y + menuHeight - 34f, menuWidth, 24f);
            var footStyle = new GUIStyle(UITheme.BodyMutedStyle)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = UITheme.TextMuted }
            };
            GUI.Label(footerRect, $"MERIDIAN HOUSE • SEED: <color=#{ColorUtility.ToHtmlStringRGB(UITheme.BrassHighlight)}>{seedCode}</color> • CASES SOLVED: <color=#{ColorUtility.ToHtmlStringRGB(UITheme.StatusAmber)}>{solvedCount}</color>", footStyle);
        }
    }
}
