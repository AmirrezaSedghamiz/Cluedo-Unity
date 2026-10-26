using System;
using UnityEngine;

namespace TomorrowsCrime.Settings
{
    /// <summary>
    /// Persistent player settings service.
    /// Manages master audio volume, mouse look sensitivity, graphics quality presets,
    /// fullscreen display mode, and HUD scale.
    /// Persists settings to PlayerPrefs across game sessions.
    /// </summary>
    public class GameSettingsManager
    {
        private static GameSettingsManager _instance;
        public static GameSettingsManager Instance => _instance ??= new GameSettingsManager();

        private const string PrefMasterVolume = "TC_Settings_MasterVolume";
        private const string PrefMouseSensitivity = "TC_Settings_MouseSensitivity";
        private const string PrefGraphicsQuality = "TC_Settings_GraphicsQuality";
        private const string PrefFullscreen = "TC_Settings_Fullscreen";
        private const string PrefHudScale = "TC_Settings_HudScale";

        public float MasterVolume { get; private set; } = 1.0f;
        public float MouseSensitivity { get; private set; } = 1.5f;
        public int GraphicsQuality { get; private set; } = 2; // Medium/High
        public bool IsFullscreen { get; private set; } = true;
        public float HudScale { get; private set; } = 1.0f;

        public event Action OnSettingsChanged;

        public GameSettingsManager()
        {
            LoadSettings();
        }

        public void LoadSettings()
        {
            MasterVolume = PlayerPrefs.GetFloat(PrefMasterVolume, 0.85f);
            MouseSensitivity = PlayerPrefs.GetFloat(PrefMouseSensitivity, 1.5f);
            GraphicsQuality = PlayerPrefs.GetInt(PrefGraphicsQuality, QualitySettings.GetQualityLevel());
            IsFullscreen = PlayerPrefs.GetInt(PrefFullscreen, Screen.fullScreen ? 1 : 0) == 1;
            HudScale = PlayerPrefs.GetFloat(PrefHudScale, 1.0f);
            ApplySettings();
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(PrefMasterVolume, MasterVolume);
            PlayerPrefs.SetFloat(PrefMouseSensitivity, MouseSensitivity);
            PlayerPrefs.SetInt(PrefGraphicsQuality, GraphicsQuality);
            PlayerPrefs.SetInt(PrefFullscreen, IsFullscreen ? 1 : 0);
            PlayerPrefs.SetFloat(PrefHudScale, HudScale);
            PlayerPrefs.Save();
            ApplySettings();
            OnSettingsChanged?.Invoke();
        }

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            AudioListener.volume = MasterVolume;
        }

        public void SetMouseSensitivity(float sensitivity)
        {
            MouseSensitivity = Mathf.Clamp(sensitivity, 0.2f, 5.0f);
        }

        public void SetGraphicsQuality(int qualityIndex)
        {
            GraphicsQuality = Mathf.Clamp(qualityIndex, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(GraphicsQuality, true);
        }

        public void SetFullscreen(bool fullscreen)
        {
            IsFullscreen = fullscreen;
            Screen.fullScreen = fullscreen;
        }

        public void SetHudScale(float scale)
        {
            HudScale = Mathf.Clamp(scale, 0.75f, 1.35f);
        }

        private void ApplySettings()
        {
            AudioListener.volume = MasterVolume;
            if (GraphicsQuality < QualitySettings.names.Length)
            {
                QualitySettings.SetQualityLevel(GraphicsQuality, true);
            }
            if (Screen.fullScreen != IsFullscreen)
            {
                Screen.fullScreen = IsFullscreen;
            }
        }
    }
}
