using UnityEngine;

namespace AHMI.Safety
{
    public class SmoothSpeedAdapter : MonoBehaviour
    {
        [Header("Speed")]
        [SerializeField] private float normalSpeed = 2f;
        [SerializeField] private float brakeRate = 2f;
        [SerializeField] private float accelerationRate = 1.5f;

        public float CurrentSpeed { get; private set; }

        private float targetSpeed;

        private void Awake()
        {
            CurrentSpeed = normalSpeed;
            targetSpeed = normalSpeed;
        }

        private void Update()
        {
            float rate = targetSpeed < CurrentSpeed ? brakeRate : accelerationRate;

            CurrentSpeed = Mathf.MoveTowards(
                CurrentSpeed,
                targetSpeed,
                rate * Time.deltaTime
            );
        }

        public void RequestStop()
        {
            targetSpeed = 0f;
        }

        public void RequestResume()
        {
            targetSpeed = normalSpeed;
        }
    }
}