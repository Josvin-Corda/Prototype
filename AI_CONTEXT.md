# AI Context: Valet Guidance & Safety VR Simulation Project

This file provides system architecture, component mappings, state machines, and code design guidelines to help AI coding assistants understand and modify this Unity VR codebase safely.

---

## 1. Subsystem Directory & Component Map

The simulation is built in Unity using VR-native interfaces (XR Interaction Toolkit) and a NavMesh-based vehicle routing backend.

```
Root Workspace
├── Assets
│   ├── PreFabs
│   │   ├── TestCar.prefab                 # Player's custom driveable car (car_chair, StartingPoint, buttons)
│   │   └── VRControlPanelCanvas.prefab    # Spawns in front of the player (2x2 grid menu)
│   │
│   ├── Resources
│   │   └── CarDatabase.json               # JSON database containing car plates, owners, passenger count, and payment card numbers
│   │
│   ├── Scripts/CarTraffic
│   │   ├── ValetGuidanceSystem.cs          # Central brain. Manages active sessions, routes, parking spots, and ETAs.
│   │   ├── PlayerCarProgression.cs        # Spawns/manages Player TestCar, tracking seating (inside/outside) and delay timers.
│   │   ├── VRSimulationController.cs      # Spawns UI canvas, manages Left Controller Y button spawning of PaymentCard.
│   │   ├── BarrierGate.cs                 # entrance/exit gate controller. NPC gate auto-mimic (silent), player physical card tap.
│   │   ├── CardReader.cs                  # Triggers on collision with PaymentCard. Plays procedural 1.2 kHz beep sounds.
│   │   ├── PaymentCard.cs                 # Attached to spawned card, stores cardNumber ("PLAYER-CARD-9999").
│   │   ├── TotemRecallHandler.cs          # Attached to Totem CardReader, recalls car if matched.
│   │   ├── NPCCarSpawner.cs               # Manages active NPC traffic spawner. Paused during player spawn.
│   │   ├── SmartCarNavigator.cs           # Car NavMesh steering controller.
│   │   ├── HumanNPCBehavior.cs            # NPC driver pedestrian state machine (elevator -> wait spot at totem 2.5s -> recall).
│   │   └── NPCManager.cs                  # Spawns human NPCs at drop-off zone.
│   │
│   ├── AHMI_MyPlayer
│   │   ├── TeleportButton.cs              # Handles physical seat entry (ReturnButton) and cockpit exit (ChangeButton) triggers.
│   │   └── CarExitManager.cs              # Legacy exit script (mostly deprecated by TeleportButton).
│   │
│   └── Editor
│       ├── CreateVRCanvasPrefab.cs        # Generates dashboard UI textures and canvas.
│       ├── SetupCardReaders.cs            # Injects card readers at gate ticket machine and totems.
│       └── SetupTestCarInteractables.cs   # Duplicates and mirrors door handles on the TestCar prefab.
```

---

## 2. Valet Simulation State Machine (`ValetState`)

Every vehicle (NPC or Player) is tracked by `ValetGuidanceSystem` in a `ValetSession`. The lifecycle proceeds as:

1.  **`Manual`**: Spawns and stops at the crosswalk. Headlights are standard.
2.  **`ApproachingDropOff`**: Headlights turn turquoise (Autonomous Mode). Steers to drop-off.
3.  **`AtDropOff`**: Car stops. 
    *   *NPC*: Waits `dropOffWaitTime` then automatically drives to parking spot.
    *   *Player*: Stays in drop-off indefinitely. When player exits (`IsPlayerInsideCar` becomes `false`), starts a **5-second delay** then drives to parking spot.
4.  **`MovingToPark`**: Autonomous routing to a randomly assigned vacant parking spot.
5.  **`Parked`**: Car is reverse parked.
    *   *NPC*: Recalled automatically when their spawned Driver NPC walks to the Totem and stands at the wait spot for **2.5 seconds**.
    *   *Player*: Stays parked indefinitely. Recalled *only* when the player physically taps their spawned `PaymentCard` on the lobby Totem's `CardReader`.
6.  **`MovingToPickUp`**: Autonomous routing to a vacant pick-up spot.
7.  **`AtPickUp`**: Car stops.
    *   *NPC*: Driver and passengers board, and car automatically exits.
    *   *Player*: Waits indefinitely. Once player boards (`IsPlayerInsideCar` becomes `true`), starts a **5-second delay** then automatically drives to the exit.
8.  **`Exiting`**: Car drives to the exit boundary.
9.  **`Exited`**:
    *   *NPC*: Car is immediately destroyed and session removed.
    *   *Player*: Car drives to the **final exit node** where NPCs normally disappear, turns off its headlights, and stops. Once the player exits the vehicle, the car is destroyed and session removed.

---

## 3. Important Development Rules & Best Practices

If you are an AI assistant making modifications to this repository, you **MUST** follow these practices:

### A. Avoid C# Reflection
*   To keep the codebase 100% compatible with standalone AOT compilers (like IL2CPP used on Meta Quest), do **not** use Reflection (`GetMethod`, `GetField`, `Invoke`) to access private members.
*   Make methods or fields `public` or `internal` (e.g. `AdvanceSessionState`, `UpdateTrafficProviderData`, and `SetPlayerInsideCar`) instead of calling them via reflection.

### B. Rigidbodies & Trigger Colliders
*   Unity trigger overlaps (`OnTriggerEnter`) require at least one participating collider to have a `Rigidbody`.
*   The dynamically spawned payment card (`PaymentCard`) has a **kinematic Rigidbody** and a trigger `BoxCollider` attached to it, ensuring it triggers static card reader colliders as the controller moves.

### C. Seating Teleportation & CharacterController Safety
*   To teleport the player Rig (`XR Origin (XR Rig)`) safely without physics conflicts or camera drift, you **must disable the `CharacterController` component** before setting the position/rotation, and re-enable it afterward:
    ```csharp
    CharacterController cc = xrOrigin.GetComponent<CharacterController>();
    if (cc != null) cc.enabled = false;
    xrOrigin.transform.position = target.position;
    xrOrigin.transform.rotation = target.rotation;
    if (cc != null) cc.enabled = true;
    ```
*   **Locomotion**: When the player is inside the vehicle (`IsPlayerInsideCar` is true), the VR locomotion system (`Locomotion` child under `XR Origin`) **must be deactivated** to prevent drift. Re-activate it upon exit.

### D. Centering VR UI Canvas
*   To position the VR dashboard canvas in front of the player without vertical tilting:
    *   Project the camera forward vector onto the horizontal XZ plane.
    *   Use `Quaternion.LookRotation` to orient the UI upright, facing the player's head.
