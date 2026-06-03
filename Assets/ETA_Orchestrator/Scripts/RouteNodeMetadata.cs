using UnityEngine;

public class RouteNodeMetadata : MonoBehaviour
{
    [Header("Node Type")]
    [SerializeField] private bool isPedestrianCrossing = false;

    [Header("Delay Settings")]
    [SerializeField] private bool overrideDefaultCrossingDelay = false;
    [SerializeField] private float crossingDelaySeconds = 8f;

    public bool IsPedestrianCrossing => isPedestrianCrossing;
    public bool OverrideDefaultCrossingDelay => overrideDefaultCrossingDelay;
    public float CrossingDelaySeconds => Mathf.Max(0f, crossingDelaySeconds);
}