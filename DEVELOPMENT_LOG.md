# Development Log — Tomorrow's Crime

This document tracks dated, factual development milestones, technical design choices, test validation results, and active technical considerations.

---

## [2026-10-24] Milestone 0: Environment Discovery & Clean Project Creation

### Verified Environment
- **Operating System:** Windows 11 Enterprise (10.0.26200, 64-bit)
- **Git Version:** 2.40.0.windows.1
- **.NET SDK:** 8.0.426
- **Unity Editor:** Unity 6 LTS (`6000.6.7f1`) at `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`
- **Project Root:** `G:\Unity-Game\TomorrowsCrime`

### Technical Actions
1. Discovered existing installed Unity 6 Editor and local environment tooling.
2. Initialized empty Unity 6 project `TomorrowsCrime` in batch mode from clean directory.
3. Configured package dependencies in `Packages/manifest.json`:
   - `com.unity.render-pipelines.universal` (17.6.0)
   - `com.unity.inputsystem` (1.20.1 — verified compatible with Unity 6000.6 TreeView refactor)
   - `com.unity.test-framework` (1.8.0)
   - `com.unity.ugui` (2.6.0)
   - `com.unity.textmeshpro` (5.0.0)
4. Added comprehensive `.gitignore` tailored for Unity 6, ignoring `Library/`, `Temp/`, `Logs/`, and user settings while preserving versioned project settings, manifests, and assets.
5. Established organized architecture folders under `Assets/_Project/`.

---

## [2026-10-24] Milestone 1: Simulation Clock, Player Controller & Graybox Prototype

### Architectural Decisions
1. **Simulation Clock Model Isolation**: The simulation clock core logic (`SimulationClockModel`) is designed as a standalone, deterministic C# class without dependencies on `UnityEngine.MonoBehaviour`. This enables millisecond-accurate EditMode unit testing with zero scene setup.
2. **Deterministic Reset Contract (`ILoopResetReceiver`)**: Created a clean interface implemented by all dynamic entities (Player, World State, Clock). Upon loop reset (after 300 seconds or via debug trigger), reset subscribers return to their initial state without requiring scene reload.
3. **Fail-Safe Developer HUD (`SimulationDebugUI`)**: Implemented an OnGUI-based developer HUD showing loop index, current time (MM:SS), time-scale multipliers, and pause state. This eliminates runtime canvas serialization bugs during early prototyping while providing immediate diagnostic feedback.
4. **Assembly Definitions (`.asmdef`)**: Created `TomorrowsCrime.Runtime` and `TomorrowsCrime.Tests.EditMode` to isolate compilation units, guarantee rapid hot-reloading, and enforce clean architectural boundaries.

### Validation & Verification
- EditMode tests verified for `SimulationClockModel` (clock tick progression, pause behavior, clamp at 300 seconds, loop increment, time scaling).
- Compilation clean in Unity Editor.


---

## [2026-10-25] Milestone 2: World State & Interactive Causal Scenario

### Architectural Decisions
1. **Deterministic World State Architecture (`WorldStateRegistry` & `WorldStateManager`)**:
   - Implemented a pure, decoupled `WorldStateRegistry` that stores mutable properties (doors, lights, props, key availability) under stable string identifiers (`key_room_102`, `door_room_102`).
   - Registration explicitly detects duplicate IDs (throws `InvalidOperationException`) and guards against uninitialized access, preventing silent bugs.
   - Captures baseline state on registration; on loop reset (`ILoopResetReceiver`), `ResetAllToBaseline()` resets all registered loop-scoped entities without reloading scenes.
2. **First-Person Interaction System (`IInteractable` & `PlayerInteraction`)**:
   - Created `IInteractable` interface with prompt and execution contracts.
   - Added `PlayerInteraction` component to the player rig with a 2.5m raycast forward from camera center, debounced input polling (`KeyCode.E`), and contextual prompt rendering.
3. **Causal Key Scenario (`KeyScenarioController`)**:
   - Implemented scheduled scenario at `00:30` (30 seconds into loop): Concierge NPC attempts to take Room 102 keycard and unlock Room 102 door.
   - Pure evaluation function `EvaluatePreconditions` tests actual world state (`PropLocationState.AtBaseline` vs `HeldByPlayer` / `DroppedElsewhere`).
   - If key is present: Door 102 unlocks and swings open.
   - If key is missing/relocated by player: Event fails with explicit report: `"Event failed: required key was not available at the expected location."`
