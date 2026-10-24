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
