using System;
using System.Collections.Generic;
using UnityEngine;
using TomorrowsCrime.Audio;
using TomorrowsCrime.Core;
using TomorrowsCrime.Investigation;
using TomorrowsCrime.NPC;
using TomorrowsCrime.Simulation;
using TomorrowsCrime.UI.Core;
using TomorrowsCrime.UI.Notebook;
using TomorrowsCrime.UI.Styling;

namespace TomorrowsCrime.UI.Modals
{
    /// <summary>
    /// Interactive Cluedo-style theatrical interrogation modal for questioning suspects and witnesses.
    /// Features:
    /// - Character identity header with wax seal badge, role designation, and demeanor.
    /// - Dynamic conversational responses reflecting NPC schedule, personality, and current loop time.
    /// - Actionable inquiry choices (Alibi, Background, Witness observation, Evidence confrontation).
    /// - Automatic recording of witness testimonies into the persistent Case Dossier / Notebook.
    /// - Direct link to formulate an Accusation against the suspect.
    /// </summary>
    public class NPCDialogueModalView : MonoBehaviour
    {
        private NPCController _activeNpc;
        private string _currentDialogueText = "";
        private string _currentTopicTitle = "INTERROGATION";
        private Vector2 _scrollPos = Vector2.zero;
        private bool _isConfrontingEvidence = false;

        private void OnGUI()
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr == null || uiMgr.CurrentState != UIState.NPCDialogueModal) return;

            _activeNpc = uiMgr.CurrentInteractingNpc;
            if (_activeNpc == null)
            {
                uiMgr.CloseModal();
                return;
            }

            // Dark theatrical backdrop
            Rect fullScreen = new Rect(0, 0, Screen.width, Screen.height);
            UITheme.DrawSolidRect(fullScreen, new Color(UITheme.MeridianMidnight.r, UITheme.MeridianMidnight.g, UITheme.MeridianMidnight.b, 0.88f));

            float modalWidth = Mathf.Min(820f, Screen.width - 40f);
            float modalHeight = Mathf.Min(560f, Screen.height - 40f);
            float x = (Screen.width - modalWidth) * 0.5f;
            float y = (Screen.height - modalHeight) * 0.5f;

            Rect modalRect = new Rect(x, y, modalWidth, modalHeight);
            UITheme.DrawSolidRect(modalRect, UITheme.ObsidianSurface);
            UITheme.DrawOrnateFrame(modalRect, UITheme.PolishedBrass, UITheme.BrassHighlight, 2.5f);

            // Modal Header
            DrawModalHeader(x, y, modalWidth);

            // Main Content: Left Portrait/Profile Column, Right Interrogation Dialogue Column
            float contentY = y + 75f;
            float contentHeight = modalHeight - 90f;
            float leftColWidth = 260f;
            float rightColWidth = modalWidth - leftColWidth - 36f;

            Rect leftRect = new Rect(x + 16f, contentY, leftColWidth, contentHeight);
            Rect rightRect = new Rect(x + leftColWidth + 24f, contentY, rightColWidth, contentHeight);

