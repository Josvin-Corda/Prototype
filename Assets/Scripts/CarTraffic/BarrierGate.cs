using UnityEngine;

/// <summary>
/// Controls the lifting and closing functionality of a barrier gate.
/// Stops approaching cars and alters their headlight colors based on entrance/exit rules.
/// </summary>
public class BarrierGate : MonoBehaviour
{
    private enum GateState { Closed, Opening, Open, Closing }

    [Header("References")]
    [Tooltip("The Transform of the moving bar. If left empty, will attempt to find 'barrier gate/Bar' automatically.")]
    [SerializeField] private Transform barTransform;

    [Header("Gate Settings")]
    [Tooltip("Check this if this gate is the exit gate. Leave unchecked for entrance gate.")]
    [SerializeField] private bool isExitGate = false;

    [Header("Distance & Safety Settings")]
    [Tooltip("Distance at which the gate detects an approaching car and starts opening.")]
    [SerializeField] private float detectionRadius = 4.5f;

    [Tooltip("Distance at which the car is stopped if the gate is not yet fully open.")]
    [SerializeField] private float stopDistance = 3.0f;

    [Tooltip("Distance (or crossing threshold) at which the car is considered to have passed.")]
    [SerializeField] private float passDistance = 4.5f;

    [Header("Rotation Settings (Pivot)")]
    [Tooltip("Local Euler angles when the bar is fully closed.")]
    [SerializeField] private Vector3 closedLocalRotation = Vector3.zero;

