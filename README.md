# Tomorrow's Crime

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
- **Render Pipeline:** Universal Render Pipeline (URP)
- **Scripting Runtime:** .NET 8 / C# 9+
- **Input:** Unity Input System
- **Testing:** Unity Test Framework (NUnit EditMode & PlayMode)
- **Platform:** PC / Standalone Windows (64-bit)

---

## 3. Project Directory Structure

All game-specific code, assets, and configurations reside under `Assets/_Project/` to ensure clean separation from package caches and third-party tools:

```
Assets/_Project/
├── Art/
│   ├── Lighting/          # URP lighting profiles, skyboxes, and light cookies
│   ├── Materials/         # Surface shaders and prototype graybox materials
│   ├── Models/            # 3D meshes (hotel architecture, furniture, props)
│   └── Textures/          # Texture maps and visual decals
├── Audio/
│   ├── Music/             # Ambient loop tracks and dynamic tension cues
│   └── SFX/               # Environmental audio, footsteps, clock ticks
├── Prefabs/
│   ├── Environment/       # Modular architectural prefabs (rooms, corridors)
│   ├── Interactables/     # Doors, switches, clocks, keys
│   ├── NPCs/              # Suspect character prefabs and rigs
│   ├── Player/            # First-person player rig, camera, and controller
│   └── UI/                # HUD elements, evidence notebook, clock display
├── Scenes/
│   └── Prototype.unity    # Initial graybox hotel lobby and simulation sandbox
├── ScriptableObjects/
│   ├── Characters/        # Immutable suspect profiles, traits, baseline schedules
│   ├── Configuration/     # Simulation clock parameters, input settings
│   └── Events/            # Static event definitions and causal dependency rules
├── Scripts/
│   ├── Core/              # Core interfaces, game states, event bus contracts
│   ├── Simulation/        # 5-minute simulation clock, time scaling, tick engine
│   ├── Events/            # Scheduled occurrences, precondition evaluations
│   ├── World/             # Reset managers, world state models (doors, power)
│   ├── Characters/        # NPC schedule runners and state machines
│   ├── Player/            # First-person locomotion, mouse look, spawn handler
│   ├── Investigation/     # Evidence registry, notebook, persistent discovery
│   ├── UI/                # Time countdown HUD, debug overlay, pause screen
│   ├── Debuging/          # Developer cheats, time scrubbers, inspector tools
│   └── Persistence/       # Cross-loop memory that survives world resets
├── Settings/              # URP asset profiles, render pipelines, input actions
├── Tests/
│   ├── EditMode/          # Pure logic unit tests (Clock, Causal rules, State)
│   └── PlayMode/          # Scene-dependent tests (Player motion, trigger resets)
└── UI/
    ├── Fonts/             # Typography for HUD and detective notebook
    └── Sprites/           # Icons, reticles, UI card frames
```

---

## 4. Getting Started

1. Open **Unity Hub**.
2. Click **Add** -> **Add project from disk**.
3. Select `TomorrowsCrime` located at `G:\Unity-Game\TomorrowsCrime`.
4. Ensure the selected Editor version is **Unity 6000.6.7f1** (or matching Unity 6 LTS).
5. Open the project.
6. In the Project window, navigate to `Assets/_Project/Scenes/` and double-click `Prototype.unity`.
7. Press the **Play** button in the Unity toolbar.

### Controls
- **Move:** W, A, S, D
- **Look:** Mouse
- **Fast Forward Time (Debug):** `1` (1x), `2` (2x), `5` (5x)
- **Pause Simulation (Debug):** `P` or Space
- **Manual Loop Reset (Debug):** `R`
