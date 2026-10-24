# Development Roadmap — Tomorrow's Crime

A phased, iterative plan for developing *Tomorrow's Crime*.

---

## Phase 1: Foundation & First Playable Prototype (Current Milestone)
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

## Phase 2: World State & Interactive Environment
- [ ] `WorldStateManager` storing mutable state:
  - Door states (locked, open, closed).
  - Light & electrical breaker circuits.
  - Portable props & keys.
- [ ] First-person Raycast interaction system (`IInteractable`).
- [ ] Snapshot & restore pattern for instant, reliable loop resets.
- [ ] EditMode unit tests for state registration, mutation, and reset verification.

---

## Phase 3: Causal Event Engine
- [ ] Immutable event definitions (`ScriptableObject`-based).
- [ ] Runtime event scheduler driven by the 5-minute clock.
- [ ] Precondition evaluation (e.g., "Event occurs at 02:15 IF Door 102 is unlocked").
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
- [ ] Player evidence notebook persisting across loop resets.
- [ ] Discovery triggers (witnessing an event, inspecting a clue, overhearing dialog).
- [ ] Hypothesis board: connecting Means, Motive, and Opportunity.
- [ ] Validation logic for player deduction and mystery resolution.

---

## Phase 6: Polish, Hotel Graybox Expansion & Portfolio Presentation
- [ ] Multi-room Meridian House layout (Lobby, Guest Rooms, Service Corridor, Manager's Office).
- [ ] Audio design (clock ticking, ambient tension, loop transition sound cues).
- [ ] Visual polish (lighting, post-processing volume profiles, materials).
- [ ] Automated integration test suite and final release build.
