using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Simplified crosswalk traffic light system.
///
/// Default state: GREEN (pedestrians can cross, cars drive through freely).
///
/// Logic:
///   1. When a car is within criticalZoneNodeThreshold nodes of the crosswalk → light goes RED.
///      (This is detection/decision only — the car is NOT stopped here.)
///   2. If an NPC is inside the crosswalk AND the car has driven close to the stop line
///      (within stopLineHaltDistance) → car is physically stopped at the stop line.
///   3. After npcClearanceWait seconds or when the NPC clears, the car is released.
///   4. After the car clears the crosswalk X zone → Cooldown → back to CarsFree (GREEN).
/// </summary>
public class CrosswalkTrafficLight : MonoBehaviour
{
    private enum SignalState
    {
        CarsFree,       // Default: GREEN for pedestrians, cars flow freely
        CarApproaching, // Car within threshold: RED for pedestrians, car may be stopped if NPC crossing
        CarCrossing,    // Car committed and crossing: RED for pedestrians
        Cooldown        // Brief gap after car clears before returning to CarsFree
    }

    // ─── Stop Lines ─────────────────────────────────────────────────────────────
    [Header("Stop Lines")]
    [Tooltip("Stop line for Car lane 1.")]
    [SerializeField] private Transform stopLine1;

    [Tooltip("Stop line for Car lane 2.")]
    [SerializeField] private Transform stopLine2;

    // ─── Pedestrian Strips (visual ground markings) ──────────────────────────────
    [Header("Pedestrian Strips (Ground Visual)")]
    [Tooltip("Renderers for the LED strips on the ground at the ends of the crosswalk.")]
    [SerializeField] private MeshRenderer[] pedestrianStrips;

    // ─── Physical Light Bars ─────────────────────────────────────────────────────
    [Header("Pedestrian Light Bars")]
    [Tooltip("Renderers for the vertical light bars at either end of the crosswalk.")]
    [FormerlySerializedAs("carLightRenderers")]
    [SerializeField] private MeshRenderer[] pedestrianLightBars;

    // ─── Predictive / Threshold Settings ─────────────────────────────────────────
    [Header("Predictive Safety Settings")]
    [Tooltip("How many route nodes ahead of the car to start the warning (critical zone).")]
    [SerializeField] private int criticalZoneNodeThreshold = 3;

    [Tooltip("X coordinate of the crosswalk centre. Used to detect when a car has cleared the crosswalk.")]
    [SerializeField] private float crosswalkCentreX = 41.53f;

    [Tooltip("Z coordinate of the crosswalk centre. Leave at 0 to auto-set from the GameObject's Z position on first run.")]
    [SerializeField] private float crosswalkCentreZ = 0f;

    [Tooltip("Half-width on the X axis that defines the crosswalk interior for NPC detection.")]
    [SerializeField] private float crosswalkHalfWidth = 3.0f;

    [Tooltip("Half-depth on the Z axis that defines the crosswalk interior for NPC detection.")]
    [SerializeField] private float crosswalkHalfDepth = 6.0f;

    // ─── Timing Settings ─────────────────────────────────────────────────────────
    [Header("Timing Settings")]
    [Tooltip("How long (seconds) to wait in Cooldown state before reverting to green.")]
    [SerializeField] private float cooldownDuration = 3.0f;

    [Tooltip("Maximum seconds to wait for crossing NPCs to clear before releasing the stopped car anyway.")]
    [SerializeField] private float npcClearanceWait = 10.0f;

    [Tooltip("How close (meters) a car must be to the stop line before it is physically stopped. " +
             "The criticalZoneNodeThreshold triggers the RED light early, but the car only brakes here.")]
    [SerializeField] private float stopLineHaltDistance = 5.0f;

    [Tooltip("Seconds the crosswalk must remain car-free after a crossing before the light turns green again. " +
             "Prevents the signal flipping green while the car is still partially over the line.")]
    [SerializeField] private float postClearDelay = 2.0f;

    // ─── Read-Only Status ─────────────────────────────────────────────────────────
    [Header("Status (Read Only)")]
    [SerializeField] private SignalState currentState = SignalState.CarsFree;
    [SerializeField] private float stateTimer = 0f;
    [SerializeField] private bool npcCurrentlyCrossing = false;

