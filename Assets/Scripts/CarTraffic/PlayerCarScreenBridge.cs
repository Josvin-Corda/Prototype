using UnityEngine;
using AHMI.InternalVehicleScreen;

/// <summary>
/// Translates player-car simulation progression into internal screen states.
/// Controls only the runtime player car and ignores all NPC activity.
/// </summary>
public class PlayerCarScreenBridge : MonoBehaviour
{
    [Header("Screen Control")]
    [SerializeField] private InternalVehicleScreenController screenController;

    [Header("Gate Settings")]
    [SerializeField] private float detectionRadius = 4.5f;
    [SerializeField] private float passDistance = 4.5f;

    // Runtime-injected scene references
    private ValetGuidanceSystem valetSystem;
    private BarrierGate entranceGate;
    private BarrierGate exitGate;
    private CardReader entranceReader;

    private SmartCarNavigator playerCarNavigator;
    private ValetSession playerSession;

    private InternalScreenState lastAppliedState = InternalScreenState.Default;
    private bool isInitialized = false;
    private bool hasLoggedInitializationError = false;
    private bool isSubscribed = false;
    private bool wasBarrierStopped = false;

    private void Awake()
    {
        // Resolve local prefab components
        if (screenController == null)
        {
            screenController = GetComponentInChildren<InternalVehicleScreenController>(true);
        }
        playerCarNavigator = GetComponentInParent<SmartCarNavigator>();
    }

    private void Start()
    {
        // ScreenController defaults to Default screen on Start.
        // We initialize lastAppliedState to Default.
        lastAppliedState = InternalScreenState.Default;
    }

    /// <summary>
    /// Injects scene-level references immediately after instantiation.
    /// </summary>
    public void Initialize(
        ValetGuidanceSystem valetSystem,
        BarrierGate entranceGate,
        BarrierGate exitGate,
        CardReader entranceReader)
    {
        this.valetSystem = valetSystem;
        this.entranceGate = entranceGate;
        this.exitGate = exitGate;
        this.entranceReader = entranceReader;

        // Establish subscriptions if component is enabled
        if (enabled)
        {
            SubscribeEvents();
        }

        isInitialized = true;
        Debug.Log($"[PlayerCarScreenBridge] {gameObject.name}: Initialized successfully.");
    }

    private void OnEnable()
    {
        if (isInitialized)
        {
            SubscribeEvents();
        }
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (isSubscribed) return;

        if (valetSystem != null)
        {
            valetSystem.OnCarArrivedAtDropOff += OnCarArrivedAtDropOff;
        }

        if (entranceReader != null)
        {
            entranceReader.OnCardRead.AddListener(OnCardRead);
        }

        isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed) return;

        if (valetSystem != null)
        {
            valetSystem.OnCarArrivedAtDropOff -= OnCarArrivedAtDropOff;
        }

        if (entranceReader != null)
        {
            entranceReader.OnCardRead.RemoveListener(OnCardRead);
        }

