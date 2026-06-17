using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AHMI.ETA
{
    public class TotemScreenSequence : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image targetImage;

        [Header("Sprites")]
        [SerializeField] private Sprite homeSprite;
        [SerializeField] private Sprite recallStep1Sprite;
        [SerializeField] private Sprite recallStep2Sprite;
        [SerializeField] private Sprite recallFinalSprite;

        [Header("Delays")]
        [SerializeField] private float delayBeforeStep1 = 0.2f;
        [SerializeField] private float delayBeforeStep2 = 0.8f;
        [SerializeField] private float delayBeforeFinal = 1.0f;

        private Coroutine sequenceCoroutine;

        private void Start()
        {
            ResetToHome();
        }

        public void StartRecallSequence()
        {
            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
                sequenceCoroutine = null;
            }

            sequenceCoroutine = StartCoroutine(RecallSequenceRoutine());
        }

        public void ResetToHome()
        {
            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
                sequenceCoroutine = null;
            }

            if (targetImage != null && homeSprite != null)
            {
                targetImage.sprite = homeSprite;
            }
        }

        private IEnumerator RecallSequenceRoutine()
        {
            // Delay before Step 1
            yield return new WaitForSeconds(delayBeforeStep1);
            if (targetImage != null && recallStep1Sprite != null)
            {
                targetImage.sprite = recallStep1Sprite;
            }

            // Delay before Step 2
            yield return new WaitForSeconds(delayBeforeStep2);
            if (targetImage != null && recallStep2Sprite != null)
            {
                targetImage.sprite = recallStep2Sprite;
            }

            // Delay before Final Step
            yield return new WaitForSeconds(delayBeforeFinal);
            if (targetImage != null && recallFinalSprite != null)
            {
                targetImage.sprite = recallFinalSprite;
            }

            sequenceCoroutine = null;
        }
    }
}