4. **Cross-Loop Persistent Knowledge (`InvestigationKnowledgeService`)**:
   - Detective memory service that explicitly survives loop turnovers.
   - When the player witnesses the scenario fail at 00:30, it permanently records discovery `key_missing_at_table`. Re-recording the same discovery is deduplicated.
5. **Observability & Diagnostics**:
   - Extended `SimulationDebugUI` to render real-time key status, door lock state, scenario status & pass/fail reason, persistent discovery list, and screen-center interaction reticle/prompt.

### Validation & Test Results
- Ran complete Unity Test Framework EditMode suite via CLI:
  - 16 total test cases executed.
  - 16 passed, 0 failed, 0 inconclusive.
  - 10 new tests in `WorldStateAndScenarioTests` covering baseline capture, state mutation, reset restoration, persistent knowledge survival, duplicate ID handling, and causal scenario evaluation.
- Verified scene construction in `Assets/_Project/Scenes/Prototype.unity` with interactive key, destination door, concierge placeholder, table, and simulation systems.

---

## [2026-10-25] Milestone 3: Causal Event Engine

### Architectural Decisions
1. **Data-Driven Event Definitions (`EventDefinition` & `EventRuntimeInstance`)**:
   - Implemented `EventDefinition` as a `ScriptableObject`-based immutable event descriptor (ID, name, scheduled time, priority, failure policy, prerequisites, preconditions, effects).
   - Separated runtime execution state into `EventRuntimeInstance` (`Pending`, `Executing`, `Executed`, `Failed`, `Cancelled`, `Skipped`).
   - Ensures definitions are completely reusable across loops without state corruption.
2. **Deterministic Causal Event Engine (`CausalEventEngine`)**:
   - Pure C# simulation engine decoupled from `MonoBehaviour` for testability and performance.
   - **Deterministic Sorting:** Events eligible at the current tick are ordered strictly by scheduled time ascending, priority descending, and event ID ordinal.
   - **Graph Validation & Cycle Detection:** Validates prerequisite IDs against registered definitions and executes DFS with 3-color cycle detection to prevent deadlocks.
   - **Multi-Pass Resolution:** Uses a fixed-point loop within each tick so dependent events scheduled at or before the current timestamp resolve immediately once prerequisite events complete in the same tick.
   - **Failure Policy Engine:** When a prerequisite reaches an unsuccessful terminal state, dependent events transition according to configured policy (`Skip`, `Fail`, `Cancel`) with explanatory logging.
   - **Preconditions and Effect Safety:** Evaluates all `IEventPrecondition` contracts before invoking any `IEventEffect`. If a precondition fails or an effect throws/fails, state transitions to `Failed` without partial side effects.
   - **Chronological History & Loop Resets:** Records immutable `EventExecutionRecord` instances per processed event. Resets all runtime instances to `Pending` and clears history upon loop reset (`ILoopResetReceiver`).
3. **Refactored Scenario Controller (`KeyScenarioController`)**:
   - Transformed the concierge scenario from hardcoded time checks into two distinct causal events:
     - `concierge_retrieves_key` (scheduled at 30.0s, priority 10): Checks `KeyLocationPrecondition`.
     - `concierge_unlocks_room102` (scheduled at 35.0s, priority 5): Depends on `concierge_retrieves_key`; applies `DoorStateEffect`.
   - Preserves player witnessing logic: if the player witnesses `concierge_retrieves_key` fail, `InvestigationKnowledgeService` logs `key_missing_at_table`.
4. **Enhanced Diagnostic UI (`SimulationDebugUI`)**:
   - Replaced static scenario status with a scrollable chronological event history list.
   - Shows event timestamps, event IDs, colored status badges (`Executed` = green, `Failed` = red, `Skipped` = orange), and result explanations.

