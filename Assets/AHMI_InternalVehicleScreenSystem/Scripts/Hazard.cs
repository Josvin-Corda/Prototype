using System.Collections;
using UnityEngine;

namespace AHMI.InternalVehicleScreen
{
    public class InternalScreenHazardRing : MonoBehaviour
    {
        [Header("Ring Overlay")]
        [SerializeField] private GameObject ringOverlay;

        [Header("Blink Settings")]
        [SerializeField] private bool blinkWhenActive = true;
        [SerializeField] private float blinkInterval = 0.25f;

        [Header("Startup")]
        [SerializeField] private bool startDisabled = true;

        [Header("Debug")]
        [SerializeField] private bool hazardDetected;

        private Coroutine blinkCoroutine;

        private void Start()
        {
            if (startDisabled)
            {
                SetHazardDetected(false);
            }
        }

        public void SetHazardDetected(bool detected)
        {
            hazardDetected = detected;

            if (ringOverlay == null)
            {
                Debug.LogWarning("[InternalScreenHazardRing] Ring Overlay is not assigned.", this);
                return;
            }

            if (!detected)
            {
                StopBlinking();
                ringOverlay.SetActive(false);
                return;
            }

            if (blinkWhenActive)
            {
                StartBlinking();
            }
            else
            {
                StopBlinking();
                ringOverlay.SetActive(true);
            }
        }

        public void ActivateHazard()
        {
            SetHazardDetected(true);
        }

        public void DeactivateHazard()
        {
            SetHazardDetected(false);
        }

        private void StartBlinking()
        {
            StopBlinking();
            blinkCoroutine = StartCoroutine(BlinkRoutine());
        }

        private void StopBlinking()
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
        }

        private IEnumerator BlinkRoutine()
        {
            while (hazardDetected)
            {
                ringOverlay.SetActive(true);
                yield return new WaitForSeconds(blinkInterval);

                ringOverlay.SetActive(false);
                yield return new WaitForSeconds(blinkInterval);
            }

            ringOverlay.SetActive(false);
        }
    }
}