    // ─── Internal ────────────────────────────────────────────────────────────────
    private TrafficNode criticalNode1;
    private TrafficNode criticalNode2;
    private float postClearTimer = 0f; // accumulates while car is clear of crosswalk

    // Cars that are currently being held at the stopline waiting for NPC to clear
    private readonly HashSet<SmartCarNavigator> heldCars = new HashSet<SmartCarNavigator>();

    private static readonly Color OffColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    // ─── Public Properties ────────────────────────────────────────────────────────
    /// <summary>True when pedestrians may cross (light is green / cars free).</summary>
    public bool CanPedestriansCross => currentState == SignalState.CarsFree;

    /// <summary>True when a car is approaching or crossing (light is red for pedestrians).</summary>
    public bool IsCarActive => currentState == SignalState.CarApproaching || currentState == SignalState.CarCrossing;

    public MeshRenderer[] PedestrianStrips    => pedestrianStrips;
    public MeshRenderer[] PedestrianLightBars => pedestrianLightBars;
    public float CrosswalkCentreX  => crosswalkCentreX;
    public float CrosswalkCentreZ  => crosswalkCentreZ;
    public float CrosswalkHalfWidth  => crosswalkHalfWidth;
    public float CrosswalkHalfDepth  => crosswalkHalfDepth;

    // ─────────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        currentState = SignalState.CarsFree;
        stateTimer   = 0f;

        // Auto-initialise Z centre from the GameObject's world position if not manually set
        if (crosswalkCentreZ == 0f)
        {
            crosswalkCentreZ = transform.position.z;
            Debug.Log($"[CrosswalkTrafficLight] {gameObject.name}: crosswalkCentreZ auto-set to {crosswalkCentreZ:F2} from transform position.");
        }

