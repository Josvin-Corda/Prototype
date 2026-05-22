using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawns NPC cars from prefabs at a configurable interval and feeds them
/// into the ValetGuidanceSystem. Cars are destroyed after they exit.
/// </summary>
public class NPCCarSpawner : MonoBehaviour
{
    [Header("Car Prefabs")]
    [Tooltip("Drag the car prefabs here (Car 1 through Car 5 from Azerilo).")]
    public GameObject[] carPrefabs;

    [Header("Spawn Settings")]
    [Tooltip("Where to spawn incoming cars (should be near the garage entrance).")]
    public Transform spawnPoint;

    [Tooltip("Rotation to apply to spawned cars (should face into the garage).")]
    public Vector3 spawnRotation = new Vector3(0f, 90f, 0f);

    [Tooltip("Maximum number of NPC cars active at any time.")]
    [Range(1, 20)]
    public int maxNPCCars = 4;

    [Tooltip("Seconds between each spawn attempt.")]
    [Range(1f, 60f)]
    public float spawnInterval = 8f;

    [Tooltip("Seconds to wait before the first spawn (let the network initialize).")]
    public float initialDelay = 3f;

    [Header("NavMeshAgent Settings (matching Car 5)")]
    public float agentSpeed = 4f;
    public float agentAngularSpeed = 100f;
    public float agentAcceleration = 8f;
    public float agentRadius = 0.6f;
    public float agentHeight = 1f;
    public float agentBaseOffset = 0.5f;
    public float agentStoppingDistance = 0.5f;
    public int agentAvoidancePriority = 50;

    [Header("Car Settings")]
    public float carScale = 0.9f;

    [Header("References")]
    [Tooltip("Reference to the ValetGuidanceSystem. Auto-found if left empty.")]
    public ValetGuidanceSystem valetSystem;

    private int spawnedCount = 0;
    private int detectedAgentTypeID = 0;
    private bool hasDetectedAgentType = false;

