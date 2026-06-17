# Valet Guidance, Safety & Payment Card Simulation Guide (Updated)

This document provides a comprehensive guide to understanding, setting up, and playtesting the fully integrated Valet Guidance, Safety, and Payment Card Simulation in the VR prototype.

---

## 1. Project Overview

This project simulates a futuristic, autonomous valet parking garage. Players can spawn a custom vehicle, board it, authenticate entry via a physical payment card, experience autonomous drop-off/parking, recall the vehicle via a lobby totem, and experience realistic dynamic ETA updates. An advanced pedestrian safety system protects wandering NPCs and the player in VR.

## 2. Current Feature Summary

- **100% VR-Native UI**: Cockpit dashboard canvas and spatial UI interactions (No keyboard needed).
- **Physical Door Triggers**: Enter and exit the vehicle via physical handles and buttons.
- **Card-Based Authentication**: Realistic hand-anchored card reader interactions for gates and totems.
- **Dynamic ETA Billboard**: Real-time traffic-delay-aware ETA calculations for vehicle recall.
- **Internal Screen Sequence**: Immersive in-car screen overlays updating based on trip progress.
- **Combined Pedestrian Safety**: Dual-source stopping logic (Safety zone + Crosswalks) with blinking light bridges and one-shot audio warnings.
- **Isolated Player Detection**: Safe player tracking outside the vehicle using a proxy collider to prevent locomotion issues.

## 3. Unity and Package Prerequisites

- Unity version matching the project settings (typically 2022+).
- XR Interaction Toolkit.
- Built-in or Universal Render Pipeline (depending on current project configuration; overlays support both).

## 4. Opening the Correct Scene

Load `Assets/Scenes/SampleScene.unity`. Ensure that it is the active scene before entering Play Mode.

## 5. Required Initial Scene State

For the simulation to function correctly, the initial scene state MUST be clean:
- **No `Player_TestCar` instances** should exist in the scene Hierarchy before Play Mode.
- On the `PlayerCarProgression` script component, the `playerCarInstance` and `playerDetectionProxy` fields MUST be `None` (empty).
- The `Test Car Prefab` field MUST be assigned to `TestCar.prefab`.

## 6. Desktop Controls

For rapid testing in the Unity Editor without a VR headset:

| Key | Action | Description |
| :--- | :--- | :--- |
| **`P`** | **Spawn Test Car** | Spawns TestCar at the entrance. |
| **`G`** | **Toggle Boarding** | Teleports player inside or outside the car. |
| **`Enter`** | **Progress Valet State** | Drives the car forward to the next stop manually. |
| **`U`** | **Toggle UI Panel** | Toggles the VR Simulation Control Panel. |
| **`C`** | **Toggle Payment Card** | Spawns or despawns the 3D payment card. |

## 7. Meta Quest / XR Controls

- **Toggle Control Panel**: **`X`** button (Left Controller).
- **Spawn / Despawn Payment Card**: **`Y`** button (Left Controller).
- **Raycast Pointer Selection**: Pointer ray + **Index Trigger**.
- **Enter Vehicle (Outside)**: Pointer ray on **`ReturnButton`** / **`ReturnButton_Passenger`** + **Index Trigger**.
- **Exit Vehicle (Inside)**: Pointer ray on cockpit **`ChangeButton`** + **Index Trigger**.
- **Tapping Reader Zones**: Bring your left hand physically close to a reader zone.

## 8. How to Start the Simulation

1. Verify the initial scene state (Section 5).
2. Enter Play Mode.
3. Put on your VR headset.

## 9. How to Spawn and Use the TestCar

1. Press **`X`** to open the Control Panel.
2. Click **SPAWN CAR** via the pointer ray.
3. Target the physical door button and click to enter.

## 10. Drop-Off, Parking, Recall, Pickup, and Exit Workflow

1. **Drop-Off**: Tap your card at the entrance gate. The car drives autonomously to the drop-off zone. Exit the car via `ChangeButton`.
2. **Auto-Park**: After a 5-second delay post-exit, the car drives to a random vacant spot and reverse-parks.
3. **Recall**: Walk to the Lobby Totem and tap your card. The ETA Billboard updates.
4. **Pick-Up**: The car drives to the pick-up sidewalk. Board the car via the door button.
5. **Exit**: After a 5-second delay, the car drives out the exit gate. Once stopped at the final node, exit the vehicle to despawn it.

## 11. Card Reader and Totem Workflow

- **Entrance**: Gate ticket machine requires a `PaymentCard` trigger to open and start the drop-off sequence.
- **Totem**: Tapping the card at the Totem reader triggers `TotemRecallHandler`, authenticates the parked session, begins the Totem Screen Sequence, and triggers ETA.

## 12. Billboard and ETA Behavior

- **ETA Logic**: Calculates base travel time + predefined pedestrian crosswalk delays + `ParkingTrafficProvider` density delay.
- **Billboard Updates**: Shows static background, dynamic plate number/pickup tag, and countdown. Updates every 10-15 seconds.
- **Terminal Display**: Reaching the Pick-Up spot clears the active route and immediately displays "YOUR CAR IS HERE".