            DrawSuspectProfileColumn(leftRect);
            DrawInterrogationColumn(rightRect);
        }

        private void DrawModalHeader(float x, float y, float width)
        {
            Rect headerRect = new Rect(x + 16f, y + 14f, width - 32f, 48f);
            GUILayout.BeginArea(headerRect);
            GUILayout.BeginHorizontal();

            GUILayout.Label("PERSON OF INTEREST • WITNESS STATEMENT", UITheme.HeaderStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Step Away [Esc]", UITheme.ButtonStyle, GUILayout.Width(130f), GUILayout.Height(30f)))
            {
                UIManager.Instance.CloseModal();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // Divider rule
            UITheme.DrawSolidRect(new Rect(x + 16f, y + 64f, width - 32f, 1.5f), UITheme.PolishedBrass);
        }

        private void DrawSuspectProfileColumn(Rect rect)
        {
            UITheme.DrawSolidRect(rect, UITheme.MeridianMidnight);
            UITheme.DrawOrnateFrame(rect, UITheme.PolishedBrass, UITheme.BrassDark, 1.5f);

            GUILayout.BeginArea(new Rect(rect.x + 14f, rect.y + 14f, rect.width - 28f, rect.height - 28f));

            // Wax Seal Stamp
            float sealSize = 56f;
            Rect sealRect = new Rect((rect.width - 28f - sealSize) * 0.5f, 0f, sealSize, sealSize);
            UITheme.DrawWaxSeal(sealRect);
            GUILayout.Space(62f);

            // Suspect Name & Role
            var nameStyle = new GUIStyle(UITheme.SubHeaderStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = UITheme.BrassHighlight }
            };
            GUILayout.Label(_activeNpc.CharacterDisplayName.ToUpper(), nameStyle);

            var roleStyle = new GUIStyle(UITheme.BodyMutedStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                normal = { textColor = UITheme.PeacockTeal }
            };
            GUILayout.Label(_activeNpc.CharacterRole.ToString().ToUpper(), roleStyle);

            GUILayout.Space(8f);

            // Profile Attributes
            var bodyStyle = new GUIStyle(UITheme.BodyStyle) { fontSize = 11 };
            if (_activeNpc.Profile != null)
            {
                var p = _activeNpc.Profile;
                GUILayout.Label($"<b>Age:</b> {p.Age}", bodyStyle);
                GUILayout.Label($"<b>Quarters:</b> {p.AssignedRoomId ?? "Reception"}", bodyStyle);
                GUILayout.Label($"<b>Demeanor:</b> {(p.Personality.Honesty < 0.4f ? "Guarded" : "Composed")}", bodyStyle);
                GUILayout.Label($"<b>Vigilance:</b> {(p.Personality.Caution > 0.6f ? "Alert" : "Routine")}", bodyStyle);
            }
            else
            {
                GUILayout.Label("<b>Post:</b> Front Desk", bodyStyle);
                GUILayout.Label("<b>Demeanor:</b> Formal, disciplined.", bodyStyle);
            }

            GUILayout.FlexibleSpace();

            // Direct Accuse Shortcut Button
            if (UITheme.DrawLuxeButton(new Rect(0, rect.height - 76f, rect.width - 28f, 38f), "ACCUSE SUSPECT", isPrimary: false))
            {
                TriggerAccusationScreen();
            }

            GUILayout.EndArea();
        }

        private void DrawInterrogationColumn(Rect rect)
        {
            UITheme.DrawSolidRect(rect, UITheme.MeridianMidnight);
            UITheme.DrawOrnateFrame(rect, UITheme.PolishedBrass, UITheme.BrassDark, 1.5f);

            GUILayout.BeginArea(new Rect(rect.x + 16f, rect.y + 14f, rect.width - 32f, rect.height - 28f));

            // Speech Bubble / Dialogue Output Box
            float dialogueBoxHeight = 160f;
            Rect dialogueBoxRect = new Rect(0, 0, rect.width - 32f, dialogueBoxHeight);
            UITheme.DrawSolidRect(dialogueBoxRect, UITheme.ObsidianSurface);
            UITheme.DrawOrnateFrame(dialogueBoxRect, UITheme.PolishedBrass, UITheme.BrassHighlight, 1.5f);

            GUILayout.BeginArea(new Rect(dialogueBoxRect.x + 14f, dialogueBoxRect.y + 10f, dialogueBoxRect.width - 28f, dialogueBoxRect.height - 20f));
            GUILayout.Label($"<color=#{ColorUtility.ToHtmlStringRGB(UITheme.BrassHighlight)}>[{_currentTopicTitle.ToUpper()}]</color>", UITheme.HeaderStyle);
            GUILayout.Space(4f);

            string quote = string.IsNullOrEmpty(_currentDialogueText)
                ? GetDefaultGreeting()
                : _currentDialogueText;

            var quoteStyle = new GUIStyle(UITheme.BodyStyle)
            {
                fontSize = 12,
                fontStyle = FontStyle.Italic,
                wordWrap = true,
                normal = { textColor = UITheme.TextIvory }
            };
            GUILayout.Label($"\"{quote}\"", quoteStyle);
            GUILayout.EndArea();

            GUILayout.Space(dialogueBoxHeight + 12f);

            // Inquiries List or Evidence Confrontation
            if (_isConfrontingEvidence)
            {
                DrawEvidenceConfrontationMenu();
            }
            else
            {
                DrawInquiryOptions();
            }

            GUILayout.EndArea();
        }

        private string GetDefaultGreeting()
        {
            if (_activeNpc.Profile != null)
            {
                return $"\"Good evening, Detective. Meridian House is at your disposal, though I assure you I have done nothing improper tonight.\"";
            }
            return $"\"Good evening, Inspector. Midnight duties require my full attention. What is the nature of your inquiry?\"";
        }

        private void DrawInquiryOptions()
        {
            GUILayout.Label("INTERROGATION TOPICS:", UITheme.SubHeaderStyle);
            GUILayout.Space(6f);

            // 1. Alibi Inquiry
            if (GUILayout.Button("[ALIBI]  \"Where were you stationed at midnight?\"", UITheme.ButtonStyle, GUILayout.Height(32f)))
            {
                AskAlibi();
            }
            GUILayout.Space(4f);

            // 2. Responsibilities / Motive Inquiry
            if (GUILayout.Button("[DUTIES]  \"What are your specific duties here tonight?\"", UITheme.ButtonStyle, GUILayout.Height(32f)))
            {
                AskResponsibilities();
            }
            GUILayout.Space(4f);

            // 3. Witness Observations
            if (GUILayout.Button("[WITNESS]  \"Did you observe any suspicious activity prior to midnight?\"", UITheme.ButtonStyle, GUILayout.Height(32f)))
            {
                AskObservations();
            }
            GUILayout.Space(4f);

            // 4. Confront with Evidence
            if (GUILayout.Button("[CONFRONT]  \"I have physical evidence to discuss with you...\"", UITheme.PrimaryButtonStyle, GUILayout.Height(32f)))
            {
                _isConfrontingEvidence = true;
                AudioManager.Instance?.PlayPaperRustle();
            }
        }

        private void DrawEvidenceConfrontationMenu()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("SELECT CLUE TO CONFRONT SUSPECT WITH:", UITheme.SubHeaderStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Back to Questions", UITheme.ButtonStyle, GUILayout.Width(130f), GUILayout.Height(24f)))
            {
                _isConfrontingEvidence = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            var discoveredClues = InvestigationKnowledgeService.Instance != null
                ? InvestigationKnowledgeService.Instance.GetAllDiscoveredEvidence()
                : new List<DiscoveredEvidenceRecord>();

            if (discoveredClues.Count == 0)
            {
                GUILayout.Label("You have not cataloged any physical evidence yet. Search the hotel for clues.", UITheme.BodyMutedStyle);
                return;
            }

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(130f));
            foreach (var clue in discoveredClues)
            {
                if (GUILayout.Button($"Confront with: {clue.Definition.DisplayName}", UITheme.ButtonStyle, GUILayout.Height(28f)))
                {
                    ConfrontWithClue(clue.Definition);
                    _isConfrontingEvidence = false;
                }
                GUILayout.Space(3f);
            }
            GUILayout.EndScrollView();
        }

        private void AskAlibi()
        {
            _currentTopicTitle = "MIDNIGHT ALIBI STATEMENT";
            AudioManager.Instance?.PlayPaperRustle();

            int loop = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                ? SimulationManager.Instance.Clock.LoopCount
                : 1;
            float simTime = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                ? SimulationManager.Instance.Clock.ElapsedSeconds
                : 0f;

            string statement;
            if (_activeNpc.Profile != null)
            {
                statement = $"I was stationed in {_activeNpc.Profile.AssignedRoomId ?? "my designated quarters"} during midnight. You may verify the hotel registry. I have no reason to harm anyone.";
            }
            else
            {
                statement = "From 00:00 to 00:30 I remained stationed behind the reception desk. At 00:30 I retrieved the Room 102 keycard from the lobby table and unlocked the suite door at 00:35. My duty memo on the desk confirms this schedule.";
            }

            _currentDialogueText = statement;

            // Record as formal observation testimony in Notebook
            InvestigationKnowledgeService.Instance?.RecordObservation(
                $"obs_alibi_{_activeNpc.CharacterDisplayName}_{loop}_{simTime:F0}",
                $"TESTIMONY: {_activeNpc.CharacterDisplayName} stated: \"{statement}\"",
                loop,
                simTime,
                null,
                null,
                ObservationMethod.WitnessedEvent,
                _activeNpc.CharacterDisplayName,
                "Alibi Inquiry");

            UIManager.Instance?.ShowToast("TESTIMONY RECORDED", $"{_activeNpc.CharacterDisplayName}'s alibi logged in Case Dossier.", UITheme.BrassHighlight);
        }

        private void AskResponsibilities()
        {
            _currentTopicTitle = "DUTIES & BACKGROUND";
            AudioManager.Instance?.PlayPaperRustle();

            if (_activeNpc.Profile != null)
            {
                _currentDialogueText = $"My role as {_activeNpc.Profile.Role} requires managing hotel operations and catering to our distinguished guests. We pride ourselves on absolute discretion.";
            }
            else
            {
                _currentDialogueText = "As Head Concierge, I oversee guest arrivals, security of the master keys, and executive suite inspections. No guest enters Room 102 without authorization.";
            }
        }

        private void AskObservations()
        {
            _currentTopicTitle = "WITNESS TESTIMONY";
            AudioManager.Instance?.PlayPaperRustle();

            int loop = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                ? SimulationManager.Instance.Clock.LoopCount
                : 1;
            float simTime = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                ? SimulationManager.Instance.Clock.ElapsedSeconds
                : 0f;

            string observation = "I noticed heavy footsteps near the upper corridor right before midnight, and the pantry door in the kitchen was unlocked. Someone was moving quietly in the shadows.";
            _currentDialogueText = observation;

            InvestigationKnowledgeService.Instance?.RecordObservation(
                $"obs_lead_{_activeNpc.CharacterDisplayName}_{loop}_{simTime:F0}",
                $"LEAD: {_activeNpc.CharacterDisplayName} observed: \"{observation}\"",
                loop,
                simTime,
                null,
                null,
                ObservationMethod.WitnessedEvent,
                _activeNpc.CharacterDisplayName,
                "Witness Interrogation");

            UIManager.Instance?.ShowToast("LEAD ACQUIRED", "Suspicious movement report logged in Timeline.", UITheme.PeacockTeal);
        }

        private void ConfrontWithClue(EvidenceDefinition clue)
        {
            _currentTopicTitle = $"CONFRONTATION: {clue.DisplayName.ToUpper()}";
            AudioManager.Instance?.PlayEvidenceDiscovery();

            _currentDialogueText = $"Where did you uncover {clue.DisplayName}?! I... I assure you that has a completely innocent explanation, Detective. Do not jump to baseless conclusions!";
            UIManager.Instance?.ShowToast("SUSPECT REACTION", $"{_activeNpc.CharacterDisplayName} became defensive when shown {clue.DisplayName}.", UITheme.StatusAmber);
        }

        private void TriggerAccusationScreen()
        {
            UIManager.Instance?.CloseModal();
            // Open Notebook to Accusation Section
            var notebook = FindAnyObjectByType<InvestigationWorkspaceView>();
            if (notebook != null)
            {
                notebook.SelectAccusationSuspect(_activeNpc.CharacterDisplayName);
            }
            UIManager.Instance?.SetState(UIState.InvestigationNotebook);
        }
    }
}
