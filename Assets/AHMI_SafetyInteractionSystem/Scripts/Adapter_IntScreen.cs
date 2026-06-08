using UnityEngine;
using TMPro;

namespace AHMI.Safety
{
    public class SafetyScreenAdapter : MonoBehaviour
    {
        [SerializeField] private GameObject waitingPanel;
        [SerializeField] private TMP_Text messageText;

        [TextArea]
        [SerializeField] private string waitingMessage = "Waiting for pedestrian / vehicle interaction";

        [TextArea]
        [SerializeField] private string normalMessage = "Autonomous driving active";

        private void Start()
        {
            if (waitingPanel == null)
            {
                waitingPanel = GameObject.Find("AHMI_InternalScreenSystem/tesla/HUD/HazardOverlay");
                if (waitingPanel == null)
                {
                    waitingPanel = GameObject.Find("HazardOverlay");
                }
            }
        }

        public void ShowWaitingMessage()
        {
            if (waitingPanel != null)
                waitingPanel.SetActive(true);

            if (messageText != null)
                messageText.text = waitingMessage;
        }

        public void HideWaitingMessage()
        {
            if (waitingPanel != null)
                waitingPanel.SetActive(false);

            if (messageText != null)
                messageText.text = normalMessage;
        }
    }
}