## 13. Internal Screen Behavior

The `PlayerCarScreenBridge` monitors geometric proximity to gates and valet session states. It automatically transitions the dashboard `InternalVehicleScreenController` overlay through:
`Welcome` -> `Select Destination` -> `Autonomous Drive Active` -> `Deployment` -> `Pick-Up` -> `Goodbye` -> `Resume Control`.

## 14. Pedestrian Safety Behavior

Vehicles combine `SafetyTriggerDetector` and `CrosswalkTrafficLight` states. When either is active (`isSafetyWaiting || isCrosswalkStopped`), the vehicle halts, activates blinking headlights, and plays a one-shot audio warning beep.

## 15. PlayerDetectionProxy Setup

The player is detected by the Safety System via a dedicated `PlayerDetectionProxy` child object under `XR Origin (XR Rig)`.
- **Layer**: `DynamicUser`
- **Collider**: `CapsuleCollider` (Is Trigger = True)
- **Rigidbody**: Kinematic (Use Gravity = False)
- This proxy is enabled automatically when outside the car and disabled when inside.

## 16. Warning Lights Setup

Uses `BlinkingLightAdapter.cs`. It captures children `Light` components and toggles intensity based on the combined safety stop logic. Pre-configured on `TestCar.prefab`.

## 17. One-Shot Warning Audio Setup

Uses `SafetyAudioAdapter.cs`.
- **AudioSource**: Must have an AudioClip assigned, `Play On Awake` = False, `Loop` = False.
- Behavior: A non-looping beep plays exactly once when the combined safety condition transitions from `False -> True`.

## 18. XR UI Setup Checklist

- Ensure the Scene has exactly one `EventSystem` configured with `XR UI Input Module`.
- The VR Canvas must be set to `World Space` and contain a `Tracked Device Graphic Raycaster`.
- UI interactions require the controller to have raycasting enabled.

## 19. Required Inspector Wiring

- `TestCar.prefab` -> `AHMI_SafetyInteractionSystem` must be present.
- `PlayerCarProgression` -> `Test Car Prefab` assigned to `TestCar.prefab`.
- UI Buttons -> Correct `UnityEvent` bindings (`TrasportaGiocatore`, `RientraInMacchina`).

## 20. Known Unsupported Prefabs/Features

- **Car 1 and Car 2 Prefabs**: Do not currently have the `AHMI_SafetyInteractionSystem` nested by default. They require manual prefab integration.

## 21. Current Limitations

- Extremely high NPC congestion can cause temporary NavMesh gridlocks.
- Avatar height offsets might shift after vehicle exit if teleport floor heights mismatch.
- Visual totems are mostly decorative until the `StartRecallSequence()` coroutine is fired via actual card authentication.

## 22. Troubleshooting Guide

### TestCar does not spawn
Check:
- No `Player_TestCar` instances exist in the scene.
- `PlayerCarProgression.playerCarInstance` is empty.
- `Test Car Prefab` still references `TestCar`.
- Console false-positive spawn message (can occur if previous instance was not cleared).

### Player appears inside car but state remains outside
Check:
- Exact `TeleportButton` method wired (e.g., `ToggleBoardingState` or `RientraInMacchina`).
- Duplicate or disabled progression components.

### Car does not move after exit
Check:
- Valet Session state is properly initialized.
- `PlayerDetectionProxy` disabled while inside and enabled outside (preventing self-blocking).
- Exit point is safely outside the vehicle safety zone.

### Warning lights do not flash
Check:
- NPC prefab has nested Safety system.
- `BlinkingLightAdapter` references child lights properly.
- Combined `isSafetyWaiting || isCrosswalkStopped` condition is triggering.

### Warning sound does not play
Check:
- AudioClip assigned.
- `Play On Awake` is off.
- `Loop` is off.
- One-shot latch resets properly (returns to false when clear).

### XR ray does not interact with UI
Check:
- `EventSystem` configuration.
- `XR UI Input Module` active.
- `Tracked Device Graphic Raycaster` on Canvas.

## 23. Final Testing Checklist

- [ ] Validate TestCar clean spawn.
- [ ] Validate physical door entry.
- [ ] Validate entrance gate card tap and drive.
- [ ] Validate physical cockpit exit.
- [ ] Validate auto-park 5s delay.
- [ ] Validate Totem recall card tap and screen update.
- [ ] Validate ETA Billboard updates and terminal message.
- [ ] Validate pedestrian proxy blocks car safely.
- [ ] Validate one-shot audio warning triggers.

## 24. Collaboration and Git Workflow

- Pull the latest `FinalBranch`.
- Do not commit scene files with serialized `Player_TestCar` instances to avoid blocking teammates.

## 25. Important Files and Folders

- `Assets/PreFabs/TestCar.prefab`
- `Assets/Scripts/CarTraffic/ValetGuidanceSystem.cs`
- `Assets/Scripts/CarTraffic/SmartCarNavigator.cs`
- `Assets/Scripts/CarTraffic/PlayerCarProgression.cs`
- `Assets/AHMI_SafetyInteractionSystem/Prefabs/AHMI_SafetyInteractionSystem.prefab`
