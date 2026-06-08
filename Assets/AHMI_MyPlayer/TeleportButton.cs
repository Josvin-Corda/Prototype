using UnityEngine;

public class TeleportButton : MonoBehaviour
{
    [Header("Riferimenti Spaziali")]
    [SerializeField] private GameObject xrOrigin;      // Il tuo "XR Origin (XR Rig)"
    [SerializeField] private Transform targetLocation; // Per uscire = ArrivalPoint | Per entrare = StartingPoint
    [SerializeField] private Transform vehicle;        // NUOVO: Trascina qui la macchina (es. tesla_car1)

    [Header("Riferimenti Avatar")]
    [SerializeField] private GameObject avatar;         // Il tuo personaggio (npc_csl_00_character...)

    /// <summary>
    /// DA USARE SUL BOTTONE INTERNO (ChangeButton) PER USCIRE
    /// </summary>
    public void TrasportaGiocatore()
    {
        if (xrOrigin == null || targetLocation == null)
        {
            Debug.LogError("TeleportButton: Mancano dei riferimenti nell'Inspector!");
            return;
        }

        xrOrigin.transform.SetParent(null);
        xrOrigin.transform.position = targetLocation.position;
        xrOrigin.transform.rotation = targetLocation.rotation;

        if (avatar != null)
        {
            avatar.transform.SetParent(xrOrigin.transform);
            avatar.transform.localPosition = Vector3.zero;
            avatar.transform.localRotation = Quaternion.identity;
            avatar.SetActive(true);
            Debug.Log("Uscito dall'auto! Avatar attivato.");
        }
        else
        {
            Debug.Log("Uscito dall'auto! (Nessun avatar da attivare)");
        }
    }

    /// <summary>
    /// NUOVO: DA USARE SUL BOTTONE ESTERNO (ReturnButton) PER RIENTRARE
    /// </summary>
    public void RientraInMacchina()
    {
        if (xrOrigin == null || targetLocation == null || vehicle == null)
        {
            Debug.LogError("TeleportButton: Mancano dei riferimenti per il rientro nell'Inspector!");
            return;
        }

        if (avatar != null)
        {
            // 1. Sgancia l'avatar dallo XR Rig e rimettilo libero nella radice della scena
            avatar.transform.SetParent(null);
            // 2. Disattiva l'avatar
            avatar.SetActive(false);
        }

        // 3. Rendi di nuovo lo XR Rig figlio della macchina
        xrOrigin.transform.SetParent(vehicle);

        // 4. Riposiziona il visore sul sedile (StartingPoint)
        xrOrigin.transform.position = targetLocation.position;
        xrOrigin.transform.rotation = targetLocation.rotation;

        Debug.Log("Rientrato in macchina! Gerarchie ripristinate.");
    }
}