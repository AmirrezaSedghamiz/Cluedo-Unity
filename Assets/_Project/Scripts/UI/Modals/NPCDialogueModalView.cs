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
            string id = _activeNpc != null ? _activeNpc.CharacterId : "npc_concierge";
            string name = _activeNpc != null ? _activeNpc.CharacterDisplayName : "Concierge Pierre";
            var role = _activeNpc != null && _activeNpc.Profile != null ? _activeNpc.Profile.Role : TomorrowsCrime.Procedural.NPCs.NPCRole.Concierge;

            return GetCharacterGreeting(id, name, role);
        }

        private static string GetCharacterGreeting(string id, string name, TomorrowsCrime.Procedural.NPCs.NPCRole role)
        {
            switch (id)
            {
                case "npc_concierge_pierre":
                    return "Ah, Detective. Good evening. I trust Meridian House meets your standards. The front desk remains entirely at your service, though I assure you protocol has been strictly maintained.";
                case "npc_receptionist_celeste":
                    return "Bonsoir, Detective! Please keep your voice down—the walls have ears tonight. What can I check in the registration ledger for you?";
                case "npc_chef_marcel":
                    return "Sacré bleu, Detective! Unless you are here to compliment my midnight consommé, make it fast! I have prep work for tomorrow and my kitchen is not a police precinct!";
                case "npc_maid_clara":
                    return "O-oh! Good evening, Detective... Please excuse the mess, I was just finishing turn-down service. Has... has something terrible happened in the hotel?";
                case "npc_engineer_otto":
                    return "Guten Abend. If you're here to complain about hot water on the upper floors, go speak to the manager. Otherwise, speak up—these boilers are deafening.";
                case "npc_manager_adler":
                    return "Ah, Detective. I trust you are conducting your inquiries with utmost discretion? Meridian House caters to an elite clientele and we cannot afford sensationalized scandal.";
                case "npc_lord_harrington":
                    return "Hmph. Who gave you permission to interrupt me, officer? If this is about the commotion in the east corridor, your staff has already failed my expectations.";
                case "npc_madame_valerie":
                    return "Oh, darling! You have the intense look of an inquisitor. Tell me, is this dreary hotel finally producing some excitement, or are you just admiring my pearls?";
                case "npc_dr_sterling":
                    return "Good evening, Inspector. If someone requires immediate resuscitation or medical attention, take me to them at once. Otherwise, I am attempting to finish my toxicology papers.";
                case "npc_journalist_monroe":
                    return "Well, well. If it isn't the law. Look, Detective, you give me the scoop on what's going down, and I'll give you the real dirt on this gilded snake pit. Deal?";
                default:
                    return $"Good evening, Inspector. I am {name}. How may I assist your investigation tonight?";
            }
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

            string id = _activeNpc != null ? _activeNpc.CharacterId : "npc_concierge";
            string name = _activeNpc != null ? _activeNpc.CharacterDisplayName : "Concierge Pierre";
            var role = _activeNpc != null && _activeNpc.Profile != null ? _activeNpc.Profile.Role : TomorrowsCrime.Procedural.NPCs.NPCRole.Concierge;

            bool isCulprit = false;
            string groundTruthAlibi = null;
            if (CaseAccusationService.Instance != null && CaseAccusationService.Instance.CurrentGroundTruth != null)
            {
                var gt = CaseAccusationService.Instance.CurrentGroundTruth;
                if (gt.CulpritId == id) isCulprit = true;
                if (gt.SuspectAlibis != null) gt.SuspectAlibis.TryGetValue(id, out groundTruthAlibi);
            }
            if (_activeNpc != null && _activeNpc.Profile != null && _activeNpc.Profile.IsCulprit)
            {
                isCulprit = true;
            }

            string statement = GetCharacterAlibi(id, name, role, isCulprit, groundTruthAlibi);
            _currentDialogueText = statement;

            // Record as formal observation testimony in Notebook
            InvestigationKnowledgeService.Instance?.RecordObservation(
                $"obs_alibi_{name.Replace(" ", "_")}_{loop}_{simTime:F0}",
                $"TESTIMONY: {name} stated: \"{statement}\"",
                loop,
                simTime,
                null,
                null,
                ObservationMethod.WitnessedEvent,
                name,
                "Alibi Inquiry");

            UIManager.Instance?.ShowToast("TESTIMONY RECORDED", $"{name}'s alibi logged in Case Dossier.", UITheme.BrassHighlight);
        }

        private static string GetCharacterAlibi(string id, string name, TomorrowsCrime.Procedural.NPCs.NPCRole role, bool isCulprit, string groundTruthAlibi)
        {
            if (isCulprit)
            {
                switch (id)
                {
                    case "npc_concierge_pierre":
                        return "I remained stationed behind the reception desk all evening, Detective! If anyone claims they saw me upstairs near the suites with an apothecary vial, they are lying to ruin my reputation!";
                    case "npc_chef_marcel":
                        return "I was at my cooking range all night, sweat dripping off my brow! If someone took my French cleaver into the executive office, they stole it while I was in the pantry! I am an artist, not a murderer!";
                    case "npc_maid_clara":
                        return "I was folding towels in housekeeping all night, Detective! Please believe me, I would never harm a guest! I... I only entered the guest rooms to drop off clean linens!";
                    case "npc_engineer_otto":
                        return "I was bleeding the valves on Boiler #2 down in the utility basement! The wrench missing from my chest? Anyone could have forced the lock while the blowoff was venting!";
                    case "npc_manager_adler":
                        return "I was locked inside my private executive office balancing the confidential hotel accounts! I had no knowledge of any violence until the alarm was raised! My door was bolted from the inside!";
                    case "npc_lord_harrington":
                        return "I was in my private suite resting, as befits a gentleman of my years and standing! I have no need to account for my movements to inquisitive detectives!";
                    case "npc_madame_valerie":
                        return "I was sitting at my vanity arranging my pearls! A lady does not account for her every minute to strangers, Detective, especially when tragedy strikes!";
                    case "npc_dr_sterling":
                        return "I was in Suite 103 reviewing clinical toxicology case studies! I had no reason to harm anyone with blunt force or poison! I took an oath to preserve human life!";
                    case "npc_journalist_monroe":
                        return "I was sitting right there in the lobby lounge typing notes for my society column! If I slipped into the corridors, it was strictly for journalistic background, not murder!";
                    default:
                        return "I was in my quarters all night, Detective. I know nothing about any crime and I assure you I am completely innocent.";
                }
            }

            switch (id)
            {
                case "npc_concierge_pierre":
                    return "From 00:00 to 01:10 I stood behind the reception desk checking keys. Between 01:10 and 02:20 I inspected the central lobby and east transport hallway. Mademoiselle Celeste saw me at the front desk when she brought the ledger.";
                case "npc_receptionist_celeste":
                    return "I was stationed at the reception counter until 01:30 logging guest telegrams. Between 01:30 and 02:45 I took billing invoices to Mr. Adler's office, then stopped by the lounge for water before returning.";
                case "npc_chef_marcel":
                    return "At midnight I was prepping beef consommé at the cooking range. At 01:20 I was in the dry pantry checking spices, and by 02:30 I carried fresh rolls into the dining room. My apron was covered in broth and flour.";
                case "npc_maid_clara":
                    return "From midnight to 01:15 I was in the linen room sorting fresh towels. Then I did turn-down service in Room 101, dusted the reading tables in the library, and swept the gallery runner. I didn't leave the second floor all night.";
                case "npc_engineer_otto":
                    return "From midnight to 01:15 I was bleeding the valves on Boiler #2. Then I checked the electrical breakers at 01:30 and walked the ground service corridor inspecting steam traps. You can still smell the machine grease on my hands.";
                case "npc_manager_adler":
                    return "I was sequestered in my executive office until 01:40 reviewing accounts. Afterward, I made a brief inspection of the dining hall and lobby before returning to lock the safe at 03:55.";
                case "npc_lord_harrington":
                    return "At midnight I was drafting correspondence in Suite 101. At 01:25 I walked to the library to consult an atlas, glanced into the chess room, and returned to my suite. My valet can confirm my routine.";
                case "npc_madame_valerie":
                    return "Midnight found me at my vanity in Suite 102 fixing my jewelry. Around 01:20 I stepped into the library archive for a volume of poetry, then took the midnight air on the balcony before returning to my desk.";
                case "npc_dr_sterling":
                    return "From midnight to 01:30 I was reading medical treatises in Suite 103. At 01:30 I took a brief stroll to the library to check a pharmacological text, then paced the gallery before turning in.";
                case "npc_journalist_monroe":
                    return "I was in the lobby lounge until 01:20 keeping my eyes peeled. Then I checked the ground corridor and had a drink at the Golden Quill bar. Several patrons saw me taking notes.";
                default:
                    return !string.IsNullOrEmpty(groundTruthAlibi)
                        ? groundTruthAlibi
                        : $"I remained at my designated post throughout the midnight window. You may consult the staff records.";
            }
        }

        private void AskResponsibilities()
        {
            _currentTopicTitle = "DUTIES & BACKGROUND";
            AudioManager.Instance?.PlayPaperRustle();

            string id = _activeNpc != null ? _activeNpc.CharacterId : "npc_concierge";
            string name = _activeNpc != null ? _activeNpc.CharacterDisplayName : "Concierge Pierre";
            var role = _activeNpc != null && _activeNpc.Profile != null ? _activeNpc.Profile.Role : TomorrowsCrime.Procedural.NPCs.NPCRole.Concierge;

            _currentDialogueText = GetCharacterDuties(id, name, role);
        }

        private static string GetCharacterDuties(string id, string name, TomorrowsCrime.Procedural.NPCs.NPCRole role)
        {
            switch (id)
            {
                case "npc_concierge_pierre":
                    return "I have served as Head Concierge at Meridian House for twelve years. I manage guest arrivals, supervise the luggage staff, and hold sole custody of the brass key cabinet. Discretion is my highest creed.";
                case "npc_receptionist_celeste":
                    return "I manage guest check-ins, telephone switchboard dispatches, and registration records. People assume receptionists just stamp receipts, but we see who arrives in the dead of night and who pays in unmarked banknotes.";
                case "npc_chef_marcel":
                    return "I am the culinary director of Meridian House! I command the kitchen, prepare the banquet courses, and protect my pantry from cheap bureaucrats like Julian Adler who try to slash my cream and truffle budget!";
                case "npc_maid_clara":
                    return "I am the second-floor chambermaid, sir. I change the linens, air out the suites, turn down beds, and polish the brass fixtures. I try to stay invisible—distinguished guests prefer not to notice servants.";
                case "npc_engineer_otto":
                    return "I keep this grand palace from falling apart. Steam boilers, electrical dynamos, elevator counterweights, plumbing valves. Without me, these wealthy aristocrats would be freezing in the dark.";
                case "npc_manager_adler":
                    return "I am the General Manager. I oversee all operations, balance the books, and ensure our distinguished clientele experiences luxury without vulgar disturbances. The hotel's reputation is in my hands.";
                case "npc_lord_harrington":
                    return "I am Lord Harrington, 7th Earl of Westlake. I do not have 'duties'; I have ancestral estates, investments to safeguard, and family members who seem incapable of living within their means.";
                case "npc_madame_valerie":
                    return "Duties? Mon cher, my duty is to look glamorous, entertain European society, and survive the suffocating boredom of provincial nobility. I am a patron of high art and fine champagne.";
                case "npc_dr_sterling":
                    return "I am a fellow of the Royal College of Surgeons, specializing in toxicology and internal pathology. I am staying at Meridian House while completing my treatise on poisonous alkaloid counter-agents.";
                case "npc_journalist_monroe":
                    return "Senior investigative correspondent for the Daily Chronicle. I uncover corporate blackmail, political payoffs, and high-society scandals. And believe me, Meridian House is crawling with secrets.";
                default:
                    return $"As {role}, I perform my assigned responsibilities diligently to maintain order in Meridian House.";
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

            string id = _activeNpc != null ? _activeNpc.CharacterId : "npc_concierge";
            string name = _activeNpc != null ? _activeNpc.CharacterDisplayName : "Concierge Pierre";
            var role = _activeNpc != null && _activeNpc.Profile != null ? _activeNpc.Profile.Role : TomorrowsCrime.Procedural.NPCs.NPCRole.Concierge;

            string observation = GetCharacterObservation(id, name, role);
            _currentDialogueText = observation;

            InvestigationKnowledgeService.Instance?.RecordObservation(
                $"obs_lead_{name.Replace(" ", "_")}_{loop}_{simTime:F0}",
                $"LEAD: {name} observed: \"{observation}\"",
                loop,
                simTime,
                null,
                null,
                ObservationMethod.WitnessedEvent,
                name,
                "Witness Interrogation");

            UIManager.Instance?.ShowToast("LEAD ACQUIRED", $"Suspicious activity lead logged by {name}.", UITheme.PeacockTeal);
        }

        private static string GetCharacterObservation(string id, string name, TomorrowsCrime.Procedural.NPCs.NPCRole role)
        {
            switch (id)
            {
                case "npc_concierge_pierre":
                    return "Around 02:00, Manager Adler left his executive office in high agitation, clutching a leather ledger. And earlier, I noticed the brass tag for Suite 102 was missing from the key cabinet.";
                case "npc_receptionist_celeste":
                    return "Keep this between us, Detective: Vivian Monroe was lurking near the elevator shaft taking notes. And Dr. Sterling looked terribly pale when he crossed the lobby—his hands were trembling as he asked for the time.";
                case "npc_chef_marcel":
                    return "Otto, that greasy wrench-turner, was snooping near my kitchen service entrance smelling of coal smoke! And Madame Valerie complained that her champagne tasted 'bitter and metallic'—my cellar is spotless!";
                case "npc_maid_clara":
                    return "While dusting outside Suite 102, I smelled a strange scent—sweet French perfume mixed with something bitter, like crushed almonds. And the interconnecting door to Suite 104 had been unlocked from inside!";
                case "npc_engineer_otto":
                    return "The freight lift was called to Floor 1 at 02:40 without my authorization. I heard the cable drum whine from the boiler room. Someone was moving between floors without using the grand staircase.";
                case "npc_manager_adler":
                    return "Lord Harrington was in a foul temper earlier—I overheard him in the card room shouting that he would disinherit Dr. Sterling. And Miss Monroe has been attempting to bribe staff for guest files.";
                case "npc_lord_harrington":
                    return "My nephew Alistair—Dr. Sterling—came to my suite demanding an advance on his inheritance to satisfy reckless debts. When I refused, he stormed out looking murderous. The man has lost all restraint.";
                case "npc_madame_valerie":
                    return "Between us, chéri, Julian Adler has been sweating through his collars all week. I saw him stuffing thick envelopes into his coat. And Pierre has been having whispered meetings by the rear door.";
                case "npc_dr_sterling":
                    return "From a clinical perspective, several guests exhibited acute sympathetic nervous arousal tonight. Manager Adler's pupils were dilated and his carotid pulse was visibly elevated. Furthermore, I detected a faint odor of cyanide near the east corridor.";
                case "npc_journalist_monroe":
                    return "Get your pencil out, Detective: Chef Marcel was in a shouting match with Adler near the scullery about withheld salaries. And at 02:15, someone slipped through the service door without turning on lights!";
                default:
                    return "I observed hurried footsteps in the corridors shortly before midnight, and someone lingered in the shadows near the service stairs.";
            }
        }

        private void ConfrontWithClue(EvidenceDefinition clue)
        {
            _currentTopicTitle = $"CONFRONTATION: {clue.DisplayName.ToUpper()}";
            AudioManager.Instance?.PlayEvidenceDiscovery();

            string id = _activeNpc != null ? _activeNpc.CharacterId : "npc_concierge";
            string name = _activeNpc != null ? _activeNpc.CharacterDisplayName : "Concierge Pierre";
            var role = _activeNpc != null && _activeNpc.Profile != null ? _activeNpc.Profile.Role : TomorrowsCrime.Procedural.NPCs.NPCRole.Concierge;

            bool isCulprit = false;
            if (CaseAccusationService.Instance != null && CaseAccusationService.Instance.CurrentGroundTruth != null)
            {
                if (CaseAccusationService.Instance.CurrentGroundTruth.CulpritId == id) isCulprit = true;
            }
            if (_activeNpc != null && _activeNpc.Profile != null && _activeNpc.Profile.IsCulprit)
            {
                isCulprit = true;
            }

            string reaction = GetCharacterConfrontation(id, name, role, isCulprit, clue);
            _currentDialogueText = reaction;

            UIManager.Instance?.ShowToast("SUSPECT REACTION", $"{name} confronted with {clue.DisplayName}.", UITheme.StatusAmber);
        }

        private static string GetCharacterConfrontation(string id, string name, TomorrowsCrime.Procedural.NPCs.NPCRole role, bool isCulprit, EvidenceDefinition clue)
        {
            string clueName = clue != null ? (clue.DisplayName ?? "").ToLower() : "";
            string clueId = clue != null ? (clue.EvidenceId ?? "").ToLower() : "";

            if (clueName.Contains("poison") || clueName.Contains("vial") || clueId.Contains("poison") || clueName.Contains("cyanide"))
            {
                if (id == "npc_dr_sterling")
                    return "Let me examine that vial... Highly concentrated potassium cyanide in an almond vehicle. That is a lethal dose for three adult men. Such compounds require a licensed chemist's signature—or theft from a dispensary!";
                if (id == "npc_madame_valerie")
                    return "A vial smelling of bitter almonds?! Heavens, get it away from me! My champagne tasted strange earlier—thank God I poured most of it into the fern on the terrace!";
                if (id == "npc_chef_marcel")
                    return "Poison?! If anyone suggests that bitter almond filth came from my spice pantry, I will duel them! Bitter almond extract is kept under lock and key!";
                if (id == "npc_concierge_pierre")
                    return "Cyanide in an apothecary vial?! That did not come from my desk! Though... Madame Valerie did mention feeling unwell after her evening aperitif...";
                return $"A vial of lethal poison?! That is horrifying, Detective. Who would bring such deadly compounds into Meridian House?";
            }

            if (clueName.Contains("cleaver") || clueId.Contains("cleaver"))
            {
                if (id == "npc_chef_marcel")
                    return "Mon dieu! My 12-inch French meat cleaver! It vanished from my magnetic kitchen rack right after 10 o'clock! Tell me who touched my steel and I will skin them!";
                if (id == "npc_concierge_pierre")
                    return "A culinary meat cleaver belongs in Chef Marcel's scullery, not among guests. Marcel is notoriously possessive of his cutlery.";
                return "A heavy kitchen cleaver with dark stains?! That clearly came from the hotel kitchen. Chef Marcel must know who touched it!";
            }

            if (clueName.Contains("candlestick") || clueId.Contains("candlestick"))
            {
                if (id == "npc_lord_harrington")
                    return "A Georgian brass candlestick? Good God, is that one of the heavy pieces from the dining table? It looks dented... Do not wave that near me!";
                if (id == "npc_maid_clara")
                    return "Oh saints preserve us! That candlestick was on the dining sideboard earlier! Is... is that blood on the base?!";
                return "A heavy brass candlestick... The blunt impact from such an object would be devastating.";
            }

            if (clueName.Contains("wrench") || clueId.Contains("wrench"))
            {
                if (id == "npc_engineer_otto")
                    return "Ja, that's a 14-inch Stilsen wrench from my tool chest. What's it doing stained? Someone forced the padlock on my locker while I was at the boilers!";
                return "A heavy mechanic's pipe wrench... That clearly originated from the basement utility workshop.";
            }

            if (clueName.Contains("key") || clueId.Contains("key"))
            {
                if (id == "npc_concierge_pierre")
                    return "That is the master brass key! The tag was taken from my cubby board while I was inspecting the lobby! Whoever held this could enter any suite unhindered.";
                if (id == "npc_receptionist_celeste")
                    return "That key! I saw it hanging on the board before dinner, but by midnight the hook was empty!";
                return "A room keycard... That proves the killer did not need to force the lock to enter the crime scene.";
            }

            if (clueName.Contains("letter") || clueName.Contains("note") || clueName.Contains("blackmail") ||
                clueName.Contains("register") || clueName.Contains("will") || clueId.Contains("letter") || clueId.Contains("register"))
            {
                if (id == "npc_manager_adler")
                    return "Where did you get those documents?! That is private hotel business! You have no legal authorization to examine confidential accounting records!";
                if (id == "npc_madame_valerie")
                    return "Mon dieu... That note! Someone was demanding five hundred pounds or threatened to ruin my reputation before Paris society!";
                if (id == "npc_lord_harrington")
                    return "My confidential testamentary draft! Yes, I intended to strike Alistair from my estate! That is my sovereign right as head of the family!";
                if (id == "npc_journalist_monroe")
                    return "Bingo! That's the smoking gun! That typing matches the ribbon ink on the manager's office typewriter—I'd bet my front-page byline on it!";
                return "This document proves premeditation and a clear motive. The paper trail doesn't lie, Detective.";
            }

            if (isCulprit)
            {
                return $"Where did you uncover that?! I... I assure you that has a completely innocent explanation, Detective! Do not jump to baseless accusations!";
            }

            return $"Where did you find {clue.DisplayName}? That certainly looks like evidence of foul play, Detective. You should examine who had access to it.";
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
