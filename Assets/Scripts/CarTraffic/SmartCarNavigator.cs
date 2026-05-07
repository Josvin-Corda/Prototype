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
    public float turnSpeed = 5f;
    public float reverseSpeed = 2f;
    public float reverseTime = 2.5f;

    private NavMeshAgent agent;
    private bool isParked = true; 
    private bool isReversing = false;

    // Routing data
    private List<TrafficNode> currentPath;
    private int pathIndex = 0;
    private Transform finalSpot;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // We handle smooth rotation manually
    }

    void Update()
    {
        if (isReversing || !agent.hasPath || agent.velocity.sqrMagnitude < 0.1f) return;

        // Smooth steering logic
        Vector3 direction = agent.velocity.normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }

        // Check if we arrived at the current target
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
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
                isParked = true;
            }
        }
    }

    public void AssignRouteAndSpot(List<TrafficNode> route, Transform targetSpot)
    {
        if (isReversing) return; 
        StartCoroutine(ExecuteManeuver(route, targetSpot));
    }

    private IEnumerator ExecuteManeuver(List<TrafficNode> route, Transform targetSpot)
    {
        // 1. REVERSE OUT OF SPOT
        if (isParked)
        {
            isReversing = true;
            agent.isStopped = true; 
            
            float timer = 0f;
            while (timer < reverseTime)
            {
                transform.Translate(Vector3.back * reverseSpeed * Time.deltaTime, Space.Self);
                timer += Time.deltaTime;
                yield return null;
            }
            isReversing = false;
        }

        // 2. SETUP ROUTE & GO
        isParked = false;
        agent.isStopped = false;
        
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
}