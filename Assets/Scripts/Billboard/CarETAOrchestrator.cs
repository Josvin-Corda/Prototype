using UnityEngine;
using System.Collections;

public class CarETAOrchestrator : MonoBehaviour
{
    [Header("Route Settings")]
    [Tooltip("Waypoints ordinati dalla posizione iniziale al pickup point.")]
    [SerializeField] private Transform[] routeNodes;

    [Tooltip("Velocità media teorica dell'auto in metri al secondo.")]
    [SerializeField] private float speedMetersPerSecond = 3f;

    [Tooltip("Tag o nome del pickup point: A, B, C.")]
    [SerializeField] private string pickupPointTag = "A";

    [Header("ETA Settings")]
    [SerializeField] private float waypointReachThreshold = 1.0f;
    [SerializeField] private float updateInterval = 1.0f;

    [Header("Runtime State")]
    [SerializeField] private bool debugStartOnPlay = false;

    private bool isCalculating = false;
    private int currentNodeIndex = 0;
    private float extraDelaySeconds = 0f;
    private Coroutine etaCoroutine;

    private void Start()
    {
        if (debugStartOnPlay)
        {
            StartETACalculation();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("StartNode") && !isCalculating)
        {
            StartETACalculation();
        }
    }

    public void StartETACalculation()
    {
        if (isCalculating)
        {
            return;
        }

        if (!ValidateRoute())
        {
            return;
        }

        currentNodeIndex = FindClosestRouteNodeIndex();
        isCalculating = true;

        SimulationEvents.RaiseETAUpdated(
            CalculateETASeconds(),
            pickupPointTag,
            "Vehicle returning"
        );

        etaCoroutine = StartCoroutine(CalculateAndBroadcastETA());
    }

    public void StopETACalculation()
    {
        if (etaCoroutine != null)
        {
            StopCoroutine(etaCoroutine);
        }

        etaCoroutine = null;
        isCalculating = false;
    }

    public void SetExtraDelay(float delaySeconds)
    {
        extraDelaySeconds = Mathf.Max(0f, delaySeconds);
    }

    public void AddTemporaryDelay(float delaySeconds)
    {
        extraDelaySeconds += Mathf.Max(0f, delaySeconds);
    }

    private IEnumerator CalculateAndBroadcastETA()
    {
        while (isCalculating)
        {
            UpdateCurrentNodeIndex();

            int etaSeconds = CalculateETASeconds();

            string statusMessage = etaSeconds > 0
                ? "Vehicle returning"
                : "Vehicle arrived";

            SimulationEvents.RaiseETAUpdated(
                etaSeconds,
                pickupPointTag,
                statusMessage
            );

            if (etaSeconds <= 0)
            {
                isCalculating = false;
                yield break;
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }

    private int CalculateETASeconds()
    {
        float remainingDistance = CalculateRemainingDistance();

        if (speedMetersPerSecond <= 0f)
        {
            Debug.LogWarning("Speed must be greater than zero.");
            return 0;
        }

        float eta = (remainingDistance / speedMetersPerSecond) + extraDelaySeconds;

        return Mathf.CeilToInt(eta);
    }

    private float CalculateRemainingDistance()
    {
        if (routeNodes == null || routeNodes.Length == 0)
        {
            return 0f;
        }

        if (currentNodeIndex >= routeNodes.Length)
        {
            return 0f;
        }

        float distance = Vector3.Distance(
            transform.position,
            routeNodes[currentNodeIndex].position
        );

        for (int i = currentNodeIndex; i < routeNodes.Length - 1; i++)
        {
            distance += Vector3.Distance(
                routeNodes[i].position,
                routeNodes[i + 1].position
            );
        }

        return distance;
    }

    private void UpdateCurrentNodeIndex()
    {
        if (routeNodes == null || routeNodes.Length == 0)
        {
            return;
        }

        if (currentNodeIndex >= routeNodes.Length)
        {
            return;
        }

        float distanceToCurrentNode = Vector3.Distance(
            transform.position,
            routeNodes[currentNodeIndex].position
        );

        if (distanceToCurrentNode <= waypointReachThreshold)
        {
            currentNodeIndex++;

            if (currentNodeIndex >= routeNodes.Length)
            {
                currentNodeIndex = routeNodes.Length;
            }
        }
    }

    private int FindClosestRouteNodeIndex()
    {
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < routeNodes.Length; i++)
        {
            if (routeNodes[i] == null)
            {
                continue;
            }

            float distance = Vector3.Distance(
                transform.position,
                routeNodes[i].position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private bool ValidateRoute()
    {
        if (routeNodes == null || routeNodes.Length < 2)
        {
            Debug.LogError("CarETAOrchestrator: routeNodes deve contenere almeno due waypoint.");
            return false;
        }

        for (int i = 0; i < routeNodes.Length; i++)
        {
            if (routeNodes[i] == null)
            {
                Debug.LogError($"CarETAOrchestrator: waypoint mancante all'indice {i}.");
                return false;
            }
        }

        if (speedMetersPerSecond <= 0f)
        {
            Debug.LogError("CarETAOrchestrator: speedMetersPerSecond deve essere maggiore di zero.");
            return false;
        }

        return true;
    }
}