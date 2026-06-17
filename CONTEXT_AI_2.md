# AI Context: Valet Guidance & Safety VR Simulation Project (Updated)

This document provides updated system architecture, component mappings, state machines, and code design guidelines to help AI coding assistants understand and modify this Unity VR codebase safely.

## 1. Document Purpose & Source-Of-Truth Rules

If contradictions arise between documents, use this priority:
1. Current C# implementation
2. Current scene and prefab serialization
3. Current Git diff and recent Git history
4. Existing documentation (AI_CONTEXT.md, README.md)

## 2. Updated Subsystem Directory & Component Map

```text
Root Workspace
├── Assets
│   ├── PreFabs
│   │   ├── TestCar.prefab                 # Player's custom driveable car (contains AHMI_SafetyInteractionSystem)
│   │   └── VRControlPanelCanvas.prefab    # Spawns in front of the player
│   │
│   ├── Scripts/CarTraffic
│   │   ├── ValetGuidanceSystem.cs         # Central brain. Manages ValetSessions.
│   │   ├── PlayerCarProgression.cs        # Spawns TestCar. Manages IsPlayerInsideCar state.
│   │   ├── VRSimulationController.cs      # Controls UI canvas and TestCar spawning input.
│   │   ├── SmartCarNavigator.cs           # Vehicle NavMesh controller and safety state bridge.
│   │   ├── NPCCarSpawner.cs               # Spawns NPC cars from prefabs.
│   │   ├── NPCManager.cs                  # Spawns human NPCs at drop-off.
│   │   ├── HumanNPCBehavior.cs            # NPC pedestrian NavMesh agent and recall logic.
│   │   ├── BarrierGate.cs                 # Controls gates and triggers screen sequence.
│   │   ├── CrosswalkTrafficLight.cs       # Signals crosswalk waiting.
│   │   ├── CardReader.cs                  # General trigger for PaymentCards.
│   │   ├── PaymentCard.cs                 # Identifies user authorization.
│   │   └── TotemRecallHandler.cs          # Recalls car and starts Totem sequence.
│   │
│   ├── AHMI_ETA_Orchestrator/Scripts
│   │   ├── CarETAOrchestrator.cs          # Calculates dynamic ETA based on distance and traffic.
│   │   ├── ParkingTrafficProvider.cs      # Traffic density penalty provider.
│   │   ├── DynamicRouteBridge.cs          # Bridges NavMesh paths to ETA system.
│   │   ├── BillboardReceiver.cs           # Updates the Lobby ETA display.
│   │   ├── SimulationEvents.cs            # Global events for ETA updates.
│   │   ├── TotemCarRequest.cs             # NPC discovery target for recall spots.
│   │   └── TotemScreenSequence.cs         # Manages visual recall sequence on Totem.
│   │
│   ├── AHMI_InternalVehicleScreenSystem/Scripts
│   │   └── ScreenController.cs            # Updates internal screen UI textures based on state.
│   │
│   ├── AHMI_SafetyInteractionSystem/Scripts
│   │   ├── SafetyInteractionState.cs      # Broadcasts safety wait events.
│   │   ├── SafetyTriggerDetector.cs       # Detects pedestrians in safety zone.
│   │   ├── Adapter_PulseLights.cs         # Prototype visual warning.
│   │   └── Adapter_Audio.cs               # One-shot pedestrian warning audio.
│   │
│   └── AHMI_MyPlayer
│       ├── TeleportButton.cs              # Handles physical boarding/unboarding teleports.
│       └── CarExitManager.cs              # Legacy exit script.
```

## 3. Core Runtime Ownership Model

- **Vehicle Movement**: `SmartCarNavigator` (guided by `ValetGuidanceSystem`) is the sole authoritative owner of vehicle routing and movement states.
- **Player State**: `PlayerCarProgression` is the authoritative owner of the player's presence in the vehicle (`IsPlayerInsideCar`).
- **Safety**: `SmartCarNavigator` bridges stopping requests from `SafetyTriggerDetector` and `CrosswalkTrafficLight`.

## 4. ValetSession and ValetState State Machine

