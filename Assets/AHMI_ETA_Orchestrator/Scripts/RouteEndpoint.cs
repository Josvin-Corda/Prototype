using UnityEngine;

public class RouteEndpoint : MonoBehaviour
{
    [Header("Endpoint Identity")]
    [SerializeField] private string endpointId = "A";

    [Header("Linked Route Node")]
    [SerializeField] private Transform linkedRouteNode;

    public string EndpointId => endpointId;
    public Transform LinkedRouteNode => linkedRouteNode;

    public bool IsValid()
    {
        return linkedRouteNode != null;
    }
}