using UnityEngine;

/// <summary>
/// Attached to the Totem's CardReader object.
/// Listens for card tap events and triggers the recall sequence for the matching car.
/// </summary>
public class TotemRecallHandler : MonoBehaviour
{
    private CardReader cardReader;
    private ValetGuidanceSystem valetSystem;

    private void Start()
    {
        cardReader = GetComponent<CardReader>();
        valetSystem = Object.FindAnyObjectByType<ValetGuidanceSystem>();

        if (cardReader != null)
        {
            cardReader.OnCardRead.AddListener(OnCardTappedAtTotem);
        }
        else
        {
            Debug.LogError($"[TotemRecallHandler] CardReader component missing on {gameObject.name}");
        }
    }

    private void OnCardTappedAtTotem(string cardNumber)
    {
        if (valetSystem == null)
        {
            valetSystem = Object.FindAnyObjectByType<ValetGuidanceSystem>();
        }

        if (valetSystem == null)
        {
            Debug.LogError("[TotemRecallHandler] ValetGuidanceSystem not found in the scene!");
            return;
        }

        // Find the active session matching this payment card number
        var session = valetSystem.activeSessions.Find(s => s.paymentCardNumber == cardNumber);
        if (session != null)
        {
            if (session.state == ValetState.Parked)
            {
                Debug.Log($"[TotemRecallHandler] Card tap matched active session for plate {session.plateNumber} ({session.car.name}). Initiating recall!");
                valetSystem.RecallCar(session.car);
                
                // Trigger ETA calculation on the associated TotemCarRequest if present
                var request = GetComponentInParent<TotemCarRequest>();
                if (request != null)
                {
                    request.RequestCar();
                }
                else
                {
                    var orchestrator = Object.FindAnyObjectByType<CarETAOrchestrator>();
                    if (orchestrator != null)
                    {
                        orchestrator.StartETACalculation();
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[TotemRecallHandler] Tapped card matches car {session.car.name}, but car is in state '{session.state}' (needs to be Parked).");
            }
        }
        else
        {
            Debug.LogWarning($"[TotemRecallHandler] No active valet session found for card: {cardNumber}");
        }
    }
}
