using System.Collections.Generic;
using UnityEngine;

public class DynamicRouteBridge : MonoBehaviour
{
    [Header("ETA System")]
    [SerializeField] private CarETAOrchestrator carETAOrchestrator;

    [Header("Debug")]
    [SerializeField] private bool logDebugInfo = true;

    public void ReceiveRoute(List<Transform> routeNodes, string pickupLabel)
    {
        if (!ValidateTransformRoute(routeNodes))
            return;

        SendRouteToETA(routeNodes, pickupLabel);
    }

    public void ReceiveRoute(Transform[] routeNodes, string pickupLabel)
    {
        if (routeNodes == null)
        {
            Debug.LogError("DynamicRouteBridge: received Transform[] route is null.");
            return;
        }

        ReceiveRoute(new List<Transform>(routeNodes), pickupLabel);
    }

    public void ReceiveRoute(List<GameObject> routeObjects, string pickupLabel)
    {
        if (routeObjects == null)
        {
            Debug.LogError("DynamicRouteBridge: received GameObject route is null.");
            return;
        }

        List<Transform> routeNodes = new List<Transform>();

        foreach (GameObject obj in routeObjects)
        {
            if (obj != null)
                routeNodes.Add(obj.transform);
        }

        ReceiveRoute(routeNodes, pickupLabel);
    }

    public void ReceiveRoute(GameObject[] routeObjects, string pickupLabel)
    {
        if (routeObjects == null)
        {
            Debug.LogError("DynamicRouteBridge: received GameObject[] route is null.");
            return;
        }

        ReceiveRoute(new List<GameObject>(routeObjects), pickupLabel);
    }

    public void ReceiveRoute(List<RouteEndpoint> routeEndpoints, string pickupLabel)
    {
        if (routeEndpoints == null)
        {
            Debug.LogError("DynamicRouteBridge: received RouteEndpoint route is null.");
            return;
        }

        List<Transform> routeNodes = new List<Transform>();

        foreach (RouteEndpoint endpoint in routeEndpoints)
        {
            if (endpoint != null && endpoint.IsValid())
                routeNodes.Add(endpoint.LinkedRouteNode);
        }

        ReceiveRoute(routeNodes, pickupLabel);
    }

    public void ReceiveRoute(RouteEndpoint[] routeEndpoints, string pickupLabel)
    {
        if (routeEndpoints == null)
        {
            Debug.LogError("DynamicRouteBridge: received RouteEndpoint[] route is null.");
            return;
        }

        ReceiveRoute(new List<RouteEndpoint>(routeEndpoints), pickupLabel);
    }

    public void ReceiveTrafficNodeRoute(List<TrafficNode> trafficNodes, string pickupLabel)
    {
        if (trafficNodes == null || trafficNodes.Count < 2)
        {
            Debug.LogError("DynamicRouteBridge: received TrafficNode route is null or contains fewer than 2 nodes.");
            return;
        }

        List<Transform> routeTransforms = new List<Transform>();

        foreach (TrafficNode node in trafficNodes)
        {
            if (node != null)
                routeTransforms.Add(node.transform);
        }

        ReceiveRoute(routeTransforms, pickupLabel);
    }

    public void ReceiveTrafficNodeRoute(TrafficNode[] trafficNodes, string pickupLabel)
    {
        if (trafficNodes == null)
        {
            Debug.LogError("DynamicRouteBridge: received TrafficNode[] route is null.");
            return;
        }

        ReceiveTrafficNodeRoute(new List<TrafficNode>(trafficNodes), pickupLabel);
    }

    public void ReceiveTrafficRouteFromCurrentCarPosition(
        Transform carTransform,
        TrafficNode targetNode,
        string pickupLabel
    )
    {
        if (carTransform == null)
        {
            Debug.LogError("DynamicRouteBridge: carTransform is null.");
            return;
        }

        if (targetNode == null)
        {
            Debug.LogError("DynamicRouteBridge: targetNode is null.");
            return;
        }

        if (TrafficNetwork.Instance == null)
        {
            Debug.LogError("DynamicRouteBridge: TrafficNetwork.Instance not found.");
            return;
        }

        TrafficNode startNode = TrafficNetwork.Instance.GetClosestNode(carTransform.position);

        if (startNode == null)
        {
            Debug.LogError("DynamicRouteBridge: could not find closest start node.");
            return;
        }

        List<TrafficNode> path = TrafficNetwork.Instance.GetPath(startNode, targetNode);

        if (path == null || path.Count < 2)
        {
            Debug.LogError(
                $"DynamicRouteBridge: no valid path found from {startNode.name} to {targetNode.name}."
            );
            return;
        }

        ReceiveTrafficNodeRoute(path, pickupLabel);
    }

    private void SendRouteToETA(List<Transform> routeNodes, string pickupLabel)
    {
        if (carETAOrchestrator == null)
        {
            Debug.LogError("DynamicRouteBridge: CarETAOrchestrator reference missing.");
            return;
        }

        carETAOrchestrator.SetDynamicRoute(routeNodes, pickupLabel);
        carETAOrchestrator.StartETACalculation();

        if (logDebugInfo)
        {
            Debug.Log(
                $"DynamicRouteBridge: route sent to ETA. Nodes: {routeNodes.Count}, Pickup: {pickupLabel}"
            );
        }
    }

    private bool ValidateTransformRoute(List<Transform> routeNodes)
    {
        if (routeNodes == null || routeNodes.Count < 2)
        {
            Debug.LogError("DynamicRouteBridge: route must contain at least 2 nodes.");
            return false;
        }

        for (int i = 0; i < routeNodes.Count; i++)
        {
            if (routeNodes[i] == null)
            {
                Debug.LogError($"DynamicRouteBridge: null node found at index {i}.");
                return false;
            }
        }

        return true;
    }
}