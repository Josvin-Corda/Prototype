using System.Collections;
using UnityEngine;

namespace AHMI.InternalVehicleScreen
{
    public enum InternalScreenState
    {
        Default,
        Welcome,
        SelectDestination,
        AutonomousDrive,
        Deployment,
        PickUp,
        Goodbye,
        ResumeControl
    }

    public class InternalVehicleScreenController : MonoBehaviour
    {
        [Header("Image Overlay Renderer")]
        [SerializeField] private Renderer overlayRenderer;

        [Tooltip("URP usually uses _BaseMap. Built-in Standard usually uses _MainTex.")]
        [SerializeField] private string texturePropertyName = "_BaseMap";

        [Header("Screen Images")]
        [SerializeField] private Texture2D defaultImage;
        [SerializeField] private Texture2D welcomeImage;
        [SerializeField] private Texture2D selectDestinationImage;
        [SerializeField] private Texture2D autonomousDriveImage;
        [SerializeField] private Texture2D deploymentImage;
        [SerializeField] private Texture2D pickUpImage;
        [SerializeField] private Texture2D goodbyeImage;
        [SerializeField] private Texture2D resumeControlImage;

        [Header("Startup")]
        [SerializeField] private bool showDefaultOnStart = true;

        [Header("Gate Sequence Timing")]
        [SerializeField] private float welcomeDuration = 1.0f;
        [SerializeField] private float acceptEntryDuration = 1.0f;

        [Header("Debug")]
        [SerializeField] private InternalScreenState currentState;

        private Material runtimeOverlayMaterial;
        private Coroutine activeSequence;

        private void Awake()
        {
            if (overlayRenderer == null)
            {
                Debug.LogError("[InternalVehicleScreenController] Overlay Renderer is not assigned.", this);
                return;
            }

            runtimeOverlayMaterial = overlayRenderer.material;
        }

        private void Start()
        {
            if (showDefaultOnStart)
            {
                ShowDefault();
            }
        }

        public void ShowDefault()
        {
            SetScreenState(InternalScreenState.Default);
        }

        public void ShowWelcome()
        {
            SetScreenState(InternalScreenState.Welcome);
        }

        public void ShowSelectDestination()
        {
            SetScreenState(InternalScreenState.SelectDestination);
        }

        public void ShowAutonomousDrive()
        {
            SetScreenState(InternalScreenState.AutonomousDrive);
        }

        public void ShowDeployment()
        {
            SetScreenState(InternalScreenState.Deployment);
        }

        public void ShowPickUp()
        {
            SetScreenState(InternalScreenState.PickUp);
        }

        public void ShowGoodbye()
        {
            SetScreenState(InternalScreenState.Goodbye);
        }

        public void ShowResumeControl()
        {
            SetScreenState(InternalScreenState.ResumeControl);
        }

        // Backward compatibility methods for unmodified trigger scripts
        public void ShowAcceptEntry()
        {
            ShowSelectDestination();
        }

        public void ShowRouteInfo()
        {
            ShowAutonomousDrive();
        }

        public void SetScreenState(InternalScreenState state)
        {
            currentState = state;

            Texture2D selectedImage = GetImageForState(state);

            if (selectedImage == null)
            {
                Debug.LogWarning($"[InternalVehicleScreenController] No image assigned for state: {state}", this);
                return;
            }

            ApplyImage(selectedImage);
        }

        public void PlayGateSequence()
        {
            if (activeSequence != null)
            {
                StopCoroutine(activeSequence);
            }

            activeSequence = StartCoroutine(GateSequenceRoutine());
        }

        private IEnumerator GateSequenceRoutine()
        {
            SetScreenState(InternalScreenState.Welcome);
            yield return new WaitForSeconds(welcomeDuration);

            SetScreenState(InternalScreenState.SelectDestination);
            yield return new WaitForSeconds(acceptEntryDuration);

            SetScreenState(InternalScreenState.AutonomousDrive);

            activeSequence = null;
        }

        private Texture2D GetImageForState(InternalScreenState state)
        {
            switch (state)
            {
                case InternalScreenState.Default:
                    return defaultImage;

                case InternalScreenState.Welcome:
                    return welcomeImage;

                case InternalScreenState.SelectDestination:
                    return selectDestinationImage;

                case InternalScreenState.AutonomousDrive:
                    return autonomousDriveImage;

                case InternalScreenState.Deployment:
                    return deploymentImage;

                case InternalScreenState.PickUp:
                    return pickUpImage;

                case InternalScreenState.Goodbye:
                    return goodbyeImage;

                case InternalScreenState.ResumeControl:
                    return resumeControlImage;

                default:
                    return defaultImage;
            }
        }

        private void ApplyImage(Texture2D image)
        {
            if (runtimeOverlayMaterial == null)
            {
                Debug.LogError("[InternalVehicleScreenController] Runtime overlay material is missing.", this);
                return;
            }

            if (runtimeOverlayMaterial.HasProperty(texturePropertyName))
            {
                runtimeOverlayMaterial.SetTexture(texturePropertyName, image);
            }
            else if (runtimeOverlayMaterial.HasProperty("_MainTex"))
            {
                runtimeOverlayMaterial.SetTexture("_MainTex", image);
            }
            else
            {
                Debug.LogWarning(
                    $"[InternalVehicleScreenController] Overlay material has neither '{texturePropertyName}' nor '_MainTex'. Image not applied.",
                    this
                );
            }
        }
    }
}