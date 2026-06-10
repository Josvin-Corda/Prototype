using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class CreateVRCanvasPrefab
{
    [MenuItem("Tools/Create VR Canvas Prefab")]
    public static void CreatePrefab()
    {
        // 1. Create canvas root
        GameObject canvasObj = new GameObject("VRControlPanelCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1000, 800);
        canvasObj.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
        
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Add TrackedDeviceGraphicRaycaster for VR raycasting
        canvasObj.AddComponent<TrackedDeviceGraphicRaycaster>();
        
        // 2. Add a panel background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.09f, 0.13f, 0.96f); // Premium dark slate/blue background
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        // Add a nice top accent line
        GameObject topBar = new GameObject("TopAccent");
        topBar.transform.SetParent(bgObj.transform, false);
        Image topBarImg = topBar.AddComponent<Image>();
        topBarImg.color = Color.cyan;
        RectTransform topBarRect = topBar.GetComponent<RectTransform>();
        topBarRect.anchorMin = new Vector2(0f, 0.98f);
        topBarRect.anchorMax = new Vector2(1f, 1f);
        topBarRect.sizeDelta = Vector2.zero;
        
        // 3. Create Welcome Panel
        GameObject welcomePanelObj = new GameObject("WelcomePanel");
        welcomePanelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform welcomeRect = welcomePanelObj.AddComponent<RectTransform>();
        welcomeRect.anchorMin = Vector2.zero;
        welcomeRect.anchorMax = Vector2.one;
        welcomeRect.sizeDelta = Vector2.zero;
        
        // Welcome Title
        GameObject welcomeTitleObj = new GameObject("WelcomeTitle");
        welcomeTitleObj.transform.SetParent(welcomePanelObj.transform, false);
        TextMeshProUGUI welcomeTitle = welcomeTitleObj.AddComponent<TextMeshProUGUI>();
        welcomeTitle.text = "WELCOME TO PARKING GARAGE SIMULATION";
        welcomeTitle.alignment = TextAlignmentOptions.Center;
        welcomeTitle.fontSize = 38;
        welcomeTitle.fontStyle = FontStyles.Bold;
        welcomeTitle.color = Color.cyan;
        RectTransform welcomeTitleRect = welcomeTitleObj.GetComponent<RectTransform>();
        welcomeTitleRect.anchorMin = new Vector2(0f, 0.7f);
        welcomeTitleRect.anchorMax = new Vector2(1f, 0.9f);
        welcomeTitleRect.sizeDelta = Vector2.zero;
        
        // Welcome Message Body
        GameObject welcomeBodyObj = new GameObject("WelcomeBody");
        welcomeBodyObj.transform.SetParent(welcomePanelObj.transform, false);
        TextMeshProUGUI welcomeBody = welcomeBodyObj.AddComponent<TextMeshProUGUI>();
        welcomeBody.text = "This VR simulation demonstrates an automated valet parking and safety interaction system.\n\n<b>Instructions:</b>\n• Press the <b>'X'</b> button on your Left Quest Controller to open or close this control panel at any time.\n• Point and click to select options in the simulation.";
        welcomeBody.alignment = TextAlignmentOptions.Center;
        welcomeBody.fontSize = 24;
        welcomeBody.color = Color.white;
        welcomeBody.richText = true;
        RectTransform welcomeBodyRect = welcomeBodyObj.GetComponent<RectTransform>();
        welcomeBodyRect.anchorMin = new Vector2(0.08f, 0.32f);
        welcomeBodyRect.anchorMax = new Vector2(0.92f, 0.65f);
        welcomeBodyRect.sizeDelta = Vector2.zero;
        
        // Enter Button
        GameObject enterButtonObj = CreateUIButton(welcomePanelObj, "EnterButton", "Enter Simulation", new Vector2(0.35f, 0.12f), new Vector2(0.65f, 0.24f), new Color(0.18f, 0.76f, 0.38f));
        
        // 4. Create Control Panel
        GameObject controlPanelObj = new GameObject("ControlPanel");
        controlPanelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform controlRect = controlPanelObj.AddComponent<RectTransform>();
        controlRect.anchorMin = Vector2.zero;
        controlRect.anchorMax = Vector2.one;
        controlRect.sizeDelta = Vector2.zero;
        controlPanelObj.SetActive(false); // Inactive on start
        
        // Control Title
        GameObject controlTitleObj = new GameObject("ControlTitle");
        controlTitleObj.transform.SetParent(controlPanelObj.transform, false);
        TextMeshProUGUI controlTitle = controlTitleObj.AddComponent<TextMeshProUGUI>();
        controlTitle.text = "SIMULATION CONTROL PANEL";
        controlTitle.alignment = TextAlignmentOptions.Center;
        controlTitle.fontSize = 38;
        controlTitle.fontStyle = FontStyles.Bold;
        controlTitle.color = Color.cyan;
        RectTransform controlTitleRect = controlTitleObj.GetComponent<RectTransform>();
        controlTitleRect.anchorMin = new Vector2(0f, 0.8f);
        controlTitleRect.anchorMax = new Vector2(1f, 0.95f);
        controlTitleRect.sizeDelta = Vector2.zero;
        
        // 4 Buttons inside Control Panel
        GameObject startBtn = CreateUIButton(controlPanelObj, "StartSimButton", "Start Simulation", new Vector2(0.1f, 0.53f), new Vector2(0.48f, 0.71f), new Color(0.18f, 0.55f, 0.85f));
        GameObject stopBtn = CreateUIButton(controlPanelObj, "StopSimButton", "Stop Simulation", new Vector2(0.52f, 0.53f), new Vector2(0.9f, 0.71f), new Color(0.85f, 0.24f, 0.24f));
        GameObject spawnBtn = CreateUIButton(controlPanelObj, "SpawnCarButton", "Spawn Test Car", new Vector2(0.1f, 0.3f), new Vector2(0.48f, 0.48f), new Color(0.95f, 0.6f, 0.1f));
        GameObject homeBtn = CreateUIButton(controlPanelObj, "HomeButton", "Home Teleport", new Vector2(0.52f, 0.3f), new Vector2(0.9f, 0.48f), new Color(0.45f, 0.47f, 0.5f));
        
        // Confirmation/Status Text
        GameObject statusTextObj = new GameObject("StatusText");
        statusTextObj.transform.SetParent(controlPanelObj.transform, false);
        TextMeshProUGUI statusText = statusTextObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "System Ready. Spawning is Paused.";
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.fontSize = 22;
        statusText.fontStyle = FontStyles.Italic;
        statusText.color = Color.yellow;
        RectTransform statusTextRect = statusTextObj.GetComponent<RectTransform>();
        statusTextRect.anchorMin = new Vector2(0.05f, 0.08f);
        statusTextRect.anchorMax = new Vector2(0.95f, 0.23f);
        statusTextRect.sizeDelta = Vector2.zero;
        
        // 5. Save Prefab
        string folderPath = "Assets/PreFabs";
        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }
        string prefabPath = "Assets/PreFabs/VRControlPanelCanvas.prefab";
        PrefabUtility.SaveAsPrefabAsset(canvasObj, prefabPath);
        Object.DestroyImmediate(canvasObj);
        
        Debug.Log("[CreateVRCanvasPrefab] Successfully created and saved VR UI Canvas Prefab: " + prefabPath);
    }
    
    private static GameObject CreateUIButton(GameObject parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Color normalColor)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent.transform, false);
        
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;
        
        Image img = btnObj.AddComponent<Image>();
        img.color = normalColor;
        
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        
        // Change colors for transitions
        ColorBlock colors = btn.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = normalColor * 1.15f;
        colors.pressedColor = normalColor * 0.85f;
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);
        colors.colorMultiplier = 1f;
        btn.colors = colors;
        
        // Add Text child
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI tmpText = txtObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.fontSize = 24;
        tmpText.fontStyle = FontStyles.Bold;
        tmpText.color = Color.white;
        
        return btnObj;
    }
}
