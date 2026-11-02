# Cluedo-Unity: Tomorrow's Crime

A first-person 3D time-loop detective mystery built in **Unity 6 (LTS)** using the **Universal Render Pipeline (URP)** and C#.

---

## 1. Game Concept

At midnight, a murder occurs inside **Meridian House**, a compact luxury hotel. A five-minute window of events repeats indefinitely. While the building and its inhabitants reset to their exact initial conditions every five minutes, the player retains accumulated investigative knowledge across loops.

Rather than relying on scripted quest branches, *Tomorrow's Crime* is built around a **deterministic causal simulation**:
- **Observe Routines**: Watch suspect paths, conversations, and habits.
- **Gather Evidence**: Discover physical clues, time stamps, and physical anomalies.
- **Intervene & Manipulate**: Alter environmental conditions (doors, power breakers, keycards, distractions) to change what suspects do.
- **Uncover Causal Chains**: Learn why an event occurred—or why an expected event failed to happen.
- **Form & Verify Hypotheses**: Deduce motive, means, and opportunity through verifiable logic.

---

## 2. Technical Stack & Requirements

- **Engine:** Unity 6 LTS (`6000.6.7f1` or later)
- **Render Pipeline:** Universal Render Pipeline (URP `17.6.0`)
- **Scripting Runtime:** .NET 8 / C# 9+
- **Input:** Unity Input System (`1.20.1`)
- **Testing:** Unity Test Framework (`1.8.0` NUnit EditMode)
- **Platform:** PC / Standalone Windows (64-bit)

---

## 3. The Interactive Causal Scenario (Milestone 2 & 3)

The `Prototype.unity` scene features a complete end-to-end causal simulation driven by the data-driven **Causal Event Engine**:
1. **The Object:** A golden Room 102 Keycard (`key_room_102`) rests on the table in the center of the lobby.
2. **The Scheduled Sequence:**
   - **Event 1 (`concierge_retrieves_key` at 00:30):** The concierge attempts to retrieve the key from the lobby table.
     - *Precondition:* Key location state must equal `PropLocationState.AtBaseline`.
   - **Event 2 (`concierge_unlocks_room102` at 00:35):** The concierge unlocks and opens Room 102 (`door_room_102`).
     - *Prerequisite:* Event 1 must have executed successfully (`PrerequisiteFailurePolicy.Skip`).
     - *Effect:* Updates world state to `DoorLockState.Unlocked` and `DoorOpenState.Open`.
3. **The Baseline Loop:** If the player leaves the keycard on the table, Event 1 succeeds at `00:30`, followed by Event 2 succeeding at `00:35`. Door 102 unlocks and opens automatically.
4. **The Intervened Loop:** If the player approaches the table and presses **`E`** to pick up the keycard before `00:30`:
   - At `00:30`, Event 1 evaluates its precondition, finds the key `HeldByPlayer`, and fails with the reason:
     `Key 'key_room_102' was expected to be AtBaseline, but was HeldByPlayer.`
   - At `00:35`, Event 2 evaluates its prerequisite policy, detects Event 1 did not execute successfully, and is automatically skipped.
5. **Persistent Knowledge:** Witnessing this failure permanently records the discovery:
   `[key_missing_at_table]: The concierge could not unlock Room 102 because the key was missing from the table.`
6. **Deterministic Reset:** When the loop reaches 5 minutes (or on pressing `R`), all world state and event instances reset to baseline:
   - The keycard detaches from the player and returns to the table.
   - Door 102 closes and locks.
   - All runtime event instances return to `Pending` status.
   - The event log clears for the new loop.
   - **Crucially: The player's persistent discoveries remain intact across all loops!**

## 4. Suspect Simulation & Navigation (Milestone 4)

The prototype features the first autonomous suspect simulation with Unity AI Navigation:
1. **Character Definition (`NPCDefinition`):** Immutable data asset specifying character identity (`npc_concierge`, "Concierge Pierre"), locomotion speed (3.0 m/s), stopping distance (0.8m), perception cone (8.0m view range, 90° FOV), and scheduled activities.
2. **Simulation-Time Schedule:**
   - `00:00 - 00:15`: `WaitAtPost` — Concierge waits at the reception desk (`Waypoint_LobbyDesk`).
   - `00:15 - 00:30`: `RetrieveKey` — Concierge navigates across the lobby floor using `NavMeshAgent` toward the lobby table (`Waypoint_LobbyTable`).
   - `00:30 - 00:45`: `UnlockDoor` — If key was acquired, walks toward Room 102 (`Waypoint_Room102Door`) and unlocks the door.
   - `00:45 - 05:00`: `WaitAtPost` — Concierge stations at Room 102.
