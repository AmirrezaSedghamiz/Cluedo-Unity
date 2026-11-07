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
    /// Streamlined, non-crowded first-person HUD for Tomorrow's Crime.
    /// Senior Game Designer & Programmer overhaul:
    /// - Minimalist Art-Deco top status strip: Combines Loop Counter, Chronometer,
    ///   and Current Location into a single compact 36px header pill, completely
    ///   clearing the upper viewport.
    /// - Integrated 2px loop progress hairline with smooth urgency transitions.
    /// - Compact single-line dispatch perception toasts with subtle left accents.
    /// - Sleek unobtrusive case dossier access pill [TAB].
    /// - Clean focus reticle and contextual interaction prompt.
    /// </summary>
    public class GameHUDView : MonoBehaviour, ILoopResetReceiver
    {
        private PlayerInteraction _playerInteraction;
        private PlayerController _playerController;
        private SimulationManager _simulationManager;
        private float _rewindVignetteTimer = 0f;
        private int _currentLoopNumber = 1;

        private void Start()
        {
            _playerInteraction = FindAnyObjectByType<PlayerInteraction>();
            _playerController = FindAnyObjectByType<PlayerController>();
            _simulationManager = SimulationManager.Instance;
            _simulationManager?.RegisterResetReceiver(this);
        }

        private void OnDestroy()
        {
            _simulationManager?.UnregisterResetReceiver(this);
        }

        public void OnLoopReset()
        {
            _rewindVignetteTimer = 2.0f;
            _currentLoopNumber = _simulationManager != null && _simulationManager.Clock != null
                ? _simulationManager.Clock.LoopCount
                : 1;
        }

        private void Update()
        {
            if (_rewindVignetteTimer > 0f)
            {
                _rewindVignetteTimer -= Time.deltaTime;
            }
        }

        private void OnGUI()
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr != null && uiMgr.CurrentState != UIState.InGameHUD && uiMgr.CurrentState != UIState.EvidenceDetailModal)
            {
                return; // Suppress HUD during full-screen modals, pause menu, and dossier workspace
            }

            DrawMinimalistTopBar();
            DrawInteractionReticle();
            DrawNotificationToasts();
            DrawDossierAccessPill();
            DrawRewindTransitionVignette();
        }

        private void DrawRewindTransitionVignette()
        {
            if (_rewindVignetteTimer <= 0f) return;

            float norm = _rewindVignetteTimer / 2.0f;

            // Flash overlay: Early temporal flash fading to deep obsidian vignette
            Color overlayColor;
            if (norm > 0.65f)
            {
                float flash = (norm - 0.65f) / 0.35f;
                overlayColor = new Color(0.85f, 0.95f, 1.0f, flash * 0.85f);
            }
            else
            {
                float fade = norm / 0.65f;
                overlayColor = new Color(UITheme.MeridianMidnight.r, UITheme.MeridianMidnight.g, UITheme.MeridianMidnight.b, fade * 0.75f);
            }

            UITheme.DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), overlayColor);

            // Sleek theatrical title card
            if (norm > 0.15f)
            {
                float bannerAlpha = Mathf.Clamp01(norm * 2f);
                float bannerWidth = Mathf.Min(480f, Screen.width - 40f);
                float bannerHeight = 72f;
                float bx = (Screen.width - bannerWidth) * 0.5f;
                float by = (Screen.height - bannerHeight) * 0.45f;

                Rect bannerRect = new Rect(bx, by, bannerWidth, bannerHeight);
                Color plaqueColor = new Color(UITheme.ObsidianSurface.r, UITheme.ObsidianSurface.g, UITheme.ObsidianSurface.b, 0.94f * bannerAlpha);
                UITheme.DrawSolidRect(bannerRect, plaqueColor);
                UITheme.DrawOrnateFrame(bannerRect,
                    new Color(UITheme.PolishedBrass.r, UITheme.PolishedBrass.g, UITheme.PolishedBrass.b, bannerAlpha),
                    new Color(UITheme.BrassHighlight.r, UITheme.BrassHighlight.g, UITheme.BrassHighlight.b, bannerAlpha), 1.5f);

                var titleStyle = new GUIStyle(UITheme.TitleStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 15,
                    normal = { textColor = new Color(UITheme.BrassHighlight.r, UITheme.BrassHighlight.g, UITheme.BrassHighlight.b, bannerAlpha) }
                };
                GUI.Label(new Rect(bx, by + 10f, bannerWidth, 26f), $"TIME FRACTURE • LOOP {_currentLoopNumber} COMMENCES", titleStyle);

                var subStyle = new GUIStyle(UITheme.SubHeaderStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    normal = { textColor = new Color(UITheme.TextIvory.r, UITheme.TextIvory.g, UITheme.TextIvory.b, bannerAlpha * 0.9f) }
                };
                GUI.Label(new Rect(bx, by + 38f, bannerWidth, 22f), "Meridian House timeline restored to 23:55. Clues persist.", subStyle);
            }
        }

        /// <summary>
        /// Single unified minimalist top status strip combining Loop, Chronometer, and Room location.
        /// Replaces the old bulky 62px pocket watch block and 24px floating location banner.
        /// </summary>
        private void DrawMinimalistTopBar()
        {
            if (_simulationManager == null || _simulationManager.Clock == null) return;
            if (_playerController == null) _playerController = FindAnyObjectByType<PlayerController>();

            var clock = _simulationManager.Clock;
            float remaining = clock.RemainingSeconds;
            int loopIndex = clock.LoopCount;
            float progress = clock.NormalizedProgress;

            // Room Location
            string roomName = _playerController != null && !string.IsNullOrEmpty(_playerController.CurrentRoomName)
                ? _playerController.CurrentRoomName
                : "Grand Lobby";

            // Urgency color styling
            Color timeColor = UITheme.BrassHighlight;
            if (remaining <= 15f)
            {
                timeColor = (Mathf.Sin(Time.time * 8f) > 0f) ? UITheme.StatusDanger : UITheme.TextIvory;
            }
            else if (remaining <= 60f)
            {
                timeColor = UITheme.StatusOrange;
            }

            int remMin = (int)(remaining / 60f);
            int remSec = (int)(remaining % 60f);
            string timeStr = $"{remMin:D2}:{remSec:D2}";

            // Dimensions: sleek, low-profile pill
            float pillWidth = Mathf.Min(480f, Screen.width - 40f);
            float pillHeight = 34f;
            float x = (Screen.width - pillWidth) * 0.5f;
            float y = 10f;
            Rect pillRect = new Rect(x, y, pillWidth, pillHeight);

            // Dark semi-transparent obsidian backdrop
            UITheme.DrawSolidRect(pillRect, new Color(0.06f, 0.08f, 0.12f, 0.90f));
            UITheme.DrawBorder(pillRect, new Color(UITheme.PolishedBrass.r, UITheme.PolishedBrass.g, UITheme.PolishedBrass.b, 0.65f), 1f);

            // Left: Loop indicator badge
            Rect loopRect = new Rect(x + 12f, y + 6f, 85f, 22f);
            var loopStyle = new GUIStyle(UITheme.BodyStyle)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = UITheme.StatusAmber }
            };
            GUI.Label(loopRect, $"LOOP {loopIndex:D2}", loopStyle);

            // Center: Chronometer countdown
            Rect timeRect = new Rect(x + (pillWidth - 90f) * 0.5f, y + 4f, 90f, 24f);
            var timeStyle = new GUIStyle(UITheme.HeaderStyle)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = timeColor }
            };
            GUI.Label(timeRect, timeStr, timeStyle);

            // Right: Location Tag
            float locWidth = pillWidth - 210f;
            Rect locRect = new Rect(x + pillWidth - locWidth - 12f, y + 6f, locWidth, 22f);
            var locStyle = new GUIStyle(UITheme.BodyStyle)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = UITheme.TextIvory }
            };
            string displayRoom = roomName.Length > 24 ? roomName.Substring(0, 22) + "..." : roomName;
            GUI.Label(locRect, $"<color=#{ColorUtility.ToHtmlStringRGB(UITheme.PeacockTeal)}>●</color> {displayRoom.ToUpper()}", locStyle);

            // Bottom edge: 2px loop progress indicator
            float barWidth = (pillWidth - 4f) * Mathf.Clamp01(1f - progress);
            Rect progBarRect = new Rect(x + 2f, y + pillHeight - 2f, barWidth, 2f);
            Color barColor = remaining <= 15f ? UITheme.StatusDanger : (remaining <= 60f ? UITheme.StatusOrange : UITheme.PolishedBrass);
            UITheme.DrawSolidRect(progBarRect, barColor);

            // Subtle crouch indicator at bottom left if active
            if (_playerController != null && _playerController.IsCrouching)
            {
                var crouchStyle = new GUIStyle(UITheme.BodyStyle)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = UITheme.StatusAmber }
                };
                GUI.Label(new Rect(20f, Screen.height - 36f, 150f, 20f), "● CROUCHING", crouchStyle);
            }
        }

        private void DrawInteractionReticle()
        {
            if (_playerInteraction == null)
            {
                _playerInteraction = FindAnyObjectByType<PlayerInteraction>();
                if (_playerInteraction == null) return;
            }

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            bool hasTarget = _playerInteraction.HasInteractable;
            string prompt = hasTarget ? _playerInteraction.CurrentPrompt : null;

            UITheme.DrawDiamondReticle(new Vector2(centerX, centerY), hasTarget, prompt);
        }

        /// <summary>
        /// Sleek single-line perception toasts that do not obstruct the 3D world.
        /// </summary>
        private void DrawNotificationToasts()
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr == null) return;

            var toasts = uiMgr.ActiveToasts;
            if (toasts.Count == 0) return;

            float startX = 20f;
            float startY = 56f;
            float toastWidth = 280f;
            float toastHeight = 36f;
            float gap = 6f;

            for (int i = 0; i < toasts.Count && i < 3; i++)
            {
                var toast = toasts[i];
                float y = startY + i * (toastHeight + gap);
                Rect toastRect = new Rect(startX, y, toastWidth, toastHeight);

                float alpha = Mathf.Clamp01(toast.TimeRemaining / 0.4f);
                Color surfaceColor = new Color(UITheme.ObsidianSurface.r, UITheme.ObsidianSurface.g, UITheme.ObsidianSurface.b, 0.92f * alpha);

                UITheme.DrawSolidRect(toastRect, surfaceColor);
                UITheme.DrawBorder(toastRect, new Color(UITheme.PolishedBrass.r, UITheme.PolishedBrass.g, UITheme.PolishedBrass.b, 0.4f * alpha), 1f);

                // Left accent stripe
                Color accent = new Color(toast.AccentColor.r, toast.AccentColor.g, toast.AccentColor.b, alpha);
                UITheme.DrawSolidRect(new Rect(startX, y, 3f, toastHeight), accent);

                // Message text
                Rect textRect = new Rect(startX + 10f, y + 7f, toastWidth - 16f, 22f);
                var style = new GUIStyle(UITheme.BodyStyle)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(UITheme.TextIvory.r, UITheme.TextIvory.g, UITheme.TextIvory.b, alpha) }
                };
                string displayMsg = $"{toast.Title}: {toast.Message}";
                if (displayMsg.Length > 38) displayMsg = displayMsg.Substring(0, 36) + "...";
                GUI.Label(textRect, displayMsg, style);
            }
        }

        /// <summary>
        /// Understated, elegant case dossier access pill at bottom right.
        /// </summary>
        private void DrawDossierAccessPill()
        {
            int evidenceCount = InvestigationKnowledgeService.Instance != null
                ? InvestigationKnowledgeService.Instance.EvidenceCount
                : 0;

            float width = 175f;
            float height = 28f;
            float x = Screen.width - width - 20f;
            float y = Screen.height - height - 16f;
            Rect pillRect = new Rect(x, y, width, height);

            UITheme.DrawSolidRect(pillRect, new Color(UITheme.ObsidianSurface.r, UITheme.ObsidianSurface.g, UITheme.ObsidianSurface.b, 0.88f));
            UITheme.DrawBorder(pillRect, new Color(UITheme.PolishedBrass.r, UITheme.PolishedBrass.g, UITheme.PolishedBrass.b, 0.5f), 1f);

            var pillStyle = new GUIStyle(UITheme.BodyStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = UITheme.TextIvory }
            };

            string text = $"<color=#{ColorUtility.ToHtmlStringRGB(UITheme.BrassHighlight)}>[TAB]</color> Dossier • <color=#{ColorUtility.ToHtmlStringRGB(UITheme.StatusAmber)}>{evidenceCount} Clues</color>";
            GUI.Label(pillRect, text, pillStyle);
        }
    }
}
