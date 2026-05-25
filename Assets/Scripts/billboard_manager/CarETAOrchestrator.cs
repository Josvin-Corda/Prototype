using UnityEngine;
using System.Collections;

public class CarETAOrchestrator : MonoBehaviour
{
    [Header("Route")]
    [SerializeField] private Transform[] routeNodes;
    [SerializeField] private float speedMetersPerSecond = 3f;
    [SerializeField] private string pickupPointTag = "A";

    [Header("ETA")]
    [SerializeField] private float updateInterval = 1f;
    [SerializeField] private float waypointReachThreshold = 1f;

    private int currentNodeIndex = 0;
    private bool isCalculating = false;
    private Coroutine etaCoroutine;

    public void StartETACalculation()
    {
        if (isCalculating)
            return;

        if (routeNodes == null || routeNodes.Length < 2)
        {
            Debug.LogError("Route nodes missing or insufficient.");
            return;
        }

        currentNodeIndex = FindClosestNodeIndex();
        isCalculating = true;

        etaCoroutine = StartCoroutine(UpdateETA());
    }

    private IEnumerator UpdateETA()
    {
        while (isCalculating)
        {
            UpdateCurrentNodeIndex();

            float remainingDistance = CalculateRemainingDistance();
            int etaSeconds = Mathf.CeilToInt(remainingDistance / speedMetersPerSecond);

            string status = etaSeconds > 0 ? "Vehicle returning" : "Vehicle arrived";

            SimulationEvents.RaiseETAUpdated(etaSeconds, pickupPointTag, status);

            if (etaSeconds <= 0)
            {
                isCalculating = false;
                yield break;
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void UpdateCurrentNodeIndex()
    {
        if (currentNodeIndex >= routeNodes.Length)
            return;

        float distance = Vector3.Distance(transform.position, routeNodes[currentNodeIndex].position);

        if (distance <= waypointReachThreshold)
            currentNodeIndex++;
    }

    private float CalculateRemainingDistance()
    {
        if (currentNodeIndex >= routeNodes.Length)
            return 0f;

        float distance = Vector3.Distance(transform.position, routeNodes[currentNodeIndex].position);

        for (int i = currentNodeIndex; i < routeNodes.Length - 1; i++)
        {
            distance += Vector3.Distance(routeNodes[i].position, routeNodes[i + 1].position);
        }

        return distance;
    }

    private int FindClosestNodeIndex()
    {
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < routeNodes.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, routeNodes[i].position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }
}