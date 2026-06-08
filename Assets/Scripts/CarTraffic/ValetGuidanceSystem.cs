using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ValetState
{
    Manual,
    ApproachingDropOff,
    AtDropOff,
    MovingToPark,
    Parked,
    MovingToPickUp,
    AtPickUp,
    Exiting,
    Exited
}

[System.Serializable]
public class ValetSession
{
    public SmartCarNavigator car;
    public ValetState state;
    public string plateNumber;
    public string ownerName;
    public string contact;
    public Transform dropOffSpot;
    public Transform designatedSpot;
    public Transform pickUpSpot;
    public float stateTimer;
    public int numPassengers;
    public int passengersPendingBoarding;
    public float returnWaitTime;
    public bool isPlayerSession;
    [System.NonSerialized]
    public bool lightReset;
    [System.NonSerialized]
    public System.Action arrivalHandler;
}

[System.Serializable]
public class CarDatabaseEntry
{
    public string plateNumber;
    public string ownerName;
    public string contact;
    public int numPassengers;
    public float returnWaitTime;
}

[System.Serializable]
public class CarDatabaseContainer
{
    public List<CarDatabaseEntry> cars;
}

public class ValetGuidanceSystem : MonoBehaviour
{
    [Header("Simulation Timing")]
    public float dropOffWaitTime = 5f;
    public float parkedWaitTime = 10f;
    public float pickUpWaitTime = 5f;

    [Header("References")]
    [Tooltip("Drag all your parking spot Transforms here.")]
    public List<Transform> allParkingSpots = new List<Transform>();

    [Tooltip("Drag your drop-off spot Transforms here.")]
    public List<Transform> dropOffSpots = new List<Transform>();

    [Tooltip("Drag your pick-up spot Transforms here.")]
    public List<Transform> pickUpSpots = new List<Transform>();

    [Tooltip("The exit node or exit target to send cars to when exiting.")]
    public Transform exitSpot;

    [Header("Testing")]
    public bool testOnStart = false;
    public SmartCarNavigator testCar;

    [Header("Active Sessions")]
    public List<ValetSession> activeSessions = new List<ValetSession>();

    public event System.Action<SmartCarNavigator, Transform> OnCarArrivedAtDropOff;

    [Header("AHMI ETA Billboard Integration")]
    [SerializeField] private DynamicRouteBridge dynamicRouteBridge;
    [SerializeField] private ParkingTrafficProvider parkingTrafficProvider;
    [SerializeField] private bool enableEtaBillboardIntegration = true;

    private Dictionary<string, CarDatabaseEntry> carDatabaseLookup = new Dictionary<string, CarDatabaseEntry>();

    private string[] dummyNames = new string[]
    {
        "John Doe", "Jane Smith", "Mario Rossi", "Luigi Bianchi", "Alice Johnson",
        "Bob Miller", "Emma Watson", "Frank Sinatra", "Grace Hopper", "David Beckham"
    };

    private void Awake()
    {
        activeSessions.Clear();
        LoadCarDatabase();

        if (allParkingSpots != null)
            allParkingSpots.RemoveAll(spot => spot == null);

        if (dropOffSpots != null)
            dropOffSpots.RemoveAll(spot => spot == null);

        if (pickUpSpots != null)
            pickUpSpots.RemoveAll(spot => spot == null);
    }

    private void Start()
    {
        if (testOnStart)
        {
            StartCoroutine(TestTriggerRoutine());
        }
    }

    private void LoadCarDatabase()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("CarDatabase");

        if (jsonAsset != null)
        {
            try
            {
                CarDatabaseContainer dbContainer = JsonUtility.FromJson<CarDatabaseContainer>(jsonAsset.text);

                if (dbContainer != null && dbContainer.cars != null)
                {
                    foreach (var car in dbContainer.cars)
                    {
                        if (!string.IsNullOrEmpty(car.plateNumber) &&
                            !carDatabaseLookup.ContainsKey(car.plateNumber))
                        {
                            carDatabaseLookup.Add(car.plateNumber, car);
                        }
                    }

                    Debug.Log($"[Valet System] Successfully loaded {carDatabaseLookup.Count} cars from database.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Valet System] Error parsing CarDatabase.json: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[Valet System] CarDatabase.json not found in Resources folder. Fallback to random details.");
        }
    }

    private IEnumerator TestTriggerRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        if (testCar == null)
        {
            testCar = Object.FindAnyObjectByType<SmartCarNavigator>();
        }

        if (testCar != null)
        {
            Debug.Log($"[Valet System] Test mode: Registering {testCar.name} on startup.");
            RegisterCar(testCar);
        }
        else
        {
            Debug.LogWarning("[Valet System] Test mode enabled, but no SmartCarNavigator found in scene.");
        }
    }

