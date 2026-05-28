using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls individual NPC navigation with NavMeshAgent, synchronizes speed/direction
/// parameters with the NPCControllerF Animator, and handles in-place turning at fork points.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class HumanNPCBehavior : MonoBehaviour
{
    public SmartCarNavigator associatedCar;
    public bool isDriver = false;
    private ValetGuidanceSystem valetSystem;

    private NavMeshAgent agent;
    private Animator animator;

    private Transform centerPoint;
    private Transform finalElevator;

    private enum NPCState
    {
        Spawning,
        WalkingToCenter,
        TurningAtCenter,
        WalkingToElevator,
        ArrivedAtElevator,
        Sitting,
        WaitingAtElevator,
        WalkingToTotem,
        WaitingAtTotem,
        WalkingToCenterForReturn,
        WaitingAtCenterForReturn,
        WalkingToPickUp,
        WaitingAtPickUp,
        Boarding
    }

    private NPCState currentState = NPCState.Spawning;
    private float turnTimer = 0f;
    private Quaternion targetRotation;

    [Header("Area Arrival Radii")]
    [SerializeField] private float centerPointRadius = 1.8f;
    [SerializeField] private float elevatorRadius = 1.5f;
    [SerializeField] private float totemRadius = 1.5f;
    [SerializeField] private float returnCenterRadius = 2.0f;
    [SerializeField] private float pickUpRadius = 2.0f;

    /// <summary>
    /// Checks if the agent has arrived at its current destination using either
    /// standard stopping distance or an area-based proximity radius on the XZ plane.
    /// </summary>
    private bool HasArrivedAtDestination(float radius)
    {
        if (agent == null) return false;

        // 1. Standard check
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            return true;
        }

        // 2. Area-based proximity check on XZ plane
        Vector3 agentPos = transform.position;
        Vector3 destPos = agent.destination;
        agentPos.y = 0;
        destPos.y = 0;

        if (Vector3.Distance(agentPos, destPos) <= radius)
        {
            return true;
        }

        return false;
    }

    private void Awake()
    {
        EnsureReferences();
    }

    /// <summary>
    /// Safely ensures references to NavMeshAgent and Animator are loaded, even when component is added at runtime.
    /// </summary>
    private void EnsureReferences()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.updatePosition = true;
                agent.updateRotation = true;
            }
        }
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (valetSystem == null)
        {
            valetSystem = Object.FindAnyObjectByType<ValetGuidanceSystem>();
        }
    }

    /// <summary>
    /// Set up targets for walking to the center point first, then the elevator.
    /// </summary>
    public void SetupRoute(Transform center, Transform elevator)
    {
        EnsureReferences();

        // 1. Warp the agent to the nearest valid humanoid NavMesh position
        if (agent != null)
        {
            UnityEngine.AI.NavMeshQueryFilter filter = new UnityEngine.AI.NavMeshQueryFilter();
            filter.agentTypeID = agent.agentTypeID;
            filter.areaMask = UnityEngine.AI.NavMesh.AllAreas;

            Debug.Log($"[NPC Diagnostic] Spawning {gameObject.name} at {transform.position}. Agent Type ID: {agent.agentTypeID}. Agent enabled: {agent.enabled}.");

            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 30.0f, filter))
            {
                agent.enabled = false;
                agent.enabled = true;
                bool warpSuccess = agent.Warp(hit.position);
                Debug.Log($"[NPC Diagnostic] NavMesh found at {hit.position} (Distance: {Vector3.Distance(transform.position, hit.position):F2}m). Warp success: {warpSuccess}. agent.isOnNavMesh: {agent.isOnNavMesh}");
            }
            else
            {
                Debug.LogWarning($"[NPC Diagnostic] Could not find any NavMesh for agentTypeID {agent.agentTypeID} within 30.0m of {transform.position}!");
            }
        }
        else
        {
            Debug.LogError($"[NPC Diagnostic] NavMeshAgent is NULL on {gameObject.name} during SetupRoute!");
        }

        centerPoint = center;
        finalElevator = elevator;

        if (centerPoint != null)
        {
            currentState = NPCState.WalkingToCenter;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(centerPoint.position);
            }
            else
            {
                Debug.LogError($"[NPC] Cannot set destination to center point because {gameObject.name} is not on a valid NavMesh. (agent: {agent != null}, isOnNavMesh: {agent?.isOnNavMesh})");
            }
        }
        else if (finalElevator != null)
        {
            currentState = NPCState.WalkingToElevator;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(finalElevator.position);
            }
            else
            {
                Debug.LogError($"[NPC] Cannot set destination to elevator because {gameObject.name} is not on a valid NavMesh. (agent: {agent != null}, isOnNavMesh: {agent?.isOnNavMesh})");
            }
        }
        else
        {
            currentState = NPCState.Sitting;
            if (animator != null)
            {
                animator.SetBool("IsSitting", true);
            }
        }
    }

    private void Update()
    {
        EnsureReferences();

        if (agent == null || animator == null) return;

        CheckCrosswalkWaiting();

        // 1. Synchronize agent velocity magnitude with Animator Speed float
        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);

        // 2. Execute behaviors depending on the current NPC state
        switch (currentState)
        {
            case NPCState.WalkingToCenter:
                if (HasArrivedAtDestination(centerPointRadius))
                {
                    if (finalElevator != null)
                    {
                        // Stop moving and prepare to turn towards the designated elevator
                        currentState = NPCState.TurningAtCenter;
                        agent.isStopped = true;
                        agent.updateRotation = false; // Disable auto-rotation to let animation rotate or manually rotation lerp
                        
                        // Calculate heading to the chosen elevator
                        Vector3 dirToElevator = (finalElevator.position - transform.position).normalized;
                        dirToElevator.y = 0;
                        targetRotation = Quaternion.LookRotation(dirToElevator);

                        // Calculate turn angle: positive (right), negative (left)
                        float angle = Vector3.SignedAngle(transform.forward, dirToElevator, Vector3.up);
                        animator.SetFloat("TurnAngle", angle);
                        
                        turnTimer = 0f;
                    }
                    else
                    {
                        currentState = NPCState.ArrivedAtElevator;
                        EnterElevator();
                    }
                }
                break;

            case NPCState.TurningAtCenter:
                turnTimer += Time.deltaTime;
                
                // Smoothly slerp towards the target elevator direction
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 3.5f);

                // Check if alignment is near complete or timeout reached
                if (Quaternion.Angle(transform.rotation, targetRotation) < 5f || turnTimer > 2.0f)
                {
                    animator.SetFloat("TurnAngle", 0f);
                    agent.updateRotation = true;
                    agent.isStopped = false;
                    
                    currentState = NPCState.WalkingToElevator;
                    agent.SetDestination(finalElevator.position);
                }
                break;

            case NPCState.WalkingToElevator:
                if (HasArrivedAtDestination(elevatorRadius))
                {
                    currentState = NPCState.ArrivedAtElevator;
                    EnterElevator();
                }
                break;

            case NPCState.WaitingAtElevator:
                turnTimer += Time.deltaTime;
                float waitTime = 15f;
                if (valetSystem != null && associatedCar != null)
                {
                    ValetSession session = valetSystem.activeSessions.Find(s => s.car == associatedCar);
                    if (session != null)
                    {
                        waitTime = session.returnWaitTime;
                    }
                }

                if (turnTimer >= waitTime)
                {
                    SetNPCVisibility(true); // Reappear when wait time is over
                    if (isDriver)
                    {
                        // Find all active GameObjects named "Totem"
                        List<GameObject> totems = new List<GameObject>();
                        foreach (GameObject obj in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                        {
                            if (obj.activeInHierarchy && obj.name == "Totem")
                            {
                                totems.Add(obj);
                            }
                        }

                        GameObject nearestTotem = null;
                        float minDistance = float.MaxValue;
                        foreach (GameObject totem in totems)
                        {
                            float dist = Vector3.Distance(transform.position, totem.transform.position);
                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                nearestTotem = totem;
                            }
                        }

                        if (nearestTotem != null)
                        {
                            currentState = NPCState.WalkingToTotem;
                            agent.isStopped = false;

                            // Look for child named WaitSpot or Wait Spot
                            Transform waitSpot = nearestTotem.transform.Find("WaitSpot");
                            if (waitSpot == null) waitSpot = nearestTotem.transform.Find("Wait Spot");
                            Vector3 destPos = waitSpot != null ? waitSpot.position : nearestTotem.transform.position;

                            agent.SetDestination(destPos);
                            Debug.Log($"[NPC] Driver {gameObject.name} finished waiting at elevator. Walking to nearest Totem WaitSpot at {destPos}");
                        }
                        else
                        {
                            Debug.LogWarning("[NPC] No active GameObjects named 'Totem' found. Sitting/Waiting at elevator.");
                            currentState = NPCState.Sitting;
                            animator.SetBool("IsSitting", true);
                        }
                    }
                    else
                    {
                        // Non-drivers head back to CenterPoint to wait
                        if (centerPoint != null)
                        {
                            currentState = NPCState.WalkingToCenterForReturn;
                            agent.isStopped = false;
                            agent.SetDestination(centerPoint.position);
                            Debug.Log($"[NPC] Passenger {gameObject.name} finished waiting at elevator. Walking back to CenterPoint at {centerPoint.position}");
                        }
                        else
                        {
                            Debug.LogWarning("[NPC] centerPoint reference missing. Sitting/Waiting at elevator.");
                            currentState = NPCState.Sitting;
                            animator.SetBool("IsSitting", true);
                        }
                    }
                }
                break;

            case NPCState.WalkingToTotem:
                if (HasArrivedAtDestination(totemRadius))
                {
                    currentState = NPCState.WaitingAtTotem;
                    agent.isStopped = true;
                    turnTimer = 0f; // Use turnTimer for 5s waiting time at the totem
                    Debug.Log($"[NPC] Driver {gameObject.name} reached Totem. Waiting 5s before recall.");
                }
                break;

            case NPCState.WaitingAtTotem:
                turnTimer += Time.deltaTime;
                if (turnTimer >= 5.0f)
                {
                    // Trigger recall of the associated car after 5s
                    if (associatedCar != null && valetSystem != null)
                    {
                        valetSystem.RecallCar(associatedCar);
                        
                        // Retrieve the assigned pickUpSpot from the valet session
                        ValetSession session = valetSystem.activeSessions.Find(s => s.car == associatedCar);
                        if (session != null && session.pickUpSpot != null)
                        {
                            Vector3 spotPos = session.pickUpSpot.position;
                            
                            // Sample a position on Humanoid NavMesh near the pickUpSpot (which maps to the adjacent sidewalk)
                            UnityEngine.AI.NavMeshHit hit;
                            UnityEngine.AI.NavMeshQueryFilter filter = new UnityEngine.AI.NavMeshQueryFilter();
                            filter.agentTypeID = agent.agentTypeID;
                            filter.areaMask = UnityEngine.AI.NavMesh.AllAreas;
                            
                            if (UnityEngine.AI.NavMesh.SamplePosition(spotPos, out hit, 15.0f, filter))
                            {
                                currentState = NPCState.WalkingToPickUp;
                                agent.isStopped = false;
                                agent.SetDestination(hit.position);
                                Debug.Log($"[NPC] Car recalled. Driver walking to pick up spot sidewalk at {hit.position}");
                            }
                            else
                            {
                                currentState = NPCState.WalkingToPickUp;
                                agent.isStopped = false;
                                agent.SetDestination(spotPos);
                                Debug.LogWarning($"[NPC] Could not find Humanoid NavMesh near pickup spot {spotPos}, walking directly.");
                            }
                        }
                        else
                        {
                            Debug.LogError($"[NPC] No valet session or pickUpSpot found for car {associatedCar.name}!");
                            currentState = NPCState.Sitting;
                            animator.SetBool("IsSitting", true);
                        }
                    }
                    else
                    {
                        Debug.LogError($"[NPC] Associated car or valetSystem is null when recalling!");
                        currentState = NPCState.Sitting;
                        animator.SetBool("IsSitting", true);
                    }
                }
                break;

            case NPCState.WalkingToCenterForReturn:
                if (HasArrivedAtDestination(returnCenterRadius))
                {
                    currentState = NPCState.WaitingAtCenterForReturn;
                    agent.isStopped = true;
                    Debug.Log($"[NPC] Passenger {gameObject.name} reached CenterPoint. Waiting for car recall.");
                }
                break;

            case NPCState.WaitingAtCenterForReturn:
                if (associatedCar != null && valetSystem != null)
                {
                    ValetSession session = valetSystem.activeSessions.Find(s => s.car == associatedCar);
                    if (session != null && (session.state == ValetState.MovingToPickUp || session.state == ValetState.AtPickUp))
                    {
                        if (session.pickUpSpot != null)
                        {
                            Vector3 spotPos = session.pickUpSpot.position;
                            
                            // Sample a position on Humanoid NavMesh near the pickUpSpot (which maps to the adjacent sidewalk)
                            UnityEngine.AI.NavMeshHit hit;
                            UnityEngine.AI.NavMeshQueryFilter filter = new UnityEngine.AI.NavMeshQueryFilter();
                            filter.agentTypeID = agent.agentTypeID;
                            filter.areaMask = UnityEngine.AI.NavMesh.AllAreas;
                            
                            if (UnityEngine.AI.NavMesh.SamplePosition(spotPos, out hit, 15.0f, filter))
                            {
                                currentState = NPCState.WalkingToPickUp;
                                agent.isStopped = false;
                                agent.SetDestination(hit.position);
                                Debug.Log($"[NPC] Passenger heading to pick up spot sidewalk at {hit.position}");
                            }
                            else
                            {
                                currentState = NPCState.WalkingToPickUp;
                                agent.isStopped = false;
                                agent.SetDestination(spotPos);
                                Debug.LogWarning($"[NPC] Could not find Humanoid NavMesh near pickup spot {spotPos}, walking directly.");
                            }
                        }
                        else
                        {
                            Debug.LogError($"[NPC] Valet session has no pickUpSpot for car {associatedCar.name}!");
                            currentState = NPCState.Sitting;
                            animator.SetBool("IsSitting", true);
                        }
                    }
                }
                break;

            case NPCState.WalkingToPickUp:
                if (HasArrivedAtDestination(pickUpRadius))
                {
                    currentState = NPCState.WaitingAtPickUp;
                    agent.isStopped = true;
                    Debug.Log($"[NPC] Reached pick up spot sidewalk. Waiting for car {associatedCar?.name}.");
                }
                break;

            case NPCState.WaitingAtPickUp:
                if (associatedCar != null && valetSystem != null)
                {
                    ValetSession session = valetSystem.activeSessions.Find(s => s.car == associatedCar);
                    if (session != null && session.state == ValetState.AtPickUp)
                    {
                        float distToCar = Vector3.Distance(transform.position, associatedCar.transform.position);
                        if (distToCar < 8.0f)
                        {
                            currentState = NPCState.Boarding;
                            agent.isStopped = false;
                            agent.SetDestination(associatedCar.transform.position);
                            Debug.Log($"[NPC] Car has arrived at pickup zone. Moving to board car.");
                        }
                    }
                }
                break;

            case NPCState.Boarding:
                if (associatedCar != null)
                {
                    float distToCar = Vector3.Distance(transform.position, associatedCar.transform.position);
                    if (distToCar < 2.5f)
                    {
                        Debug.Log($"[NPC] Boarded car {associatedCar.name} successfully. Notifying valet system.");
                        if (valetSystem != null)
                        {
                            valetSystem.PassengerBoarded(associatedCar);
                        }
                        Destroy(gameObject);
                    }
                    else
                    {
                        // Keep setting destination to the car in case it moved/drew closer
                        agent.SetDestination(associatedCar.transform.position);
                    }
                }
                break;
        }
    }

    private void EnterElevator()
    {
        Debug.Log($"[NPC] {gameObject.name} reached destination elevator. Transitioning to WaitingAtElevator.");
        currentState = NPCState.WaitingAtElevator;
        turnTimer = 0f; // Use as wait timer
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        SetNPCVisibility(false); // Hide NPC when entering elevator
    }

    private void SetNPCVisibility(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.enabled = visible;
        }

        if (animator != null)
        {
            animator.enabled = visible;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = visible;
        }
    }

    /// <summary>
    /// Can be called to snap the NPC to a chair slot and enter the sit animation.
    /// </summary>
    public void GoSit(Transform chairTransform)
    {
        EnsureReferences();

        currentState = NPCState.Sitting;
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        transform.position = chairTransform.position;
        transform.rotation = chairTransform.rotation;
        
        if (animator != null)
        {
            animator.SetBool("IsSitting", true);
        }
    }

    private void CheckCrosswalkWaiting()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        // Only halt NPCs that are actively walking
        if (currentState != NPCState.WalkingToCenter &&
            currentState != NPCState.WalkingToElevator &&
            currentState != NPCState.WalkingToTotem &&
            currentState != NPCState.WalkingToCenterForReturn &&
            currentState != NPCState.WalkingToPickUp &&
            currentState != NPCState.Boarding)
        {
            return;
        }

        CrosswalkTrafficLight[] crosswalks = Object.FindObjectsByType<CrosswalkTrafficLight>(FindObjectsSortMode.None);
        bool shouldStop = false;

        foreach (var crosswalk in crosswalks)
        {
            if (crosswalk == null) continue;

            // Light is only green (CanPedestriansCross) when no car is active.
            // If a car is active, pedestrians must wait.
            if (crosswalk.CanPedestriansCross) continue;

            // Define the crosswalk approach zone: slightly in front of the crosswalk bounds
            float centreX     = crosswalk.CrosswalkCentreX;
            float halfWidth   = crosswalk.CrosswalkHalfWidth;
            float halfDepth   = crosswalk.CrosswalkHalfDepth;
            float approachPad = 2.0f; // extra buffer before the crosswalk edge

            float minX = centreX - halfWidth - approachPad;
            float maxX = centreX + halfWidth + approachPad;
            float centreZ = crosswalk.CrosswalkCentreZ;
            float minZ = centreZ - halfDepth - approachPad;
            float maxZ = centreZ + halfDepth + approachPad;

            Vector3 pos = transform.position;

            // Is the NPC inside (or approaching) the crosswalk zone?
            bool inZone = pos.x >= minX && pos.x <= maxX && pos.z >= minZ && pos.z <= maxZ;
            if (!inZone) continue;

            // Is the NPC heading towards (or across) the crosswalk centre?
            Vector3 toCentre = new Vector3(centreX, pos.y, centreZ) - pos;
            Vector3 toDest   = agent.destination - pos;
            toCentre.y = 0f;
            toDest.y   = 0f;

            if (toCentre.sqrMagnitude < 0.001f || toDest.sqrMagnitude < 0.001f) continue;

            float dot = Vector3.Dot(toCentre.normalized, toDest.normalized);
            if (dot > 0.1f)
            {
                shouldStop = true;
                break;
            }
        }

        if (shouldStop)
        {
            if (!agent.isStopped)
            {
                agent.isStopped = true;
                Debug.Log($"[HumanNPCBehavior] {gameObject.name}: Stopping — car is active at crosswalk (signal RED).");
            }
        }
        else
        {
            if (agent.isStopped)
            {
                agent.isStopped = false;
                Debug.Log($"[HumanNPCBehavior] {gameObject.name}: Resuming — crosswalk is GREEN.");
            }
        }
    }

}
