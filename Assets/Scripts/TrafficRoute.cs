using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Place this on an empty GameObject. Add empty GameObjects as children to create a path.
/// </summary>
public class TrafficRoute : MonoBehaviour
{
    [Header("Route Visuals")]
    public Color routeColor = Color.yellow;
    public bool isLoop = true; // Does the end connect back to the start?

    public List<Transform> nodes = new List<Transform>();

    // This special Unity method draws visible lines in the Editor Scene view!
    private void OnDrawGizmos()
    {
        Gizmos.color = routeColor;

        // Get all children automatically
        Transform[] pathTransforms = GetComponentsInChildren<Transform>();
        nodes = new List<Transform>();

        for (int i = 0; i < pathTransforms.Length; i++)
        {
            if (pathTransforms[i] != transform)
            {
                nodes.Add(pathTransforms[i]);
            }
        }

        // Draw lines connecting the nodes
        for (int i = 0; i < nodes.Count; i++)
        {
            Vector3 currentNode = nodes[i].position;
            Vector3 previousNode = Vector3.zero;

            if (i > 0)
            {
                previousNode = nodes[i - 1].position;
            }
            else if (i == 0 && nodes.Count > 1 && isLoop)
            {
                previousNode = nodes[nodes.Count - 1].position;
            }

            Gizmos.DrawSphere(currentNode, 0.5f); // Draw a sphere at the waypoint

            if (i > 0 || isLoop)
            {
                Gizmos.DrawLine(previousNode, currentNode); // Draw a line between them
            }
        }
    }
}