`ValetGuidanceSystem` tracks each vehicle via a `ValetSession`:
1. `Manual`: Stopped at crosswalk.
2. `ApproachingDropOff`: Routing to drop-off.
3. `AtDropOff`: Stopped. NPC waits `dropOffWaitTime`. Player waits until exited, then 5s delay.
4. `MovingToPark`: Routing to parking spot.
5. `Parked`: NPC recalled by `HumanNPCBehavior` reaching Totem (2.5s wait). Player recalled by `TotemRecallHandler` card tap.
6. `MovingToPickUp`: Routing to pick-up spot.
7. `AtPickUp`: Stopped. Player `CarETAOrchestrator` cleared and terminal ETA broadcast ("YOUR CAR IS HERE"). NPC waits for boarding. Player waits until boarded, then 5s delay.
8. `Exiting`: Routing to final exit node.
9. `Exited`: NPC despawned instantly. Player vehicle stops, waits for player to exit, then despawns.

## 5. Player TestCar Lifecycle

The player's `TestCar` is not pre-placed. It is instantiated at runtime via `PlayerCarProgression.SpawnPlayerCar()`. Spawning requires that no stale scene instances exist and that serialized runtime fields (`playerCarInstance`) are strictly empty. Once spawned, the car follows the `ValetState` flow governed by player entry/exit.

## 6. NPC Vehicle & Pedestrian Lifecycle

`NPCCarSpawner` instantiates vehicles. `NPCManager` spawns `HumanNPCBehavior` agents at the drop-off. Agents walk to the elevator, wait, walk back to a `TotemCarRequest`, wait 2.5s, trigger `RecallCar()`, walk to the pick-up sidewalk, wait for the vehicle, and board. The vehicle then exits.

## 7. VR Boarding/Unboarding Architecture

`TeleportButton.cs` is the authoritative boarding handler.
- **Entry**: Disables `CharacterController`, detaches avatar, moves/parents `XR Origin` to `TestCar`, sets `IsPlayerInsideCar = true`.
- **Exit**: Disables `CharacterController`, unparents `XR Origin` to root, enables avatar, moves to exterior door, sets `IsPlayerInsideCar = false`. Smart redirection handles Driver vs Passenger side proximity.

## 8. Card, Gate, Totem, and Recall Architecture

- `CardReader` triggers on kinematic Rigidbody overlap with `PaymentCard`.
- `BarrierGate` subscribes to `CardReader` to authenticate entry.
- `TotemRecallHandler` subscribes to `CardReader` to authenticate recall. If parked, it triggers `ValetGuidanceSystem.RecallCar()`, `TotemScreenSequence.StartRecallSequence()`, and `CarETAOrchestrator.StartETACalculation()`.

## 9. ETA, Traffic Provider, and Billboard Pipeline

1. `ValetGuidanceSystem` triggers recall.
2. `DynamicRouteBridge` passes NavMesh route to `CarETAOrchestrator`.
3. `CarETAOrchestrator` calculates ETA = (Distance/Speed) + Pedestrian Delays + `ParkingTrafficProvider` delay.
4. `SimulationEvents.RaiseETAUpdated` broadcasts updates every 10s or 15s delta.
5. `BillboardReceiver` updates the physical UI.
6. Upon reaching `AtPickUp`, the route is cleared and a terminal "YOUR CAR IS HERE" ETA is broadcast (Player only).

## 10. Internal Screen Architecture

`PlayerCarScreenBridge` monitors gates and session state to advance `InternalVehicleScreenController`:
`Default` → `Welcome` → `SelectDestination` → `AutonomousDrive` → `Deployment` → `PickUp` → `Goodbye` → `ResumeControl`

## 11. Crosswalk and Safety Architecture

Vehicle stops from two independent sources:
1. `SafetyTriggerDetector` sets `isSafetyWaiting`.
2. `CrosswalkTrafficLight` sets `IsCrosswalkStopped`.
`SmartCarNavigator` combines these (`isSafetyWaiting || isCrosswalkStopped`) to halt movement, activate `BlinkingLightAdapter`, and trigger a one-shot audio latch on `SafetyAudioAdapter.PlayAlert()`.

## 12. PlayerDetectionProxy Architecture

The player detection is isolated to avoid CharacterController and locomotion conflicts:
`XR Origin` → `PlayerDetectionProxy` (Child GameObject)
- Layer: `DynamicUser`
- Collider: `CapsuleCollider` (Trigger = true)
- Rigidbody: Kinematic (Gravity = false)
- Handled by `PlayerCarProgression` (Enabled outside, Disabled inside).

