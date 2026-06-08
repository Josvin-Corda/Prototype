using UnityEngine;

public class TotemCarRequest : MonoBehaviour
{
    [SerializeField] private CarETAOrchestrator carETAOrchestrator;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    private bool playerInside = false;

    private void Update()
    {
        if (playerInside && Input.GetKeyDown(interactionKey))
        {
            RequestCar();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInside = false;
    }

    public void RequestCar()
    {
        if (carETAOrchestrator == null)
        {
            carETAOrchestrator = Object.FindAnyObjectByType<CarETAOrchestrator>();
        }

        if (carETAOrchestrator == null)
        {
            Debug.LogError("CarETAOrchestrator reference missing.");
            return;
        }

        carETAOrchestrator.StartETACalculation();
    }
}