### Validation & Test Results
- Ran complete Unity Test Framework EditMode suite via CLI:
  - 31 total test cases executed.
  - 31 passed, 0 failed, 0 inconclusive (100% pass rate).
  - All 15 required causal event engine test cases verified in `CausalEventEngineTests`:
    - Requirement 1: Event executes when time reached and preconditions pass.
    - Requirement 2: Event remains pending before scheduled time.
    - Requirement 3: Event fails with explanation when precondition false.
    - Requirement 4: Event executes at most once per loop.
    - Requirement 5: Events with same timestamp follow deterministic ordering.
    - Requirement 6: Event does not execute before pending prerequisite succeeds.
    - Requirement 7: Terminal unsuccessful prerequisite triggers configured dependency policy (`Skip`, `Fail`).
    - Requirement 8: Failed event does not partially apply required effects.
    - Requirement 9: Invalid event references are reported.
    - Requirement 10: Dependency cycles are detected.
    - Requirement 11: Loop reset restores event runtime state.
    - Requirement 12: Events execute correctly again in new loop.
    - Requirement 13: Moving key changes expected event outcome.
    - Requirement 14: Large clock advance does not miss eligible events.
    - Requirement 15: Exact loop boundary behavior matches documented policy.
  - All 16 previous tests in `SimulationClockModelTests` and `WorldStateAndScenarioTests` continue to pass.
- Verified scene execution and component integration in `Prototype.unity`.

---

## [2026-10-25] Milestone 4: Suspect Simulation & Navigation

### Architectural Decisions
1. **Separation of Character Definition and Runtime State (`NPCDefinition` & `NPCRuntimeState`)**:
   - Implemented `NPCDefinition` as a `ScriptableObject` containing static configuration: identity (`npc_concierge`, "Concierge Pierre"), locomotion settings (speed 3.0 m/s, stopping distance 0.8m), perception parameters (view range 8.0m, FOV 90°), and scheduled activities.
   - Isolated ephemeral execution state in `NPCRuntimeState` (`BehaviorState`, `NavigationStatus`, `CurrentActivity`, `HasNoticedMissingKey`, `LastDecisionReason`, `LoopIndex`).
2. **Pure C# Deterministic Schedule Evaluator (`NPCScheduleEvaluator`)**:
   - Decoupled from `MonoBehaviour` for 100% testability in EditMode.
   - Evaluates simulation clock time across half-open intervals `[StartTime, EndTime)`.
   - Concierge 5-minute routine:
     - `00:00 - 00:15`: `WaitAtPost` at reception desk (`Waypoint_LobbyDesk`).
     - `00:15 - 00:30`: `RetrieveKey` — navigates across lobby toward table (`Waypoint_LobbyTable`).
     - `00:30 - 00:45`: `UnlockDoor` — navigates toward Room 102 (`Waypoint_Room102Door`) and opens door.
     - `00:45 - 05:00`: `WaitAtPost` — stations at Room 102.
3. **Unity AI Navigation (`com.unity.ai.navigation` 2.0.16)**:
   - Added `NavMeshSurface` to `[Environment_HotelLobby]` and baked walkable geometry taking into account lobby walls, pillars, reception counter, and table.
   - Equipped `NPC_Concierge` with `NavMeshAgent` and waypoint destination system.
4. **Reactive Behaviors**:
   - **Reaction A (Missing Key):** When arriving at the lobby table, if the key was moved or picked up by the player, the concierge enters `Investigating` behavior state, logs reason: `"Investigating: expected keycard is missing from lobby table!"`, and aborts unlocking Room 102.
   - **Reaction B (Blocked Route):** If a navigation path is invalid or partial, detects failure, enters `Blocked` state with recovery cooldown, avoiding infinite request loops.
5. **Basic Sensory Perception (`NPCPerception`)**:
   - Implemented distance, FOV angle, and physics raycast line-of-sight checks.
   - Ensures the NPC cannot perceive player actions occurring behind its back or through walls.
6. **Deterministic Loop Reset (`ILoopResetReceiver`)**:
   - Warps `NavMeshAgent` back to baseline station, resets paths and velocity, resets runtime state to `Idle`, and clears transient awareness without duplicate callback registrations.
7. **Observability**:
   - Extended `SimulationDebugUI` to render suspect ID, display name, behavior state, navigation status, active activity, and decision reasons.

