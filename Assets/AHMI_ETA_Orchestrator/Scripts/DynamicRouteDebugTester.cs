using System.Collections.Generic;
using UnityEngine;

public class DynamicRouteDebugTester : MonoBehaviour
{
    [Header("Bridge")]
    [SerializeField] private DynamicRouteBridge dynamicRouteBridge;

    [Header("Debug Route")]
    [SerializeField] private List<Transform> testRouteNodes = new List<Transform>();
    [SerializeField] private string testPickupLabel = "A";

    [Header("Input")]
    [SerializeField] private KeyCode sendRouteKey = KeyCode.R;

    private void Update()
    {
        if (Input.GetKeyDown(sendRouteKey))
        {
            SendTestRoute();
        }
    }

    public void SendTestRoute()
    {
        if (dynamicRouteBridge == null)
        {
            Debug.LogError("DynamicRouteDebugTester: DynamicRouteBridge reference missing.");
            return;
        }

        if (testRouteNodes == null || testRouteNodes.Count < 2)
        {
            Debug.LogError("DynamicRouteDebugTester: test route must contain at least 2 nodes.");
            return;
        }

        dynamicRouteBridge.ReceiveRoute(testRouteNodes, testPickupLabel);
    }
}