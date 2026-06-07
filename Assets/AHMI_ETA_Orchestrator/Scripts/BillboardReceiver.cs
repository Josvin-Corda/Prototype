using UnityEngine;
using TMPro;

public class BillboardReceiver : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI pickupTagText;
    [SerializeField] private TextMeshProUGUI statusText;

    private void OnEnable()
    {
        SimulationEvents.OnETAUpdated += UpdateDisplay;
    }

    private void OnDisable()
    {
        SimulationEvents.OnETAUpdated -= UpdateDisplay;
    }

    private void UpdateDisplay(int secondsRemaining, string pickupTag, string statusMessage)
    {
        if (timeText != null)
            timeText.text = FormatETA(secondsRemaining);

        if (pickupTagText != null)
            pickupTagText.text = $"Pickup {pickupTag}";

        if (statusText != null)
            statusText.text = statusMessage;
    }

    private string FormatETA(int seconds)
    {
        if (seconds <= 0)
            return "Arrived";

        int minutes = seconds / 60;
        int remainingSeconds = seconds % 60;

        return $"{minutes}:{remainingSeconds:00}";
    }
}