    [Tooltip("Local Euler angles when the bar is fully open.")]
    [SerializeField] private Vector3 openLocalRotation = new Vector3(-90f, 0f, 0f);

    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] private float openSpeed = 90f;

    [Tooltip("Seconds to wait after the car passes before starting to close the gate.")]
    [SerializeField] private float closeDelay = 1.5f;

    [Header("Status (Read Only)")]
    [SerializeField] private GateState currentState = GateState.Closed;
    [SerializeField] private SmartCarNavigator activeCar = null;

    private float closeTimer = 0f;
    private bool carStopped = false;
    private bool lightTurnedOn = false;
    private float sequenceTimer = 0f;

    private void Start()
    {
        if (barTransform == null)
        {
            // Try to find the child named "Bar" in the standard prefab hierarchy
            barTransform = transform.Find("barrier gate/Bar");
            if (barTransform == null)
            {
                barTransform = transform.Find("Bar");
            }
        }

        if (barTransform == null)
        {
            Debug.LogError($"[BarrierGate] {gameObject.name} could not find Bar Transform! Please assign it in the Inspector.");
        }
        else
        {
            // Initialize at closed rotation
            barTransform.localRotation = Quaternion.Euler(closedLocalRotation);
        }
        currentState = GateState.Closed;
    }

    private void Update()
    {
        if (barTransform == null) return;

        // Smoothly rotate the bar
        bool shouldBeOpen = (currentState == GateState.Opening || currentState == GateState.Open);
        Quaternion targetRot = Quaternion.Euler(shouldBeOpen ? openLocalRotation : closedLocalRotation);
        barTransform.localRotation = Quaternion.RotateTowards(barTransform.localRotation, targetRot, openSpeed * Time.deltaTime);

        // State transition check based on rotation angle
        if (currentState == GateState.Opening)
        {
            if (Quaternion.Angle(barTransform.localRotation, targetRot) < 0.05f)
            {
                currentState = GateState.Open;
                Debug.Log($"[BarrierGate] {gameObject.name} is now fully OPEN.");
            }
        }
        else if (currentState == GateState.Closing)
        {
            if (Quaternion.Angle(barTransform.localRotation, targetRot) < 0.05f)
            {
                currentState = GateState.Closed;
                Debug.Log($"[BarrierGate] {gameObject.name} is now fully CLOSED.");
            }
        }

        // Active car tracking and scanning
        if (activeCar == null)
        {
            FindApproachingCar();

            // If still no car, and we are open/opening, countdown to close
            if (activeCar == null && (currentState == GateState.Open || currentState == GateState.Opening))
            {
                closeTimer -= Time.deltaTime;
                if (closeTimer <= 0)
                {
                    currentState = GateState.Closing;
                    Debug.Log($"[BarrierGate] {gameObject.name} closing due to no approaching cars.");
                }
            }
        }
        else
        {
            MonitorActiveCar();
        }
    }

    private void FindApproachingCar()
    {
        SmartCarNavigator[] allCars = Object.FindObjectsByType<SmartCarNavigator>(FindObjectsSortMode.None);
        SmartCarNavigator closestCar = null;
        float minDistance = float.MaxValue;

        foreach (var car in allCars)
        {
            if (car == null) continue;

            Vector3 carToGate = car.transform.position - transform.position;
            float distance = carToGate.magnitude;
            float dot = Vector3.Dot(carToGate, transform.forward);

            // Car is within detection radius and on the approaching (front) side of the gate
            if (distance <= detectionRadius && dot > 0.1f)
            {
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestCar = car;
                }
            }
        }

        if (closestCar != null)
        {
            activeCar = closestCar;
            carStopped = false;
            lightTurnedOn = false;
            sequenceTimer = 0f;
            Debug.Log($"[BarrierGate] {gameObject.name} detected approaching car: {activeCar.name} at distance {minDistance:F2}m.");
        }
    }

    private void MonitorActiveCar()
    {
        if (activeCar == null) return;

        // Check if car was destroyed/disabled
        if (!activeCar.gameObject.activeInHierarchy)
        {
            activeCar = null;
            closeTimer = closeDelay;
            carStopped = false;
            lightTurnedOn = false;
            return;
        }

        Vector3 carToGate = activeCar.transform.position - transform.position;
        float distance = carToGate.magnitude;
        float dot = Vector3.Dot(carToGate, transform.forward);

        // Check if the car has passed the gate (moved behind it or too far away)
        bool hasPassed = (dot < -0.5f) || (distance > passDistance);

        if (hasPassed)
        {
            Debug.Log($"[BarrierGate] {gameObject.name}: Car {activeCar.name} has passed the gate.");
            activeCar.IsBarrierStopped = false;
            activeCar = null;
            closeTimer = closeDelay;
            carStopped = false;
            lightTurnedOn = false;
            return;
        }

        // 1. Force the car to stop when it reaches the stop distance
        if (!carStopped)
        {
            if (distance <= stopDistance)
            {
                activeCar.IsBarrierStopped = true;
                carStopped = true;
                sequenceTimer = 0.8f; // Delay after stopping before turning on lights
                Debug.Log($"[BarrierGate] {gameObject.name} stopped {activeCar.name} at distance {distance:F2}m. Waiting to turn on lights.");
            }
        }
        // 2. Turn on/update headlights after stopping delay
        else if (!lightTurnedOn)
        {
            sequenceTimer -= Time.deltaTime;
            if (sequenceTimer <= 0f)
            {
                lightTurnedOn = true;
                sequenceTimer = 0.8f; // Delay after turning on lights before gate starts opening
                
                if (!isExitGate)
                {
                    // Entrance: turn headlight turquoise
                    activeCar.SetAutoparkLightColor(new Color(0f, 0.9f, 0.9f, 0.12f));
                    Debug.Log($"[BarrierGate] {gameObject.name} set headlight to TURQUOISE for {activeCar.name}.");
                }
                else
                {
                    // Exit: reset headlight to normal
                    activeCar.ResetAutoparkLight();
                    Debug.Log($"[BarrierGate] {gameObject.name} reset headlight to NORMAL for {activeCar.name}.");
                }
            }
        }
        // 3. Start opening the gate after the light delay
        else if (currentState == GateState.Closed || currentState == GateState.Closing)
        {
            sequenceTimer -= Time.deltaTime;
            if (sequenceTimer <= 0f)
            {
                currentState = GateState.Opening;
                Debug.Log($"[BarrierGate] {gameObject.name} starting to open for {activeCar.name} after light sequence.");
            }
        }
        // 4. Release the car when the gate is fully open
        else if (currentState == GateState.Open)
        {
            if (activeCar.IsBarrierStopped)
            {
                activeCar.IsBarrierStopped = false;
                Debug.Log($"[BarrierGate] {gameObject.name} resumed movement for {activeCar.name} (gate fully open).");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw detection and stop zones in Editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        // Draw forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
    }
}