    /// <summary>
    /// Registers a car entering the garage, assigns spots, and starts the valet sequence.
    /// </summary>
    public void RegisterCar(SmartCarNavigator car)
    {
        if (car == null)
        {
            Debug.LogWarning("[Valet System] RegisterCar called with null car.");
            return;
        }

        // Avoid duplicate registration.
        if (activeSessions.Exists(s => s.car == car))
            return;

        // 1. Get a free parking spot.
        Transform parkingSpot = GetFreeParkingSpot();
        if (parkingSpot == null)
        {
            Debug.LogWarning($"[Valet System] No free parking spots available for car {car.name}!");
            return;
        }

        // 2. Get a free drop-off spot.
        Transform dropSpot = GetFreeDropOffSpot();
        if (dropSpot == null)
        {
            Debug.LogWarning($"[Valet System] No free drop-off spots available for car {car.name}!");
            return;
        }

        // 3. Create session & assign details
        string finalPlate = "";
        string finalOwner = "";
        string finalContact = "";
        int finalNumPassengers = 1;
        float finalReturnWaitTime = 15f;

        if (!string.IsNullOrEmpty(car.plateNumber) &&
            carDatabaseLookup.TryGetValue(car.plateNumber, out var dbEntry))
        {
            finalPlate = dbEntry.plateNumber;
            finalOwner = dbEntry.ownerName;
            finalContact = dbEntry.contact;
            finalNumPassengers = dbEntry.numPassengers;
            finalReturnWaitTime = dbEntry.returnWaitTime;

            Debug.Log($"[Valet System] Database match found for plate: {finalPlate}");
        }
        else
        {
            finalPlate = !string.IsNullOrEmpty(car.plateNumber) ? car.plateNumber : GeneratePlate();
            finalOwner = dummyNames[Random.Range(0, dummyNames.Length)];
            finalContact = GenerateContact();
            finalNumPassengers = Random.Range(1, 5); // 1 to 4 passengers
            finalReturnWaitTime = Random.Range(10f, 30f); // 10s to 30s wait time

            Debug.Log($"[Valet System] No database match for plate: '{car.plateNumber}'. Generated random owner details.");
        }

        ValetSession session = new ValetSession
        {
            car = car,
            state = ValetState.ApproachingDropOff,
            plateNumber = finalPlate,
            ownerName = finalOwner,
            contact = finalContact,
            designatedSpot = parkingSpot,
            dropOffSpot = dropSpot,
            pickUpSpot = null,
            stateTimer = 0f,
            numPassengers = finalNumPassengers,
            returnWaitTime = finalReturnWaitTime,
            passengersPendingBoarding = 0,
            lightReset = false
        };

        Debug.Log($"<color=cyan>[Valet System] Registered Car: {car.name}</color>\n" +
                  $"  Plate: {session.plateNumber}\n" +
                  $"  Owner: {session.ownerName} ({session.contact})\n" +
                  $"  Drop-Off Spot: {dropSpot.name}\n" +
                  $"  Assigned Park Spot: {parkingSpot.name}");

        // 4. Calculate path to Drop-Off.
        TrafficNode startNode = TrafficNetwork.Instance.GetClosestNode(car.transform.position);
        List<TrafficNode> path = new List<TrafficNode>();

        AngledSpot angledDropSpot = dropSpot.GetComponent<AngledSpot>();
        if (angledDropSpot != null)
        {
            if (angledDropSpot.laneNode != null)
            {
                path = TrafficNetwork.Instance.GetPath(startNode, angledDropSpot.laneNode);
            }

            if (angledDropSpot.entryNode != null)
                path.Add(angledDropSpot.entryNode);

            if (angledDropSpot.spotNode != null)
                path.Add(angledDropSpot.spotNode);
        }
        else
        {
            TrafficNode endNode = TrafficNetwork.Instance.GetClosestNode(dropSpot.position);
            path = TrafficNetwork.Instance.GetPath(startNode, endNode);
        }

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"[Valet System] No valid path found to drop-off spot {dropSpot.name}.");
        }

        // 5. Subscribe to arrival event and start moving.
        System.Action handler = () => OnCarReachedDestination(session);
        session.arrivalHandler = handler;
        car.OnDestinationReached += handler;
        activeSessions.Add(session);

        // Update external traffic data for the ETA system.
        UpdateTrafficProviderData();

        // The light will be set to turquoise by the Entrance Barrier Gate script once it opens.
        // car.SetAutoparkLightColor(new Color(0f, 0.9f, 0.9f, 0.12f));

        car.AssignRouteAndSpot(path, dropSpot, reverseOnStart: false);
    }

    private void Update()
    {
        // Use a reverse loop because we might remove sessions from the list
        for (int i = activeSessions.Count - 1; i >= 0; i--)
        {
            ValetSession session = activeSessions[i];

            if (session.state == ValetState.AtDropOff)
            {
                if (!session.isPlayerSession)
                {
                    session.stateTimer -= Time.deltaTime;
                    if (session.stateTimer <= 0)
                    {
                        AdvanceSessionState(session);
                    }
                }
            }
            else if (session.state == ValetState.Exiting)
            {
                // Turn off the turquoise light once the car crosses the exit boundary (X < 2.5f)
                if (!session.lightReset && session.car != null && session.car.transform.position.x < 2.5f)
                {
                    session.lightReset = true;
                    session.car.ResetAutoparkLight();
                }
            }
        }
    }

    private void OnCarReachedDestination(ValetSession session)
    {
        if (session == null || session.car == null)
            return;

        Debug.Log($"[Valet System] {session.car.name} reached destination in state {session.state}");

        switch (session.state)
        {
            case ValetState.ApproachingDropOff:
                session.state = ValetState.AtDropOff;
                session.stateTimer = dropOffWaitTime;
                Debug.Log($"[Valet System] {session.car.name} is dropping off passengers for {dropOffWaitTime}s.");
                OnCarArrivedAtDropOff?.Invoke(session.car, session.dropOffSpot);
                break;

            case ValetState.MovingToPark:
                session.state = ValetState.Parked;
                session.stateTimer = parkedWaitTime;
                Debug.Log($"[Valet System] {session.car.name} is parked. Will stay parked for {parkedWaitTime}s.");
                break;

            case ValetState.MovingToPickUp:
                session.state = ValetState.AtPickUp;
                session.stateTimer = pickUpWaitTime;
                Debug.Log($"[Valet System] {session.car.name} reached pick-up zone. Waiting for passengers for {pickUpWaitTime}s.");
                break;

            case ValetState.Exiting:
                session.state = ValetState.Exited;
                Debug.Log($"[Valet System] {session.car.name} has exited the parking lot.");

                // Turn off the light upon crossing/reaching the exit
                session.car.ResetAutoparkLight();

                // Clean up events
                if (session.arrivalHandler != null)
                {
                    session.car.OnDestinationReached -= session.arrivalHandler;
                    session.arrivalHandler = null;
                }

                // If this is the player's car, unparent the VR player before destroying the vehicle
                if (session.isPlayerSession)
                {
                    GameObject xrOriginObj = GameObject.Find("XR Origin (XR Rig)");
                    if (xrOriginObj != null)
                    {
                        xrOriginObj.transform.SetParent(null);
                        xrOriginObj.transform.position = new Vector3(-14.0f, 0.12f, -17.37f); // starting position
                        xrOriginObj.transform.rotation = Quaternion.identity;

                        CharacterController cc = xrOriginObj.GetComponent<CharacterController>();
                        if (cc != null)
                        {
                            cc.enabled = true;
                        }
                        Debug.Log("[Valet System] Safely unparented and reset XR Origin position before destroying player car.");
                    }
                }

                // Destroy spawned vehicle to free memory
                Destroy(session.car.gameObject);
                activeSessions.Remove(session);

                UpdateTrafficProviderData();
                break;
        }
    }

    private void AdvanceSessionState(ValetSession session)
    {
        if (session == null || session.car == null)
            return;

        if (session.state == ValetState.AtDropOff)
        {
            session.state = ValetState.MovingToPark;
            Debug.Log($"[Valet System] Passengers got down. Driving {session.car.name} to assigned spot {session.designatedSpot.name}.");

            TrafficNode startNode;

            AngledSpot angledDropSpot = session.dropOffSpot != null
                ? session.dropOffSpot.GetComponent<AngledSpot>()
                : null;

            if (angledDropSpot != null && angledDropSpot.laneNode != null)
            {
                startNode = angledDropSpot.laneNode;
            }
            else
            {
                startNode = TrafficNetwork.Instance.GetClosestNode(session.car.transform.position);
            }

            TrafficNode endNode = TrafficNetwork.Instance.GetClosestNode(session.designatedSpot.position);
            List<TrafficNode> path = TrafficNetwork.Instance.GetPath(startNode, endNode);

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning($"[Valet System] No valid path found from drop-off to parking spot {session.designatedSpot.name}.");
            }

            bool reverse = session.dropOffSpot != null &&
                           session.dropOffSpot.GetComponent<AngledSpot>() != null;

            session.car.AssignRouteAndSpot(path, session.designatedSpot, reverseOnStart: reverse);
        }
        else if (session.state == ValetState.Parked)
        {
            session.state = ValetState.MovingToPickUp;
            session.pickUpSpot = GetFreePickUpSpot();

            if (session.pickUpSpot == null)
            {
                Debug.LogWarning($"[Valet System] No free pick-up spot available for {session.car.name}.");
                return;
            }

            Debug.Log($"[Valet System] Recall triggered! Driving {session.car.name} from {session.designatedSpot.name} to pick-up spot {session.pickUpSpot.name}.");

            TrafficNode startNode;

            AngledSpot angledDesignatedSpot = session.designatedSpot != null
                ? session.designatedSpot.GetComponent<AngledSpot>()
                : null;

            if (angledDesignatedSpot != null && angledDesignatedSpot.laneNode != null)
            {
                startNode = angledDesignatedSpot.laneNode;
            }
            else
            {
                startNode = TrafficNetwork.Instance.GetClosestNode(session.car.transform.position);
            }

            List<TrafficNode> path = new List<TrafficNode>();

            AngledSpot angledPickSpot = session.pickUpSpot != null
                ? session.pickUpSpot.GetComponent<AngledSpot>()
                : null;

            if (angledPickSpot != null)
            {
                if (angledPickSpot.laneNode != null)
                {
                    path = TrafficNetwork.Instance.GetPath(startNode, angledPickSpot.laneNode);
                }

                if (angledPickSpot.entryNode != null)
                    path.Add(angledPickSpot.entryNode);

                if (angledPickSpot.spotNode != null)
                    path.Add(angledPickSpot.spotNode);
            }
            else
            {
                TrafficNode endNode = TrafficNetwork.Instance.GetClosestNode(session.pickUpSpot.position);
                path = TrafficNetwork.Instance.GetPath(startNode, endNode);
            }

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning($"[Valet System] No valid path found from parking spot {session.designatedSpot.name} to pickup spot {session.pickUpSpot.name}.");
            }

            UpdateTrafficProviderData();

            SendPickupRouteToETABillboard(session, path);

            session.car.AssignRouteAndSpot(path, session.pickUpSpot, reverseOnStart: true);
        }
        else if (session.state == ValetState.AtPickUp)
        {
            session.state = ValetState.Exiting;
            Debug.Log($"[Valet System] Departure confirmed. Driving {session.car.name} to the exit.");

            TrafficNode startNode;

            AngledSpot angledPickSpot = session.pickUpSpot != null
                ? session.pickUpSpot.GetComponent<AngledSpot>()
                : null;

            if (angledPickSpot != null && angledPickSpot.laneNode != null)
            {
                startNode = angledPickSpot.laneNode;
            }
            else
            {
                startNode = TrafficNetwork.Instance.GetClosestNode(session.car.transform.position);
            }

            TrafficNode endNode = TrafficNetwork.Instance.GetClosestNode(exitSpot.position);
            List<TrafficNode> path = TrafficNetwork.Instance.GetPath(startNode, endNode);

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning($"[Valet System] No valid path found from pickup spot to exit.");
            }

            bool reverse = session.pickUpSpot != null &&
                           session.pickUpSpot.GetComponent<AngledSpot>() != null;

            session.car.AssignRouteAndSpot(path, exitSpot, reverseOnStart: reverse);
        }
    }

    public void RecallCar(SmartCarNavigator car)
    {
        ValetSession session = activeSessions.Find(s => s.car == car);
        if (session != null && session.state == ValetState.Parked)
        {
            AdvanceSessionState(session);
        }
        else
        {
            Debug.LogWarning($"[Valet System] Cannot recall car {car.name} because it is in state {(session != null ? session.state.ToString() : "null")}");
        }
    }

    public void CompletePassengerBoarding(SmartCarNavigator car)
    {
        ValetSession session = activeSessions.Find(s => s.car == car);
        if (session != null && session.state == ValetState.AtPickUp)
        {
            AdvanceSessionState(session);
        }
        else
        {
            Debug.LogWarning($"[Valet System] Cannot complete boarding for car {car.name} because it is in state {(session != null ? session.state.ToString() : "null")}");
        }
    }

    public void PassengerBoarded(SmartCarNavigator car)
    {
        ValetSession session = activeSessions.Find(s => s.car == car);
        if (session != null)
        {
            session.passengersPendingBoarding--;
            Debug.Log($"[Valet System] Passenger boarded {car.name}. Remaining passengers: {session.passengersPendingBoarding}");
            if (session.passengersPendingBoarding <= 0)
            {
                Debug.Log($"[Valet System] All passengers boarded {car.name}. Triggering exit departure.");
                CompletePassengerBoarding(car);
            }
        }
    }

    private void SendPickupRouteToETABillboard(ValetSession session, List<TrafficNode> path)
    {
        if (!enableEtaBillboardIntegration)
            return;

        if (dynamicRouteBridge == null)
        {
            Debug.LogWarning("[Valet System] DynamicRouteBridge reference missing. ETA billboard not updated.");
            return;
        }

        if (session == null || session.pickUpSpot == null)
        {
            Debug.LogWarning("[Valet System] Missing session or pickup spot. ETA billboard not updated.");
            return;
        }

        if (path == null || path.Count < 2)
        {
            Debug.LogWarning("[Valet System] Invalid pickup path. ETA billboard not updated.");
            return;
        }

        string pickupLabel = GetPickupLabel(session.pickUpSpot);

        if (session.car != null)
        {
            CarETAOrchestrator orchestrator = session.car.GetComponent<CarETAOrchestrator>();
            if (orchestrator != null)
            {
                dynamicRouteBridge.SetCarETAOrchestrator(orchestrator);
            }
        }

        dynamicRouteBridge.ReceiveTrafficNodeRoute(path, pickupLabel);

        Debug.Log($"[Valet System] ETA billboard route sent. Pickup={pickupLabel}, Nodes={path.Count}");
    }

    private string GetPickupLabel(Transform pickupSpot)
    {
        if (pickupSpot == null)
            return "-";

        int index = pickUpSpots.IndexOf(pickupSpot);

        if (index >= 0 && index < 26)
        {
            char label = (char)('A' + index);
            return label.ToString();
        }

        return pickupSpot.name;
    }

    private void UpdateTrafficProviderData()
    {
        if (parkingTrafficProvider == null)
            return;

        int totalCarsInSimulation = Mathf.Max(1, allParkingSpots != null ? allParkingSpots.Count : 1);
        int activeCarsCount = activeSessions != null ? activeSessions.Count : 0;

        parkingTrafficProvider.SetExternalTrafficData(
            totalCarsInSimulation,
            activeCarsCount
        );
    }

    private Transform GetFreeParkingSpot()
    {
        List<Transform> freeSpots = new List<Transform>();
        foreach (var spot in allParkingSpots)
        {
            if (spot == null)
                continue;

            bool isOccupied = false;

            foreach (var s in activeSessions)
            {
                if (s.designatedSpot == spot &&
                    s.state != ValetState.Exiting &&
                    s.state != ValetState.Exited)
                {
                    isOccupied = true;
                    break;
                }
            }

            if (!isOccupied)
            {
                freeSpots.Add(spot);
            }
        }

        if (freeSpots.Count > 0)
        {
            int randomIndex = Random.Range(0, freeSpots.Count);
            return freeSpots[randomIndex];
        }

        return null;
    }

    private Transform GetFreeDropOffSpot()
    {
        foreach (var spot in dropOffSpots)
        {
            if (spot == null)
                continue;

            bool isOccupied = false;

            foreach (var s in activeSessions)
            {
                if (s.dropOffSpot == spot &&
                    (s.state == ValetState.ApproachingDropOff ||
                     s.state == ValetState.AtDropOff))
                {
                    isOccupied = true;
                    break;
                }
            }

            if (!isOccupied)
                return spot;
        }

        return dropOffSpots.Count > 0 ? dropOffSpots[0] : null;
    }

    private Transform GetFreePickUpSpot()
    {
        foreach (var spot in pickUpSpots)
        {
            if (spot == null)
                continue;

            bool isOccupied = false;

            foreach (var s in activeSessions)
            {
                if (s.pickUpSpot == spot &&
                    (s.state == ValetState.MovingToPickUp ||
                     s.state == ValetState.AtPickUp))
                {
                    isOccupied = true;
                    break;
                }
            }

            if (!isOccupied)
                return spot;
        }

        return pickUpSpots.Count > 0 ? pickUpSpots[0] : null;
    }

    private string GeneratePlate()
    {
        char c1 = (char)Random.Range('A', 'Z' + 1);
        char c2 = (char)Random.Range('A', 'Z' + 1);
        int num = Random.Range(100, 999);
        char c3 = (char)Random.Range('A', 'Z' + 1);
        char c4 = (char)Random.Range('A', 'Z' + 1);

        return $"{c1}{c2}-{num}-{c3}{c4}";
    }

    private string GenerateContact()
    {
        return "+39 333 " + Random.Range(1000000, 9999999).ToString();
    }
}