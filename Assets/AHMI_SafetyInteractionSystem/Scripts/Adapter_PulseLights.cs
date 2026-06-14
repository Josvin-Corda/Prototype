using UnityEngine;

namespace AHMI.Safety
{
    public class BlinkingLightAdapter : MonoBehaviour
    {
        [SerializeField] private Light[] lights;
        [SerializeField] private float blinkSpeed = 3f;
        [SerializeField] private float minIntensity = 0.1f;
        [SerializeField] private float maxIntensity = 3f;

        private bool active;

        public void Initialize(Light[] lightsToPulse)
        {
            lights = lightsToPulse ?? System.Array.Empty<Light>();
        }

        private void Awake()
        {
            StopBlinking();
        }

        private void Update()
        {
            if (!active || lights == null)
                return;

            float pulse = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);

            foreach (Light light in lights)
            {
                if (light != null)
                    light.intensity = intensity;
            }
        }

        public void StartBlinking()
        {
            active = true;

            if (lights == null) return;

            foreach (Light light in lights)
            {
                if (light != null)
                    light.enabled = true;
            }
        }

        public void StopBlinking()
        {
            active = false;

            if (lights == null) return;

            foreach (Light light in lights)
            {
                if (light != null)
                {
                    light.intensity = 0f;
                    light.enabled = false;
                }
            }
        }
    }
}