### Validation & Test Results
- Ran complete Unity Test Framework EditMode suite via CLI:
  - 43 total test cases executed.
  - 43 passed, 0 failed, 0 inconclusive (100% pass rate).
  - All 12 required Step 4 test cases verified in `NPCSimulationTests`:
    - Requirement 1: Character starts in configured initial state.
    - Requirement 2: Schedule evaluation returns expected activity for given simulation time.
    - Requirement 3: Schedule boundaries follow documented policy `[StartTime, EndTime)`.
    - Requirement 4: Schedule uses simulation time rather than wall-clock time.
    - Requirement 5: Missing-key condition produces intended response (`Investigating`, aborted unlock).
    - Requirement 6: NPC does not gain knowledge of player actions it could not perceive.
    - Requirement 7: Unreachable destination produces defined blocked or recovery state.
    - Requirement 8: Reset restores initial NPC state and schedule progress.
    - Requirement 9: Reset clears stale navigation intentions.
    - Requirement 10: Repeated resets do not duplicate event callbacks.
    - Requirement 11: NPC actions integrate with causal event engine without duplicate execution.
    - Requirement 12: Identical initial conditions and action sequences produce identical decisions.
  - All 31 previous tests continue to pass without regression.
- Programmatically baked NavMesh and regenerated `Prototype.unity` via batchmode.

---

## [2026-10-25] Milestone 5: Investigation System & Persistent Notebook

### Architectural Decisions
1. **Core Principle: Simulation Knowledge != Player Knowledge**:
   - The causal event engine tracks internal simulation truth for determinism and debugging.
   - The player's persistent notebook only captures evidence directly inspected (`InspectableEvidence`) or events personally witnessed (`PlayerObservationEntry`).
   - Unobserved events (e.g. concierge routine when player is elsewhere) never enter player knowledge.
2. **Immutable Clue Definitions (`EvidenceDefinition`) & Runtime Records (`DiscoveredEvidenceRecord`)**:
   - `EvidenceDefinition`: Authored as `ScriptableObject` containing stable `evidenceId`, display name, category, short summary, detailed inspection text, and related event/evidence links.
   - `DiscoveredEvidenceRecord`: Pure runtime record capturing the `EvidenceDefinition`, the loop number (`DiscoveredInLoop`), and the exact simulation timestamp (`DiscoveredAtSimTime`).
3. **Player-Witnessed Observation Timeline (`PlayerObservationEntry`)**:
   - Captures factual, personally witnessed occurrences with loop number, timestamp, and optional causal event links.
   - Distinct from internal engine event logs; provides the player with an authentic cross-loop comparison record.
4. **Player Hypotheses & General Notes (`HypothesisEntry` & `InvestigationKnowledgeService`)**:
   - Formulates theories linking player-authored titles and explanations to discovered evidence IDs.
   - Full CRUD capability: create, edit, and delete hypotheses.
   - Dedicated freeform general notes field for player scratchpad observations.
5. **Interactive Three-Tab IMGUI Notebook (`NotebookUIController`)**:
   - Toggled via `Tab` or `N`.
   - Built using robust IMGUI architecture, eliminating UI canvas serialization issues while providing interactive tabs:
     - **Evidence**: List of discovered clues with category badges and full inspection detail panel.
     - **Timeline**: Chronological player-witnessed timeline across all loops.
     - **Hypotheses & Notes**: Interactive hypothesis editor with title/explanation inputs and supporting evidence checkboxes, plus persistent general notes.
6. **Input Blocking with Running Simulation**:
   - Added `IsInputBlocked` and `SetInputBlocked(bool)` to `PlayerController`.
   - When notebook is open, player movement and camera rotation are blocked, and cursor is unlocked (`CursorLockMode.None`, `Cursor.visible = true`).
   - The live hotel simulation continues running in the background. Closing the notebook restores first-person control and locks the cursor.
7. **Seamless Cross-Loop Persistence**:
   - When the 5-minute loop turns over or `R` is pressed, `WorldStateManager`, `NPCController`, and `CausalEventScheduler` reset the physical scene to baseline.
   - `InvestigationKnowledgeService` preserves all discovered evidence, observation history, hypotheses, and notes without duplicate subscriptions or static memory leaks.

