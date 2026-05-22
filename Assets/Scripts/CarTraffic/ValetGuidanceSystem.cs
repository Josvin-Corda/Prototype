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
    [Tooltip("Drag all your parking spot Transforms here (A1, B1, C1, D1).")]
    public List<Transform> allParkingSpots = new List<Transform>();

    [Tooltip("Drag your drop-off spot Transforms here.")]
    public List<Transform> dropOffSpots = new List<Transform>();

    [Tooltip("Drag your pick-up spot Transforms here.")]
    public List<Transform> pickUpSpots = new List<Transform>();

    [Tooltip("The exit node (e.g. Node_02 (42)) to send cars to when exiting.")]
    public Transform exitSpot;

    [Header("Testing")]
    public bool testOnStart = false;
    public SmartCarNavigator testCar;

    [Header("Active Sessions")]
    public List<ValetSession> activeSessions = new List<ValetSession>();

    private Dictionary<string, CarDatabaseEntry> carDatabaseLookup = new Dictionary<string, CarDatabaseEntry>();

    private void Awake()
    {
        activeSessions.Clear();
        LoadCarDatabase();
        allParkingSpots.RemoveAll(spot => spot == null);
        dropOffSpots.RemoveAll(spot => spot == null);
        pickUpSpots.RemoveAll(spot => spot == null);
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
                        if (!string.IsNullOrEmpty(car.plateNumber) && !carDatabaseLookup.ContainsKey(car.plateNumber))
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

    private string[] dummyNames = new string[] {
        "John Doe", "Jane Smith", "Mario Rossi", "Luigi Bianchi", "Alice Johnson",
        "Bob Miller", "Emma Watson", "Frank Sinatra", "Grace Hopper", "David Beckham"
    };

    private void Start()
    {
        if (testOnStart)
        {
            StartCoroutine(TestTriggerRoutine());
        }
    }

    private IEnumerator TestTriggerRoutine()
    {
        yield return new WaitForSeconds(1.5f); // wait for network to initialize
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
        // Avoid duplicate registration
        if (activeSessions.Exists(s => s.car == car)) return;

        // 1. Get a free parking spot
        Transform parkingSpot = GetFreeParkingSpot();
        if (parkingSpot == null)
        {
            Debug.LogWarning($"[Valet System] No free parking spots available for car {car.name}!");
            return;
        }

        // 2. Get a free drop-off spot
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

        if (!string.IsNullOrEmpty(car.plateNumber) && carDatabaseLookup.TryGetValue(car.plateNumber, out var dbEntry))
        {
            finalPlate = dbEntry.plateNumber;
            finalOwner = dbEntry.ownerName;
            finalContact = dbEntry.contact;
            Debug.Log($"[Valet System] Database match found for plate: {finalPlate}");
        }
        else
        {
            finalPlate = !string.IsNullOrEmpty(car.plateNumber) ? car.plateNumber : GeneratePlate();
            finalOwner = dummyNames[Random.Range(0, dummyNames.Length)];
            finalContact = GenerateContact();
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
            stateTimer = 0f
        };

        Debug.Log($"<color=cyan>[Valet System] Registered Car: {car.name}</color>\n" +
                  $"  Plate: {session.plateNumber}\n" +
                  $"  Owner: {session.ownerName} ({session.contact})\n" +
                  $"  Drop-Off Spot: {dropSpot.name}\n" +
                  $"  Assigned Park Spot: {parkingSpot.name}");

        // 4. Calculate path to Drop-Off
        TrafficNode startNode = TrafficNetwork.Instance.GetClosestNode(car.transform.position);
        List<TrafficNode> path = new List<TrafficNode>();
        AngledSpot angledDropSpot = dropSpot.GetComponent<AngledSpot>();
        if (angledDropSpot != null)
        {
            if (angledDropSpot.laneNode != null)
            {
                path = TrafficNetwork.Instance.GetPath(startNode, angledDropSpot.laneNode);
            }
            if (angledDropSpot.entryNode != null) path.Add(angledDropSpot.entryNode);
            if (angledDropSpot.spotNode != null) path.Add(angledDropSpot.spotNode);
        }
        else
        {
            TrafficNode endNode = TrafficNetwork.Instance.GetClosestNode(dropSpot.position);
            path = TrafficNetwork.Instance.GetPath(startNode, endNode);
        }

        // 5. Subscribe to arrived event and start moving
        System.Action handler = () => OnCarReachedDestination(session);
        session.arrivalHandler = handler;
        car.OnDestinationReached += handler;
        activeSessions.Add(session);

        // Turn the light turquoise to indicate autopark mode (semi-transparent)
        car.SetAutoparkLightColor(new Color(0f, 0.9f, 0.9f, 0.12f));

        car.AssignRouteAndSpot(path, dropSpot, reverseOnStart: false);
    }

    private void Update()
    {
        // Use a reverse loop because we might remove sessions from the list
        for (int i = activeSessions.Count - 1; i >= 0; i--)
        {
            ValetSession session = activeSessions[i];

            if (session.state == ValetState.AtDropOff ||
                session.state == ValetState.Parked ||
                session.state == ValetState.AtPickUp)
            {
                session.stateTimer -= Time.deltaTime;
                if (session.stateTimer <= 0)
                {
                    AdvanceSessionState(session);
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
        Debug.Log($"[Valet System] {session.car.name} reached destination in state {session.state}");

        switch (session.state)
        {
            case ValetState.ApproachingDropOff:
                session.state = ValetState.AtDropOff;
                session.stateTimer = dropOffWaitTime;
                Debug.Log($"[Valet System] {session.car.name} is dropping off passengers for {dropOffWaitTime}s.");
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
                
                // Destroy spawned vehicle to free memory
                Destroy(session.car.gameObject);
                
                activeSessions.Remove(session);
                break;
        }
    }

    private void AdvanceSessionState(ValetSession session)
    {
        if (session.state == ValetState.AtDropOff)
        {
            session.state = ValetState.MovingToPark;
            Debug.Log($"[Valet System] Passengers got down. Driving {session.car.name} to assigned spot {session.designatedSpot.name}.");

            TrafficNode startNode;
            AngledSpot angledDropSpot = session.dropOffSpot != null ? session.dropOffSpot.GetComponent<AngledSpot>() : null;
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

            bool reverse = session.dropOffSpot != null && session.dropOffSpot.GetComponent<AngledSpot>() != null;
            session.car.AssignRouteAndSpot(path, session.designatedSpot, reverseOnStart: reverse);
        }
        else if (session.state == ValetState.Parked)
        {
            session.state = ValetState.MovingToPickUp;
            session.pickUpSpot = GetFreePickUpSpot();
            Debug.Log($"[Valet System] Recall triggered! Driving {session.car.name} from {session.designatedSpot.name} to pick-up spot {session.pickUpSpot.name}.");

            TrafficNode startNode;
            AngledSpot angledDesignatedSpot = session.designatedSpot != null ? session.designatedSpot.GetComponent<AngledSpot>() : null;
            if (angledDesignatedSpot != null && angledDesignatedSpot.laneNode != null)
            {
                startNode = angledDesignatedSpot.laneNode;
            }
            else
            {
                startNode = TrafficNetwork.Instance.GetClosestNode(session.car.transform.position);
            }

            List<TrafficNode> path = new List<TrafficNode>();
            AngledSpot angledPickSpot = session.pickUpSpot != null ? session.pickUpSpot.GetComponent<AngledSpot>() : null;
            if (angledPickSpot != null)
            {
                if (angledPickSpot.laneNode != null)
                {
                    path = TrafficNetwork.Instance.GetPath(startNode, angledPickSpot.laneNode);
                }
                if (angledPickSpot.entryNode != null) path.Add(angledPickSpot.entryNode);
                if (angledPickSpot.spotNode != null) path.Add(angledPickSpot.spotNode);
            }
            else
            {
                TrafficNode endNode = TrafficNetwork.Instance.GetClosestNode(session.pickUpSpot.position);
                path = TrafficNetwork.Instance.GetPath(startNode, endNode);
            }

            session.car.AssignRouteAndSpot(path, session.pickUpSpot, reverseOnStart: true);
        }
        else if (session.state == ValetState.AtPickUp)
        {
            session.state = ValetState.Exiting;
            Debug.Log($"[Valet System] Departure confirmed. Driving {session.car.name} to the exit.");

            TrafficNode startNode;
            AngledSpot angledPickSpot = session.pickUpSpot != null ? session.pickUpSpot.GetComponent<AngledSpot>() : null;
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

            bool reverse = session.pickUpSpot != null && session.pickUpSpot.GetComponent<AngledSpot>() != null;
            session.car.AssignRouteAndSpot(path, exitSpot, reverseOnStart: reverse);
        }
    }

    private Transform GetFreeParkingSpot()
    {
        foreach (var spot in allParkingSpots)
        {
            if (spot == null) continue;
            bool isOccupied = false;
            foreach (var s in activeSessions)
            {
                if (s.designatedSpot == spot && s.state != ValetState.Exiting && s.state != ValetState.Exited)
                {
                    isOccupied = true;
                    break;
                }
            }
            if (!isOccupied) return spot;
        }
        return null;
    }

    private Transform GetFreeDropOffSpot()
    {
        foreach (var spot in dropOffSpots)
        {
            if (spot == null) continue;
            bool isOccupied = false;
            foreach (var s in activeSessions)
            {
                if (s.dropOffSpot == spot && (s.state == ValetState.ApproachingDropOff || s.state == ValetState.AtDropOff))
                {
                    isOccupied = true;
                    break;
                }
            }
            if (!isOccupied) return spot;
        }
        return dropOffSpots.Count > 0 ? dropOffSpots[0] : null;
    }

    private Transform GetFreePickUpSpot()
    {
        foreach (var spot in pickUpSpots)
        {
            if (spot == null) continue;
            bool isOccupied = false;
            foreach (var s in activeSessions)
            {
                if (s.pickUpSpot == spot && (s.state == ValetState.MovingToPickUp || s.state == ValetState.AtPickUp))
                {
                    isOccupied = true;
                    break;
                }
            }
            if (!isOccupied) return spot;
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
