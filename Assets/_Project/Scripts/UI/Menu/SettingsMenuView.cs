using System;
using UnityEngine;
using TomorrowsCrime.Settings;
using TomorrowsCrime.UI.Core;
using TomorrowsCrime.UI.Styling;

namespace TomorrowsCrime.UI.Menu
{
    /// <summary>
    /// Interactive Settings view allowing player to tune audio volume, mouse sensitivity,
    /// graphics quality presets, display mode, and HUD scaling.
    /// Rebuilt for Step 7: Modern Cluedo Art Direction (Meridian House Aesthetic).
    /// </summary>
    public class SettingsMenuView : MonoBehaviour
    {
        private float _volume;
        private float _sensitivity;
        private int _quality;
        private bool _fullscreen;
        private float _hudScale;
        private bool _isLoaded;

        private void OnEnable()
        {
            LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            var sm = GameSettingsManager.Instance;
            if (sm != null)
            {
                _volume = sm.MasterVolume;
                _sensitivity = sm.MouseSensitivity;
                _quality = sm.GraphicsQuality;
                _fullscreen = sm.IsFullscreen;
                _hudScale = sm.HudScale;
                _isLoaded = true;
            }
        }

        private void OnGUI()
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr == null || uiMgr.CurrentState != UIState.SettingsMenu) return;

            if (!_isLoaded) LoadFromSettings();

            // Background
            UITheme.DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.06f, 0.09f, 0.90f));

            float width = Mathf.Min(520f, Screen.width - 60f);
            float height = 520f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;
            Rect rect = new Rect(x, y, width, height);

            UITheme.DrawSolidRect(rect, UITheme.ObsidianSurface);
            UITheme.DrawOrnateFrame(rect, UITheme.PolishedBrass, UITheme.BrassHighlight, 2.5f);

            // Header Plaque
            Rect headerRect = new Rect(x + 12f, y + 12f, width - 24f, 54f);
            UITheme.DrawGoldHeader(headerRect, "INVESTIGATION PREFERENCES", "SYSTEM CONTROLS & GRAPHICS CONFIGURATION");

            GUILayout.BeginArea(new Rect(x + 28f, y + 78f, width - 56f, height - 146f));

            // 1. Audio Volume
            GUILayout.Label($"<b>Master Audio Volume:</b> <color=#{ColorUtility.ToHtmlStringRGB(UITheme.BrassHighlight)}>{Mathf.RoundToInt(_volume * 100f)}%</color>", UITheme.SubHeaderStyle);
            _volume = GUILayout.HorizontalSlider(_volume, 0f, 1f);
            GUILayout.Space(12f);

            // 2. Mouse Sensitivity
            GUILayout.Label($"<b>Mouse Look Sensitivity:</b> <color=#{ColorUtility.ToHtmlStringRGB(UITheme.BrassHighlight)}>{_sensitivity:F1}x</color>", UITheme.SubHeaderStyle);
            _sensitivity = GUILayout.HorizontalSlider(_sensitivity, 0.2f, 4.0f);
            GUILayout.Space(12f);

            // 3. Graphics Quality
            GUILayout.Label("<b>Visual Quality Preset:</b>", UITheme.SubHeaderStyle);
            GUILayout.BeginHorizontal();
            string[] names = QualitySettings.names;
            for (int i = 0; i < Mathf.Min(4, names.Length); i++)
            {
                var btnStyle = _quality == i ? UITheme.PrimaryButtonStyle : UITheme.ButtonStyle;
                if (GUILayout.Button(names[i].ToUpper(), btnStyle, GUILayout.Height(28f)))
                {
                    _quality = i;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(12f);

            // 4. Display Mode
            GUILayout.Label("<b>Display Mode:</b>", UITheme.SubHeaderStyle);
            GUILayout.BeginHorizontal();
            var fullStyle = _fullscreen ? UITheme.PrimaryButtonStyle : UITheme.ButtonStyle;
            if (GUILayout.Button("FULLSCREEN EXCLUSIVE", fullStyle, GUILayout.Height(28f)))
            {
                _fullscreen = true;
            }
            var winStyle = !_fullscreen ? UITheme.PrimaryButtonStyle : UITheme.ButtonStyle;
            if (GUILayout.Button("WINDOWED", winStyle, GUILayout.Height(28f)))
            {
                _fullscreen = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(12f);

            // 5. HUD Scaling
            GUILayout.Label($"<b>Interface Scale:</b> <color=#{ColorUtility.ToHtmlStringRGB(UITheme.BrassHighlight)}>{_hudScale:F2}x</color>", UITheme.SubHeaderStyle);
            _hudScale = GUILayout.HorizontalSlider(_hudScale, 0.75f, 1.25f);

            GUILayout.EndArea();

            // Footer Actions
            float btnWidth = (width - 66f) * 0.5f;
            float btnY = y + height - 58f;

            Rect btnApply = new Rect(x + 28f, btnY, btnWidth, 42f);
            if (UITheme.DrawLuxeButton(btnApply, "APPLY & SAVE", isPrimary: true))
            {
                var sm = GameSettingsManager.Instance;
                if (sm != null)
                {
                    sm.SetMasterVolume(_volume);
                    sm.SetMouseSensitivity(_sensitivity);
                    sm.SetGraphicsQuality(_quality);
                    sm.SetFullscreen(_fullscreen);
                    sm.SetHudScale(_hudScale);
                }
                uiMgr.HandleEscape();
            }

            Rect btnCancel = new Rect(x + 38f + btnWidth, btnY, btnWidth, 42f);
            if (UITheme.DrawLuxeButton(btnCancel, "DISCARD [BACK]", isPrimary: false))
            {
                LoadFromSettings();
                uiMgr.HandleEscape();
            }
        }
    }
}
