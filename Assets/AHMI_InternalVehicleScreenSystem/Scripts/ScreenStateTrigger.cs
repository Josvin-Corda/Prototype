using UnityEngine;

namespace AHMI.InternalVehicleScreen
{
    public enum InternalScreenTriggerAction
    {
        ShowDefault,
        ShowWelcome,
        ShowAcceptEntry,
        ShowRouteInfo,
        PlayGateSequence
    }

    public class InternalScreenStateTrigger : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private InternalVehicleScreenController screenController;

        [Header("Trigger Action")]
        [SerializeField] private InternalScreenTriggerAction action = InternalScreenTriggerAction.PlayGateSequence;

        [Header("Object Filter")]
        [Tooltip("Optional. If assigned, only this object or its children can activate the trigger.")]
        [SerializeField] private GameObject requiredRootObject;

        [Header("Behaviour")]
        [SerializeField] private bool triggerOnlyOnce = true;

        [Header("Debug")]
        [SerializeField] private bool hasTriggered;

        private void Reset()
        {
            Collider col = GetComponent<Collider>();

            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered && triggerOnlyOnce)
            {
                return;
            }

            if (!IsValidTriggerObject(other))
            {
                return;
            }

            if (screenController == null)
            {
                Debug.LogError("[InternalScreenStateTrigger] Screen Controller is not assigned.", this);
                return;
            }

            ExecuteAction();

            hasTriggered = true;
        }

        private bool IsValidTriggerObject(Collider other)
        {
            if (requiredRootObject == null)
            {
                return true;
            }

            Transform current = other.transform;

            while (current != null)
            {
                if (current.gameObject == requiredRootObject)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private void ExecuteAction()
        {
            switch (action)
            {
                case InternalScreenTriggerAction.ShowDefault:
                    screenController.ShowDefault();
                    break;

                case InternalScreenTriggerAction.ShowWelcome:
                    screenController.ShowWelcome();
                    break;

                case InternalScreenTriggerAction.ShowAcceptEntry:
                    screenController.ShowAcceptEntry();
                    break;

                case InternalScreenTriggerAction.ShowRouteInfo:
                    screenController.ShowRouteInfo();
                    break;

                case InternalScreenTriggerAction.PlayGateSequence:
                    screenController.PlayGateSequence();
                    break;
            }
        }

        public void ResetTrigger()
        {
            hasTriggered = false;
        }
    }
}