### Validation & Test Results
- Ran complete Unity Test Framework EditMode suite via CLI:
  - 57 total test cases executed.
  - 57 passed, 0 failed, 0 inconclusive (100% pass rate).
  - All 14 required Step 5 test cases verified in `InvestigationSystemTests`:
    - Requirement 1: Inspecting valid target records evidence (`InspectableEvidence.Interact`).
    - Requirement 2: Repeated discovery does not duplicate evidence.
    - Requirement 3: Evidence cannot be discovered when prerequisites are unmet (`RequiresHoldingProp`).
    - Requirement 4: Undiscovered evidence is not exposed through player notebook data.
    - Requirement 5: Player observations recorded only through valid observation pathways.
    - Requirement 6: Unobserved simulation events do not enter player timeline.
    - Requirement 7: Hypotheses can be created, edited, and deleted.
    - Requirement 8: Notes and hypotheses survive loop reset.
    - Requirement 9: Discovered evidence survives loop reset.
    - Requirement 10: Previous valid observations remain available after reset.
    - Requirement 11: Loop-scoped world and NPC state still reset correctly.
    - Requirement 12: Resetting does not duplicate callbacks or subscriptions.
    - Requirement 13: Opening and closing notebook enables and disables player input.
    - Requirement 14: Discovering additional evidence in a later loop does not erase earlier discoveries.
  - All 43 previous tests continue to pass without regression:
    - `CausalEventEngineTests`: 15 / 15 passed
    - `NPCSimulationTests`: 12 / 12 passed
    - `SimulationClockTests`: 6 / 6 passed
    - `WorldStateAndScenarioTests`: 10 / 10 passed
- Rebuilt `Prototype.unity` via batchmode CLI with `Prop_Room102_Keycard`, `Prop_Reception_ScheduleMemo`, `Prop_Door102_CardReader`, and `NotebookUIController`.

---

## [2026-10-26] Milestone 6: Major UI Overhaul, Investigation 2.0 & Cross-Loop Deduction

### Architectural Decisions
1. **Design System & Visual Language (`UITheme` & `UIState`)**:
   - Designed a comprehensive, period-appropriate modern detective UI palette: Deep Charcoal (`#17191C`), Dark Slate (`#23272C`), Aged Warm Paper (`#E9E2D5`), Polished Brass (`#B69A61`), Verdigris Teal (`#6D9C98`), Warm Amber (`#D99A4A`), and Crimson (`#B85C58`).
   - Centralized styling tokens into `UITheme`, providing procedural solid textures, custom box/card styles, badges, buttons, sliders, and typography styles.
   - Structured screen states through `UIState`: `InGameHUD`, `InvestigationNotebook`, `EvidenceDetailModal`, `PauseMenu`, `SettingsMenu`, `MainMenu`, `LoopSummaryOverlay`.
2. **Central UI Coordinator (`UIManager`)**:
   - Manages modal transitions, back navigation stack, and single-point input arbitration (`IsModalOpen`, `SetUIState`).
   - Dispatches modal open/close events to lock/unlock mouse cursor (`Cursor.lockState`, `Cursor.visible`) and arbitrate player control.
   - Maintains an animated toast notification queue (`QueueToast`) for discovered evidence and witnessed observations.
   - Arbitrates simulation pausing: non-blocking for detective investigation/inspection, paused for pause menu and settings.
