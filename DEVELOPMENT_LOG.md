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
