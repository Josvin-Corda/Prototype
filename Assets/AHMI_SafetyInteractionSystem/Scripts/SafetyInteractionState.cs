using UnityEngine;
using UnityEngine.Events;

namespace AHMI.Safety
{
    public class SafetyInteractionState : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool isWaiting = false;

        [Header("Events")]
        public UnityEvent OnSafetyWaitStarted;
        public UnityEvent OnSafetyWaitEnded;

        public bool IsWaiting => isWaiting;

        public void StartSafetyWait()
        {
            if (isWaiting)
                return;

            isWaiting = true;
            OnSafetyWaitStarted?.Invoke();
        }

        public void EndSafetyWait()
        {
            if (!isWaiting)
                return;

            isWaiting = false;
            OnSafetyWaitEnded?.Invoke();
        }
    }
}