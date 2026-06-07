using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarETAOrchestrator : MonoBehaviour
{
    [Header("ETA Base Settings")]
    [SerializeField] private float speedMetersPerSecond = 3f;
    [SerializeField] private float waypointReachThreshold = 1f;

    [Header("Delay Providers")]
    [SerializeField] private ParkingTrafficProvider parkingTrafficProvider;

    [Header("Route Delay Settings")]
    [SerializeField] private float defaultPedestrianCrossingDelaySeconds = 8f;
    [SerializeField] private float safetyBufferSeconds = 5f;

    [Header("Update Policy")]
    [SerializeField] private float internalCalculationInterval = 1f;
    [SerializeField] private float billboardPublishInterval = 10f;
    [SerializeField] private int etaChangeThresholdSeconds = 15;
    [SerializeField] private int roundEtaToNearestSeconds = 30;

    [Header("Debug")]
    [SerializeField] private bool logDebugInfo = true;

    private readonly List<Transform> currentRoute = new List<Transform>();

    private string currentPickupLabel = "-";
    private int currentNodeIndex = 0;
    private bool isCalculating = false;
    private Coroutine etaCoroutine;

    private int lastPublishedEtaSeconds = -1;
    private string lastPublishedStatus = "";
    private float lastPublishTime = -999f;

    public bool HasRoute => currentRoute.Count >= 2;
    public string CurrentPickupLabel => currentPickupLabel;

    public void SetDynamicRoute(IList<Transform> routeNodes, string pickupLabel)
    {
        ClearRoute();

        if (routeNodes == null || routeNodes.Count < 2)
        {
            Debug.LogError("CarETAOrchestrator: dynamic route is null or contains fewer than 2 nodes.");
            return;
        }

        foreach (Transform node in routeNodes)
        {
            if (node != null)
                currentRoute.Add(node);
        }

        if (currentRoute.Count < 2)
        {
            Debug.LogError("CarETAOrchestrator: valid dynamic route contains fewer than 2 nodes.");
            return;
        }

        currentPickupLabel = string.IsNullOrWhiteSpace(pickupLabel) ? "-" : pickupLabel;
        currentNodeIndex = FindClosestNodeIndex();

        lastPublishedEtaSeconds = -1;
        lastPublishedStatus = "";
        lastPublishTime = -999f;

        if (logDebugInfo)
        {
            Debug.Log($"CarETAOrchestrator: dynamic route set. Nodes: {currentRoute.Count}, Pickup: {currentPickupLabel}");
        }
    }

    public void StartETACalculation()
    {
        if (!HasRoute)
        {
            Debug.LogError("CarETAOrchestrator: cannot start ETA calculation. No valid route assigned.");
            return;
        }

        if (speedMetersPerSecond <= 0f)
        {
            Debug.LogError("CarETAOrchestrator: speedMetersPerSecond must be greater than zero.");
            return;
        }

        if (etaCoroutine != null)
            StopCoroutine(etaCoroutine);

        isCalculating = true;
        etaCoroutine = StartCoroutine(UpdateETA());
    }

    public void StopETACalculation()
    {
        if (etaCoroutine != null)
        {
            StopCoroutine(etaCoroutine);
            etaCoroutine = null;
        }

        isCalculating = false;
    }

    public void ClearRoute()
    {
        StopETACalculation();

        currentRoute.Clear();
        currentPickupLabel = "-";
        currentNodeIndex = 0;

        lastPublishedEtaSeconds = -1;
        lastPublishedStatus = "";
        lastPublishTime = -999f;
    }

    private IEnumerator UpdateETA()
    {
        while (isCalculating)
        {
            UpdateCurrentNodeIndex();

            int rawEtaSeconds = CalculateRawETASeconds();
            int displayedEtaSeconds = RoundETA(rawEtaSeconds);

            string status = rawEtaSeconds > 0 ? "Vehicle returning" : "Vehicle arrived";

            if (ShouldPublish(displayedEtaSeconds, status))
            {
                PublishETA(displayedEtaSeconds, status);
            }

            if (rawEtaSeconds <= 0)
            {
                isCalculating = false;
                etaCoroutine = null;
                yield break;
            }

            yield return new WaitForSeconds(internalCalculationInterval);
        }
    }

    private int CalculateRawETASeconds()
    {
        float remainingDistance = CalculateRemainingDistance();

        float baseTravelTime = remainingDistance / speedMetersPerSecond;
        float pedestrianDelay = CalculateRemainingPedestrianCrossingDelay();
        float trafficDelay = parkingTrafficProvider != null
            ? parkingTrafficProvider.GetTrafficDelaySeconds()
            : 0f;

        float totalEta =
            baseTravelTime +
            pedestrianDelay +
            Mathf.Max(0f, trafficDelay) +
            Mathf.Max(0f, safetyBufferSeconds);

        if (logDebugInfo)
        {
            Debug.Log(
                $"CarETAOrchestrator ETA: base={baseTravelTime:F1}s, " +
                $"pedestrianDelay={pedestrianDelay:F1}s, " +
                $"trafficDelay={trafficDelay:F1}s, " +
                $"safetyBuffer={safetyBufferSeconds:F1}s, " +
                $"total={totalEta:F1}s"
            );
        }

        return Mathf.CeilToInt(totalEta);
    }

    private float CalculateRemainingDistance()
    {
        if (currentRoute.Count < 2)
            return 0f;

        if (currentNodeIndex >= currentRoute.Count)
            return 0f;

        float distance = Vector3.Distance(
            transform.position,
            currentRoute[currentNodeIndex].position
        );

        for (int i = currentNodeIndex; i < currentRoute.Count - 1; i++)
        {
            distance += Vector3.Distance(
                currentRoute[i].position,
                currentRoute[i + 1].position
            );
        }

        return distance;
    }

    private float CalculateRemainingPedestrianCrossingDelay()
    {
        float delay = 0f;

        for (int i = currentNodeIndex; i < currentRoute.Count; i++)
        {
            RouteNodeMetadata metadata = currentRoute[i].GetComponent<RouteNodeMetadata>();

            if (metadata == null)
                continue;

            if (!metadata.IsPedestrianCrossing)
                continue;

            delay += metadata.OverrideDefaultCrossingDelay
                ? metadata.CrossingDelaySeconds
                : Mathf.Max(0f, defaultPedestrianCrossingDelaySeconds);
        }

        return delay;
    }

    private void UpdateCurrentNodeIndex()
    {
        if (currentNodeIndex >= currentRoute.Count)
            return;

        float distance = Vector3.Distance(
            transform.position,
            currentRoute[currentNodeIndex].position
        );

        if (distance <= waypointReachThreshold)
        {
            currentNodeIndex++;
        }
    }

    private int FindClosestNodeIndex()
    {
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < currentRoute.Count; i++)
        {
            float distance = Vector3.Distance(
                transform.position,
                currentRoute[i].position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private int RoundETA(int etaSeconds)
    {
        if (etaSeconds <= 0)
            return 0;

        if (roundEtaToNearestSeconds <= 0)
            return etaSeconds;

        return Mathf.CeilToInt((float)etaSeconds / roundEtaToNearestSeconds)
               * roundEtaToNearestSeconds;
    }

    private bool ShouldPublish(int etaSeconds, string status)
    {
        if (lastPublishedEtaSeconds < 0)
            return true;

        if (etaSeconds <= 0)
            return true;

        if (status != lastPublishedStatus)
            return true;

        if (Time.time - lastPublishTime >= billboardPublishInterval)
            return true;

        if (Mathf.Abs(etaSeconds - lastPublishedEtaSeconds) >= etaChangeThresholdSeconds)
            return true;

        return false;
    }

    private void PublishETA(int etaSeconds, string status)
    {
        SimulationEvents.RaiseETAUpdated(
            etaSeconds,
            currentPickupLabel,
            status
        );

        lastPublishedEtaSeconds = etaSeconds;
        lastPublishedStatus = status;
        lastPublishTime = Time.time;

        if (logDebugInfo)
        {
            Debug.Log($"CarETAOrchestrator: published ETA={etaSeconds}s, Pickup={currentPickupLabel}, Status={status}");
        }
    }
}