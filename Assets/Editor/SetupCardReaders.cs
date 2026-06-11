using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to find the ticket machine at the entrance gate and the totems in the lobby,
/// and automatically instantiate and configure CardReader trigger zones.
/// </summary>
public class SetupCardReaders
{
    [MenuItem("Tools/Setup Card Readers")]
    public static void Setup()
    {
        // 1. Setup Entrance Reader
        var gates = Object.FindObjectsByType<BarrierGate>(FindObjectsSortMode.None);
        BarrierGate entranceGate = null;
        foreach (var gate in gates)
        {
            var exitField = gate.GetType().GetField("isExitGate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (exitField != null)
            {
                bool isExit = (bool)exitField.GetValue(gate);
                if (!isExit)
                {
                    entranceGate = gate;
                    break;
                }
            }
        }

        if (entranceGate != null)
        {
            // Find all GameObjects in the scene containing "machine_ticket"
            var allSceneObjs = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            GameObject closestTicketMachine = null;
            float minDistance = float.MaxValue;
            foreach (var obj in allSceneObjs)
            {
                if (obj.name.Contains("machine_ticket"))
                {
                    float dist = Vector3.Distance(entranceGate.transform.position, obj.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestTicketMachine = obj;
                    }
                }
            }

            if (closestTicketMachine != null)
            {
                Transform ticketMachine = closestTicketMachine.transform;
                Transform readerTrans = ticketMachine.Find("EntranceCardReader");
                GameObject readerObj;
                if (readerTrans == null)
                {
                    readerObj = new GameObject("EntranceCardReader");
                    readerObj.transform.SetParent(ticketMachine, false);
                    // Position it slightly in front of the machine where ticket slot is
                    readerObj.transform.localPosition = new Vector3(0f, 1.1f, -0.4f);
                }
                else
                {
                    readerObj = readerTrans.gameObject;
                }

                var readerComp = readerObj.GetComponent<CardReader>();
                if (readerComp == null) readerComp = readerObj.AddComponent<CardReader>();
                readerComp.readerID = "EntranceGateReader";

                var collider = readerObj.GetComponent<BoxCollider>();
                if (collider == null) collider = readerObj.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(0.5f, 0.5f, 0.5f);
                readerComp.detectionTrigger = collider;

                // Wire up the gate reference
                var gateReaderField = entranceGate.GetType().GetField("entranceReader", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (gateReaderField != null)
                {
                    gateReaderField.SetValue(entranceGate, readerComp);
                }

                Debug.Log($"[SetupCardReaders] Successfully configured Entrance Gate Reader on {closestTicketMachine.name} (dist: {minDistance:F2}m).");
            }
            else
            {
                Debug.LogError("[SetupCardReaders] Could not find any GameObjects named 'machine_ticket' in the scene.");
            }
        }
        else
        {
            Debug.LogError("[SetupCardReaders] Entrance Gate not found in scene!");
        }

        // 2. Setup Totem Reader
        var allObjs = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int totemCount = 0;
        foreach (var obj in allObjs)
        {
            if (obj == null) continue;
            if (obj.name == "Totem" || obj.name == "Totem1" || obj.name == "Totem2" || obj.GetComponent<TotemCarRequest>() != null)
            {
                Transform readerTrans = obj.transform.Find("TotemCardReader");
                GameObject readerObj;
                if (readerTrans == null)
                {
                    readerObj = new GameObject("TotemCardReader");
                    readerObj.transform.SetParent(obj.transform, false);
                    // Position it on the front panel
                    readerObj.transform.localPosition = new Vector3(0f, 1.2f, -0.25f);
                }
                else
                {
                    readerObj = readerTrans.gameObject;
                }

                var readerComp = readerObj.GetComponent<CardReader>();
                if (readerComp == null) readerComp = readerObj.AddComponent<CardReader>();
                readerComp.readerID = "TotemReader_" + obj.name;

                var collider = readerObj.GetComponent<BoxCollider>();
                if (collider == null) collider = readerObj.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(0.4f, 0.4f, 0.4f);
                readerComp.detectionTrigger = collider;

                // Wire card reader event to recall the car
                readerComp.OnCardRead.RemoveAllListeners();
                var handler = readerObj.GetComponent<TotemRecallHandler>();
                if (handler == null) handler = readerObj.AddComponent<TotemRecallHandler>();

                totemCount++;
                Debug.Log($"[SetupCardReaders] Configured Totem Reader on {obj.name}.");
            }
        }

        if (totemCount == 0)
        {
            Debug.LogWarning("[SetupCardReaders] No Totems found in the scene.");
        }
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[SetupCardReaders] Scene marked dirty and readers successfully configured.");
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildRecursive(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}
