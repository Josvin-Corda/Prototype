using UnityEngine;
using UnityEngine.AI;

namespace AHMI.Safety
{
    public class NavMeshSafetyBrakeAdapter : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private float normalSpeed = 2f;
        [SerializeField] private float brakeRate = 2f;
        [SerializeField] private float accelerationRate = 1.5f;

        private float targetSpeed;

        private void Awake()
        {
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();

            targetSpeed = normalSpeed;

            if (agent != null)
                agent.speed = normalSpeed;
        }

        private void Update()
        {
            if (agent == null)
                return;

            float rate = targetSpeed < agent.speed ? brakeRate : accelerationRate;

            agent.speed = Mathf.MoveTowards(
                agent.speed,
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