        isSubscribed = false;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            // Failure handling: If dependencies are missing and not initialized, log one error and disable polling.
            if (!hasLoggedInitializationError)
            {
                Debug.LogError($"[PlayerCarScreenBridge] {gameObject.name}: Required scene dependencies are missing! Screen progression disabled.", this);
                hasLoggedInitializationError = true;
            }
            enabled = false;
            return;
        }

        // Cache player session if not already done
        if (playerSession == null && valetSystem != null && playerCarNavigator != null)
        {
            playerSession = valetSystem.activeSessions.Find(s => s.car == playerCarNavigator);
        }

        if (playerSession == null)
        {
            // Return until player session registration completes in the valet system
            return;
        }

        // Check Approach states (Welcome, Goodbye) and passed condition (ResumeControl)
        CheckApproachAndPassedStates();

        // Track barrier stop changes to detect autonomous drive release
        CheckAutonomousDriveRelease();
    }

    private void CheckApproachAndPassedStates()
    {
        if (playerCarNavigator == null) return;

        Vector3 carPos = playerCarNavigator.transform.position;

        // 1. Welcome state
        if (lastAppliedState < InternalScreenState.Welcome && entranceGate != null)
        {
            Vector3 toGate = carPos - entranceGate.transform.position;
            float dist = toGate.magnitude;
            float dot = Vector3.Dot(toGate, entranceGate.transform.forward);

            // Car is within radius and approaching the entrance gate front
            if (dist <= detectionRadius && dot > 0.1f)
            {
                TransitionTo(InternalScreenState.Welcome);
            }
        }

        // 2. Goodbye state
        if (lastAppliedState < InternalScreenState.Goodbye && lastAppliedState >= InternalScreenState.PickUp && exitGate != null)
        {
            Vector3 toGate = carPos - exitGate.transform.position;
            float dist = toGate.magnitude;
            float dot = Vector3.Dot(toGate, exitGate.transform.forward);

            // Car is within radius and approaching the exit gate front
            if (dist <= detectionRadius && dot > 0.1f)
            {
                TransitionTo(InternalScreenState.Goodbye);
            }
        }

        // 3. ResumeControl state
        if (lastAppliedState == InternalScreenState.Goodbye && exitGate != null)
        {
            Vector3 toGate = carPos - exitGate.transform.position;
            float dot = Vector3.Dot(toGate, exitGate.transform.forward);

            // Geometric Passed Check: match the exit gate's passed threshold
            if (dot < -0.5f)
            {
                TransitionTo(InternalScreenState.ResumeControl);
            }
        }

        // 4. PickUp state polling
        if (lastAppliedState < InternalScreenState.PickUp && lastAppliedState >= InternalScreenState.Deployment && playerSession != null)
        {
            if (playerSession.state == ValetState.AtPickUp)
            {
                TransitionTo(InternalScreenState.PickUp);
            }
        }
    }

    private void CheckAutonomousDriveRelease()
    {
        if (playerCarNavigator == null) return;

        bool currentBarrierStopped = playerCarNavigator.IsBarrierStopped;

        // AutonomousDrive state transition
        if (lastAppliedState == InternalScreenState.SelectDestination)
        {
            // Triggered only when the entrance gate releases this player car from barrier stop (IsBarrierStopped changes true -> false)
            if (wasBarrierStopped && !currentBarrierStopped)
            {
                TransitionTo(InternalScreenState.AutonomousDrive);
            }
        }

        wasBarrierStopped = currentBarrierStopped;
    }

    private void OnCardRead(string cardNumber)
    {
        // Enforce that we already reached Welcome before payment can be selected
        if (lastAppliedState != InternalScreenState.Welcome) return;

        // Player-car filtering: Verify the card authorization belongs to this player session
        if (playerSession == null && valetSystem != null && playerCarNavigator != null)
        {
            playerSession = valetSystem.activeSessions.Find(s => s.car == playerCarNavigator);
        }

        if (playerSession != null && playerSession.paymentCardNumber == cardNumber)
        {
            TransitionTo(InternalScreenState.SelectDestination);
        }
    }

    private void OnCarArrivedAtDropOff(SmartCarNavigator car, Transform spot)
    {
        // Player-car filtering: Verify the arrived car is our player car
        if (car == playerCarNavigator)
        {
            if (lastAppliedState < InternalScreenState.Deployment)
            {
                TransitionTo(InternalScreenState.Deployment);
            }
        }
    }

    private void TransitionTo(InternalScreenState newState)
    {
        if (newState <= lastAppliedState)
        {
            // Enforce strictly forward-only transitions
            return;
        }

        lastAppliedState = newState;

        if (screenController != null)
        {
            screenController.SetScreenState(newState);
            Debug.Log($"[PlayerCarScreenBridge] {gameObject.name}: Transitioned screen to state {newState}");
        }
        else
        {
            Debug.LogWarning($"[PlayerCarScreenBridge] {gameObject.name}: Tried to set screen state to {newState} but screenController is null!");
        }
    }
}
