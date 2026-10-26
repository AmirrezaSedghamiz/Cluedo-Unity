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
    /// Persists preferences via GameSettingsManager.
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
            UITheme.DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.06f, 0.07f, 0.08f, 0.88f));

            float width = 480f;
            float height = 480f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;
            Rect rect = new Rect(x, y, width, height);

            UITheme.DrawSolidRect(rect, UITheme.SlateSurface);
            UITheme.DrawBorder(rect, UITheme.MutedBrass, 2f);

            GUILayout.BeginArea(new Rect(x + 24f, y + 24f, width - 48f, height - 48f));

            GUILayout.Label("INVESTIGATION SETTINGS", UITheme.TitleStyle);
            GUILayout.Space(14f);

            // 1. Audio Volume
            GUILayout.Label($"Master Audio Volume: {Mathf.RoundToInt(_volume * 100f)}%", UITheme.SubHeaderStyle);
            _volume = GUILayout.HorizontalSlider(_volume, 0f, 1f);
            GUILayout.Space(10f);

            // 2. Mouse Sensitivity
            GUILayout.Label($"Mouse Look Sensitivity: {_sensitivity:F1}x", UITheme.SubHeaderStyle);
            _sensitivity = GUILayout.HorizontalSlider(_sensitivity, 0.2f, 4.0f);
            GUILayout.Space(10f);

            // 3. Graphics Quality
            GUILayout.Label("Graphics Quality Preset:", UITheme.SubHeaderStyle);
            GUILayout.BeginHorizontal();
            string[] names = QualitySettings.names;
            for (int i = 0; i < Mathf.Min(4, names.Length); i++)
            {
                var btnStyle = _quality == i ? UITheme.PrimaryButtonStyle : UITheme.ButtonStyle;
                if (GUILayout.Button(names[i], btnStyle, GUILayout.Height(26f)))
                {
                    _quality = i;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);

            // 4. Display Mode
            GUILayout.Label("Display Mode:", UITheme.SubHeaderStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Fullscreen", _fullscreen ? UITheme.PrimaryButtonStyle : UITheme.ButtonStyle, GUILayout.Height(26f)))
            {
                _fullscreen = true;
            }
            if (GUILayout.Button("Windowed", !_fullscreen ? UITheme.PrimaryButtonStyle : UITheme.ButtonStyle, GUILayout.Height(26f)))
            {
                _fullscreen = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);

            // 5. HUD Scaling
            GUILayout.Label($"HUD Scale: {Mathf.RoundToInt(_hudScale * 100f)}%", UITheme.SubHeaderStyle);
            GUILayout.BeginHorizontal();
            float[] scales = { 0.8f, 1.0f, 1.2f };
            string[] scaleLabels = { "80%", "100% (Default)", "120%" };
            for (int i = 0; i < scales.Length; i++)
            {
                bool active = Mathf.Approximately(_hudScale, scales[i]);
                if (GUILayout.Button(scaleLabels[i], active ? UITheme.PrimaryButtonStyle : UITheme.ButtonStyle, GUILayout.Height(26f)))
                {
                    _hudScale = scales[i];
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            // Bottom Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply & Save Preferences", UITheme.PrimaryButtonStyle, GUILayout.Height(36f)))
            {
                var sm = GameSettingsManager.Instance;
                if (sm != null)
                {
                    sm.SetMasterVolume(_volume);
                    sm.SetMouseSensitivity(_sensitivity);
                    sm.SetGraphicsQuality(_quality);
                    sm.SetFullscreen(_fullscreen);
                    sm.SetHudScale(_hudScale);
                    sm.SaveSettings();
                }
                uiMgr.HandleEscape();
            }

            if (GUILayout.Button("Back [Esc]", UITheme.ButtonStyle, GUILayout.Width(100f), GUILayout.Height(36f)))
            {
                uiMgr.HandleEscape();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }
    }
}
