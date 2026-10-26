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
    /// Bespoke first-person game HUD for Tomorrow's Crime (Modern Cluedo Art Direction).
    /// Renders:
    /// - Vintage Art-Deco pocket chronometer with elapsed & remaining countdown,
    ///   normalized loop progress indicator, and dynamic urgency color transitions.
    /// - Animated diamond focus reticle with decorative brass corner brackets.
    /// - Dispatch case-card perception toasts with brass left accents and wax seal stamp.
    /// - Luxury leather dossier access pill displaying discovered clues count.
    /// Non-obtrusive peripheral layout maintaining immersive 3D world readability.
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

            DrawTimeLoopChronometer();
            DrawRoomLocationBanner();
            DrawInteractionReticle();
            DrawNotificationToasts();
            DrawDossierAccessPill();
            DrawRewindTransitionVignette();
        }

        private void DrawRewindTransitionVignette()
        {
            if (_rewindVignetteTimer <= 0f) return;

            float norm = _rewindVignetteTimer / 2.0f; // 1.0 down to 0.0

            // Flash overlay: Early phase is bright temporal flash, fading to deep obsidian vignette
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

            // Theatrical title card
            if (norm > 0.15f)
            {
                float bannerAlpha = Mathf.Clamp01(norm * 2f);
                float bannerWidth = Mathf.Min(560f, Screen.width - 40f);
                float bannerHeight = 90f;
                float bx = (Screen.width - bannerWidth) * 0.5f;
                float by = (Screen.height - bannerHeight) * 0.45f;

                Rect bannerRect = new Rect(bx, by, bannerWidth, bannerHeight);
                Color plaqueColor = new Color(UITheme.ObsidianSurface.r, UITheme.ObsidianSurface.g, UITheme.ObsidianSurface.b, 0.92f * bannerAlpha);
                UITheme.DrawSolidRect(bannerRect, plaqueColor);
                UITheme.DrawOrnateFrame(bannerRect,
                    new Color(UITheme.PolishedBrass.r, UITheme.PolishedBrass.g, UITheme.PolishedBrass.b, bannerAlpha),
                    new Color(UITheme.BrassHighlight.r, UITheme.BrassHighlight.g, UITheme.BrassHighlight.b, bannerAlpha), 2f);

                var titleStyle = new GUIStyle(UITheme.TitleStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 18,
                    normal = { textColor = new Color(UITheme.BrassHighlight.r, UITheme.BrassHighlight.g, UITheme.BrassHighlight.b, bannerAlpha) }
                };
                GUI.Label(new Rect(bx, by + 12f, bannerWidth, 32f), $"TIME FRACTURE • LOOP {_currentLoopNumber} COMMENCES", titleStyle);

                var subStyle = new GUIStyle(UITheme.SubHeaderStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    normal = { textColor = new Color(UITheme.TextIvory.r, UITheme.TextIvory.g, UITheme.TextIvory.b, bannerAlpha * 0.9f) }
                };
                GUI.Label(new Rect(bx, by + 46f, bannerWidth, 24f), "The grandfather clock tolls midnight. Meridian House resets to 23:55.", subStyle);
            }
        }

        private void DrawTimeLoopChronometer()
        {
            if (_simulationManager == null || _simulationManager.Clock == null) return;

            var clock = _simulationManager.Clock;
            float elapsed = clock.ElapsedSeconds;
            float remaining = clock.RemainingSeconds;
            int loopIndex = clock.LoopCount;
            float progress = clock.NormalizedProgress;

            // Pocket watch chronometer card positioned at top center
            float width = 340f;
            float height = 62f;
            float x = (Screen.width - width) * 0.5f;
            float y = 16f;
            Rect clockRect = new Rect(x, y, width, height);

            UITheme.DrawPocketWatchClock(clockRect, elapsed, remaining, loopIndex, progress);
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

        private void DrawNotificationToasts()
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr == null) return;

            var toasts = uiMgr.ActiveToasts;
            if (toasts.Count == 0) return;

            float startX = 24f;
            float startY = 24f;
            float toastWidth = 380f;
            float toastHeight = 64f;
            float gap = 10f;

            for (int i = 0; i < toasts.Count; i++)
            {
                var toast = toasts[i];
                float y = startY + i * (toastHeight + gap);
                Rect toastRect = new Rect(startX, y, toastWidth, toastHeight);

                float alpha = Mathf.Clamp01(toast.TimeRemaining / 0.5f);
                Color surfaceColor = new Color(UITheme.ObsidianSurface.r, UITheme.ObsidianSurface.g, UITheme.ObsidianSurface.b, 0.95f * alpha);

                UITheme.DrawSolidRect(toastRect, surfaceColor);
                UITheme.DrawOrnateFrame(toastRect, new Color(UITheme.PolishedBrass.r, UITheme.PolishedBrass.g, UITheme.PolishedBrass.b, alpha),
                    new Color(UITheme.BrassHighlight.r, UITheme.BrassHighlight.g, UITheme.BrassHighlight.b, alpha), 1.5f);

                // Left Accent Stripe
                Rect accentRect = new Rect(startX, y, 5f, toastHeight);
                Color accentColor = new Color(toast.AccentColor.r, toast.AccentColor.g, toast.AccentColor.b, alpha);
                UITheme.DrawSolidRect(accentRect, accentColor);

                // Wax Seal Icon Stamp on Left
                Rect sealRect = new Rect(startX + 12f, y + 10f, 44f, 44f);
                var prevColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                UITheme.DrawWaxSeal(sealRect);
                GUI.color = prevColor;

                // Title and Message
                float textX = startX + 64f;
                float textWidth = toastWidth - 72f;

                Rect titleRect = new Rect(textX, y + 10f, textWidth, 20f);
                var titleStyle = new GUIStyle(UITheme.SubHeaderStyle)
                {
                    fontSize = 12,
                    normal = { textColor = accentColor }
                };
                GUI.Label(titleRect, toast.Title.ToUpper(), titleStyle);

                Rect msgRect = new Rect(textX, y + 30f, textWidth, 28f);
                var msgStyle = new GUIStyle(UITheme.BodyStyle)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(UITheme.TextIvory.r, UITheme.TextIvory.g, UITheme.TextIvory.b, alpha) }
                };
                GUI.Label(msgRect, toast.Message, msgStyle);
            }
        }

        private void DrawDossierAccessPill()
        {
            int evidenceCount = InvestigationKnowledgeService.Instance != null
                ? InvestigationKnowledgeService.Instance.EvidenceCount
                : 0;

            float width = 270f;
            float height = 40f;
            float x = Screen.width - width - 24f;
            float y = Screen.height - height - 24f;
            Rect pillRect = new Rect(x, y, width, height);

            UITheme.DrawSolidRect(pillRect, UITheme.ObsidianSurface);
            UITheme.DrawOrnateFrame(pillRect, UITheme.PolishedBrass, UITheme.BrassDark, 1.5f);

            // Left Wax Seal Stamp Icon
            Rect sealRect = new Rect(x + 6f, y + 6f, 28f, 28f);
            UITheme.DrawWaxSeal(sealRect);

            var pillStyle = new GUIStyle(UITheme.BodyStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = UITheme.TextIvory }
            };

            string text = $"<color=#{ColorUtility.ToHtmlStringRGB(UITheme.BrassHighlight)}>[TAB / N]</color> CASE DOSSIER • <color=#{ColorUtility.ToHtmlStringRGB(UITheme.StatusAmber)}>{evidenceCount} CLUES</color>";
            GUI.Label(new Rect(x + 40f, y + 2f, width - 44f, height - 4f), text, pillStyle);
        }

        private void DrawRoomLocationBanner()
        {
            if (_playerController == null) _playerController = FindAnyObjectByType<PlayerController>();
            if (_playerController == null) return;

            string room = _playerController.CurrentRoomName;
            if (!string.IsNullOrEmpty(room))
            {
                float width = 380f;
                float height = 24f;
                float x = (Screen.width - width) * 0.5f;
                float y = 82f;

                var style = new GUIStyle(UITheme.BodyMutedStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = UITheme.BrassHighlight }
                };

                GUI.Label(new Rect(x, y, width, height), $"<color=#{ColorUtility.ToHtmlStringRGB(UITheme.PeacockTeal)}>LOCATION:</color> {room.ToUpper()}", style);
            }

            if (_playerController.IsCrouching)
            {
                var crouchStyle = new GUIStyle(UITheme.BodyStyle)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 11,
                    normal = { textColor = UITheme.StatusAmber }
                };
                GUI.Label(new Rect(24f, Screen.height - 40f, 180f, 24f), "● CROUCHING [C / CTRL]", crouchStyle);
            }
        }
    }
}