3. **In-Game First-Person HUD (`GameHUDView`)**:
   - Modernized status header with loop counter, countdown clock (`MM:SS`), and normalized loop progress bar.
   - Color shifts based on remaining loop time: Polished Brass (>60s), Amber warning (15–60s), and pulsing Crimson danger (<15s).
   - Contextual center reticle dynamically expanding into an interactive prompt badge when targeting inspectable objects within range.
   - Bottom-right investigation indicator pill displaying real-time discovered clue and observation counts with `Tab / N` prompt.
   - Diagnostic developer HUD (`SimulationDebugUI`) is now off by default and hidden behind `F1` / `` ` ``, automatically suppressed during active modals.
4. **Dedicated Evidence Reading View (`EvidenceInspectionModalView`)**:
   - Inspecting clues or documents in the world triggers a warm paper modal showing clue category badge, full examination text, discovery timestamp, and linked causal events.
   - Closes seamlessly via `Escape`, `E`, or the Return button, returning control to first-person view.
5. **Investigation Workspace 2.0 (`InvestigationWorkspaceView`)**:
   - 3-column layout featuring five workspace tabs:
     - **Evidence Library:** Category filters (`Physical`, `Document`, `Testimony`, `Environmental`), clue list with badges, and detailed inspector panel.
     - **Timeline & Loop Comparison:** Chronological player observations tagged with loop index, sim timestamp, observation method (`PersonallyWitnessed`, `PhysicalInspection`, `HeardNoise`), and context summary. Includes loop-over-loop comparison mode (`CompareLoops(loopA, loopB)`) highlighting interventions and anomalies between runs.
     - **Deduction Board:** 2D canvas workspace tracking coordinate nodes (`DeductionBoardNode`) with player notes, card dragging, and typed semantic relationships (`Supports`, `Contradicts`, `RelatedTo`, `OccurredBefore`, `OccurredAfter`) with explanations. Rejects relationships referencing undiscovered evidence.
     - **Hypotheses Manager:** Formulate, modify, and delete deduction theories with attached evidence links and validity checks.
     - **Field Notes:** Persistent freeform markdown-style scratchpad.
6. **Persistent User Settings (`GameSettingsManager`)**:
   - Manages Master Volume (0-1), Mouse Sensitivity (0.1-3.0), Graphics Quality Level (0-2), Display Mode (Fullscreen/Windowed), and HUD Scale (0.75-1.25).
   - Automatically loads on awake and persists to `PlayerPrefs` on change.
   - Integrated with `PlayerController.LookSensitivity` for immediate responsive feel.
7. **Pause Menu & Main Menu (`PauseMenuView`, `SettingsMenuView`, `MainMenuView`)**:
   - In-game Pause Menu (`Escape` or `P`) featuring Resume, Restart Loop, Settings, and Return to Menu options.
   - Settings menu with sliders and dropdown-style buttons.
8. **Scene Enhancement (`PrototypeSceneSetup`)**:
   - Upgraded lobby atmosphere in `Prototype.unity`: added warm ceiling pendant light (`Color(1.0f, 0.92f, 0.78f)`), charcoal carpet flooring, rich mahogany doors/desks, walnut tables, and warm plaster walls.
   - Added `[UserInterface]` hierarchy containing `UIManager`, `GameHUDView`, `EvidenceInspectionModalView`, `InvestigationWorkspaceView`, `PauseMenuView`, `SettingsMenuView`, `MainMenuView`, and `GameSettingsManager`.

### Validation & Test Results
- Authored 14 new comprehensive unit tests in `Assets/_Project/Tests/EditMode/InvestigationSystem2Tests.cs` validating all Step 6 Part J Section 11 requirements:
  - Requirement 1: UI coordinator correctly manages active screen states.
  - Requirement 2: Opening modal disables player look and movement input.
  - Requirement 3: Closing modal restores player movement and look input.
  - Requirement 4: Modal visibility correctly pauses or continues simulation per policy.
  - Requirement 5: Evidence inspection modal displays correct metadata and full inspection text.
  - Requirement 6: Timeline view organizes observations chronologically.
  - Requirement 7: Cross-loop comparison isolates and compares observations between distinct loops.
  - Requirement 8: Creating relationship between discovered evidence succeeds and persists.
  - Requirement 9: Creating relationship referencing undiscovered evidence is rejected.
  - Requirement 10: Deduction board node positions and notes persist.
  - Requirement 11: Notification toasts queue and trigger on new evidence discovery.
  - Requirement 12: Notification toasts queue and trigger on new player observation.
  - Requirement 13: Settings manager modifies and persists player preferences.
  - Requirement 14: Settings changes immediately update dependent gameplay systems (mouse sensitivity).
- Ran complete Unity Test Framework EditMode suite via CLI:
  - 71 total test cases executed.
  - 71 passed, 0 failed, 0 inconclusive (100% pass rate).
  - All test suites passing:
    - `InvestigationSystem2Tests`: 14 / 14 passed
    - `InvestigationSystemTests`: 14 / 14 passed
    - `CausalEventEngineTests`: 15 / 15 passed
    - `NPCSimulationTests`: 12 / 12 passed
    - `WorldStateAndScenarioTests`: 10 / 10 passed
    - `SimulationClockTests`: 6 / 6 passed
- Executed `Tomorrow's Crime -> Build Prototype Scene` batchmode rebuild and confirmed clean scene serialization and NavMesh baking.

