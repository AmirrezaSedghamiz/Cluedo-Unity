using UnityEngine;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.UI.Core;
using TomorrowsCrime.UI.Styling;

namespace TomorrowsCrime.UI.Modals
{
    /// <summary>
    /// Dedicated reading and inspection view for discovered clues and physical evidence.
    /// Rendered with authentic warm paper styling, high text contrast, and actionable buttons.
    /// </summary>
    public class EvidenceInspectionModalView : MonoBehaviour
    {
        private void OnGUI()
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr == null || uiMgr.CurrentState != UIState.EvidenceDetailModal) return;

            var evidence = uiMgr.CurrentInspectedEvidence;
            if (evidence == null) return;

            // Semi-transparent atmospheric dark overlay behind modal
            UITheme.DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.05f, 0.05f, 0.06f, 0.75f));

            // Modal container
            float modalWidth = Mathf.Min(680f, Screen.width - 60f);
            float modalHeight = Mathf.Min(480f, Screen.height - 60f);
            float x = (Screen.width - modalWidth) * 0.5f;
            float y = (Screen.height - modalHeight) * 0.5f;
            Rect modalRect = new Rect(x, y, modalWidth, modalHeight);

            // Paper document background
            UITheme.DrawSolidRect(modalRect, UITheme.PaperSurface);
            UITheme.DrawBorder(modalRect, UITheme.MutedBrass, 2f);

            // Document Header Bar
            Rect headerBar = new Rect(x, y, modalWidth, 50f);
            UITheme.DrawSolidRect(headerBar, UITheme.SlateSurface);
            UITheme.DrawBorder(headerBar, UITheme.BorderSubtle, 1f);

            Rect titleRect = new Rect(x + 20f, y + 12f, modalWidth - 160f, 28f);
            var headerStyle = new GUIStyle(UITheme.HeaderStyle)
            {
                fontSize = 17,
                normal = { textColor = UITheme.MutedBrass }
            };
            GUI.Label(titleRect, $"FIELD EVIDENCE: {evidence.DisplayName.ToUpper()}", headerStyle);

            // Category badge in header
            Rect categoryBadge = new Rect(x + modalWidth - 130f, y + 14f, 110f, 22f);
            UITheme.DrawSolidRect(categoryBadge, UITheme.SlateSurfaceLight);
            UITheme.DrawBorder(categoryBadge, UITheme.DesaturatedTeal, 1f);
            var catStyle = new GUIStyle(UITheme.BadgeStyle)
            {
                normal = { textColor = UITheme.TextPrimary }
            };
            GUI.Label(categoryBadge, evidence.Category, catStyle);

            // Content Body (Paper Document Style)
            float contentY = y + 70f;

            // Short summary
            Rect summaryRect = new Rect(x + 30f, contentY, modalWidth - 60f, 40f);
            var subStyle = new GUIStyle(UITheme.PaperBodyStyle)
            {
                fontSize = 14,
                fontStyle = FontStyle.Italic
            };
            GUI.Label(summaryRect, $"\"{evidence.ShortDescription}\"", subStyle);

            contentY += 45f;
            UITheme.DrawSolidRect(new Rect(x + 30f, contentY, modalWidth - 60f, 1f), new Color(0.75f, 0.70f, 0.65f));
            contentY += 15f;

            // Detailed inspection text
            Rect detailRect = new Rect(x + 30f, contentY, modalWidth - 60f, modalHeight - 210f);
            var detailStyle = new GUIStyle(UITheme.PaperBodyStyle)
            {
                fontSize = 13,
                wordWrap = true
            };
            GUI.Label(detailRect, evidence.DetailedInspectionText, detailStyle);

            // Related event tag if defined
            if (!string.IsNullOrEmpty(evidence.RelatedEventId))
            {
                Rect eventTagRect = new Rect(x + 30f, y + modalHeight - 90f, modalWidth - 60f, 24f);
                var tagStyle = new GUIStyle(UITheme.PaperBodyStyle)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                };
                GUI.Label(eventTagRect, $"Causal Relationship: Associated with schedule routine [{evidence.RelatedEventId}]", tagStyle);
            }

            // Bottom Action Bar
            Rect actionBar = new Rect(x, y + modalHeight - 55f, modalWidth, 55f);
            UITheme.DrawSolidRect(actionBar, UITheme.SlateSurface);
            UITheme.DrawBorder(actionBar, UITheme.BorderSubtle, 1f);

            // Close button
            Rect closeBtnRect = new Rect(x + modalWidth - 190f, y + modalHeight - 45f, 170f, 34f);
            if (GUI.Button(closeBtnRect, "Return to Search [Esc]", UITheme.PrimaryButtonStyle))
            {
                uiMgr.CloseModal();
            }

            // Open in notebook button
            Rect notebookBtnRect = new Rect(x + 20f, y + modalHeight - 45f, 210f, 34f);
            if (GUI.Button(notebookBtnRect, "Open Full Notebook [Tab]", UITheme.ButtonStyle))
            {
                uiMgr.SetState(UIState.InvestigationNotebook);
            }
        }
    }
}
