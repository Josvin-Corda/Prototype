using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Handles realistic car movement, follows node paths, and reverses out of spots.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class SmartCarNavigator : MonoBehaviour
{
    [Header("Car Physics")]
    public float reverseSpeed = 2f;
    public float reverseTime = 2.5f;
    public float wheelbase = 1.2f;
    public float wheelRadius = 0.35f;
    public float maxSteerAngle = 50f;

    [Header("Valet Configuration")]
    [Tooltip("Unique registration plate number of the vehicle. If empty, the valet system will generate a random one.")]
    public string plateNumber;

    private NavMeshAgent agent;
    private bool isParked = true; 
    private bool isReversing = false;

    private Transform wheelFL;
    private Transform wheelFR;
    private Transform wheelRL;
    private Transform wheelRR;
    private float currentSpinAngle = 0f;
    private Transform currentSpot;

    public bool IsParked
    {
        get => isParked;
        set => isParked = value;
    }

    public event System.Action OnDestinationReached;

    // Routing data
    private List<TrafficNode> currentPath;
    private int pathIndex = 0;
    private Transform finalSpot;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updatePosition = false; // Decouple agent position from transform
            agent.updateRotation = false; // Decouple agent rotation from transform
        }

        // Cache wheels
        wheelFL = transform.Find("Wheel_FL");
        wheelFR = transform.Find("Wheel_FR");
        wheelRL = transform.Find("Wheel_RL");
        wheelRR = transform.Find("Wheel_RR");
    }

    void Start()
    {
        if (agent != null)
        {
            if (agent.isOnNavMesh)
            {
                return;
            }

            UnityEngine.AI.NavMeshQueryFilter filter = new UnityEngine.AI.NavMeshQueryFilter();
            filter.agentTypeID = agent.agentTypeID;
            filter.areaMask = UnityEngine.AI.NavMesh.AllAreas;

            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 10f, filter))
            {
                Vector3 warpPos = new Vector3(hit.position.x, hit.position.y + agent.baseOffset, hit.position.z);
                agent.Warp(warpPos);
                transform.position = warpPos;
                agent.nextPosition = transform.position;
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[SmartCarNavigator] Could not find NavMesh for agent type {agent.agentTypeID} near {transform.position}");
            }
        }
    }

    void Update()
    {
        if (isReversing) return;

        float currentSpeed = 0f;
        float steerAngle = 0f;

        if (isParked)
        {
            if (agent.enabled && agent.isOnNavMesh && !agent.isStopped)
            {
                agent.isStopped = true;
            }

            // Smoothly align vehicle with the designated spot's position and rotation
            if (currentSpot != null)
            {
                Vector3 targetPos = currentSpot.position;
                
                // Align with the spot's rotation (or 180 degrees offset if the car is facing the other way)
                Quaternion targetRot = currentSpot.rotation;
                if (Vector3.Dot(transform.forward, currentSpot.forward) < 0f)
                {
                    targetRot = currentSpot.rotation * Quaternion.Euler(0f, 180f, 0f);
                }

                // Keep target position at the grounded height (NavMesh height + baseOffset)
                float groundedY = transform.position.y;
                if (agent.enabled && agent.isOnNavMesh)
                {
                    groundedY = agent.nextPosition.y;
                }
                else
                {
                    UnityEngine.AI.NavMeshHit hit;
                    UnityEngine.AI.NavMeshQueryFilter filter = new UnityEngine.AI.NavMeshQueryFilter();
                    filter.agentTypeID = agent.agentTypeID;
                    filter.areaMask = UnityEngine.AI.NavMesh.AllAreas;
                    if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out hit, 10f, filter))
                    {
                        groundedY = hit.position.y + agent.baseOffset;
                    }
                }
                targetPos.y = groundedY;

                Vector3 newPos = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime * 3f);
                newPos.y = groundedY;
                transform.position = newPos;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 6f);

                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.nextPosition = transform.position;
                }
            }
        }
        else if (agent.enabled && agent.isOnNavMesh && !agent.isStopped)
        {
            // Get target speed from agent's pathfinding velocity
            Vector3 desiredVel = agent.desiredVelocity;
            currentSpeed = desiredVel.magnitude;

            if (currentSpeed > 0.01f)
            {
                // Smoothly rotate the heading towards the steering target
                Vector3 targetDir = agent.steeringTarget - transform.position;
                targetDir.y = 0f; // Keep rotation in horizontal plane
                if (targetDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(targetDir.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 6f);
                }

                // Calculate visual wheel steer angle
                Vector3 localTarget = transform.InverseTransformPoint(agent.steeringTarget);
                float targetAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
                steerAngle = Mathf.Clamp(targetAngle, -maxSteerAngle, maxSteerAngle);
            }

            // Move the vehicle strictly forward along its current heading (horizontal movement)
            Vector3 movement = transform.forward * currentSpeed * Time.deltaTime;
            transform.position += movement;

            // Snap the transform's Y coordinate to the agent's nextPosition.y (the NavMesh height)
            transform.position = new Vector3(transform.position.x, agent.nextPosition.y, transform.position.z);

            // Sync the agent's internal simulated position with the actual vehicle position
            agent.nextPosition = transform.position;

            // Prevent path planning drift
            if (Vector3.Distance(transform.position, agent.nextPosition) > 1.5f)
            {
                agent.nextPosition = transform.position;
            }
        }

        // Update wheel spin and steering visuals
        UpdateVisualWheels(currentSpeed, steerAngle);

        // Check if we arrived at the current target
        if (agent.enabled && agent.isOnNavMesh && !agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance)
        {
            // 1. Are there more road nodes to follow?
            if (currentPath != null && pathIndex < currentPath.Count - 1)
            {
                pathIndex++;
                agent.SetDestination(currentPath[pathIndex].transform.position);
            }
            // 2. We finished the road. Now drive into the actual parking spot.
            else if (finalSpot != null)
            {
                agent.SetDestination(finalSpot.position);
                finalSpot = null; // Clear so we don't loop
            }
            // 3. We are fully parked.
            else
            {
                if (!isParked)
                {
                    isParked = true;
                    OnDestinationReached?.Invoke();
                }
            }
        }
    }

    public void AssignRouteAndSpot(List<TrafficNode> route, Transform targetSpot, bool reverseOnStart = true)
    {
        if (isReversing) return; 
        StartCoroutine(ExecuteManeuver(route, targetSpot, reverseOnStart));
    }

    private IEnumerator ExecuteManeuver(List<TrafficNode> route, Transform targetSpot, bool reverseOnStart)
    {
        // 1. REVERSE OUT OF SPOT
        if (isParked && reverseOnStart)
        {
            isReversing = true;
            agent.enabled = false; // Disable NavMeshAgent during manual reverse to prevent position conflicts

            AngledSpot currentAngledSpot = null;
            if (currentSpot != null)
            {
                currentAngledSpot = currentSpot.GetComponent<AngledSpot>();
            }

            if (currentAngledSpot != null)
            {
                // Execute a rigid reverse along the AngledSpot's reverse path: entryNode -> laneNode
                List<TrafficNode> reversePath = new List<TrafficNode>();
                if (currentAngledSpot.entryNode != null) reversePath.Add(currentAngledSpot.entryNode);
                if (currentAngledSpot.laneNode != null) reversePath.Add(currentAngledSpot.laneNode);

                foreach (var targetNode in reversePath)
                {
                    if (targetNode == null) continue;
                    Vector3 targetPos = targetNode.transform.position;
                    
                    // Keep targetPos at the grounded height (NavMesh height + baseOffset)
                    UnityEngine.AI.NavMeshHit revHit;
                    UnityEngine.AI.NavMeshQueryFilter revFilter = new UnityEngine.AI.NavMeshQueryFilter();
                    revFilter.agentTypeID = agent.agentTypeID;
                    revFilter.areaMask = UnityEngine.AI.NavMesh.AllAreas;
                    if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out revHit, 10f, revFilter))
                    {
                        targetPos.y = revHit.position.y + agent.baseOffset;
                    }
                    else
                    {
                        targetPos.y = transform.position.y;
                    }
                    
                    while (Vector3.Distance(transform.position, targetPos) > 0.1f)
                    {
                        // Rotate the heading so that the rear of the car faces targetPos
                        Vector3 dirToTarget = targetPos - transform.position;
                        dirToTarget.y = 0f;
                        if (dirToTarget.sqrMagnitude > 0.001f)
                        {
                            // Rear faces target, so forward faces away
                            Quaternion targetRot = Quaternion.LookRotation(-dirToTarget.normalized, Vector3.up);
                            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 6f);
                        }

                        // Move the vehicle strictly backward towards the target node
                        transform.position = Vector3.MoveTowards(transform.position, targetPos, reverseSpeed * Time.deltaTime);

                        // Update visual wheel spin and steering visuals
                        Vector3 localTarget = transform.InverseTransformPoint(targetPos);
                        float steerAngle = Mathf.Atan2(localTarget.x, -localTarget.z) * Mathf.Rad2Deg;
                        steerAngle = Mathf.Clamp(steerAngle, -maxSteerAngle, maxSteerAngle);
                        UpdateVisualWheels(-reverseSpeed, steerAngle);

                        yield return null;
                    }
                }
            }
            else
            {
                // Fallback: Simple straight reverse for standard spots
                float timer = 0f;
                while (timer < reverseTime)
                {
                    // Translate backward along local heading
                    transform.position -= transform.forward * reverseSpeed * Time.deltaTime;
                    
                    // Keep the car grounded at NavMesh height + baseOffset
                    float groundedY = transform.position.y;
                    UnityEngine.AI.NavMeshHit fallbackHit;
                    UnityEngine.AI.NavMeshQueryFilter fallbackFilter = new UnityEngine.AI.NavMeshQueryFilter();
                    fallbackFilter.agentTypeID = agent.agentTypeID;
                    fallbackFilter.areaMask = UnityEngine.AI.NavMesh.AllAreas;
                    if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out fallbackHit, 10f, fallbackFilter))
                    {
                        groundedY = fallbackHit.position.y + agent.baseOffset;
                    }
                    transform.position = new Vector3(transform.position.x, groundedY, transform.position.z);

                    // Update visual wheel rolling (no steering)
                    UpdateVisualWheels(-reverseSpeed, 0f);

                    timer += Time.deltaTime;
                    yield return null;
                }
            }

            // Straighten front wheels after reversing
            UpdateVisualWheels(0f, 0f);
            
            agent.enabled = true;
            UnityEngine.AI.NavMeshHit hit;
            UnityEngine.AI.NavMeshQueryFilter filter = new UnityEngine.AI.NavMeshQueryFilter();
            filter.agentTypeID = agent.agentTypeID;
            filter.areaMask = UnityEngine.AI.NavMesh.AllAreas;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 10f, filter))
            {
                Vector3 warpPos = new Vector3(hit.position.x, hit.position.y + agent.baseOffset, hit.position.z);
                agent.Warp(warpPos);
                transform.position = warpPos;
                agent.nextPosition = transform.position;
            }
            else
            {
                agent.Warp(transform.position);
                agent.nextPosition = transform.position;
            }
            isReversing = false;
        }

        // 2. SETUP ROUTE & GO
        isParked = false;
        currentSpot = targetSpot;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
        
        currentPath = route;
        pathIndex = 0;
        finalSpot = targetSpot;

        // Start by driving to the first traffic node
        if (currentPath != null && currentPath.Count > 0)
        {
            agent.SetDestination(currentPath[0].transform.position);
        }
        else
        {
            // Fallback: If no road nodes exist, drive straight to spot
            agent.SetDestination(targetSpot.position);
        }
    }

    private void UpdateVisualWheels(float speed, float steer)
    {
        // Calculate spin angle based on speed
        currentSpinAngle += (speed / wheelRadius) * Mathf.Rad2Deg * Time.deltaTime;

        // Apply visual rotation to front wheels (spin + steer)
        if (wheelFL != null)
        {
            wheelFL.localRotation = Quaternion.Euler(currentSpinAngle, steer, 0f);
        }
        if (wheelFR != null)
        {
            wheelFR.localRotation = Quaternion.Euler(-currentSpinAngle, 180f + steer, 0f);
        }

        // Apply visual rotation to rear wheels (spin only)
        if (wheelRL != null)
        {
            wheelRL.localRotation = Quaternion.Euler(currentSpinAngle, 0f, 0f);
        }
        if (wheelRR != null)
        {
            wheelRR.localRotation = Quaternion.Euler(-currentSpinAngle, 180f, 0f);
        }
    }

    /// <summary>
    /// Finds the lightGlass child object recursively and turns its color to turquoise.
    /// </summary>
    public void SetAutoparkLightColor(Color color)
    {
        Transform lightGlass = FindChildRecursive(transform, "lightGlass");
        if (lightGlass != null)
        {
            MeshRenderer renderer = lightGlass.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
                if (renderer.material.HasProperty("_BaseColor"))
                {
                    renderer.material.SetColor("_BaseColor", color);
                }
                renderer.material.EnableKeyword("_EMISSION");
                // Scale emission brightness down to preserve visual transparency
                Color emissionColor = new Color(color.r, color.g, color.b) * 0.2f;
                renderer.material.SetColor("_EmissionColor", emissionColor);
            }
        }
        else
        {
            Debug.LogWarning($"[SmartCarNavigator] Could not find child 'lightGlass' on {gameObject.name}");
        }
    }

    /// <summary>
    /// Resets the lightGlass color to its default transparent look and turns off emission.
    /// </summary>
    public void ResetAutoparkLight()
    {
        Transform lightGlass = FindChildRecursive(transform, "lightGlass");
        if (lightGlass != null)
        {
            MeshRenderer renderer = lightGlass.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Color defaultColor = new Color(0f, 0f, 0f, 0.091f);
                renderer.material.color = defaultColor;
                if (renderer.material.HasProperty("_BaseColor"))
                {
                    renderer.material.SetColor("_BaseColor", defaultColor);
                }
                renderer.material.DisableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", Color.clear);
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}