    private void Start()
    {
        if (valetSystem == null)
        {
            valetSystem = GetComponent<ValetGuidanceSystem>();
        }
        if (valetSystem == null)
        {
            valetSystem = Object.FindAnyObjectByType<ValetGuidanceSystem>();
        }

        if (valetSystem == null)
        {
            Debug.LogError("[NPCCarSpawner] No ValetGuidanceSystem found! Spawner disabled.");
            return;
        }

        if (carPrefabs == null || carPrefabs.Length == 0)
        {
            Debug.LogError("[NPCCarSpawner] No car prefabs assigned! Spawner disabled.");
            return;
        }

        // Try to find any NavMeshAgent in the scene to copy its agentTypeID
        NavMeshAgent existingAgent = Object.FindAnyObjectByType<NavMeshAgent>();
        if (existingAgent != null)
        {
            detectedAgentTypeID = existingAgent.agentTypeID;
            hasDetectedAgentType = true;
            Debug.Log($"[NPCCarSpawner] Auto-detected agentTypeID: {detectedAgentTypeID} from {existingAgent.name}");
        }
        else
        {
            // Fallback to what we saw on Car 5
            detectedAgentTypeID = -1923039037;
            hasDetectedAgentType = true;
            Debug.LogWarning("[NPCCarSpawner] No existing NavMeshAgent found in scene to copy agentTypeID. Falling back to -1923039037");
        }

        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            // Only spawn if under the limit
            int activeCount = valetSystem.activeSessions.Count;
            if (activeCount < maxNPCCars)
            {
                SpawnCar();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnCar()
    {
        // Pick a random prefab
        int prefabIndex = Random.Range(0, carPrefabs.Length);
        GameObject prefab = carPrefabs[prefabIndex];

        if (prefab == null)
        {
            Debug.LogWarning("[NPCCarSpawner] Null prefab at index " + prefabIndex);
            return;
        }

        // Determine spawn position and rotation
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = Quaternion.Euler(spawnRotation);

        // Find nearest NavMesh position using the correct agentTypeID
        Vector3 spawnPos = pos;
        NavMeshHit hit;
        NavMeshQueryFilter filter = new NavMeshQueryFilter();
        filter.agentTypeID = hasDetectedAgentType ? detectedAgentTypeID : -1923039037;
        filter.areaMask = NavMesh.AllAreas;

        if (NavMesh.SamplePosition(pos, out hit, 15f, filter))
        {
            spawnPos = hit.position;
        }
        else
        {
            Debug.LogWarning($"[NPCCarSpawner] Could not find NavMesh close to spawn position {pos} for agentType {filter.agentTypeID}!");
        }

        // Instantiate the car directly at the valid NavMesh position
        GameObject carObj = Instantiate(prefab, spawnPos, rot);
        carObj.transform.localScale = Vector3.one * carScale;
        spawnedCount++;
        carObj.name = $"NPC_{prefab.name}_{spawnedCount}";

        // Ensure it has a NavMeshAgent
        NavMeshAgent agent = carObj.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = carObj.AddComponent<NavMeshAgent>();
        }
        ConfigureAgent(agent);

        // Ensure it has a SmartCarNavigator
        SmartCarNavigator navigator = carObj.GetComponent<SmartCarNavigator>();
        if (navigator == null)
        {
            navigator = carObj.AddComponent<SmartCarNavigator>();
        }
        // Set the car as not initially parked (it's arriving, not sitting in a spot)
        navigator.IsParked = false;

        // Assign a random plate number (the valet system will generate owner details)
        navigator.plateNumber = GenerateRandomPlate();

        // Warp the agent onto the NavMesh
        if (NavMesh.SamplePosition(spawnPos, out hit, 10f, filter))
        {
            Vector3 warpPos = new Vector3(hit.position.x, hit.position.y + agent.baseOffset, hit.position.z);
            agent.Warp(warpPos);
            carObj.transform.position = warpPos;
        }

        Debug.Log($"<color=yellow>[NPCCarSpawner] Spawned {carObj.name} at {carObj.transform.position}</color>");

        // Register with the valet system
        valetSystem.RegisterCar(navigator);
    }

    private void ConfigureAgent(NavMeshAgent agent)
    {
        if (hasDetectedAgentType)
        {
            agent.agentTypeID = detectedAgentTypeID;
        }
        else
        {
            agent.agentTypeID = -1923039037; // Fallback
        }
        agent.speed = agentSpeed;
        agent.angularSpeed = agentAngularSpeed;
        agent.acceleration = agentAcceleration;
        agent.radius = agentRadius;
        agent.height = agentHeight;
        agent.baseOffset = agentBaseOffset;
        agent.stoppingDistance = agentStoppingDistance;
        agent.avoidancePriority = agentAvoidancePriority;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.autoBraking = true;
        agent.autoRepath = true;
    }

    private string GenerateRandomPlate()
    {
        char c1 = (char)Random.Range('A', 'Z' + 1);
        char c2 = (char)Random.Range('A', 'Z' + 1);
        int num = Random.Range(100, 999);
        char c3 = (char)Random.Range('A', 'Z' + 1);
        char c4 = (char)Random.Range('A', 'Z' + 1);
        return $"{c1}{c2}-{num}-{c3}{c4}";
    }

    private void OnGUI()
    {
        // Design a sleek dark-themed GUI box in the top-left corner
        GUI.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.85f);
        GUILayout.BeginArea(new Rect(10f, 10f, 280f, 220f), GUI.skin.box);
        GUILayout.BeginVertical();

        // Title
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.fontSize = 14;
        titleStyle.normal.textColor = Color.cyan;
        GUILayout.Label("Valet Spawner Controls", titleStyle);
        GUILayout.Space(5f);

        // Stats readout
        int activeCount = valetSystem != null ? valetSystem.activeSessions.Count : 0;
        GUILayout.Label($"Active Cars: {activeCount} / {maxNPCCars}", GUI.skin.label);
        GUILayout.Space(5f);

        // Max NPC Cars Slider
        GUILayout.Label($"Max NPC Cars: {maxNPCCars}", GUI.skin.label);
        maxNPCCars = Mathf.RoundToInt(GUILayout.HorizontalSlider(maxNPCCars, 1f, 20f));
        GUILayout.Space(5f);

        // Spawn Interval Slider
        GUILayout.Label($"Spawn Interval: {spawnInterval:F1} seconds", GUI.skin.label);
        spawnInterval = GUILayout.HorizontalSlider(spawnInterval, 1f, 60f);
        GUILayout.Space(10f);

        // Manual Spawn button
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Force Spawn Car Now", GUILayout.Height(30f)))
        {
            if (valetSystem != null)
            {
                SpawnCar();
            }
        }
        GUI.backgroundColor = Color.white;

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
