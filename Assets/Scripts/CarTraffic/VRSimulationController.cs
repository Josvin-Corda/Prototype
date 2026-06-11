using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class VRSimulationController : MonoBehaviour
{
    [Header("UI Canvas Asset")]
    [Tooltip("Drag the 'VRControlPanelCanvas' prefab here.")]
    public GameObject uiCanvasPrefab;

    [Header("Input Bindings")]
    [Tooltip("Left controller X button binding path.")]
    public string toggleUIBinding = "<XRController>{LeftHand}/primaryButton";
    
    [Tooltip("Keyboard binding path for testing in Editor.")]
    public string toggleUIKeyboardBinding = "<Keyboard>/u";

    [Header("Card Spawning Settings")]
    [Tooltip("Left controller Y button binding path.")]
    public string spawnCardBinding = "<XRController>{LeftHand}/secondaryButton";
    
    [Tooltip("Keyboard binding path for card spawning testing in Editor.")]
    public string spawnCardKeyboardBinding = "<Keyboard>/c";

    [Header("UI Distance & Layout Settings")]
    [Tooltip("Distance in meters to spawn the UI in front of the player.")]
    public float uiSpawnDistance = 1.5f;
    
    [Tooltip("Vertical offset relative to the player's eye level (negative is lower).")]
    public float uiVerticalOffset = -0.1f;

    [Header("System References")]
    public NPCCarSpawner spawner;
    public PlayerCarProgression playerProgression;
    public ValetGuidanceSystem valetSystem;

    // Runtime instantiated references
    private GameObject canvasInstance;
    private GameObject welcomePanel;
    private GameObject controlPanel;
    private Button enterButton;
    private Button startSimButton;
    private Button stopSimButton;
    private Button spawnCarButton;
    private Button homeButton;
    private TextMeshProUGUI statusText;
    private InputAction toggleUIAction;
    private InputAction spawnCardAction;
    private bool isWelcomeClosed = false;
    private GameObject spawnedCardInstance;

    private void Awake()
    {
        // 1. Configure the input action for toggling UI
        toggleUIAction = new InputAction("ToggleUI");
        toggleUIAction.AddBinding(toggleUIBinding);
        toggleUIAction.AddBinding(toggleUIKeyboardBinding);
        
        toggleUIAction.started += ctx => OnToggleUI();

        // 2. Configure the input action for spawning payment card (Left Controller Y Button / Key C)
        spawnCardAction = new InputAction("SpawnCard");
        spawnCardAction.AddBinding(spawnCardBinding);
        spawnCardAction.AddBinding(spawnCardKeyboardBinding);
        
        spawnCardAction.started += ctx => ToggleSpawnCard();
    }

    private void OnEnable()
    {
        if (toggleUIAction != null) toggleUIAction.Enable();
        if (spawnCardAction != null) spawnCardAction.Enable();
    }

    private void OnDisable()
    {
        if (toggleUIAction != null) toggleUIAction.Disable();
        if (spawnCardAction != null) spawnCardAction.Disable();
    }

    private void Start()
    {
        // Auto-find references if missing
        if (spawner == null) spawner = Object.FindAnyObjectByType<NPCCarSpawner>();
        if (playerProgression == null) playerProgression = Object.FindAnyObjectByType<PlayerCarProgression>();
        if (valetSystem == null) valetSystem = Object.FindAnyObjectByType<ValetGuidanceSystem>();

        // Ensure spawner is paused on start
        if (spawner != null)
        {
            spawner.isSpawningPaused = true;
            Debug.Log("[VRSimulationController] Spawner default-paused on startup.");
        }

        // 2. Set up the UI Canvas
        InitializeUI();

        // 3. Guarantee EventSystem is present with XRUIInputModule for VR UI Interaction
        EnsureEventSystem();
    }

    private void Update()
    {
        // Grey out Spawn Test Car button if a Test Car is already active in the scene
        if (spawnCarButton != null)
        {
            bool testCarExists = GameObject.Find("Player_TestCar") != null;
            spawnCarButton.interactable = !testCarExists;
        }
    }

    private void InitializeUI()
    {
        if (uiCanvasPrefab == null)
        {
            Debug.LogError("[VRSimulationController] uiCanvasPrefab is not assigned! UI cannot be initialized.");
            return;
        }

        // Instantiate canvas in the scene
        canvasInstance = Instantiate(uiCanvasPrefab);
        canvasInstance.name = "VRControlPanelCanvas_Instance";
        
        // Find panel references
        Transform welcomeTrans = canvasInstance.transform.Find("WelcomePanel");
        if (welcomeTrans != null) welcomePanel = welcomeTrans.gameObject;

        Transform controlTrans = canvasInstance.transform.Find("ControlPanel");
        if (controlTrans != null) controlPanel = controlTrans.gameObject;

        // Find button references
        if (welcomePanel != null)
        {
            Transform enterBtnTrans = welcomePanel.transform.Find("EnterButton");
            if (enterBtnTrans != null) enterButton = enterBtnTrans.GetComponent<Button>();
        }

        if (controlPanel != null)
        {
            Transform startBtnTrans = controlPanel.transform.Find("StartSimButton");
            if (startBtnTrans != null) startSimButton = startBtnTrans.GetComponent<Button>();

            Transform stopBtnTrans = controlPanel.transform.Find("StopSimButton");
            if (stopBtnTrans != null) stopSimButton = stopBtnTrans.GetComponent<Button>();

            Transform spawnBtnTrans = controlPanel.transform.Find("SpawnCarButton");
            if (spawnBtnTrans != null) spawnCarButton = spawnBtnTrans.GetComponent<Button>();

            Transform homeBtnTrans = controlPanel.transform.Find("HomeButton");
            if (homeBtnTrans != null) homeButton = homeBtnTrans.GetComponent<Button>();
 
            Transform statusTextTrans = controlPanel.transform.Find("StatusText");
            if (statusTextTrans != null) statusText = statusTextTrans.GetComponent<TextMeshProUGUI>();

        }

        // Bind button actions
        if (enterButton != null)
        {
            enterButton.onClick.AddListener(EnterSimulation);
        }

        if (startSimButton != null)
        {
            startSimButton.onClick.AddListener(StartSimulation);
        }

        if (stopSimButton != null)
        {
            stopSimButton.onClick.AddListener(StopSimulation);
        }

        if (spawnCarButton != null)
        {
            spawnCarButton.onClick.AddListener(SpawnTestCar);
        }

        if (homeButton != null)
        {
            homeButton.onClick.AddListener(ReturnHome);
        }

        // Ensure canvas starts in front of player
        PositionCanvasInFrontOfPlayer();

        // Welcome panel is visible by default, control panel is hidden
        if (welcomePanel != null) welcomePanel.SetActive(true);
        if (controlPanel != null) controlPanel.SetActive(false);
        
        canvasInstance.SetActive(true); // Canvas is open on start (welcome screen)
        isWelcomeClosed = false;
        
        Debug.Log("[VRSimulationController] VR UI Canvas initialized and positioned.");
    }

    private void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
            Debug.Log("[VRSimulationController] EventSystem and XRUIInputModule created dynamically.");
        }
    }

    private void PositionCanvasInFrontOfPlayer()
    {
        if (canvasInstance == null) return;

        Transform camTransform = Camera.main != null ? Camera.main.transform : null;
        if (camTransform != null)
        {
            // Project camera's forward vector horizontally (flat XZ plane)
            Vector3 forward = camTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            // Position canvas in front of player
            Vector3 spawnPos = camTransform.position + forward * uiSpawnDistance;
            spawnPos.y = camTransform.position.y + uiVerticalOffset;
            
            canvasInstance.transform.position = spawnPos;
            
            // Rotate canvas to look back at the camera
            canvasInstance.transform.LookAt(camTransform);
            canvasInstance.transform.Rotate(0f, 180f, 0f);
        }
        else
        {
            // Fallback
            canvasInstance.transform.position = new Vector3(0f, 1.4f, 1.5f);
            canvasInstance.transform.rotation = Quaternion.identity;
        }
    }

    private void OnToggleUI()
    {
        if (canvasInstance == null) return;

        if (!isWelcomeClosed)
        {
            // If welcome screen is open, first press of X closes welcome and hides UI
            isWelcomeClosed = true;
            if (welcomePanel != null) welcomePanel.SetActive(false);
            if (controlPanel != null) controlPanel.SetActive(true);
            canvasInstance.SetActive(false);
            Debug.Log("[VRSimulationController] Welcome panel closed via X button shortcut.");
        }
        else
        {
            // Subsequent presses toggle control panel open/closed
            bool nextState = !canvasInstance.activeSelf;
            canvasInstance.SetActive(nextState);
            
            if (nextState)
            {
                PositionCanvasInFrontOfPlayer();
                Debug.Log("[VRSimulationController] Control panel toggled OPEN.");
            }
            else
            {
                Debug.Log("[VRSimulationController] Control panel toggled CLOSED.");
            }
        }
    }

    // --- UI Button Click Event Handlers ---

    private void EnterSimulation()
    {
        isWelcomeClosed = true;
        if (welcomePanel != null) welcomePanel.SetActive(false);
        if (controlPanel != null) controlPanel.SetActive(true);
        if (canvasInstance != null) canvasInstance.SetActive(false); // Hide Canvas by default after entering
        
        Debug.Log("[VRSimulationController] Entered simulation. UI hidden by default.");
    }

    private void StartSimulation()
    {
        if (spawner != null)
        {
            spawner.isSpawningPaused = false;
            UpdateStatusText("<color=green>Simulation Started. NPC cars are spawning.</color>");
            Debug.Log("[VRSimulationController] Spawner resumed.");
        }
        else
        {
            UpdateStatusText("<color=red>Error: Spawner reference missing.</color>");
        }
    }

    private void StopSimulation()
    {
        if (spawner != null)
        {
            spawner.isSpawningPaused = true;
            Debug.Log("[VRSimulationController] Spawner paused.");
        }

        ClearSimulation();
        UpdateStatusText("<color=red>Simulation Stopped. Cleared all cars and NPCs.</color>");
    }

    private void SpawnTestCar()
    {
        if (playerProgression != null)
        {
            playerProgression.SpawnPlayerCar();
            UpdateStatusText("<color=yellow>Test Car Spawned at entrance.</color>");
            Debug.Log("[VRSimulationController] Test car spawned.");
        }
        else
        {
            UpdateStatusText("<color=red>Error: Player Progression reference missing.</color>");
        }
    }

    private void ReturnHome()
    {
        // 1. If player is inside the Test Car, unparent the XR Origin first
        GameObject xrOriginObj = GameObject.Find("XR Origin (XR Rig)");
        if (xrOriginObj != null)
        {
            if (xrOriginObj.transform.parent != null)
            {
                xrOriginObj.transform.SetParent(null);
            }

            // Sync PlayerCarProgression internal states if inside
            if (playerProgression != null)
            {
                playerProgression.SetPlayerInsideCar(false, triggerEvents: false);
            }

            // Teleport player rig safely
            CharacterController cc = xrOriginObj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false; // Disable to bypass physics collisions during teleport

            xrOriginObj.transform.position = new Vector3(-14.0f, 0.12f, -17.37f);
            xrOriginObj.transform.rotation = Quaternion.identity;

            if (cc != null) cc.enabled = true; // Re-enable CharacterController

            // Stop the TestCar if it is active in the scene
            var testCar = GameObject.Find("Player_TestCar");
            if (testCar != null)
            {
                var navigator = testCar.GetComponent<SmartCarNavigator>();
                if (navigator != null)
                {
                    navigator.IsCrosswalkStopped = true;
                }
            }

            UpdateStatusText("<color=cyan>Teleported back to Entrance Lobby.</color>");
            Debug.Log("[VRSimulationController] Player teleported to home.");
        }
        else
        {
            UpdateStatusText("<color=red>Error: XR Origin not found in scene.</color>");
        }
    }

    private void ClearSimulation()
    {
        // 1. Unparent player first if currently in the car, to prevent player being destroyed
        GameObject xrOriginObj = GameObject.Find("XR Origin (XR Rig)");
        if (xrOriginObj != null && xrOriginObj.transform.parent != null)
        {
            xrOriginObj.transform.SetParent(null);
            if (playerProgression != null)
            {
                playerProgression.SetPlayerInsideCar(false, triggerEvents: false);
            }
            
            // Snap player safely to lobby entrance to avoid falling through geometry
            CharacterController cc = xrOriginObj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            xrOriginObj.transform.position = new Vector3(-14.0f, 0.12f, -17.37f);
            xrOriginObj.transform.rotation = Quaternion.identity;
            if (cc != null) cc.enabled = true;
        }

        // 2. Destroy all spawned vehicles in the scene
        var navigators = Object.FindObjectsByType<SmartCarNavigator>(FindObjectsSortMode.None);
        foreach (var car in navigators)
        {
            Destroy(car.gameObject);
        }

        // 3. Destroy all spawned NPCs in the scene
        var npcs = Object.FindObjectsByType<HumanNPCBehavior>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            Destroy(npc.gameObject);
        }

        // 4. Clear active valet sessions list in the guidance system
        if (valetSystem != null)
        {
            valetSystem.activeSessions.Clear();
            
            valetSystem.UpdateTrafficProviderData();
        }

        // 5. Clear ETA billboard tracked orchestrator reference to prevent rendering stale routes
        var dynamicBridge = Object.FindAnyObjectByType<DynamicRouteBridge>();
        if (dynamicBridge != null)
        {
            dynamicBridge.SetCarETAOrchestrator(null);
        }

        Debug.Log("[VRSimulationController] Simulation cleared completely.");
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void ToggleSpawnCard()
    {
        if (spawnedCardInstance == null)
        {
            // 1. Find Left Hand Controller
            Transform leftHand = null;
            GameObject xrOriginObj = GameObject.Find("XR Origin (XR Rig)");
            if (xrOriginObj != null)
            {
                leftHand = FindChildRecursive(xrOriginObj.transform, "Left Controller");
                if (leftHand == null)
                {
                    leftHand = FindChildRecursive(xrOriginObj.transform, "LeftHand Controller");
                }
                if (leftHand == null)
                {
                    leftHand = FindChildRecursiveContaining(xrOriginObj.transform, "Left");
                }
            }

            if (leftHand == null)
            {
                UpdateStatusText("<color=red>Error: Left Hand Controller not found in XR Rig.</color>");
                Debug.LogError("[VRSimulationController] Left Controller not found under XR Origin.");
                return;
            }

            // 2. Spawn Card GameObject dynamically
            spawnedCardInstance = new GameObject("PaymentCard");
            spawnedCardInstance.transform.SetParent(leftHand, false);

            // Position it in front of the hand
            spawnedCardInstance.transform.localPosition = new Vector3(0f, -0.03f, 0.12f);
            spawnedCardInstance.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);

            // Add PaymentCard component
            PaymentCard cardComp = spawnedCardInstance.AddComponent<PaymentCard>();
            cardComp.cardNumber = "PLAYER-CARD-9999";

            // Add Rigidbody (kinematic so it registers triggers as it moves)
            Rigidbody rb = spawnedCardInstance.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Create visual cube representation of card
            GameObject cardVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cardVisual.name = "Visual";
            cardVisual.transform.SetParent(spawnedCardInstance.transform, false);
            cardVisual.transform.localScale = new Vector3(0.12f, 0.08f, 0.005f); // 12cm x 8cm x 0.5cm (visible in VR)

            // Ensure the visual collider is set to trigger
            BoxCollider col = cardVisual.GetComponent<BoxCollider>();
            if (col != null) col.isTrigger = true;

            // Create material
            Shader shader = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null 
                ? Shader.Find("Standard") 
                : Shader.Find("Universal Render Pipeline/Lit");
            Material cardMat = new Material(shader);
            cardMat.color = new Color(0f, 0.6f, 0.9f, 0.9f); // Sleek tech blue/cyan
            cardVisual.GetComponent<Renderer>().sharedMaterial = cardMat;

            UpdateStatusText("<color=cyan>[Y Button] Payment Card attached to Left Hand.</color>");
            Debug.Log("[VRSimulationController] Spawned Payment Card on Left Hand via Left Controller Y button.");
        }
        else
        {
            // Despawn Card
            Destroy(spawnedCardInstance);
            spawnedCardInstance = null;

            UpdateStatusText("<color=yellow>[Y Button] Payment Card despawned.</color>");
            Debug.Log("[VRSimulationController] Despawned Payment Card via Left Controller Y button.");
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildRecursive(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    private Transform FindChildRecursiveContaining(Transform parent, string substring)
    {
        if (parent.name.Contains(substring)) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildRecursiveContaining(parent.GetChild(i), substring);
            if (result != null) return result;
        }
        return null;
    }
}
