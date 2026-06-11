using UnityEngine;

public class TeleportButton : MonoBehaviour
{
    [Header("Riferimenti Spaziali")]
    [SerializeField] private GameObject xrOrigin;      // Il tuo "XR Origin (XR Rig)"
    [SerializeField] private Transform targetLocation; // Per uscire = ArrivalPoint | Per entrare = StartingPoint
    [SerializeField] private Transform exitLocation;   // NUOVO: Punto di uscita (es. ArrivalPoint o ArrivalPoint_Passenger)
    [SerializeField] private Transform vehicle;        // NUOVO: Trascina qui la macchina (es. tesla_car1)

    [Header("Riferimenti Avatar")]
    [SerializeField] private GameObject avatar;         // Il tuo personaggio (npc_csl_00_character...)

    private void Start()
    {
        // Auto-resolve references at runtime if null
        if (xrOrigin == null)
        {
            xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        }
        
        if (vehicle == null)
        {
            // Search upwards to find the car root (with SmartCarNavigator)
            Transform t = transform;
            while (t != null)
            {
                if (t.GetComponent<SmartCarNavigator>() != null)
                {
                    vehicle = t;
                    break;
                }
                t = t.parent;
            }
        }
    }

    /// <summary>
    /// DA USARE SUL BOTTONE INTERNO (ChangeButton) PER USCIRE
    /// </summary>
    public void TrasportaGiocatore()
    {
        Transform dest = exitLocation;
        if (dest == null) dest = targetLocation;

        if (xrOrigin == null || dest == null)
        {
            Debug.LogError("TeleportButton: Mancano dei riferimenti nell'Inspector!");
            return;
        }

        // Smart Exit redirection:
        // If we are exiting to one side but the player's current position is closer to the other seat,
        // dynamically redirect to the corresponding exit point.
        if (vehicle != null)
        {
            Transform carChair = vehicle.Find("ModeVisuals/car_chair");
            Transform pedFrontseat = vehicle.Find("ModeVisuals/ped_frontseat");
            if (carChair != null && pedFrontseat != null)
            {
                float distToDriver = Vector3.Distance(xrOrigin.transform.position, carChair.position);
                float distToPassenger = Vector3.Distance(xrOrigin.transform.position, pedFrontseat.position);

                if (dest.name == "ArrivalPoint" && distToPassenger < distToDriver)
                {
                    Transform passengerExit = vehicle.Find("ModeVisuals/ArrivalPoint_Passenger");
                    if (passengerExit != null)
                    {
                        dest = passengerExit;
                        Debug.Log("Smart Exit: Redirected exit to passenger side (ArrivalPoint_Passenger) based on seat proximity.");
                    }
                }
                else if (dest.name == "ArrivalPoint_Passenger" && distToDriver < distToPassenger)
                {
                    Transform driverExit = vehicle.Find("ModeVisuals/ArrivalPoint");
                    if (driverExit != null)
                    {
                        dest = driverExit;
                        Debug.Log("Smart Exit: Redirected exit to driver side (ArrivalPoint) based on seat proximity.");
                    }
                }
            }
        }

        // Disable CharacterController before teleporting to avoid physics override issues
        CharacterController cc = xrOrigin.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        xrOrigin.transform.SetParent(null);
        xrOrigin.transform.position = dest.position;
        xrOrigin.transform.rotation = dest.rotation;

        if (cc != null) cc.enabled = true; // Re-enable on exit

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

        // Sync with PlayerCarProgression state
        PlayerCarProgression progression = Object.FindAnyObjectByType<PlayerCarProgression>();
        if (progression != null)
        {
            progression.IsPlayerInsideCar = false;
            
            // If the car is in initial approach state and player exited, stop the car
            SmartCarNavigator navigator = vehicle != null ? vehicle.GetComponent<SmartCarNavigator>() : null;
            if (navigator != null && progression.valetSystem != null)
            {
                ValetState state = ValetState.Manual;
                var session = progression.valetSystem.activeSessions.Find(s => s.car == navigator);
                if (session != null) state = session.state;

                if (state == ValetState.ApproachingDropOff)
                {
                    navigator.IsCrosswalkStopped = true;
                    Debug.Log("Player exited during approach. Stopping Test Car.");
                }
            }
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

        // Disable CharacterController before teleporting and keep it disabled to avoid jitter while inside the moving car
        CharacterController cc = xrOrigin.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        if (avatar != null)
        {
            // 1. Sgancia l'avatar dallo XR Rig e rimettilo libero nella radice della scena
            avatar.transform.SetParent(null);
            // 2. Disattiva l'avatar
            avatar.SetActive(false);
        }

        // 3. Riposiziona il visore sul sedile (StartingPoint/targetLocation) in world space first
        // Find the Main Camera under the XR Origin to offset the rig base correctly
        Camera mainCam = xrOrigin.GetComponentInChildren<Camera>();
        if (mainCam != null)
        {
            Vector3 camToRigOffset = xrOrigin.transform.position - mainCam.transform.position;
            xrOrigin.transform.position = targetLocation.position + camToRigOffset;
        }
        else
        {
            xrOrigin.transform.position = targetLocation.position;
        }
        xrOrigin.transform.rotation = targetLocation.rotation;

        // 4. Rendi di nuovo lo XR Rig figlio della macchina
        xrOrigin.transform.SetParent(vehicle);

        Debug.Log("Rientrato in macchina! Gerarchie ripristinate.");

        // Sync with PlayerCarProgression state
        PlayerCarProgression progression = Object.FindAnyObjectByType<PlayerCarProgression>();
        if (progression != null)
        {
            progression.IsPlayerInsideCar = true;

            // Start moving the car now that the player has boarded (if in initial approach phase)
            SmartCarNavigator navigator = vehicle.GetComponent<SmartCarNavigator>();
            if (navigator != null && progression.valetSystem != null)
            {
                ValetState state = ValetState.Manual;
                var session = progression.valetSystem.activeSessions.Find(s => s.car == navigator);
                if (session != null) state = session.state;

                if (state == ValetState.ApproachingDropOff)
                {
                    navigator.IsCrosswalkStopped = false;
                    Debug.Log("Player boarded. Releasing Test Car to proceed.");
                }
            }
        }
    }

    /// <summary>
    /// Unified entry/exit toggle: Enters the car if outside, exits if inside.
    /// </summary>
    public void ToggleBoardingState()
    {
        PlayerCarProgression progression = Object.FindAnyObjectByType<PlayerCarProgression>();
        bool isInside = progression != null && progression.IsPlayerInsideCar;

        if (isInside)
        {
            Debug.Log("ToggleBoardingState: Player is inside the car, triggering exit.");
            TrasportaGiocatore();
        }
        else
        {
            Debug.Log("ToggleBoardingState: Player is outside the car, triggering entrance.");
            RientraInMacchina();
        }
    }
}