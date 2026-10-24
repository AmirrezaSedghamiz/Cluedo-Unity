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

---

## 4. Controls

- **Move:** `W`, `A`, `S`, `D`
- **Look:** Mouse (Cursor is automatically locked to screen; press `Esc` to toggle)
- **Sprint:** Hold `Left Shift`
- **Interact / Pick Up / Place:** `E` (Aim at table keycard or doors when prompt appears)
- **Fast Forward Time (Debug):** `1` (1x), `2` (2x), `5` (5x), `10x` button
- **Pause / Resume Simulation:** `P`
- **Manual Loop Reset:** `R`
- **Toggle Diagnostic HUD:** `F1` or `~` (Features scrollable chronological causal event history)

---

## 5. Getting Started in Unity

1. Open **Unity Hub**.
2. Click **Add** -> **Add project from disk**.
3. Select `TomorrowsCrime` located at `G:\Unity-Game\TomorrowsCrime`.
4. Ensure the selected Editor version is **Unity 6000.6.7f1** (or matching Unity 6 LTS).
5. Open the project.
6. In the Project window, navigate to `Assets/_Project/Scenes/` and double-click `Prototype.unity`.
7. Press the **Play** button in the Unity toolbar.
