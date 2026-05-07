using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCCarController : MonoBehaviour
{
    [Header("Traffic System")]
    [Tooltip("Drag the TrafficRoute object here!")]
    public TrafficRoute currentRoute;
    
    private NavMeshAgent agent;
    private int currentNodeIndex = 0;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Car driving physics - adjust these to make turns smoother or sharper
        agent.speed = 3.5f; 
        agent.acceleration = 5f;
        agent.angularSpeed = 120f;
        agent.stoppingDistance = 1f;

        if (currentRoute != null && currentRoute.nodes.Count > 0)
        {
            // Start driving to the first node in the route
            agent.SetDestination(currentRoute.nodes[currentNodeIndex].position);
        }
        else
        {
            Debug.LogWarning("Car has no route assigned!");
        }
    }

    void Update()
    {
        // Do nothing if we have no route assigned or no valid path
        if (currentRoute == null || currentRoute.nodes.Count == 0 || !agent.hasPath) return;

        // Check if the car has arrived at the current node
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            // Ensure we aren't still calculating the path
            if (!agent.pathPending)
            {
                MoveToNextNode();
            }
        }
    }

    private void MoveToNextNode()
    {
        // Advance to the next node in the list
        currentNodeIndex++;

        // If we reach the end of the route...
        if (currentNodeIndex >= currentRoute.nodes.Count)
        {
            // Check if the route loops back to the start
            if (currentRoute.isLoop)
            {
                currentNodeIndex = 0; // Reset to the first node
            }
            else
            {
                return; // Reached the end, stop driving
            }
        }

        // Set the new destination for the NavMeshAgent!
        agent.SetDestination(currentRoute.nodes[currentNodeIndex].position);
    }
}