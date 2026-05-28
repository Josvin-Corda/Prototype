using System.Collections.Generic;
using UnityEngine;

namespace AHMI.Safety
{
    [RequireComponent(typeof(Collider))]
    public class SafetyTriggerDetector : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private LayerMask detectableLayers;

        [Header("State Controller")]
        [SerializeField] private SafetyInteractionState safetyState;

        private readonly HashSet<Collider> detectedObjects = new HashSet<Collider>();

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;

            safetyState = GetComponentInParent<SafetyInteractionState>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsDetectable(other))
                return;

            detectedObjects.Add(other);

            if (safetyState != null)
                safetyState.StartSafetyWait();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsDetectable(other))
                return;

            detectedObjects.Remove(other);

            if (detectedObjects.Count == 0)
            {
                if (safetyState != null)
                    safetyState.EndSafetyWait();
            }
        }

        private bool IsDetectable(Collider other)
        {
            return (detectableLayers.value & (1 << other.gameObject.layer)) != 0;
        }

        public void ClearDetection()
        {
            detectedObjects.Clear();

            if (safetyState != null)
                safetyState.EndSafetyWait();
        }
    }
}