---

## [2026-10-26] Milestone 7: Complete Visual Reboot (Modern Cluedo-Inspired Art Direction)

### Executive Summary
Replaced the game's visual presentation from the ground up to establish a distinctive, theatrical, and luxurious aesthetic inspired by the modern reimagined *CLUEDO* board game (Hasbro 2023) and its official digital adaptation (Marmalade Game Studio 2023). Replaced all graybox cubes, capsules, flat UI panels, and prototype materials with bespoke 3D models, rich PBR materials, procedural textures, dramatic chiaroscuro lighting, and an archival case-dossier user interface. All non-visual simulation systems (five-minute clock, causal events, NPC patrol, persistent notebook, and deduction graph) were preserved with a 100% test pass rate (71/71 tests).

### Reference Research & Art Direction Specification
1. **Reference Study**:
   - Analyzed official artwork and gameplay from Hasbro's 2023 *Clue* reboot and Marmalade Game Studio's digital release.
   - Identified core aesthetic pillars: distinctive stylized character silhouettes with sculpted planes, opulent interior architecture with dramatic warm/cool chiaroscuro contrast, confident jewel-toned palettes, tactile physical materials (wood grain, marble, velvet, brass), and archival case-dossier UI cards with wax seal accents.
2. **Art Bible (`Docs/ART_DIRECTION.md`)**:
   - Authored a comprehensive 10-section art direction guide defining character design rules, the 8-color jewel-tone system, architectural specifications for Meridian House, lighting setups, tactile material parameters, and UI design standards.

### Asset Creation & Pipeline Infrastructure
1. **Custom Stylized 3D OBJ Models (`Assets/_Project/Art/Models/`)**:
   - `NPC_Concierge_Pierre.obj` & `.mtl`: 1.82m stylized human concierge model featuring sculpted facial planes, swept hair, high white collar, burgundy silk cravat, double-breasted royal navy livery coat with dual brass button rows and gold epaulets, charcoal trousers, and black Oxford shoes.
   - `HotelLobby_ReceptionDesk.obj`: Curved Art Deco reception counter with fluted mahogany panels, black granite top, polished brass trim, and rear key-cubby rack.
   - `HotelLobby_CenterTable.obj`: Round burled walnut center table with cabriole legs and brass ring apron.
   - `HotelLobby_Door102.obj`: Heavy paneled mahogany double door with raised moldings, brass kickplate, brass lever handle, and fluted architrave casing.
   - `HotelLobby_Pillar.obj`: Fluted architectural column with carved dark mahogany plinth and Corinthian brass capital.
   - `HotelLobby_Chandelier.obj`: 6-arm Art Deco brass chandelier with candle sleeves and glowing amber caps.
   - `HotelLobby_WallSconce.obj`: Geometric Art Deco brass wall sconce with frosted amber shade.
   - `Prop_BankerLamp.obj`: Brass desk lamp with glowing emerald green glass shade.
   - `Prop_Keycard_Detailed.obj`: Stamped brass keycard with "102 - MERIDIAN" emblem and magnetic stripe.
   - `HotelLobby_Armchair.obj`: Bordeaux velvet armchair with dark walnut frame.
2. **High-Resolution Textures (`Assets/_Project/Art/Textures/` and `Assets/_Project/UI/Sprites/`)**:
   - `Floor_CheckerMarble.png`: 512x512 tileable emerald and ivory checker marble with brass inlay lines.
   - `Wallpaper_ArtDecoDamask.png`: 512x512 emerald damask wallpaper with gold fan arches.
   - `Wood_PolishedMahogany.png`: 512x512 polished dark mahogany wood grain.
   - `UI_PocketWatch_Dial.png`: 512x512 antique pocket watch chronometer dial with roman numerals.
   - `UI_Parchment_Card.png`: 512x512 archival case card with gold foil border and corner ornaments.
   - `UI_WaxSeal_Gold.png`: 256x256 stamped Meridian House gold wax seal monogram.
   - `UI_Dossier_Folder.png`: 512x512 midnight navy leather dossier folder texture.
