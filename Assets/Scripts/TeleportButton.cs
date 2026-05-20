using UnityEngine;

public class TeleportButton : MonoBehaviour
{
    [Header("Riferimenti Spaziali")]
    [Tooltip("Trascina qui l'XR Origin della tua scena")]
    public Transform xrOrigin; 
    
    [Tooltip("Trascina qui il Punto di Arrivo (Empty Object)")]
    public Transform targetLocation; 

    // Questa è la funzione che verrà richiamata dal pulsante
    public void TrasportaGiocatore()
    {
        // Se i riferimenti sono stati inseriti correttamente nell'editor
        if (xrOrigin != null && targetLocation != null)
        {
            // Sovrascrive la posizione e la rotazione attuali con quelle di destinazione
            xrOrigin.position = targetLocation.position;
            xrOrigin.rotation = targetLocation.rotation;
            
            Debug.Log("Teletrasporto eseguito con successo!");
        }
        else
        {
            Debug.LogWarning("Mancano i riferimenti allo script TeleportButton!");
        }
    }
}