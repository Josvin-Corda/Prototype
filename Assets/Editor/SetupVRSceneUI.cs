using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SetupVRSceneUI
{
    [MenuItem("Tools/Setup VR Scene UI")]
    public static void SetupScene()
    {
        string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError("[SetupVRSceneUI] Failed to open scene: " + scenePath);
            return;
        }

        // 1. Check if SimulationControlManager already exists in scene, and destroy old one if it does
        GameObject existingManager = GameObject.Find("SimulationControlManager");
        if (existingManager != null)
        {
            Object.DestroyImmediate(existingManager);
            Debug.Log("[SetupVRSceneUI] Destroyed existing SimulationControlManager in scene.");
        }

        // 2. Create the Manager GameObject
        GameObject managerObj = new GameObject("SimulationControlManager");
        VRSimulationController controller = managerObj.AddComponent<VRSimulationController>();

        // 3. Load the Canvas prefab
        string prefabPath = "Assets/PreFabs/VRControlPanelCanvas.prefab";
        GameObject canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (canvasPrefab == null)
        {
            Debug.LogError("[SetupVRSceneUI] Prefab not found at path: " + prefabPath);
            return;
        }

        // 4. Assign references
        controller.uiCanvasPrefab = canvasPrefab;
        
        // Auto-find references and assign them
        controller.spawner = Object.FindAnyObjectByType<NPCCarSpawner>();
        controller.playerProgression = Object.FindAnyObjectByType<PlayerCarProgression>();
        controller.valetSystem = Object.FindAnyObjectByType<ValetGuidanceSystem>();

        // Mark the scene dirty and save it
        EditorUtility.SetDirty(managerObj);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SetupVRSceneUI] Successfully set up SimulationControlManager in " + scenePath + " and saved scene.");
    }
}
