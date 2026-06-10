using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the player's custom VR TestCar. Handles spawning, boarding (parenting the XR Origin),
/// and manually progressing through the valet parking states via keyboard controls.
/// </summary>
public class PlayerCarProgression : MonoBehaviour
{
    [Header("Player Car Configuration")]
    [Tooltip("Drag the 'TestCar' prefab from Assets/PreFabs here.")]
    public GameObject testCarPrefab;

    [Header("VR Rig Configuration")]
    [Tooltip("Drag the 'XR Origin (XR Rig)' from the scene hierarchy here.")]
    public GameObject xrOrigin;

    [Tooltip("Seat offset relative to the car position for sitting inside.")]
    public Vector3 seatLocalOffset = new Vector3(-0.35f, 0.5f, 0.2f);

    [Header("System References")]
    public NPCCarSpawner spawner;
    public ValetGuidanceSystem valetSystem;

    [Header("Status (Read Only)")]
    [SerializeField] private GameObject playerCarInstance;
    [SerializeField] private SmartCarNavigator playerCarNavigator;
    [SerializeField] private bool isPlayerInsideCar = false;
    [SerializeField] private ValetState playerCarState = ValetState.Manual;

    private void Start()
    {
        // Auto-find references if missing
        if (spawner == null)
        {
            spawner = Object.FindAnyObjectByType<NPCCarSpawner>();
        }
        if (valetSystem == null)
        {
            valetSystem = Object.FindAnyObjectByType<ValetGuidanceSystem>();
        }
        if (xrOrigin == null)
        {
            xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        }

        if (spawner == null || valetSystem == null)
        {
            Debug.LogError("[PlayerCarProgression] Spawner or ValetGuidanceSystem reference is missing!");
        }
        if (xrOrigin == null)
        {
            Debug.LogWarning("[PlayerCarProgression] XR Origin (XR Rig) not found by name in scene. You may need to assign it manually.");
        }
    }

    private void Update()
    {
        // Check if keyboard is available
        if (Keyboard.current == null) return;

        // 1. Spawning the car (Key P)
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            SpawnPlayerCar();
        }

