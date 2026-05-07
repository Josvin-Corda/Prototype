using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A modular central system to test assigning spots to cars using the new Node Network.
/// </summary>
public class ParkingSimulator : MonoBehaviour
{
    [Header("Test Mode")]
    [Tooltip("Check this to send ONE specific car to ONE specific spot to test your node paths.")]
    public bool useTestMode = false;
    public SmartCarNavigator testCar;
    public Transform testSpot;

    [Header("Random Allotment Configuration")]
    [Tooltip("How often should a new spot be assigned? (in seconds)")]
    public float assignmentInterval = 5f;

    [Header("References")]
    [Tooltip("Drag all your parking spot Transforms here.")]
    public List<Transform> allParkingSpots;
    
    [Tooltip("Drag the cars currently in your scene here.")]
    public List<SmartCarNavigator> activeCars;

    private void Start()
    {
        if (useTestMode)
        {
            if (testCar != null && testSpot != null)
            {
                // Run the isolated test
                StartCoroutine(TestSingleRoute());
            }
            else
            {
                Debug.LogWarning("Test Mode is ON, but Test Car or Test Spot is missing in the inspector!");
            }
        }
        else
        {
            // Normal operation
            if (allParkingSpots.Count > 0 && activeCars.Count > 0)
            {
                // Start the timed loop
                StartCoroutine(AssignmentLoop());
            }
            else
            {
                Debug.LogWarning("Simulator is missing spots or cars in the inspector!");
            }
        }
    }

    private IEnumerator TestSingleRoute()
    {
        // Give the network 1 second to fully initialize all nodes on startup
        yield return new WaitForSeconds(1f);

        Debug.Log($"[Test Mode] Routing {testCar.gameObject.name} to {testSpot.gameObject.name}");

        // 1. Find the closest road node to where the car currently is
        TrafficNode startNode = TrafficNetwork.Instance.GetClosestNode(testCar.transform.position);
        
        // 2. Find the closest road node to the assigned parking spot
        TrafficNode endNode = TrafficNetwork.Instance.GetClosestNode(testSpot.position);

        // 3. Ask the network to calculate the shortest path adhering to one-way rules
        List<TrafficNode> calculatedPath = TrafficNetwork.Instance.GetPath(startNode, endNode);

        if (calculatedPath.Count == 0 && startNode != endNode)
        {
            Debug.LogWarning($"[Traffic Network] TEST FAILED: No legal path from {startNode.name} to {endNode.name}! Check your one-way node links.");
        }
        else
        {
            Debug.Log($"[Traffic Network] TEST SUCCESS: Found path with {calculatedPath.Count} nodes.");
        }

        // 4. Send the route AND the final parking spot to the car
        testCar.AssignRouteAndSpot(calculatedPath, testSpot);
    }

    private IEnumerator AssignmentLoop()
    {
        // Give the network 1 second to fully initialize all nodes on startup
        yield return new WaitForSeconds(1f);

        while (true)
        {
            yield return new WaitForSeconds(assignmentInterval);

            // Pick a random car and a random spot
            SmartCarNavigator randomCar = activeCars[Random.Range(0, activeCars.Count)];
            Transform randomSpot = allParkingSpots[Random.Range(0, allParkingSpots.Count)];

            Debug.Log($"[Central System] Routing {randomCar.gameObject.name} to {randomSpot.gameObject.name}");

            // --- THE NEW UNIVERSAL ROUTING LOGIC ---
            
            // 1. Find the closest road node to where the car currently is
            TrafficNode startNode = TrafficNetwork.Instance.GetClosestNode(randomCar.transform.position);
            
            // 2. Find the closest road node to the assigned parking spot
            TrafficNode endNode = TrafficNetwork.Instance.GetClosestNode(randomSpot.position);

            // 3. Ask the network to calculate the shortest path adhering to one-way rules
            List<TrafficNode> calculatedPath = TrafficNetwork.Instance.GetPath(startNode, endNode);

            if (calculatedPath.Count == 0 && startNode != endNode)
            {
                Debug.LogWarning($"[Traffic Network] No legal path from {startNode.name} to {endNode.name}!");
            }

            // 4. Send the route AND the final parking spot to the car
            randomCar.AssignRouteAndSpot(calculatedPath, randomSpot);
        }
    }
}