3. **Reactive Behaviors:**
   - **Reaction A (Missing Key):** When reaching the table at ~00:30, if the player moved or picked up the keycard, the concierge notices the absence, enters the `Investigating` behavior state, logs reason: `"Investigating: expected keycard is missing from lobby table!"`, and does NOT attempt to unlock Room 102.
   - **Reaction B (Blocked Route):** If the path to a destination is blocked or unreachable, the NPC transitions to `Blocked` navigation status, avoids infinite path spamming, and enters a clear waiting state.
4. **Basic Sensory Model (`NPCPerception`):** NPCs only perceive information within their sensory cone (distance, horizontal angle, and line-of-sight raycasts). If the player takes the key behind the concierge's back, the NPC does not magically know until reaching the table.
5. **Deterministic Reset:** Upon loop reset, the `NavMeshAgent` is warped back to the baseline desk position, paths are cleared, behavior state resets to `Idle`, and transient awareness is wiped clean.

## 5. Investigation System & Persistent Notebook (Milestone 5)

Step 5 implements the core detective loop, adhering to the foundational rule: **Simulation Knowledge != Player Knowledge**.

### Core Architecture & Features:
1. **Separation of World Truth & Detective Knowledge:**
   - The simulation engine tracks all world state and NPC activities. The player's persistent notebook ONLY records what the player has directly inspected or personally witnessed.
   - Unobserved events (e.g. an NPC's internal routine when the player is absent) never contaminate the player's timeline.
2. **Three-Tab Detective Notebook (`Tab` or `N`):**
   - **Evidence:** Displays all discovered clues (`Room 102 Keycard`, `Concierge Duty Memo`, `Room 102 Door & Lock`). Selecting an item reveals its category, detailed inspection notes, and linked events.
   - **Timeline:** Displays chronological player-witnessed observations (e.g., witnessing the concierge finding the key missing at 00:30). Allows comparing loop-over-loop deviations.
   - **Hypotheses & Notes:** Formulate theories by entering titles and explanations, with optional supporting evidence selection. Hypotheses can be created, edited, and deleted. Freeform general notes persist automatically across loops.
3. **Live Simulation & Input Blocking:**
   - Opening the notebook (`Tab`/`N`) disables player movement and mouse look, unlocking the cursor for interactive note-taking.
   - The live hotel simulation continues running uninterrupted in the background while the notebook is open. Closing the notebook restores first-person control and locks the cursor.
4. **Cross-Loop Persistence:**
   - When a loop ends (at 05:00 or via `R`), all world objects, NPC positions, and event instances reset to baseline.
   - All discovered evidence, observation entries, hypotheses, and notes are preserved in `InvestigationKnowledgeService`. Discovering additional clues in subsequent loops seamlessly adds to the notebook without duplicating or overwriting previous findings.

---

## 6. Major UI Overhaul, Investigation 2.0 & Cross-Loop Deduction (Milestone 6)

Milestone 6 transforms the prototype from a developer graybox into a cohesive, atmospheric 1920s-tinged modern detective experience with a complete UI design system and an advanced cross-loop deduction workspace.

### Core Features & Systems:
1. **Unified Visual Identity & Styling (`UITheme`):**
   - Professional detective color palette: Deep Charcoal (`#17191C`), Dark Slate (`#23272C`), Aged Warm Paper (`#E9E2D5`), Polished Brass (`#B69A61`), Verdigris Teal (`#6D9C98`), Warm Amber (`#D99A4A`), and Evidence Crimson (`#B85C58`).
   - Reusable procedural texture generation, styled card panels, rounded pill badges, tab bars, and typography styles.
2. **First-Person In-Game HUD (`GameHUDView`):**
   - **Time-Loop Clock & Progress Bar:** Displays current loop index and countdown timer (`MM:SS`) with a normalized fill bar.
   - **Dynamic Urgency Tints:** Subdued brass during standard loop time, warm amber below 60 seconds, and pulsing red under 15 seconds.
   - **Contextual Reticle & Prompt:** Dot reticle dynamically morphs into an action prompt badge (`[E] Inspect Memo`, `[E] Pick up Keycard`) when gazing at interactables within range.
   - **Discovery Toast Notification System:** Slide-in alert cards for newly discovered clues and witnessed observations.
   - **Investigation Pill Indicator:** Quick-access notebook status widget displaying active clue and observation counts.
3. **Dedicated Evidence Inspection Modal (`EvidenceInspectionModalView`):**
   - Dedicated warm paper reading view opened immediately upon inspecting documents or physical clues.
   - Shows clue category, display name, full narrative examination text, and related causal event tags.
4. **Investigation Workspace 2.0 (`InvestigationWorkspaceView`):**
   - **3-Column Detective Workspace:** Fast section tabs (`Evidence Library`, `Timeline & Comparison`, `Deduction Board`, `Hypotheses`, `Field Notes`).
   - **Evidence Library & Details Panel:** Category filters, clue counts, inspection summaries, and relationship connections.
   - **Loop Timeline & Cross-Loop Comparison:** Chronological event timeline recording loop index, timestamp, observation method (`PersonallyWitnessed`, `PhysicalInspection`, `HeardNoise`), and context. Includes comparison mode (`CompareLoops(loopA, loopB)`) highlighting interventions and deviations between runs.
   - **Interactive Deduction Board:** 2D evidence workspace allowing detectives to position clue cards, attach notes, and create typed semantic relationships (`Supports`, `Contradicts`, `RelatedTo`, `OccurredBefore`, `OccurredAfter`) with explanations. Prevents connections to undiscovered evidence.
   - **Hypothesis Manager:** Create, edit, and validate deduction theories with attached supporting/contradicting evidence.
   - **Field Notes:** Persistent freeform markdown-style scratchpad.
5. **Pause Menu, Main Menu & Game Settings (`GameSettingsManager`):**
   - Centralized modal coordinator (`UIManager`) managing focus, input blocking, and transition states.
   - In-game Pause Menu (`Esc` or `P`) allowing loop restart, settings configuration, or return to title.
   - Persistent user settings via `PlayerPrefs`: Master Volume, Mouse Sensitivity (hooked to `PlayerController`), Graphics Quality, Display Mode (Fullscreen/Windowed), and HUD Scale.
6. **Non-Blocking Simulation vs Pause Policy:**
   - **Investigation Workspace & Clue Inspection:** The 5-minute Meridian House simulation continues ticking in the background, creating authentic tension while reviewing clues.
   - **Pause Menu & Settings:** Explicitly halts simulation time (`SimulationManager.Instance.IsPaused = true`).
7. **Developer Diagnostics (`SimulationDebugUI`):**
   - The developer HUD is now hidden by default (`showUI = false`). It can be toggled on demand via `F1` or `` ` `` for debugging and is automatically suppressed while UI modals are open.

---

## 7. Modern Cluedo-Inspired Visual Reboot (Milestone 7)

Milestone 7 replaces the game's visual presentation from the ground up, establishing an opulent, theatrical aesthetic inspired by the modern reimagined *CLUEDO* board game (Hasbro 2023) and its official digital adaptation (Marmalade Game Studio 2023).

### Key Visual Reboot Pillars:
1. **Stylized 3D Character Presentation (`NPC_Concierge_Pierre.obj`)**:
   - 1.82m stylized human concierge model with strong silhouette, sculpted facial planes, side-parted styled hair, high stiff white collar, and burgundy silk cravat.
   - Double-breasted royal navy livery coat with dual brass button rows, gold epaulets, burgundy pocket square, charcoal pressed trousers, and black Oxford shoes.
   - Fully integrated with Unity `NavMeshAgent` and patrol schedule.
2. **Architectural Meridian House Lobby (`Prototype.unity`)**:
   - **Flooring:** High-contrast emerald green and ivory checker marble bordered by gold brass inlays, bisected by a plush bordeaux velvet runner.
   - **Walls:** Two-tier architectural walls with dark mahogany wainscoting, brass dado rails, and emerald damask wallpaper with gold fan arches.
   - **Architectural Details:** Fluted architectural columns with Corinthian brass capitals and a grand 6-arm Art Deco brass chandelier with warm tungsten illumination (`2700K`).
   - **Furniture & Props:** Curved fluted mahogany reception desk with rear key-cubby rack, glowing emerald glass banker's desk lamp, round burled walnut center table with stamped brass Room 102 keycard, and bordeaux velvet armchairs.
   - **Room 102 Entrance:** Heavy paneled mahogany double door with raised moldings, brass kickplate, brass lever handle, flanking geometric wall sconces, and electronic card reader with crimson indicator LED.
3. **Theatrical Case-Dossier Interface**:
   - **Visual Tokens (`UITheme`):** Jewel-tone palette featuring Meridian Midnight (`#0D1B2A`), Polished Brass (`#D4AF37`), Bordeaux Crimson (`#6B1D2F`), Peacock Teal (`#1D4E5B`), Emerald Green (`#1B4332`), and Aged Parchment (`#F3EBDD`).
   - **Main Menu:** Theatrical centered title card with gold wax seal monogram, serif typography, and luxury interactive buttons.
   - **In-Game HUD:** Antique pocket watch chronometer, animated diamond focus reticle, wax seal perception toasts, and leather dossier access pill.
   - **Evidence Reading Modal:** Archival parchment card with category wax stamp, quotation callout, narrative analysis, and causal event links.
   - **Investigation Workspace:** 3-column confidential dossier layout with Roman numeral tabs, gold metrics, and category filters.
4. **Verified High-Resolution Screenshots (`Docs/Screenshots/`)**:
   - `Shot_01_Lobby_Overview.png`: Grand architectural lobby overview.
   - `Shot_02_Concierge_Pierre_Character.png`: Portrait of Concierge Pierre.
   - `Shot_03_Reception_Desk_BankerLamp.png`: Reception desk and glowing emerald banker's lamp.
   - `Shot_04_Table_And_Keycard.png`: Central walnut table with stamped gold keycard.
   - `Shot_05_Room102_Entrance.png`: Room 102 double door, sconces, and card reader.
   - `Shot_06_Main_Menu_View.png`: Theatrical main menu with gold monogram seal.
   - `Shot_07_InGame_HUD_View.png`: In-game HUD with pocket chronometer and toast alert.
   - `Shot_08_Case_Dossier_Workspace.png`: 3-column case dossier workspace.
   - `Shot_09_Evidence_Inspection_Modal.png`: Archival evidence inspection card.
5. **Art Bible Reference:**
   - Detailed art direction principles, reference studies, and specifications are documented in [`Docs/ART_DIRECTION.md`](Docs/ART_DIRECTION.md).

---

## 8. The Living Hotel: Procedural World, NPC Ecosystem & Replayable Mystery Generation (Milestone 8)

Milestone 8 transforms *Tomorrow's Crime* into a replayable detective simulation where every new investigation produces a distinct, coherent, and provably solvable murder mystery within an expanded multi-floor environment.

### Core Procedural Architecture & Systems:
1. **Multi-Floor Hotel Layout (`HotelLayoutTemplate.cs`, `HotelBuilder.cs`)**:
   - 12 distinct rooms across 2 floors with explicit 3D bounds and typed placement anchors:
     - **Ground Floor:** Grand Lobby (`room_lobby`), Front Reception (`room_reception`), Dining Hall (`room_dining`), Hotel Kitchen (`room_kitchen`), East Service Corridor & Staircase (`room_corridor_ground`), Manager's Office (`room_staff_office`), Maintenance & Utility Room (`room_utility`).
     - **Upper Floor:** Second Floor Gallery & Landing (`room_upper_corridor`), Suite 101 (`room_guest_101`), VIP Suite 102 (`room_guest_102`), Guest Room 103 (`room_guest_103`), Housekeeping Storage (`room_housekeeping`).
   - Central 16-step mahogany staircase with smooth ramp collider for seamless navigation between floors.
   - Solid floor slabs, ceiling slabs, and perimeter walls with framed doorway openings.
2. **Physical Boundary & Collision Hardening (`WorldCollisionAuditor.cs`)**:
   - Corrected `CharacterController` geometry: center `(0, 1.0, 0)`, height `2.0m`, step offset `0.35m`, slope limit `45°`, skin width `0.08m`.
   - Added solid box colliders to lobby pillars and all environmental props.
   - Comprehensive audit utility validating solid boundaries across all rooms, floors, ceilings, and doors.
3. **Room-Aware Object Placement (`RoomObjectPlacer.cs`)**:
   - 14+ contextual item catalog across 7 categories (`Key`, `Utensil`, `Valuable`, `Container`, `Tool`, `Document`, `Clutter`).
   - Mandatory scenario evidence placed first at eligible anchors (weapon in culprit's domain, key in lobby, blackmail letter at crime scene).
   - Remaining anchors populated with ambient clutter using a 40% empty search probability for authentic detective investigation.
4. **NPC Ecosystem & Simulation-Driven Needs (`NPCPopulationGenerator.cs`, `NPCNeedsModel.cs`)**:
   - 10-person character roster: Pierre (Concierge), Celeste (Receptionist), Marcel (Chef), Clara (Maid), Otto (Engineer), Adler (Manager), Lord Harrington (101), Madame Valerie (102), Dr. Sterling (103), Vivian Monroe (Journalist).
   - Dynamic simulation-time needs model (`Energy`, `Hunger`, `Stress`, `Social`).
   - 300-second scheduled routine plans with activities, destinations, and station requirements across rooms.
5. **Replayable Murder Mystery Generation & Solvability Validator (`CrimeScenarioGenerator.cs`, `MysterySolvabilityValidator.cs`)**:
   - Generates internal Ground Truth: victim, culprit, motive, murder method, weapon, crime scene, murder timestamp, causal timeline, and mandatory evidence threads.
   - Bounded solver evaluating 8 foundational solvability criteria before allowing an investigation to start.
   - Causal event compilation via `ScenarioEventBridge` registering timeline steps with `CausalEventEngine`.
6. **Reproducible Randomness & Domain Streams (`SeededRandom.cs`, `InvestigationSeed.cs`)**:
   - Deterministic SplitMix64 / Xoshiro256** PRNG with isolated domain streams (`Layout`, `NPCs`, `Objects`, `Scenario`, `Cosmetics`).
   - **New Investigation:** Generates new seed, world configuration, and mystery; resets player knowledge.
   - **Time-Loop Reset:** Reuses current seed and ground truth; preserves player discoveries, hypotheses, and timeline.
7. **Developer Generation Inspector (`GenerationInspectorUI.cs`)**:
   - Toggled via **`F2`**.
   - Displays seed code, Ground Truth, NPC ecosystem, hotel layout, object placements, and 8-criteria validation diagnostics.
   - Interactive controls: Load Seed, New Random Seed, Replay Investigation.

---

## 9. Controls

- **Move:** `W`, `A`, `S`, `D`
- **Look:** Mouse (Sensitivity configurable in Settings)
- **Sprint:** Hold `Left Shift`
- **Interact / Inspect / Pick Up / Place:** `E` (Aim at objects when prompt appears)
- **Investigation Workspace 2.0:** `Tab` or `N`
- **Pause Menu / Dismiss Modal:** `Escape` or `P`
- **Manual Loop Reset:** `R`
- **Toggle Developer Diagnostic HUD:** `F1` or `` ` `` (Suppressed while modals are active)
- **Toggle Developer Generation Inspector:** `F2` (Inspect seed, ground truth, and NPC routines)
- **Time Acceleration (Debug):** `1` (1x), `2` (2x), `5` (5x) (Active when Diagnostic HUD is enabled)

---

## 10. Getting Started & Running in Unity

1. Open **Unity Hub**.
2. Click **Add** -> **Add project from disk**.
3. Select `TomorrowsCrime` located at `G:\Unity-Game\TomorrowsCrime`.
4. Ensure the selected Editor version is **Unity 6000.6.7f1** (or matching Unity 6 LTS).
5. Open the project.
6. In the Project window, navigate to `Assets/_Project/Scenes/` and double-click `Prototype.unity`.
7. **(Optional) Re-baking NavMesh & Scene:** Execute the menu item `Tomorrow's Crime -> Build Prototype Scene` to automatically configure lighting, materials, and bake navigation geometry.
8. Press the **Play** button in the Unity toolbar.

---

## 11. Automated Test Suite

The project includes an EditMode test suite with 100% pass rate:
- **Total Tests:** 87
- **Passed:** 87 / 87
- **Failed:** 0
- **Suites:**
  - `ProceduralGenerationTests` (16 tests, including 100-seed automated batch stress test)
  - `InvestigationSystem2Tests` (14 tests)
  - `InvestigationSystemTests` (14 tests)
  - `CausalEventEngineTests` (15 tests)
  - `NPCSimulationTests` (12 tests)
  - `WorldStateAndScenarioTests` (10 tests)
  - `SimulationClockTests` (6 tests)
- **Suites:**