        AutoAssignLightBars();
        UpdateVisuals();
    }

    private void Update()
    {
        npcCurrentlyCrossing = IsNPCInsideCrosswalk();

        switch (currentState)
        {
            // ── Default: green for peds, cars flow freely ──────────────────────
            case SignalState.CarsFree:
                if (IsCarInCriticalZone())
                {
                    EnterCarApproaching();
                }
                break;

            // ── Car detected approaching within threshold ───────────────────────
            case SignalState.CarApproaching:
                stateTimer += Time.deltaTime;

                if (npcCurrentlyCrossing)
                {
                    // Hold all approaching cars at the stopline
                    HoldCarsAtStopLine();

                    // Wait up to npcClearanceWait, then release anyway
                    if (stateTimer >= npcClearanceWait)
                    {
                        ReleaseCarsFromStopLine();
                        EnterCarCrossing();
                    }
                }
                else
                {
                    // No NPC crossing — car drives through freely
                    ReleaseCarsFromStopLine();
                    EnterCarCrossing();
                }
                break;

            // ── Car is crossing the crosswalk ──────────────────────────────────
            case SignalState.CarCrossing:
                if (!IsCarNearCrosswalk())
                {
                    // Car appears clear — start (or continue) the confirmation timer
                    postClearTimer += Time.deltaTime;
                    if (postClearTimer >= postClearDelay)
                    {
                        postClearTimer = 0f;
                        currentState   = SignalState.Cooldown;
                        stateTimer     = 0f;
                        UpdateVisuals();
                    }
                }
                else
                {
                    // Car re-entered the zone (e.g. slow crosser) — reset the timer
                    postClearTimer = 0f;
                }
                break;

            // ── Brief cooldown after car clears ───────────────────────────────
            case SignalState.Cooldown:
                stateTimer += Time.deltaTime;
                if (stateTimer >= cooldownDuration)
                {
                    currentState = SignalState.CarsFree;
                    stateTimer   = 0f;
                    UpdateVisuals();
                    Debug.Log($"[CrosswalkTrafficLight] {gameObject.name}: Crosswalk cleared → GREEN");
                }
                break;
        }
    }

    // ─── State Transitions ────────────────────────────────────────────────────────
    private void EnterCarApproaching()
    {
        currentState = SignalState.CarApproaching;
        stateTimer   = 0f;
        UpdateVisuals();
        Debug.Log($"[CrosswalkTrafficLight] {gameObject.name}: Car approaching → RED");
    }

    private void EnterCarCrossing()
    {
        currentState = SignalState.CarCrossing;
        stateTimer   = 0f;
        UpdateVisuals();
        Debug.Log($"[CrosswalkTrafficLight] {gameObject.name}: Car crossing → holding RED");
    }

    // ─── Car Stop / Release ───────────────────────────────────────────────────────
    /// <summary>
    /// Called every frame while an NPC is crossing and a car is approaching.
    /// Only stops a car once it has driven close enough to the stop line
    /// (within stopLineHaltDistance). Cars farther away keep driving normally.
    /// </summary>
    private void HoldCarsAtStopLine()
    {
        SmartCarNavigator[] allCars = Object.FindObjectsByType<SmartCarNavigator>(FindObjectsSortMode.None);
        foreach (var car in allCars)
        {
            if (car == null || car.IsParked) continue;
            if (heldCars.Contains(car)) continue; // already held

            // Only consider cars that are actually heading for this crosswalk
            bool isApproaching = (criticalNode1 != null && car.IsNodeWithinNextSegments(criticalNode1, criticalZoneNodeThreshold))
                              || (criticalNode2 != null && car.IsNodeWithinNextSegments(criticalNode2, criticalZoneNodeThreshold));
            if (!isApproaching) continue;

            // Check distance to each stop line — only brake once the car is close
            float distToLine1 = stopLine1 != null
                ? Vector3.Distance(new Vector3(car.transform.position.x, 0f, car.transform.position.z),
                                   new Vector3(stopLine1.position.x,     0f, stopLine1.position.z))
                : float.MaxValue;

            float distToLine2 = stopLine2 != null
                ? Vector3.Distance(new Vector3(car.transform.position.x, 0f, car.transform.position.z),
                                   new Vector3(stopLine2.position.x,     0f, stopLine2.position.z))
                : float.MaxValue;

            float nearest = Mathf.Min(distToLine1, distToLine2);

            if (nearest <= stopLineHaltDistance)
            {
                car.IsCrosswalkStopped = true;
                heldCars.Add(car);
                Debug.Log($"[CrosswalkTrafficLight] {gameObject.name}: Stopping car '{car.name}' at stop line ({nearest:F1}m away) — NPC crossing.");
            }
            // else: car is still far away, let it drive closer naturally
        }
    }

    private void ReleaseCarsFromStopLine()
    {
        foreach (var car in heldCars)
        {
            if (car != null)
            {
                car.IsCrosswalkStopped = false;
                Debug.Log($"[CrosswalkTrafficLight] {gameObject.name}: Released car '{car.name}' from stopline.");
            }
        }
        heldCars.Clear();
    }

    // ─── Detection ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Checks whether any NPC is physically inside the crosswalk bounding rectangle.
    /// Uses a simple X/Z range check based on the crosswalk centre and half-extents.
    /// </summary>
    private bool IsNPCInsideCrosswalk()
    {
        HumanNPCBehavior[] allNPCs = Object.FindObjectsByType<HumanNPCBehavior>(FindObjectsSortMode.None);
        float minX = crosswalkCentreX - crosswalkHalfWidth;
        float maxX = crosswalkCentreX + crosswalkHalfWidth;
        float minZ = crosswalkCentreZ - crosswalkHalfDepth;
        float maxZ = crosswalkCentreZ + crosswalkHalfDepth;

        foreach (var npc in allNPCs)
        {
            if (npc == null) continue;
            Vector3 pos = npc.transform.position;
            if (pos.x >= minX && pos.x <= maxX && pos.z >= minZ && pos.z <= maxZ)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns true if any moving car is within the node-based critical zone.
    /// </summary>
    private bool IsCarInCriticalZone()
    {
        // Resolve stop-line nodes once
        if (TrafficNetwork.Instance != null)
        {
            if (criticalNode1 == null && stopLine1 != null)
                criticalNode1 = TrafficNetwork.Instance.GetClosestNode(stopLine1.position);
            if (criticalNode2 == null && stopLine2 != null)
                criticalNode2 = TrafficNetwork.Instance.GetClosestNode(stopLine2.position);
        }

        SmartCarNavigator[] allCars = Object.FindObjectsByType<SmartCarNavigator>(FindObjectsSortMode.None);
        foreach (var car in allCars)
        {
            if (car == null || car.IsParked) continue;

            if (criticalNode1 != null && car.IsNodeWithinNextSegments(criticalNode1, criticalZoneNodeThreshold)) return true;
            if (criticalNode2 != null && car.IsNodeWithinNextSegments(criticalNode2, criticalZoneNodeThreshold)) return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if any active car is still near the crosswalk centre X (hasn't fully cleared it yet).
    /// </summary>
    private bool IsCarNearCrosswalk()
    {
        SmartCarNavigator[] allCars = Object.FindObjectsByType<SmartCarNavigator>(FindObjectsSortMode.None);
        foreach (var car in allCars)
        {
            if (car == null || car.IsParked) continue;
            float dx = Mathf.Abs(car.transform.position.x - crosswalkCentreX);
            if (dx <= crosswalkHalfWidth + 3f) // small extra margin
                return true;
        }
        return false;
    }

    // ─── Visuals ──────────────────────────────────────────────────────────────────
    private void UpdateVisuals()
    {
        // GREEN when cars free, RED during CarApproaching / CarCrossing / Cooldown
        Color pedColor = (currentState == SignalState.CarsFree) ? Color.green : Color.red;

        foreach (var strip in pedestrianStrips)
        {
            if (strip != null) SetRendererVisual(strip, pedColor, true);
        }
        foreach (var bar in pedestrianLightBars)
        {
            if (bar != null) SetRendererVisual(bar, pedColor, true);
        }
    }

    private void SetRendererVisual(MeshRenderer renderer, Color activeColor, bool isActive)
    {
        if (renderer == null) return;

        Color targetColor = isActive ? activeColor : OffColor;
        renderer.material.color = targetColor;

        if (renderer.material.HasProperty("_BaseColor"))
            renderer.material.SetColor("_BaseColor", targetColor);

        if (isActive)
        {
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", activeColor * 0.4f);
        }
        else
        {
            renderer.material.DisableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", Color.clear);
        }
    }

    // ─── Light Bar Auto-Assignment ────────────────────────────────────────────────
    private void AutoAssignLightBars()
    {
        bool needsAssigning = pedestrianLightBars == null || pedestrianLightBars.Length == 0;
        if (!needsAssigning)
        {
            foreach (var r in pedestrianLightBars)
            {
                if (r == null) { needsAssigning = true; break; }
            }
        }

        if (!needsAssigning) return;

        string suffix = gameObject.name.EndsWith("R") ? "R" : "L";
        var bar1 = GameObject.Find($"LightBar{suffix}1");
        var bar2 = GameObject.Find($"LightBar{suffix}2");

        var list = new List<MeshRenderer>();
        if (bar1 != null) { var r = bar1.GetComponent<MeshRenderer>(); if (r != null) list.Add(r); }
        if (bar2 != null) { var r = bar2.GetComponent<MeshRenderer>(); if (r != null) list.Add(r); }

        if (list.Count > 0)
        {
            pedestrianLightBars = list.ToArray();
            Debug.Log($"[CrosswalkTrafficLight] {gameObject.name}: Auto-assigned light bars: {string.Join(", ", list.ConvertAll(r => r.gameObject.name))}");
        }
        else
        {
            Debug.LogWarning($"[CrosswalkTrafficLight] {gameObject.name}: Could not find LightBar{suffix}1/2 in the scene.");
        }
    }

    // ─── Gizmos ───────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // Draw the NPC detection bounding box (green when free, red otherwise)
        Gizmos.color = (currentState == SignalState.CarsFree) ? Color.green : Color.red;
        float gizmoZ = (crosswalkCentreZ == 0f) ? transform.position.z : crosswalkCentreZ;
        Vector3 centre = new Vector3(crosswalkCentreX, transform.position.y + 1f, gizmoZ);
        Vector3 size   = new Vector3(crosswalkHalfWidth * 2f, 2f, crosswalkHalfDepth * 2f);
        Gizmos.DrawWireCube(centre, size);

        // Draw stop-line spheres in orange
        Gizmos.color = new Color(1f, 0.5f, 0f);
        if (stopLine1 != null) Gizmos.DrawWireSphere(stopLine1.position, 1f);
        if (stopLine2 != null) Gizmos.DrawWireSphere(stopLine2.position, 1f);
    }
}
