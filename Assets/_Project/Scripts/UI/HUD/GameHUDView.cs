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
                return; // Suppress HUD during full-screen modals, pause menu, and dossier workspace
            }

            DrawTimeLoopChronometer();
            DrawInteractionReticle();
            DrawNotificationToasts();
            DrawDossierAccessPill();
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
    }
}
