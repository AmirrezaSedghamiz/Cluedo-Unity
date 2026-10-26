using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.Player;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.UI.Core;
using TomorrowsCrime.UI.Styling;

namespace TomorrowsCrime.UI.HUD
{
    /// <summary>
    /// Atmospheric first-person game HUD for Tomorrow's Crime.
    /// Renders:
    /// - Loop time remaining and progress indicator with urgency tinting.
    /// - Minimalist center reticle and contextual interaction prompt.
    /// - Discovery feedback and event perception toasts.
    /// - Notebook shortcut badge with clue count.
    /// Restrained peripheral layout preserving visibility of 3D scene.
    /// </summary>
    public class GameHUDView : MonoBehaviour
    {
        private PlayerInteraction _playerInteraction;
        private SimulationManager _simulationManager;

        private void Start()
        {
            _playerInteraction = FindAnyObjectByType<PlayerInteraction>();
            _simulationManager = SimulationManager.Instance;
        }

        private void OnGUI()
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr != null && uiMgr.CurrentState != UIState.InGameHUD && uiMgr.CurrentState != UIState.EvidenceDetailModal)
            {
                return; // Hide HUD when modal full-screen menus or notebook are open
            }

            DrawTimeLoopClock();
            DrawInteractionPrompt();
            DrawNotificationToasts();
            DrawNotebookAccessPill();
        }

        private void DrawTimeLoopClock()
        {
            if (_simulationManager == null || _simulationManager.Clock == null) return;

            var clock = _simulationManager.Clock;
            float elapsed = clock.ElapsedSeconds;
            float remaining = clock.RemainingSeconds;
            int loopIndex = clock.LoopCount;
            float progress = clock.NormalizedProgress;

            // Clock container at top center
            float width = 280f;
            float height = 54f;
            float x = (Screen.width - width) * 0.5f;
            float y = 16f;
            Rect clockRect = new Rect(x, y, width, height);

            // Surface background
            UITheme.DrawSolidRect(clockRect, UITheme.SlateSurface);
            UITheme.DrawBorder(clockRect, UITheme.BorderSubtle, 1f);

            // Warning bar calculation
            Color progressColor = UITheme.MutedBrass;
            if (remaining <= 30f)
            {
                progressColor = UITheme.MutedRed;
            }
            else if (remaining <= 60f)
            {
                progressColor = UITheme.RestrainedAmber;
            }

            // Progress bar at bottom of clock card
            float barHeight = 4f;
            Rect progressBg = new Rect(x + 2, y + height - barHeight - 2, width - 4, barHeight);
            UITheme.DrawSolidRect(progressBg, UITheme.CharcoalBackground);
            Rect progressFill = new Rect(x + 2, y + height - barHeight - 2, (width - 4) * progress, barHeight);
            UITheme.DrawSolidRect(progressFill, progressColor);

            // Time text formatting
            int elapsedMin = (int)(elapsed / 60f);
            int elapsedSec = (int)(elapsed % 60f);
            int remMin = (int)(remaining / 60f);
            int remSec = (int)(remaining % 60f);

            // Loop badge
            Rect badgeRect = new Rect(x + 12, y + 10, 68, 20);
            UITheme.DrawSolidRect(badgeRect, UITheme.SlateSurfaceLight);
            UITheme.DrawBorder(badgeRect, progressColor, 1f);
            var badgeStyle = new GUIStyle(UITheme.BadgeStyle)
            {
                normal = { textColor = UITheme.TextPrimary }
            };
            GUI.Label(badgeRect, $"LOOP {loopIndex:D2}", badgeStyle);

            // Elapsed and remaining clock
            Rect timeRect = new Rect(x + 88, y + 8, width - 100, 24);
            var timeStyle = new GUIStyle(UITheme.HeaderStyle)
            {
                fontSize = 17,
                normal = { textColor = progressColor },
                alignment = TextAnchor.MiddleRight
            };
            GUI.Label(timeRect, $"{elapsedMin:00}:{elapsedSec:00} <size=11><color=#9BA0A6>(-{remMin:00}:{remSec:00})</color></size>", timeStyle);
        }

        private void DrawInteractionPrompt()
        {
            if (_playerInteraction == null)
            {
                _playerInteraction = FindAnyObjectByType<PlayerInteraction>();
                if (_playerInteraction == null) return;
            }

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;

            // Reticle dot
            float dotSize = 4f;
            Rect reticleRect = new Rect(centerX - dotSize * 0.5f, centerY - dotSize * 0.5f, dotSize, dotSize);
            UITheme.DrawSolidRect(reticleRect, _playerInteraction.HasInteractable ? UITheme.MutedBrass : new Color(1f, 1f, 1f, 0.45f));

            // Prompt Card below reticle
            if (_playerInteraction.HasInteractable && !string.IsNullOrEmpty(_playerInteraction.CurrentPrompt))
            {
                string prompt = _playerInteraction.CurrentPrompt;
                float promptWidth = Mathf.Max(240f, prompt.Length * 9f + 40f);
                float promptHeight = 36f;
                Rect promptRect = new Rect(centerX - promptWidth * 0.5f, centerY + 30f, promptWidth, promptHeight);

                UITheme.DrawSolidRect(promptRect, UITheme.SlateSurface);
                UITheme.DrawBorder(promptRect, UITheme.MutedBrass, 1.5f);

                var promptStyle = new GUIStyle(UITheme.BodyStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = UITheme.TextPrimary }
                };
                GUI.Label(promptRect, prompt, promptStyle);
            }
        }

        private void DrawNotificationToasts()
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr == null) return;

            var toasts = uiMgr.ActiveToasts;
            if (toasts.Count == 0) return;

            float startX = 20f;
            float startY = 20f;
            float toastWidth = 360f;
            float toastHeight = 60f;
            float gap = 8f;

            for (int i = 0; i < toasts.Count; i++)
            {
                var toast = toasts[i];
                float y = startY + i * (toastHeight + gap);
                Rect toastRect = new Rect(startX, y, toastWidth, toastHeight);

                // Alpha based on time remaining
                float alpha = Mathf.Clamp01(toast.TimeRemaining / 0.5f);
                Color surfaceColor = new Color(UITheme.SlateSurface.r, UITheme.SlateSurface.g, UITheme.SlateSurface.b, 0.94f * alpha);

                UITheme.DrawSolidRect(toastRect, surfaceColor);
                UITheme.DrawBorder(toastRect, new Color(UITheme.BorderSubtle.r, UITheme.BorderSubtle.g, UITheme.BorderSubtle.b, alpha), 1f);

                // Accent stripe on the left
                Rect accentRect = new Rect(startX, y, 4f, toastHeight);
                Color accentColor = new Color(toast.AccentColor.r, toast.AccentColor.g, toast.AccentColor.b, alpha);
                UITheme.DrawSolidRect(accentRect, accentColor);

                // Content
                Rect titleRect = new Rect(startX + 14f, y + 6f, toastWidth - 24f, 20f);
                var titleStyle = new GUIStyle(UITheme.SubHeaderStyle)
                {
                    fontSize = 12,
                    normal = { textColor = accentColor }
                };
                GUI.Label(titleRect, toast.Title, titleStyle);

                Rect msgRect = new Rect(startX + 14f, y + 26f, toastWidth - 24f, 30f);
                var msgStyle = new GUIStyle(UITheme.BodyStyle)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(UITheme.TextPrimary.r, UITheme.TextPrimary.g, UITheme.TextPrimary.b, alpha) }
                };
                GUI.Label(msgRect, toast.Message, msgStyle);
            }
        }

        private void DrawNotebookAccessPill()
        {
            int evidenceCount = InvestigationKnowledgeService.Instance != null
                ? InvestigationKnowledgeService.Instance.EvidenceCount
                : 0;

            float width = 230f;
            float height = 36f;
            float x = Screen.width - width - 20f;
            float y = Screen.height - height - 20f;
            Rect pillRect = new Rect(x, y, width, height);

            UITheme.DrawSolidRect(pillRect, UITheme.SlateSurface);
            UITheme.DrawBorder(pillRect, UITheme.BorderSubtle, 1f);

            var pillStyle = new GUIStyle(UITheme.BodyStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                normal = { textColor = UITheme.TextPrimary }
            };

            string text = $"<color=#B69A61>[TAB / N]</color> Notebook  <color=#6D9C98>({evidenceCount} Clues)</color>";
            GUI.Label(pillRect, text, pillStyle);
        }
    }
}
