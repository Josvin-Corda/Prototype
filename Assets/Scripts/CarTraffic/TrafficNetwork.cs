using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The pathfinding manager that calculates the legal route across the Traffic Nodes.
/// </summary>
public class TrafficNetwork : MonoBehaviour
{
    public static TrafficNetwork Instance;
    
    [HideInInspector]
    public List<TrafficNode> allNodes = new List<TrafficNode>();

    void Awake()
    {
        Instance = this;
        // Automatically find all nodes in the children of this GameObject
        allNodes.AddRange(GetComponentsInChildren<TrafficNode>());
    }

    /// <summary>
    /// Finds the closest physical road node to a given 3D position.
    /// </summary>
    public TrafficNode GetClosestNode(Vector3 position)
    {
        TrafficNode closest = null;
        float minDistance = Mathf.Infinity;
        
        foreach (TrafficNode node in allNodes)
        {
            float dist = Vector3.Distance(position, node.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = node;
            }
        }
        return closest;
    }

    /// <summary>
    /// Breadth-First Search (BFS) to find the shortest legal path enforcing one-way rules.
    /// </summary>
    public List<TrafficNode> GetPath(TrafficNode start, TrafficNode end)
    {
        Queue<TrafficNode> queue = new Queue<TrafficNode>();
        Dictionary<TrafficNode, TrafficNode> cameFrom = new Dictionary<TrafficNode, TrafficNode>();

        queue.Enqueue(start);
        cameFrom[start] = null;

        while (queue.Count > 0)
        {
            TrafficNode current = queue.Dequeue();

            if (current == end) break; // Reached destination

            foreach (TrafficNode neighbor in current.nextNodes)
            {
                if (!cameFrom.ContainsKey(neighbor))
                {
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        // Reconstruct the path from end to start
        List<TrafficNode> path = new List<TrafficNode>();
        if (!cameFrom.ContainsKey(end)) return path; // No valid path exists

        TrafficNode curr = end;
        while (curr != null)
        {
            path.Add(curr);
            curr = cameFrom[curr];
        }
        
        path.Reverse(); // Flip it to go from start to end
        return path;
    }
}