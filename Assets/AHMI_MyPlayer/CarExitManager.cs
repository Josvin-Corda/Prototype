using UnityEngine;

public class CarExitManager : MonoBehaviour
{
    [Header("Riferimenti VR")]
    [SerializeField] private GameObject xrOrigin;      // Il tuo "XR Origin (XR Rig)"
    [SerializeField] private Transform arrivalPoint;   // L' "Arrival Point" figlio della tesla

    [Header("Riferimenti Avatar")]
    [SerializeField] private GameObject avatar;         // Il tuo "npc_csl_00_character_01f_02"

    /// <summary>
    /// Metodo da chiamare quando si clicca il pulsante per uscire dalla macchina.
    /// </summary>
    public void ExitCar()
    {
        // Controllo di sicurezza per evitare errori se ti dimentichi di assegnare qualcosa nell'Inspector
        if (xrOrigin == null || arrivalPoint == null)
        {
            Debug.LogError("CarExitManager: Mancano dei riferimenti nell'Inspector!");
            return;
        }

        // 1. Sgancia l'XR Origin dalla macchina (diventa un oggetto a sé nella radice della scena)
        xrOrigin.transform.SetParent(null);

        // 2. Posiziona e ruota l'XR Origin esattamente come l'Arrival Point sulla portiera
        xrOrigin.transform.position = arrivalPoint.position;
        xrOrigin.transform.rotation = arrivalPoint.rotation;

        if (avatar != null)
        {
            // 3. Rendi l'avatar figlio dell'XR Origin in modo che si muova con lui
            avatar.transform.SetParent(xrOrigin.transform);

            // 4. Allinea l'avatar al centro dell'XR Origin (localmente a coordinate 0,0,0)
            avatar.transform.localPosition = Vector3.zero;
            avatar.transform.localRotation = Quaternion.identity;

            // 5. Attiva l'avatar nella scena
            avatar.SetActive(true);
            Debug.Log("Giocatore sceso dall'auto con successo! Avatar attivato.");
        }
        else
        {
            Debug.Log("Giocatore sceso dall'auto con successo! (Nessun avatar da attivare)");
        }
    }
}