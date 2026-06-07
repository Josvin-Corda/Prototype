using UnityEngine;

public class ParkingTrafficProvider : MonoBehaviour
{
    public enum TrafficInputMode
    {
        ManualInput,
        ExternalTrafficOrchestrator
    }

    [Header("Input Mode")]
    [SerializeField] private TrafficInputMode inputMode = TrafficInputMode.ManualInput;

    [Header("Manual Input")]
    [SerializeField] private int manualTotalCarsInSimulation = 0;
    [SerializeField] private int manualActiveCarsCount = 0;

    [Header("External Traffic Orchestrator Input")]
    [SerializeField] private int externalTotalCarsInSimulation = 0;
    [SerializeField] private int externalActiveCarsCount = 0;

    [Header("Delay Model")]
    [SerializeField] private float fixedTrafficBaseDelaySeconds = 3f;
    [SerializeField] private float delayPerActiveCarSeconds = 2f;
    [SerializeField] private float congestionSensitivity = 1f;
    [SerializeField] private float maxTrafficDelaySeconds = 30f;

    [Header("Debug")]
    [SerializeField] private bool logDebugInfo = false;

    public TrafficInputMode CurrentInputMode => inputMode;

    public int TotalCarsInSimulation
    {
        get
        {
            return inputMode == TrafficInputMode.ManualInput
                ? Mathf.Max(0, manualTotalCarsInSimulation)
                : Mathf.Max(0, externalTotalCarsInSimulation);
        }
    }

    public int ActiveCarsCount
    {
        get
        {
            int totalCars = TotalCarsInSimulation;

            int activeCars = inputMode == TrafficInputMode.ManualInput
                ? manualActiveCarsCount
                : externalActiveCarsCount;

            return Mathf.Clamp(activeCars, 0, totalCars);
        }
    }

    public float OccupancyRatio
    {
        get
        {
            if (TotalCarsInSimulation <= 0)
                return 0f;

            return (float)ActiveCarsCount / TotalCarsInSimulation;
        }
    }

    public float GetTrafficDelaySeconds()
    {
        float congestionFactor =
            1f + OccupancyRatio * Mathf.Max(0f, congestionSensitivity);

        float rawDelay =
            Mathf.Max(0f, fixedTrafficBaseDelaySeconds) +
            ActiveCarsCount *
            Mathf.Max(0f, delayPerActiveCarSeconds) *
            congestionFactor;

        float clampedDelay = Mathf.Clamp(
            rawDelay,
            0f,
            Mathf.Max(0f, maxTrafficDelaySeconds)
        );

        if (logDebugInfo)
        {
            Debug.Log(
                $"ParkingTrafficProvider: mode={inputMode}, " +
                $"totalCars={TotalCarsInSimulation}, " +
                $"activeCars={ActiveCarsCount}, " +
                $"occupancyRatio={OccupancyRatio:F2}, " +
                $"trafficDelay={clampedDelay:F1}s"
            );
        }

        return clampedDelay;
    }

    public void SetExternalTrafficData(int totalCars, int activeCars)
    {
        externalTotalCarsInSimulation = Mathf.Max(0, totalCars);
        externalActiveCarsCount = Mathf.Clamp(
            activeCars,
            0,
            externalTotalCarsInSimulation
        );
    }

    public void SetExternalTotalCarsInSimulation(int totalCars)
    {
        externalTotalCarsInSimulation = Mathf.Max(0, totalCars);
        externalActiveCarsCount = Mathf.Clamp(
            externalActiveCarsCount,
            0,
            externalTotalCarsInSimulation
        );
    }

    public void SetExternalActiveCarsCount(int activeCars)
    {
        externalActiveCarsCount = Mathf.Clamp(
            activeCars,
            0,
            externalTotalCarsInSimulation
        );
    }

    public void SetManualTrafficData(int totalCars, int activeCars)
    {
        manualTotalCarsInSimulation = Mathf.Max(0, totalCars);
        manualActiveCarsCount = Mathf.Clamp(
            activeCars,
            0,
            manualTotalCarsInSimulation
        );
    }
}