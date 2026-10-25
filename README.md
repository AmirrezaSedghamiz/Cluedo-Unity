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

## 4. Suspect Simulation & Navigation (Milestone 4)

The prototype features the first autonomous suspect simulation with Unity AI Navigation:
1. **Character Definition (`NPCDefinition`):** Immutable data asset specifying character identity (`npc_concierge`, "Concierge Pierre"), locomotion speed (3.0 m/s), stopping distance (0.8m), perception cone (8.0m view range, 90° FOV), and scheduled activities.
2. **Simulation-Time Schedule:**
   - `00:00 - 00:15`: `WaitAtPost` — Concierge waits at the reception desk (`Waypoint_LobbyDesk`).
   - `00:15 - 00:30`: `RetrieveKey` — Concierge navigates across the lobby floor using `NavMeshAgent` toward the lobby table (`Waypoint_LobbyTable`).
   - `00:30 - 00:45`: `UnlockDoor` — If key was acquired, walks toward Room 102 (`Waypoint_Room102Door`) and unlocks the door.
   - `00:45 - 05:00`: `WaitAtPost` — Concierge stations at Room 102.
3. **Reactive Behaviors:**
   - **Reaction A (Missing Key):** When reaching the table at ~00:30, if the player moved or picked up the keycard, the concierge notices the absence, enters the `Investigating` behavior state, logs reason: `"Investigating: expected keycard is missing from lobby table!"`, and does NOT attempt to unlock Room 102.
   - **Reaction B (Blocked Route):** If the path to a destination is blocked or unreachable, the NPC transitions to `Blocked` navigation status, avoids infinite path spamming, and enters a clear waiting state.
4. **Basic Sensory Model (`NPCPerception`):** NPCs only perceive information within their sensory cone (distance, horizontal angle, and line-of-sight raycasts). If the player takes the key behind the concierge's back, the NPC does not magically know until reaching the table.
5. **Deterministic Reset:** Upon loop reset, the `NavMeshAgent` is warped back to the baseline desk position, paths are cleared, behavior state resets to `Idle`, and transient awareness is wiped clean.

## 5. Investigation System & Persistent Notebook (Milestone 5)

Step 5 implements the core detective loop, adhering to the foundational rule: **Simulation Knowledge != Player Knowledge**.

### Core Architecture & Features:
1. **Separation of World Truth & Detective Knowledge:**
   - The simulation engine tracks all world state and NPC activities. The player's persistent notebook ONLY records what the player has directly inspected or personally witnessed.
   - Unobserved events (e.g. an NPC's internal routine when the player is absent) never contaminate the player's timeline.
2. **Three-Tab Detective Notebook (`Tab` or `N`):**
   - **Evidence:** Displays all discovered clues (`Room 102 Keycard`, `Concierge Duty Memo`, `Room 102 Door & Lock`). Selecting an item reveals its category, detailed inspection notes, and linked events.
   - **Timeline:** Displays chronological player-witnessed observations (e.g., witnessing the concierge finding the key missing at 00:30). Allows comparing loop-over-loop deviations.
   - **Hypotheses & Notes:** Formulate theories by entering titles and explanations, with optional supporting evidence selection. Hypotheses can be created, edited, and deleted. Freeform general notes persist automatically across loops.
3. **Live Simulation & Input Blocking:**
   - Opening the notebook (`Tab`/`N`) disables player movement and mouse look, unlocking the cursor for interactive note-taking.
   - The live hotel simulation continues running uninterrupted in the background while the notebook is open. Closing the notebook restores first-person control and locks the cursor.
4. **Cross-Loop Persistence:**
   - When a loop ends (at 05:00 or via `R`), all world objects, NPC positions, and event instances reset to baseline.
   - All discovered evidence, observation entries, hypotheses, and notes are preserved in `InvestigationKnowledgeService`. Discovering additional clues in subsequent loops seamlessly adds to the notebook without duplicating or overwriting previous findings.

---

## 6. Controls

- **Move:** `W`, `A`, `S`, `D`
- **Look:** Mouse (Cursor is automatically locked to screen)
- **Sprint:** Hold `Left Shift`
- **Interact / Inspect / Pick Up / Place:** `E` (Aim at objects when prompt appears)
- **Toggle Investigation Notebook:** `Tab` or `N` (Opens Evidence, Timeline & Hypotheses)
- **Fast Forward Time (Debug):** `1` (1x), `2` (2x), `5` (5x), `10x` button
- **Pause / Resume Simulation:** `P`
- **Manual Loop Reset:** `R`
- **Toggle Diagnostic HUD:** `F1` or `~` (Shows clock, world state, causal history, and live NPC simulation diagnostics)

---

## 7. Getting Started & Baking Navigation in Unity

1. Open **Unity Hub**.
2. Click **Add** -> **Add project from disk**.
3. Select `TomorrowsCrime` located at `G:\Unity-Game\TomorrowsCrime`.
4. Ensure the selected Editor version is **Unity 6000.6.7f1** (or matching Unity 6 LTS).
5. Open the project.
6. In the Project window, navigate to `Assets/_Project/Scenes/` and double-click `Prototype.unity`.
7. **(Optional) Re-baking NavMesh:** Select `[Environment_HotelLobby]` in the Hierarchy. In the Inspector, locate the `NavMeshSurface` component and click **Bake**. (The scene is already pre-baked via `Tomorrow's Crime -> Build Prototype Scene`).
8. Press the **Play** button in the Unity toolbar.
