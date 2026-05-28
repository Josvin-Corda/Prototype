using UnityEngine;

namespace AHMI.Safety
{
    public class SafetyAudioAdapter : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;

        public void PlayAlert()
        {
            if (audioSource != null && !audioSource.isPlaying)
                audioSource.Play();
        }

        public void StopAlert()
        {
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
        }
    }
}