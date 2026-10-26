using UnityEngine;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.UI.Core;
using TomorrowsCrime.UI.Styling;

namespace TomorrowsCrime.UI.Modals
{
    /// <summary>
    /// Dedicated inspection view for examined clues and physical evidence.
    /// Rebuilt for Step 7: Modern Cluedo Art Direction (Meridian House Aesthetic).
    /// Displays archival case card, gold foil borders, category stamp, full examination narrative,
    /// discovery context, and direct links to the master dossier.
    /// </summary>
    public class EvidenceInspectionModalView : MonoBehaviour
    {
        private void OnGUI()
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr == null || uiMgr.CurrentState != UIState.EvidenceDetailModal) return;

            var evidence = uiMgr.CurrentInspectedEvidence;
            if (evidence == null) return;

            // Semi-transparent dramatic vignette backdrop
            UITheme.DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.06f, 0.09f, 0.85f));

            // Main Archival Modal Dimensions
            float modalWidth = Mathf.Min(740f, Screen.width - 60f);
            float modalHeight = Mathf.Min(540f, Screen.height - 60f);
            float x = (Screen.width - modalWidth) * 0.5f;
            float y = (Screen.height - modalHeight) * 0.5f;
            Rect modalRect = new Rect(x, y, modalWidth, modalHeight);

            // Aged Parchment Surface with Gold Foil Framing
            UITheme.DrawParchmentCard(modalRect);
            UITheme.DrawOrnateFrame(modalRect, UITheme.PolishedBrass, UITheme.BrassHighlight, 2.5f);

            // Top Header Plaque (Obsidian & Gold)
            Rect headerBar = new Rect(x + 10f, y + 10f, modalWidth - 20f, 54f);
            UITheme.DrawSolidRect(headerBar, UITheme.ObsidianSurface);
            UITheme.DrawOrnateFrame(headerBar, UITheme.PolishedBrass, UITheme.BrassDark, 1.5f);

            // Wax Seal Icon on Left of Header
            Rect sealRect = new Rect(headerBar.x + 8f, headerBar.y + 7f, 40f, 40f);
            UITheme.DrawWaxSeal(sealRect);

            // Evidence Title
            Rect titleRect = new Rect(headerBar.x + 56f, headerBar.y + 12f, headerBar.width - 230f, 30f);
            var titleStyle = new GUIStyle(UITheme.HeaderStyle)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = UITheme.BrassHighlight }
            };
            GUI.Label(titleRect, evidence.DisplayName.ToUpper(), titleStyle);

            // Category Stamp Badge on Right of Header
            float catWidth = 200f;
            Rect catBadge = new Rect(headerBar.xMax - catWidth - 12f, headerBar.y + 14f, catWidth, 26f);
            UITheme.DrawSolidRect(catBadge, UITheme.MeridianMidnight);
            UITheme.DrawBorder(catBadge, UITheme.PeacockTeal, 1.5f);
            var catStyle = new GUIStyle(UITheme.BadgeStyle)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = UITheme.TextIvory }
            };
            GUI.Label(catBadge, $"CATEGORY: {evidence.Category.ToUpper()}", catStyle);

            // Document Content Area (Paper Body)
            float contentY = y + 80f;

            // Short Description Quote Banner
            Rect summaryRect = new Rect(x + 36f, contentY, modalWidth - 72f, 44f);
            UITheme.DrawSolidRect(summaryRect, new Color(0.90f, 0.86f, 0.78f, 0.6f));
            UITheme.DrawBorder(summaryRect, new Color(0.70f, 0.60f, 0.45f, 0.5f), 1f);

            var summaryStyle = new GUIStyle(UITheme.PaperBodyStyle)
            {
                fontSize = 14,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = UITheme.TextDarkInk }
            };
            GUI.Label(summaryRect, $"\"{evidence.ShortDescription}\"", summaryStyle);

            contentY += 58f;
            // Divider Line
            UITheme.DrawSolidRect(new Rect(x + 36f, contentY, modalWidth - 72f, 1.5f), UITheme.PolishedBrass);
            contentY += 16f;

            // Full Examination Narrative
            Rect detailRect = new Rect(x + 36f, contentY, modalWidth - 72f, modalHeight - 260f);
            var detailStyle = new GUIStyle(UITheme.PaperBodyStyle)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = UITheme.TextDarkInk }
            };
            GUI.Label(detailRect, evidence.DetailedInspectionText, detailStyle);

            // Context & Causal Link Footer
            float footerY = y + modalHeight - 88f;
            if (!string.IsNullOrEmpty(evidence.RelatedEventId))
            {
                Rect eventRect = new Rect(x + 36f, footerY - 26f, modalWidth - 72f, 22f);
                var eventStyle = new GUIStyle(UITheme.PaperBodyStyle)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = UITheme.PeacockTeal }
                };
                GUI.Label(eventRect, $"LINKED INCIDENT EVENT: [{evidence.RelatedEventId}]", eventStyle);
            }

            // Action Buttons
            float btnWidth = (modalWidth - 92f) * 0.5f;
            Rect btnNotebook = new Rect(x + 36f, footerY, btnWidth, 42f);
            if (UITheme.DrawLuxeButton(btnNotebook, "OPEN IN CASE DOSSIER [TAB]", isPrimary: true))
            {
                uiMgr.SetState(UIState.InvestigationNotebook);
            }

            Rect btnClose = new Rect(x + 46f + btnWidth, footerY, btnWidth, 42f);
            if (UITheme.DrawLuxeButton(btnClose, "RESUME INVESTIGATION [ESC / E]", isPrimary: false))
            {
                uiMgr.SetState(UIState.InGameHUD);
            }
        }
    }
}
