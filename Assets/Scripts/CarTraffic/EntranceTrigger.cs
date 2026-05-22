using UnityEngine;

/// <summary>
/// Attached to the entrance trigger collider to detect vehicles and register them with the guidance system.
/// </summary>
public class EntranceTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Try to get SmartCarNavigator from the entering collider or its parent
        SmartCarNavigator car = other.GetComponentInParent<SmartCarNavigator>();
        if (car == null)
        {
            car = other.GetComponent<SmartCarNavigator>();
        }

        if (car != null)
        {
            var guidanceSystem = Object.FindAnyObjectByType<ValetGuidanceSystem>();
            if (guidanceSystem != null)
            {
                guidanceSystem.RegisterCar(car);
            }
            else
            {
                Debug.LogWarning("[EntranceTrigger] ValetGuidanceSystem not found in the scene.");
            }
        }
    }
}
