using UnityEngine;

/// <summary>
/// Place this script on an empty GameObject in your very first scene.
/// It wakes up any secondary monitors (like your tablet cast as a generic display).
/// </summary>
public class MultiDisplayManager : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"[Display] Detected {Display.displays.Length} connected displays.");

        // Check if a second monitor is plugged in (or casted natively)
        if (Display.displays.Length > 1)
        {
            // Activate Display 2
            Display.displays[1].Activate();
            Debug.Log("[Display] Activated Display 2 (Infotainment Screen).");
        }
        
        // If you ever add a 3rd monitor for the instrument cluster:
        if (Display.displays.Length > 2)
        {
            Display.displays[2].Activate();
        }
    }
}