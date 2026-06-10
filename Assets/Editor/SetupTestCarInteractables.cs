using UnityEditor;
using UnityEngine;

public class SetupTestCarInteractables
{
    [MenuItem("Tools/Setup TestCar Interactables")]
    public static void SetupInteractables()
    {
        string prefabPath = "Assets/PreFabs/TestCar.prefab";
        GameObject prefabObj = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabObj == null)
        {
            Debug.LogError("[SetupTestCarInteractables] Prefab not found at path: " + prefabPath);
            return;
        }

        Transform modeVisuals = prefabObj.transform.Find("ModeVisuals");
        if (modeVisuals == null)
        {
            Debug.LogError("[SetupTestCarInteractables] ModeVisuals not found in prefab root.");
            PrefabUtility.UnloadPrefabContents(prefabObj);
            return;
        }

        Transform vrBtn = modeVisuals.Find("VR_InteractionButton");
        if (vrBtn == null)
        {
            Debug.LogError("[SetupTestCarInteractables] VR_InteractionButton not found under ModeVisuals.");
            PrefabUtility.UnloadPrefabContents(prefabObj);
            return;
        }

        Transform returnBtn = vrBtn.Find("ReturnButton");
        if (returnBtn == null)
        {
            Debug.LogError("[SetupTestCarInteractables] ReturnButton not found under VR_InteractionButton.");
            PrefabUtility.UnloadPrefabContents(prefabObj);
            return;
        }

        // Check if ReturnButton_Passenger already exists and destroy it
        Transform existingPassenger = vrBtn.Find("ReturnButton_Passenger");
        if (existingPassenger != null)
        {
            Object.DestroyImmediate(existingPassenger.gameObject);
            Debug.Log("[SetupTestCarInteractables] Destroyed existing ReturnButton_Passenger.");
        }

        // Duplicate ReturnButton
        GameObject passengerBtnObj = Object.Instantiate(returnBtn.gameObject, vrBtn);
        passengerBtnObj.name = "ReturnButton_Passenger";
        
        // Calculated localPosition and localRotation in VR_InteractionButton space:
        // LocalPos: (0.79, -0.10, 35.81)
        // LocalRot (Euler): (69.55, 223.48, 358.08)
        passengerBtnObj.transform.localPosition = new Vector3(0.79f, -0.10f, 35.81f);
        passengerBtnObj.transform.localRotation = Quaternion.Euler(69.55f, 223.48f, 358.08f);
        
        // Save the prefab contents
        PrefabUtility.SaveAsPrefabAsset(prefabObj, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabObj);

        Debug.Log("[SetupTestCarInteractables] Successfully created ReturnButton_Passenger on passenger door side in TestCar.prefab and saved prefab.");
    }
}
