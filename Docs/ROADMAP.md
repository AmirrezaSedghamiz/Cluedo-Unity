# Development Roadmap — Tomorrow's Crime

A phased, iterative plan for developing *Tomorrow's Crime*.

---

## Phase 1: Foundation & First Playable Prototype (Completed)
- [x] Unity 6 LTS project setup with Universal Render Pipeline (URP).
- [x] Package resolution: Input System, Test Framework, uGUI, TextMeshPro.
- [x] Git repository initialization, `.gitignore` setup, and baseline documentation.
- [x] Clean architectural folder hierarchy under `Assets/_Project/`.
- [x] Pure C# `SimulationClockModel` with comprehensive EditMode unit tests.
- [x] `SimulationManager` and `ILoopResetReceiver` interface for deterministic loop turnovers.
- [x] First-person CharacterController player with mouse look and loop reset handling.
- [x] Graybox hotel lobby test environment in `Prototype.unity`.
- [x] Real-time developer debug HUD (clock readout, speed multipliers, manual reset).

---

## Phase 2: World State & Interactive Causal Scenario (Completed)
- [x] `WorldStateRegistry` & `WorldStateManager` storing mutable state:
  - Door states (`DoorLockState.Locked`, `DoorLockState.Unlocked`, `DoorOpenState.Closed`, `DoorOpenState.Open`).
  - Keycard & portable prop location (`PropLocationState.AtBaseline`, `HeldByPlayer`, `DroppedElsewhere`).
  - Strict duplicate ID detection and uninitialized key guards.
- [x] First-person Raycast interaction system (`IInteractable` & `PlayerInteraction` with 2.5m range and contextual prompt).
- [x] Portable key pickup and drop mechanics (`InteractableKey`).
- [x] Interactive swing door (`InteractableDoor`).
- [x] Complete scheduled causal scenario (`KeyScenarioController` at `00:30`):
  - Preconditions evaluated against live world state.
  - Succeeded branch: concierge unlocks and opens Door 102.
  - Intervened branch: if key is moved, scenario fails with explicit reason.
- [x] Cross-loop persistent knowledge service (`InvestigationKnowledgeService`) preserving witnessed discoveries across loop resets.
- [x] Extended debug HUD displaying live world state, scenario pass/fail reasons, discovery notebook, and interaction reticle.
- [x] 10 EditMode automated unit tests (16/16 total passing in suite).

---

## Phase 3: Causal Event Engine (Completed)
- [x] Immutable event definitions (`ScriptableObject`-based `EventDefinition`).
- [x] Pure C# deterministic causal event engine (`CausalEventEngine`).
- [x] Graph validation and cycle detection using depth-first search.
- [x] Deterministic execution ordering (timestamp asc, priority desc, ID ordinal).
- [x] Preconditions reading mutable world state (`IEventPrecondition`).
- [x] Effects modifying mutable world state with failure safety (`IEventEffect`).
- [x] Prerequisite dependency policies (`Skip`, `Fail`, `Cancel`).
- [x] Multi-pass same-tick dependency resolution.
- [x] Loop reset restoring runtime instances and clearing loop history.
- [x] Data-driven concierge key and door scenario refactored into event engine.
- [x] Real-time chronological event history with explanations in `SimulationDebugUI`.
- [x] Comprehensive 15-requirement EditMode test suite (31/31 passing in total suite).

---

## Phase 4: Suspect Simulation & Navigation (Completed)
- [x] Hotel NavMesh setup using `com.unity.ai.navigation` (`NavMeshSurface`).
- [x] Character definition data asset (`NPCDefinition`).
- [x] Pure C# deterministic schedule evaluator (`NPCScheduleEvaluator`) using `[StartTime, EndTime)`.
- [x] Locomotion controller (`NPCController`) with `NavMeshAgent` and waypoint navigation.
- [x] Dynamic reaction to missing key (**Reaction A**: enters `Investigating` state, stops door unlock).
- [x] Dynamic reaction to blocked route (**Reaction B**: enters `Blocked` state with recovery cooldown).
- [x] Sensory perception model (`NPCPerception`) checking distance, FOV angle, and line of sight.
- [x] Loop reset integration restoring baseline position, schedule, and clearing transient awareness.
- [x] Observability diagnostics in HUD showing live NPC behavior state, activity, and decision reason.
- [x] 12 new automated EditMode unit tests (43/43 passing in total suite).

---

## Phase 5: Investigation System & Persistent Notebook (Completed)
- [x] Immutable `EvidenceDefinition` ScriptableObjects and runtime `DiscoveredEvidenceRecord` structures.
- [x] Separation of internal simulation truth from player-witnessed timeline (`PlayerObservationEntry`).
- [x] Player hypotheses formulation system (`HypothesisEntry`) with CRUD support and supporting evidence linking.
- [x] Freeform persistent general notes storage across loop resets.
- [x] Persistent singleton knowledge service (`InvestigationKnowledgeService`) preserving data across resets without duplicate subscriptions.
- [x] Interactive three-tab notebook IMGUI controller (`NotebookUIController`) toggled via `Tab` or `N`.
- [x] Input blocking system (`PlayerController.SetInputBlocked`) pausing look/movement and unlocking cursor while live simulation runs.
- [x] World evidence inspection component (`InspectableEvidence`) integrated with player raycast interaction and item prerequisites.
- [x] Playable scenario evidence items: Room 102 Keycard, Concierge Duty Memo, Room 102 Door & Lock Card Reader.
- [x] 14 new automated EditMode unit tests (57/57 total passing across full suite).

---