        // 2. Getting in/out of the car (Key G)
        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            ToggleBoarding();
        }

        // 3. Progressing to the next valet state (Key Enter/Return)
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            ProgressValetState();
        }

        // Sync visual status for inspector
        UpdateStatus();
    }

    public void SpawnPlayerCar()
    {
        if (playerCarInstance != null)
        {
            Debug.LogWarning("[PlayerCarProgression] Player car has already been spawned!");
            return;
        }

        if (testCarPrefab == null)
        {
            Debug.LogError("[PlayerCarProgression] Cannot spawn: TestCarPrefab is not assigned!");
            return;
        }

        if (spawner == null || spawner.spawnPoint == null)
        {
            Debug.LogError("[PlayerCarProgression] Spawner or spawn point is missing!");
            return;
        }

        // 1. Pause NPC Spawner
        spawner.isSpawningPaused = true;
        Debug.Log("[PlayerCarProgression] NPC Car Spawner has been paused for player insertion.");

        // 2. Get correct agent type ID from spawner
        int targetAgentTypeID = -1923039037;
        NavMeshAgent spawnerAgent = spawner.carPrefabs.Length > 0 ? spawner.carPrefabs[0].GetComponent<NavMeshAgent>() : null;
        if (spawnerAgent != null)
        {
            targetAgentTypeID = spawnerAgent.agentTypeID;
        }

        // Determine Spawn Pos and Rotation (preserving prefab rotation offsets)
        Vector3 spawnPos = spawner.spawnPoint.position;
        Quaternion spawnRot = Quaternion.Euler(spawner.spawnRotation) * testCarPrefab.transform.localRotation;

        NavMeshHit hit;
        NavMeshQueryFilter filter = new NavMeshQueryFilter();
        filter.agentTypeID = targetAgentTypeID;
        filter.areaMask = NavMesh.AllAreas;

        if (NavMesh.SamplePosition(spawnPos, out hit, 15f, filter))
        {
            spawnPos = hit.position;
        }

        // 3. Instantiate the vehicle (deactivate prefab temporarily to prevent NavMeshAgent awake binding error)
        bool wasActive = testCarPrefab.activeSelf;
        testCarPrefab.SetActive(false);
        playerCarInstance = Instantiate(testCarPrefab, spawnPos, spawnRot);
        testCarPrefab.SetActive(wasActive);

        playerCarInstance.name = "Player_TestCar";
        playerCarInstance.transform.localScale = testCarPrefab.transform.localScale;

        // 4. Configure Components (matching NPC car configuration)
        NavMeshAgent agent = playerCarInstance.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = playerCarInstance.AddComponent<NavMeshAgent>();
        }
        
        // Configure correct type first
        agent.agentTypeID = targetAgentTypeID;
        agent.speed = spawner.agentSpeed;
        agent.angularSpeed = spawner.agentAngularSpeed;
        agent.acceleration = spawner.agentAcceleration;
        agent.radius = spawner.agentRadius;
        agent.height = spawner.agentHeight;
        agent.baseOffset = spawner.agentBaseOffset;
        agent.stoppingDistance = spawner.agentStoppingDistance;
        agent.avoidancePriority = 10; // High priority for player's car
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.autoBraking = true;
        agent.autoRepath = true;

        playerCarNavigator = playerCarInstance.GetComponent<SmartCarNavigator>();
        if (playerCarNavigator == null)
        {
            playerCarNavigator = playerCarInstance.AddComponent<SmartCarNavigator>();
        }
        playerCarNavigator.IsParked = false;
        
        // Hold the car at the spawn point initially
        playerCarNavigator.IsCrosswalkStopped = true;

        if (string.IsNullOrEmpty(playerCarNavigator.plateNumber))
        {
            playerCarNavigator.plateNumber = "PLAYER-1";
        }

        // Activate and Warp agent onto NavMesh
        playerCarInstance.SetActive(true);
        if (NavMesh.SamplePosition(spawnPos, out hit, 10f, filter))
        {
            Vector3 warpPos = new Vector3(hit.position.x, hit.position.y + agent.baseOffset, hit.position.z);
            agent.Warp(warpPos);
            playerCarInstance.transform.position = warpPos;
        }

        // 5. Register with valet system
        if (valetSystem != null)
        {
            valetSystem.RegisterCar(playerCarNavigator);
            ValetSession session = valetSystem.activeSessions.Find(s => s.car == playerCarNavigator);
            if (session != null)
            {
                session.isPlayerSession = true;
                Debug.Log("<color=green>[PlayerCarProgression] Player car registered in valet system as PlayerSession.</color>");
            }
        }
    }

    public void ToggleBoarding()
    {
        if (playerCarInstance == null)
        {
            Debug.LogWarning("[PlayerCarProgression] Cannot board: Player car is not spawned yet.");
            return;
        }

        if (xrOrigin == null)
        {
            Debug.LogError("[PlayerCarProgression] XR Origin reference is missing!");
            return;
        }

        CharacterController cc = xrOrigin.GetComponent<CharacterController>();

        if (!isPlayerInsideCar)
        {
            // Get In
            if (cc != null) cc.enabled = false; // Disable character controller to allow manual positioning
            
            // Calculate world position of the seat
            Vector3 seatWorldPos = playerCarInstance.transform.TransformPoint(seatLocalOffset);

            // Find the Main Camera under the XR Origin to offset the rig base correctly
            Camera mainCam = xrOrigin.GetComponentInChildren<Camera>();
            if (mainCam != null)
            {
                // Align so that the camera itself (player's head) is in the seat, not the rig's feet/floor pivot
                Vector3 camToRigOffset = xrOrigin.transform.position - mainCam.transform.position;
                xrOrigin.transform.position = seatWorldPos + camToRigOffset;
            }
            else
            {
                xrOrigin.transform.position = seatWorldPos;
            }

            xrOrigin.transform.rotation = playerCarInstance.transform.rotation;
            
            // Parent to the car (maintains the world position we just set)
            xrOrigin.transform.SetParent(playerCarInstance.transform);
            
            isPlayerInsideCar = true;
            Debug.Log("[PlayerCarProgression] Boarded the car. Parented XR Origin to vehicle.");

            // Start moving the car now that the player has boarded (if we are in the initial approach phase)
            if (playerCarNavigator != null && playerCarState == ValetState.ApproachingDropOff)
            {
                playerCarNavigator.IsCrosswalkStopped = false;
                Debug.Log("[PlayerCarProgression] Player has boarded. Releasing car to proceed to entrance.");
            }
        }
        else
        {
            // Get Out
            xrOrigin.transform.SetParent(null);
            
            // Spawn next to the driver's side (left)
            Vector3 exitPos = playerCarInstance.transform.position - playerCarInstance.transform.right * 1.5f;
            // Snap to ground
            NavMeshHit hit;
            if (NavMesh.SamplePosition(exitPos, out hit, 3.0f, NavMesh.AllAreas))
            {
                exitPos = hit.position;
            }
            else
            {
                exitPos.y = playerCarInstance.transform.position.y;
            }

            xrOrigin.transform.position = exitPos;
            xrOrigin.transform.rotation = Quaternion.LookRotation(playerCarInstance.transform.forward);

            if (cc != null) cc.enabled = true; // Re-enable character controller
            isPlayerInsideCar = false;
            Debug.Log("[PlayerCarProgression] Exited the car. Unparented XR Origin.");

            // Stop the car if the player gets out before reaching the drop-off
            if (playerCarNavigator != null && playerCarState == ValetState.ApproachingDropOff)
            {
                playerCarNavigator.IsCrosswalkStopped = true;
                Debug.Log("[PlayerCarProgression] Player exited during approach. Stopping car.");
            }
        }
    }

    private void ProgressValetState()
    {
        if (playerCarNavigator == null || valetSystem == null)
        {
            Debug.LogWarning("[PlayerCarProgression] No player car or valet system found to progress.");
            return;
        }

        ValetSession session = valetSystem.activeSessions.Find(s => s.car == playerCarNavigator);
        if (session == null)
        {
            Debug.LogWarning("[PlayerCarProgression] No active valet session for the player's car.");
            return;
        }

        switch (session.state)
        {
            case ValetState.AtDropOff:
                // Move from DropOff to Parking Spot
                // Use reflection or access fields to call AdvanceSessionState
                // Since AdvanceSessionState is private, we can invoke it or make it public if needed.
                // Wait! Let's check: in ValetGuidanceSystem.cs, is there a public method?
                // RecallCar and CompletePassengerBoarding are public and call AdvanceSessionState!
                // Wait, but for AtDropOff to MovingToPark, there is no public method because it was automatic.
                // Let's check if we can make a public progression call or reflection.
                // To keep it simple, we can make ValetGuidanceSystem's AdvanceSessionState public, OR use reflection!
                // Let's use reflection so we don't need to change other scripts too much, or actually we can just call it via reflection.
                // Reflection is very clean and doesn't require modifying public API.
                // Let's invoke AdvanceSessionState via reflection:
                InvokePrivateMethod(valetSystem, "AdvanceSessionState", session);
                
                // Resume NPC Spawner since player is clear of the entrance
                if (spawner != null)
                {
                    spawner.isSpawningPaused = false;
                    Debug.Log("[PlayerCarProgression] Resumed NPC Spawner: Player car has left drop-off zone.");
                }
                break;

            case ValetState.Parked:
                // Move from Parked to Pickup Spot
                valetSystem.RecallCar(playerCarNavigator);
                break;

            case ValetState.AtPickUp:
                // Move from Pickup to Exit
                valetSystem.CompletePassengerBoarding(playerCarNavigator);
                break;

            default:
                Debug.Log($"[PlayerCarProgression] Cannot progress valet state manually while car is in '{session.state}' state.");
                break;
        }
    }

    private void InvokePrivateMethod(object target, string methodName, params object[] parameters)
    {
        var method = target.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(target, parameters);
        }
        else
        {
            Debug.LogError($"[PlayerCarProgression] Method '{methodName}' not found on type '{target.GetType().Name}'");
        }
    }

    private void UpdateStatus()
    {
        if (playerCarNavigator != null && valetSystem != null)
        {
            ValetSession session = valetSystem.activeSessions.Find(s => s.car == playerCarNavigator);
            if (session != null)
            {
                playerCarState = session.state;
            }
            else
            {
                playerCarState = ValetState.Exited;
                playerCarInstance = null;
                playerCarNavigator = null;
                isPlayerInsideCar = false;
            }
        }
        else
        {
            playerCarState = ValetState.Manual;
        }
    }

}