## 13. XR UI Interaction Architecture

The project uses a single Unity EventSystem with `XR UI Input Module`. Canvases use `Tracked Device Graphic Raycaster`. VR Interaction relies on controller ray/near-far interactors.

## 14. Important Event & Method Call Chains

- **Spawn**: `VRSimulationController.SpawnPlayerCar()` → `PlayerCarProgression.SpawnPlayerCar()`
- **Recall**: `CardReader.OnTriggerEnter` → `TotemRecallHandler.OnCardTappedAtTotem` → `ValetGuidanceSystem.RecallCar()` → `TotemScreenSequence.StartRecallSequence()` / `CarETAOrchestrator.StartETACalculation()`
- **Safety Audio**: `isSafetyWaiting` / `IsCrosswalkStopped` changed → `RefreshPedestrianWarningAudio()` → `if false->true` → `SafetyAudioAdapter.PlayAlert()`

## 15. Required Scene and Prefab Wiring

- `SampleScene.unity`: Must NOT contain `Player_TestCar` instance. `PlayerCarProgression.playerCarInstance` and `playerDetectionProxy` MUST be `None`.
- `TestCar.prefab`: Must contain `PlayerDetectionProxy` (Layer: `DynamicUser`) and `AHMI_SafetyInteractionSystem`.
- `AHMI_SafetyInteractionSystem.prefab`: Must have `SafetyAudioAdapter` with `Play On Awake` false and `Loop` false on the `AudioSource`.

## 16. Development Invariants and Prohibited Patterns

- **No Reflection**: AOT compilation requires explicit public/internal methods.
- **No `GameObject.Find`**: Use injected references or serialized fields.
- **No `SendMessage`**: Use UnityEvents or direct method calls.
- **No Per-Frame Polling for State**: Use event subscriptions.
- **Visuals Do Not Own Logic**: UI components must only reflect state, not drive navigation.
- **No Modifying XR Root Layer**: Player layer manipulation is handled strictly via the isolated `PlayerDetectionProxy`.

## 17. Known Risks and Failure Modes

- **Serialization Stale State**: If `playerCarInstance` is serialized in the scene as non-null, the TestCar will fail to spawn at runtime despite console success messages.
- **Avatar Height**: Exiting the vehicle may result in an incorrect avatar height offset depending on teleportation math.
- **NPC Deadlocks**: Extreme congestion can trap NavMeshAgents if prioritization fails.

## 18. Current Implementation Status

| Feature | Status | Notes |
| :--- | :--- | :--- |
| Valet Auto-Park & Auto-Exit | COMPLETED | Verified. |
| ETA Billboard & Logic | COMPLETED | Terminal "YOUR CAR IS HERE" implemented. |
| PlayerDetectionProxy | IMPLEMENTED, NOT YET RUNTIME-TESTED | Proxy enabled/disabled correctly in script. |
| Safety Audio One-Shot | IMPLEMENTED, NOT YET RUNTIME-TESTED | Latch implemented in SmartCarNavigator. |
| TestCar Spawn Fix | OPEN | Serialized scene reference still needs clearing. |
| Car 1 / Car 2 Safety | DEFERRED | Needs manual integration in prefabs. |

## 19. Safe Modification Checklist for AI Assistants

1. Always check for serialized scene instances before assuming runtime spawn logic works.
2. When modifying safety checks, preserve the combined state logic (`isSafetyWaiting || isCrosswalkStopped`).
3. Ensure audio `AudioSource` configurations prevent overlaps (one-shot latches, loop off).
4. Do not alter `CharacterController` bounds for detection; use the `PlayerDetectionProxy`.

## 20. Focused Troubleshooting Decision Trees

**TestCar Does Not Spawn**
- Does console log success? Yes → Check `SampleScene` for stale `Player_TestCar` or `PlayerCarProgression.playerCarInstance`.
- No → Check `Test Car Prefab` reference in inspector.

**Warning Sound Overlaps/Loops**
- Check `AudioSource.loop` is false.
- Check `RefreshPedestrianWarningAudio()` latch boolean is behaving properly.

**Player Not Detected By Cars**
- Check `PlayerDetectionProxy` is active.
- Check Layer is exactly `DynamicUser`.
- Check `SafetyTriggerDetector` layer mask.
