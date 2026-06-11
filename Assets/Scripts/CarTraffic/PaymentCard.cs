using UnityEngine;

/// <summary>
/// Attached to the payment card GameObject to store its unique payment card number identifier.
/// </summary>
public class PaymentCard : MonoBehaviour
{
    [Tooltip("The unique payment card number associated with this card.")]
    public string cardNumber = "PLAYER-CARD-9999";
}
