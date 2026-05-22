using UnityEngine;

/// <summary>
/// Manages spawning of human NPCs when cars arrive at the drop-off zone,
/// and assigns their targets (Center Point, Elevator L/R).
/// </summary>
public class NPCManager : MonoBehaviour
{
    [Header("NPC Prefab Settings")]
    [Tooltip("Drag the NPC character prefab here (e.g. npc_csl_00_character_01f_01).")]
    public GameObject npcPrefab;

    [Tooltip("Drag multiple NPC character prefabs here to choose from them randomly.")]
    public System.Collections.Generic.List<GameObject> npcPrefabs = new System.Collections.Generic.List<GameObject>();

    [Header("Valet System Reference")]
    public ValetGuidanceSystem valetSystem;

    [Header("Pathing Targets")]
    [Tooltip("The initial target point the passenger walks to before splitting left/right.")]
    public Transform centerPoint;
    
    [Tooltip("Left elevator target.")]
    public Transform elevatorL;

    [Tooltip("Right elevator target.")]
    public Transform elevatorR;

    [Header("Passenger Settings")]
    [Tooltip("Offset from the car center to spawn the passenger (passenger-side door).")]
    public Vector3 spawnOffset = new Vector3(1.2f, 0f, 0f);

    private void Awake()
    {
#if UNITY_EDITOR
        if (npcPrefabs == null || npcPrefabs.Count == 0)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/PreFabs/NPC" });
            if (guids != null && guids.Length > 0)
            {
                npcPrefabs.Clear();
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        npcPrefabs.Add(prefab);
                    }
                }
                Debug.Log($"[NPCManager] Dynamically loaded {npcPrefabs.Count} NPC prefabs from Assets/PreFabs/NPC.");
            }
            else
            {
                Debug.LogWarning("[NPCManager] No prefabs found in Assets/PreFabs/NPC!");
            }
        }
#endif
    }

    private void Start()
    {
        if (valetSystem == null)
        {
            valetSystem = Object.FindAnyObjectByType<ValetGuidanceSystem>();
        }

        if (valetSystem != null)
        {
            valetSystem.OnCarArrivedAtDropOff += SpawnNPCsFromCar;
        }
        else
        {
            Debug.LogError("[NPCManager] ValetGuidanceSystem reference missing!");
        }
    }

    private void OnDestroy()
    {
        if (valetSystem != null)
        {
            valetSystem.OnCarArrivedAtDropOff -= SpawnNPCsFromCar;
        }
    }

    private void SpawnNPCsFromCar(SmartCarNavigator car, Transform dropOffSpot)
    {
        ValetSession session = null;
        if (valetSystem != null)
        {
            session = valetSystem.activeSessions.Find(s => s.car == car);
        }

        int passengersToSpawn = 1;
        if (session != null)
        {
            passengersToSpawn = session.numPassengers;
            session.passengersPendingBoarding = 0; // Reset counter before spawning
        }

        for (int i = 0; i < passengersToSpawn; i++)
        {
            GameObject chosenPrefab = npcPrefab;
            if (npcPrefabs != null && npcPrefabs.Count > 0)
            {
                chosenPrefab = npcPrefabs[Random.Range(0, npcPrefabs.Count)];
            }

            if (chosenPrefab == null)
            {
                Debug.LogWarning("[NPCManager] NPC Prefab is not assigned in the inspector!");
                continue;
            }

            // Calculate spawn position on passenger side (relative to car's heading), adding linear offset for each passenger
            // We space them out along the local Z axis (forward/backward) so they form a line along the side of the car
            Vector3 localOffset = spawnOffset + new Vector3(0f, 0f, -i * 0.8f);
            Vector3 passengerSpawnPos = car.transform.position + car.transform.TransformDirection(localOffset);

            // Ensure spawn position is snapped onto the NavMesh
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(passengerSpawnPos, out hit, 3.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                passengerSpawnPos = hit.position;
            }

            // 2. Instantiate passenger
            GameObject npcObj = Instantiate(chosenPrefab, passengerSpawnPos, car.transform.rotation);
            npcObj.SetActive(true);
            npcObj.name = $"Passenger_{car.plateNumber}_{i + 1}";

            // 3. Configure the NPC Behavior Script
            HumanNPCBehavior behavior = npcObj.GetComponent<HumanNPCBehavior>();
            if (behavior == null)
            {
                behavior = npcObj.AddComponent<HumanNPCBehavior>();
            }

            // Link the spawning car so the passenger can trigger its recall later
            behavior.associatedCar = car;
            behavior.isDriver = (i == 0);

            if (session != null)
            {
                session.passengersPendingBoarding++;
            }

            // Randomly assign Elevator Left or Right
            Transform chosenElevator = (Random.value > 0.5f) ? elevatorL : elevatorR;

            // Initialize pathing targets
            behavior.SetupRoute(centerPoint, chosenElevator);
        }
    }
}
