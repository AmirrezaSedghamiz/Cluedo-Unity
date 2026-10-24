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

## Phase 3: Causal Event Engine (Next Milestone)
- [ ] Immutable event definitions (`ScriptableObject`-based).
- [ ] Generalized runtime event scheduler driven by the 5-minute clock.
- [ ] Precondition evaluation dependency graph (e.g., "Event occurs at 02:15 IF Door 102 is unlocked AND Power is ON").
- [ ] Consequence execution and causal invalidation reporting.
- [ ] Narrative log documenting why events occurred or failed.

---

## Phase 4: Suspect Simulation & Navigation
- [ ] Hotel NavMesh setup using `com.unity.ai.navigation`.
- [ ] Suspect behavior controllers with baseline daily routines.
- [ ] Dynamic reactions when expected conditions change (e.g., locked path, missing prop).
- [ ] Suspect visual placeholders with directional cones of vision and auditory awareness.

---

## Phase 5: Investigation System & Persistent Notebook
- [ ] Player evidence notebook UI persisting across loop resets.
- [ ] Multi-trigger discovery pipeline (witnessing an event, inspecting a clue, overhearing dialog).
- [ ] Hypothesis board: connecting Means, Motive, and Opportunity.
- [ ] Validation logic for player deduction and mystery resolution.

---

## Phase 6: Polish, Hotel Graybox Expansion & Portfolio Presentation
- [ ] Multi-room Meridian House layout (Lobby, Guest Rooms, Service Corridor, Manager's Office).
- [ ] Audio design (clock ticking, ambient tension, loop transition sound cues).
- [ ] Visual polish (lighting, post-processing volume profiles, materials).
- [ ] Automated integration test suite and final release build.
