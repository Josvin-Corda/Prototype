using UnityEngine;

/// <summary>
/// Defines the rigid path nodes for entering and leaving an angled spot.
/// </summary>
public class AngledSpot : MonoBehaviour
{
    [Tooltip("The node in the main lane where the car branches off.")]
    public TrafficNode laneNode;

    [Tooltip("The intermediate node that aligns the car with the spot's angle.")]
    public TrafficNode entryNode;

    [Tooltip("The node inside the spot itself (typically attached to the spot GameObject).")]
    public TrafficNode spotNode;
}