## Phase 6: Major UI Overhaul, Investigation 2.0 & Cross-Loop Deduction (Completed)
- [x] Cohesive visual identity & styling system (`UITheme`):
  - Charcoal, slate, warm paper, brass, teal, amber, and crimson palette.
  - Custom card styles, badges, buttons, sliders, and typography tokens.
- [x] Central UI State Coordinator (`UIManager`):
  - Managed UI states (`InGameHUD`, `InvestigationNotebook`, `EvidenceDetailModal`, `PauseMenu`, `SettingsMenu`, `MainMenu`, `LoopSummaryOverlay`).
  - Single-point input arbitration (cursor locking/unlocking, movement & look disabling).
  - Non-blocking investigation policy vs pause-menu simulation suspension.
  - Toast notification queue for clues and witnessed observations.
- [x] First-person In-Game HUD (`GameHUDView`):
  - Countdown clock (`MM:SS`) with normalized loop progress bar.
  - Dynamic urgency color shifts (>60s brass, <60s amber, <15s pulsing crimson).
  - Contextual reticle and interactive prompt badge on gaze.
  - Investigation status indicator pill (`Tab / N`).
- [x] Dedicated Evidence Reading Modal (`EvidenceInspectionModalView`):
  - Warm paper reading view for discovered clues and documents.
  - Clue category, examination narrative, and linked causal event tags.
- [x] Investigation Workspace 2.0 (`InvestigationWorkspaceView`):
  - 3-column layout with 5 integrated tabs.
  - Evidence Library with category filtering and inspection details.
  - Chronological Observation Timeline with cross-loop diff comparison (`CompareLoops`).
  - Interactive Deduction Board with 2D card placement, notes, and semantic relationships (`Supports`, `Contradicts`, `RelatedTo`, `OccurredBefore`, `OccurredAfter`).
  - Rejection of relationships referencing undiscovered evidence.
  - Hypothesis Manager with theory formulation and evidence linking.
  - Persistent freeform Field Notes scratchpad.
- [x] Pause Menu, Main Menu & Game Settings (`GameSettingsManager`):
  - Persistent settings via `PlayerPrefs`: Master Volume, Mouse Sensitivity, Quality Level, Fullscreen, and HUD Scale.
  - Immediate reflection of mouse look sensitivity in `PlayerController`.
- [x] Scene Visual Polish (`PrototypeSceneSetup`):
  - Warm pendant ceiling light, charcoal flooring, mahogany doors/desks, walnut furniture.
  - Integration of `[UserInterface]` hierarchy into `Prototype.unity`.
- [x] Comprehensive automated test coverage:
  - 14 new tests in `InvestigationSystem2Tests`.
  - 71 / 71 total EditMode unit tests passing (100% pass rate).

---

## Phase 7: Complete Visual Reboot — Modern Cluedo-Inspired Art Direction (Completed)
- [x] Reference study of modern *CLUEDO* (Hasbro 2023 / Marmalade 2023) and comprehensive Art Bible (`Docs/ART_DIRECTION.md`).
- [x] Bespoke stylized 3D human character model: `NPC_Concierge_Pierre.obj` (1.82m concierge with sculpted facial planes, styled swept hair, stiff white collar, burgundy silk cravat, double-breasted navy coat with brass buttons and epaulets).
- [x] Custom 3D architectural & prop models: reception desk with key-cubbies, round walnut center table, Room 102 mahogany door, fluted columns with Corinthian capitals, 6-arm Art Deco brass chandelier, geometric wall sconces, glowing emerald banker's lamp, stamped gold keycard, and bordeaux velvet armchairs.
- [x] High-resolution texture suite: checker marble with brass inlays, Art Deco emerald damask wallpaper, polished mahogany wood grain, antique pocket watch dial, archival parchment card, gold wax seal monogram, and leather dossier folder texture.
- [x] Standard PBR material generation pipeline (`ArtAssetSetup.cs`) supporting Built-in and URP pipelines with 17 materials.
- [x] Architectural Meridian House lobby rebuild (`PrototypeSceneSetup.cs`) with two-tier wainscoted walls, warm tungsten chandelier (`2700K`), and NavMesh surface baking.
- [x] Complete UI reboot:
  - Jewel-tone visual tokens in `UITheme` (Meridian Midnight, Polished Brass, Bordeaux Crimson, Peacock Teal, Emerald Green, Aged Parchment).
  - Theatrical Main Menu with gold wax seal monogram and Art Deco double framing.
  - In-Game HUD with antique pocket watch chronometer, animated diamond reticle, and wax seal toasts.
  - Archival Evidence Reading Modal with quotation callout and causal event links.
  - 3-column Confidential Dossier Workspace with Roman numeral tabs, gold metrics, and category filters.
  - Matching luxury styling across Pause Menu and Settings.
- [x] Automated visual inspection pipeline (`VisualRebootInspector.cs` and `RuntimeVisualCapture.cs`) capturing 9 verified 1920x1080 screenshots in `Docs/Screenshots/`.
- [x] 100% test preservation: 71 / 71 tests passing with zero gameplay regressions.

---

## Phase 8: Hotel Environment Expansion & Full Suspect Ensemble
- [ ] Multi-room Meridian House layout expansion (Lobby, Guest Room 102 interior, Service Corridor, Manager's Office).
- [ ] Suspect ensemble expansion: author definitions and routines for remaining suspects.
- [ ] Additional physical clues, documents, and environmental manipulation devices (breakers, phone lines).
- [ ] Audio design (clock ticking, ambient tension, loop transition stingers, footstep audio).
- [ ] Visual polish (lighting, post-processing volume profiles, materials).
- [ ] Automated integration test suite and final release build.