3. **PBR Material Generation Pipeline (`Assets/_Project/Scripts/Editor/ArtAssetSetup.cs`)**:
   - Generates 17 Standard PBR materials with dual-assigned properties (`_Color`/`_BaseColor`, `_Glossiness`/`_Smoothness`, `_Metallic`, `_MainTex`, `_BaseMap`, `_EmissionColor`) supporting both Built-in and URP pipelines.

### Environment & Character Integration
1. **Meridian House Lobby Rebuild (`Assets/_Project/Scripts/Editor/PrototypeSceneSetup.cs`)**:
   - Replaced graybox floor with checker marble and burgundy carpet runner.
   - Added two-tier architectural walls with mahogany wainscoting, brass dado trim, and emerald damask wallpaper.
   - Placed fluted columns, grand 6-arm chandelier with warm tungsten point light (`2700K`, 3.2 intensity), reception desk with glowing emerald banker's lamp, center table with stamped brass keycard, and bordeaux armchairs.
   - Integrated Room 102 double door with brass lever, flanking wall sconces, and electronic card reader.
   - Replaced blue capsule concierge with `NPC_Concierge_Pierre` model, wired to `NavMeshAgent` and patrol waypoints. Baked `NavMeshSurface`.

### Complete User Interface Redesign
1. **Design Tokens (`UITheme.cs`)**:
   - Implemented jewel-tone palette: Meridian Midnight (`#0D1B2A`), Polished Brass (`#D4AF37`), Bordeaux Crimson (`#6B1D2F`), Peacock Teal (`#1D4E5B`), Emerald Green (`#1B4332`), Aged Parchment (`#F3EBDD`).
   - Procedural Art Deco double-line frames (`DrawOrnateFrame`), section headers with Roman numerals, and wax seal stamps.
2. **In-Game HUD (`GameHUDView.cs`)**:
   - Vintage Art Deco pocket watch chronometer showing remaining loop countdown (`MM:SS`) and normalized loop progress.
   - Animated diamond focus reticle with brass corner brackets.
   - Sealed perception toasts and leather dossier access pill.
3. **Main Menu (`MainMenuView.cs`)**:
   - Theatrical centered title card with gold wax seal monogram, serif typography, and luxury buttons.
4. **Evidence Inspection Modal (`EvidenceInspectionModalView.cs`)**:
   - Archival parchment card with wax category seal, quote banner, examination narrative, and causal event links.
5. **Investigation Workspace (`InvestigationWorkspaceView.cs`)**:
   - 3-column dossier workspace with Roman numeral tabs, investigation metrics, and category filter buttons.
6. **Menus (`PauseMenuView.cs`, `SettingsMenuView.cs`)**:
   - Rebuilt with matching brass headers, sliders, and luxury button styling.

### Verification & Visual Inspection
1. **Screenshot Captures (`Docs/Screenshots/`)**:
   - Authored `VisualRebootInspector.cs` (capturing 3D perspectives) and `RuntimeVisualCapture.cs` (capturing live PlayMode UI).
   - Captured and visually inspected 9 high-resolution 1920x1080 PNG screenshots:
     - `Shot_01_Lobby_Overview.png`: Architectural grand lobby from entrance.
     - `Shot_02_Concierge_Pierre_Character.png`: Medium-close portrait of Concierge Pierre.
     - `Shot_03_Reception_Desk_BankerLamp.png`: Curved reception desk and emerald banker's lamp.
     - `Shot_04_Table_And_Keycard.png`: Central walnut table with stamped gold keycard.
     - `Shot_05_Room102_Entrance.png`: Room 102 mahogany door, sconces, and card reader.
     - `Shot_06_Main_Menu_View.png`: Theatrical main menu with gold monogram seal.
     - `Shot_07_InGame_HUD_View.png`: First-person HUD with pocket chronometer and toast.
     - `Shot_08_Case_Dossier_Workspace.png`: 3-column case dossier workspace.
     - `Shot_09_Evidence_Inspection_Modal.png`: Archival evidence parchment card.
2. **Automated Test Results**:
   - Ran complete Unity EditMode test suite via batch mode CLI:
     - 71 total tests executed across 6 test suites.
     - 71 passed, 0 failed, 0 inconclusive (100% pass rate).
     - Zero gameplay regressions.

