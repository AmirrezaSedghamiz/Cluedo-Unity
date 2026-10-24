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
