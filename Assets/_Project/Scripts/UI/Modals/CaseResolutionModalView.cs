using System;
using UnityEngine;
using TomorrowsCrime.Audio;
using TomorrowsCrime.Core;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.UI.Core;
using TomorrowsCrime.UI.Styling;

namespace TomorrowsCrime.UI.Modals
{
    /// <summary>
    /// Theatrical Case Resolution modal displaying the dramatic outcome of the player's final accusation.
    /// In Victory mode: presents the complete reconstructed crime narrative, detective scorecard, and sleuth rank.
    /// In Defeat mode: details the alibi rebuttal, flaws in deduction, and initiates a dramatic loop reset.
    /// </summary>
    public class CaseResolutionModalView : MonoBehaviour
    {
        private AccusationResult _lastResult;
        private Vector2 _scrollPos = Vector2.zero;

        public void SetResult(AccusationResult result)
        {
            _lastResult = result;
        }

        private void OnGUI()
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr == null || uiMgr.CurrentState != UIState.CaseResolutionModal || _lastResult == null) return;

            // Fullscreen backdrop
            Rect fullScreen = new Rect(0, 0, Screen.width, Screen.height);
            Color backdropColor = _lastResult.IsCorrect
                ? new Color(0.04f, 0.08f, 0.12f, 0.94f) // Deep Navy victory
                : new Color(0.12f, 0.04f, 0.04f, 0.94f); // Deep Crimson failure
            UITheme.DrawSolidRect(fullScreen, backdropColor);

            float modalWidth = Mathf.Min(780f, Screen.width - 40f);
            float modalHeight = Mathf.Min(600f, Screen.height - 40f);
            float x = (Screen.width - modalWidth) * 0.5f;
            float y = (Screen.height - modalHeight) * 0.5f;

            Rect modalRect = new Rect(x, y, modalWidth, modalHeight);
            UITheme.DrawSolidRect(modalRect, UITheme.ObsidianSurface);

            Color framePrimary = _lastResult.IsCorrect ? UITheme.PolishedBrass : UITheme.TieBurgundy;
            Color frameHighlight = _lastResult.IsCorrect ? UITheme.BrassHighlight : UITheme.StatusAmber;
            UITheme.DrawOrnateFrame(modalRect, framePrimary, frameHighlight, 3.0f);

            GUILayout.BeginArea(new Rect(x + 24f, y + 20f, modalWidth - 48f, modalHeight - 40f));

            // Top Wax Seal Stamp
            float sealSize = 64f;
            Rect sealRect = new Rect((modalWidth - 48f - sealSize) * 0.5f, 0f, sealSize, sealSize);
            UITheme.DrawWaxSeal(sealRect);
            GUILayout.Space(70f);

            // Title
            var titleStyle = new GUIStyle(UITheme.TitleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                normal = { textColor = _lastResult.IsCorrect ? UITheme.BrassHighlight : UITheme.TieBurgundy }
            };
            GUILayout.Label(_lastResult.Title, titleStyle);

            var subStyle = new GUIStyle(UITheme.SubHeaderStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                normal = { textColor = _lastResult.IsCorrect ? UITheme.PeacockTeal : UITheme.StatusAmber }
            };
            GUILayout.Label(_lastResult.IsCorrect ? $"DETECTIVE EVALUATION: {_lastResult.SleuthRank.ToUpper()}" : "TIME LOOP COLLAPSE IMMINENT", subStyle);

            GUILayout.Space(12f);
            UITheme.DrawSolidRect(GUILayoutUtility.GetRect(modalWidth - 48f, 1.5f), framePrimary);
            GUILayout.Space(12f);

            // Narrative Resolution Scroll Box
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(190f));
            var narrativeStyle = new GUIStyle(UITheme.BodyStyle)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = UITheme.TextIvory }
            };
            GUILayout.Label(_lastResult.DetailedResolution, narrativeStyle);
            GUILayout.EndScrollView();

            GUILayout.Space(10f);

            // Metrics Card on Victory
            if (_lastResult.IsCorrect)
            {
                GUILayout.BeginVertical(UITheme.CardStyle);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<b>Loops Required:</b> {_lastResult.LoopsElapsed}", UITheme.BodyStyle);
                GUILayout.Label($"<b>Sim Clock:</b> {(int)(_lastResult.SimSeconds / 60f):D2}:{(int)(_lastResult.SimSeconds % 60f):D2}", UITheme.BodyStyle);
                GUILayout.Label($"<b>Evidence Discovered:</b> {_lastResult.EvidenceDiscoveredCount}", UITheme.BodyStyle);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.FlexibleSpace();

            // Action Buttons
            GUILayout.BeginHorizontal();
            if (_lastResult.IsCorrect)
            {
                if (UITheme.DrawLuxeButton(GUILayoutUtility.GetRect(220f, 44f), "PLAY NEXT CASE", isPrimary: true))
                {
                    StartNextInvestigation();
                }
                GUILayout.Space(12f);
                if (UITheme.DrawLuxeButton(GUILayoutUtility.GetRect(220f, 44f), "MAIN MENU", isPrimary: false))
                {
                    ReturnToMainMenu();
                }
            }
            else
            {
                if (UITheme.DrawLuxeButton(GUILayoutUtility.GetRect(modalWidth - 48f, 44f), "REWIND TIME LOOP TO 00:00 [CONTINUE]", isPrimary: true))
                {
                    DismissAndResetLoop();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void StartNextInvestigation()
        {
            UIManager.Instance?.CloseModal();

            // Trigger new procedural case through GameSessionManager or GenerationPipeline
            var session = FindAnyObjectByType<TomorrowsCrime.Procedural.Core.GameSessionManager>();
            if (session != null)
            {
                session.StartNewProceduralCase();
            }
            else
            {
                SimulationManager.Instance?.ResetSimulation();
                InvestigationKnowledgeService.Instance?.Clear();
            }

            UIManager.Instance?.SetState(UIState.InGameHUD);
            UIManager.Instance?.ShowToast("NEW CASE BEGUN", "A fresh investigation has commenced at Meridian House.", UITheme.BrassHighlight);
        }

        private void ReturnToMainMenu()
        {
            UIManager.Instance?.CloseModal();
            UIManager.Instance?.SetState(UIState.MainMenu);
        }

        private void DismissAndResetLoop()
        {
            UIManager.Instance?.CloseModal();
            SimulationManager.Instance?.ResetSimulation();
            UIManager.Instance?.SetState(UIState.InGameHUD);
        }
    }
}
