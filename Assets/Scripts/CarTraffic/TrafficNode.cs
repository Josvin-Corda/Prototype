using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this to empty GameObjects to create your road network.
/// </summary>
public class TrafficNode : MonoBehaviour
{
    [Tooltip("The nodes a car is allowed to drive to from here. This enforces one-way lanes!")]
    public List<TrafficNode> nextNodes = new List<TrafficNode>();

    private void OnDrawGizmos()
    {
        // Draw the node
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.4f);

        if (nextNodes == null) return;

        // Draw directional lines to show traffic flow
        Gizmos.color = Color.yellow;
        foreach (TrafficNode node in nextNodes)
        {
            if (node != null)
            {
                Gizmos.DrawLine(transform.position, node.transform.position);
                
                // Draw a small block halfway to indicate the ONE-WAY direction
                Vector3 direction = (node.transform.position - transform.position);
                Gizmos.DrawCube(transform.position + direction * 0.5f, Vector3.one * 0.3f);
            }
        }
    }
}