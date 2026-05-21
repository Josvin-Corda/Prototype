using UnityEngine;

public class TotemCarRequest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CarETAOrchestrator carETAOrchestrator;

    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private bool playerInsideInteractionArea = false;

    [Header("Debug")]
    [SerializeField] private bool allowKeyboardDebug = true;

    private void Update()
    {
        if (!allowKeyboardDebug)
        {
            return;
        }

        if (playerInsideInteractionArea && Input.GetKeyDown(interactionKey))
        {
            RequestCar();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInsideInteractionArea = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInsideInteractionArea = false;
        }
    }

    public void RequestCar()
    {
        if (carETAOrchestrator == null)
        {
            Debug.LogError("TotemCarRequest: riferimento a CarETAOrchestrator mancante.");
            return;
        }

        carETAOrchestrator.StartETACalculation();
    }
}