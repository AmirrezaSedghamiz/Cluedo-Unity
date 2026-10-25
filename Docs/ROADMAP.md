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

## Phase 6: Hotel Environment Expansion & Full Suspect Ensemble
- [ ] Multi-room Meridian House layout expansion (Lobby, Guest Room 102 interior, Service Corridor, Manager's Office).
- [ ] Suspect ensemble expansion: author definitions and routines for remaining suspects.
- [ ] Additional physical clues, documents, and environmental manipulation devices (breakers, phone lines).
- [ ] Audio design (clock ticking, ambient tension, loop transition stingers, footstep audio).
- [ ] Visual polish (lighting, post-processing volume profiles, materials).
- [ ] Automated integration test suite and final release build.
