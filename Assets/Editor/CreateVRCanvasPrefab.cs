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
        // 1. Programmatically generate textures
        GenerateDashboardBG();
        GenerateButtonBG();
        
        AssetDatabase.Refresh();
        
        // 2. Configure import settings for the generated textures
        ConfigureTextureImport("Assets/PreFabs/DashboardBG.png", Vector4.zero);
        ConfigureTextureImport("Assets/PreFabs/ButtonBG.png", new Vector4(32f, 32f, 32f, 32f));
        
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PreFabs/DashboardBG.png");
        Sprite btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PreFabs/ButtonBG.png");

        if (bgSprite == null || btnSprite == null)
        {
            Debug.LogError("[CreateVRCanvasPrefab] Failed to load programmatically generated sprites!");
            return;
        }

        // 3. Create canvas root
        GameObject canvasObj = new GameObject("VRControlPanelCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1000, 800);
        canvasObj.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
        
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        canvasObj.AddComponent<TrackedDeviceGraphicRaycaster>();
        
        // 4. Background Image
        GameObject bgObj = CreateUIElement("Background", canvasObj, Vector2.zero, Vector2.one);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = bgSprite;
        bgImg.type = Image.Type.Simple;
        bgImg.color = Color.white; // Keeps original colors of generated texture
        
        // 5. Create Welcome Panel
        GameObject welcomePanelObj = CreateUIElement("WelcomePanel", canvasObj, Vector2.zero, Vector2.one);
        
        CreateUIText(welcomePanelObj, "WelcomeTitle", "WELCOME TO AUTOMATED VALET SIMULATION", 
            new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.88f), 32, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            
        CreateUIText(welcomePanelObj, "WelcomeSubtitle", "SAFETY INTERACTION SYSTEM ONLINE", 
            new Vector2(0.1f, 0.67f), new Vector2(0.9f, 0.72f), 16, Color.cyan, TextAlignmentOptions.Center, FontStyles.Normal);

        CreateUIText(welcomePanelObj, "WelcomeBody", 
            "This simulation demonstrates an automated valet parking guidance system\nintegrated with active pedestrian crosswalk safety sensors.\n\n<b>Instructions:</b>\n• Press <b>'X'</b> on your Left Quest Controller to open/close this panel.\n• Point and click using your controller pointer ray to select options.", 
            new Vector2(0.1f, 0.32f), new Vector2(0.9f, 0.60f), 22, new Color(0.85f, 0.9f, 0.95f), TextAlignmentOptions.Center, FontStyles.Normal);
        
        CreateUIButton(welcomePanelObj, "EnterButton", "ENTER SYSTEM", new Vector2(0.38f, 0.16f), new Vector2(0.62f, 0.26f), new Color(0f, 0.85f, 1f, 0.85f), btnSprite);
        
        // 6. Create Control Panel
        GameObject controlPanelObj = CreateUIElement("ControlPanel", canvasObj, Vector2.zero, Vector2.one);
        controlPanelObj.SetActive(false); // Inactive on start
        
        // Titles
        CreateUIText(controlPanelObj, "ControlTitle", "VALET PROGRESSION HUB", 
            new Vector2(0.2f, 0.84f), new Vector2(0.8f, 0.94f), 32, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            
        CreateUIText(controlPanelObj, "ControlSubtitle", "SIMULATION ONLINE", 
            new Vector2(0.2f, 0.79f), new Vector2(0.8f, 0.84f), 15, Color.cyan, TextAlignmentOptions.Center, FontStyles.Normal);
            
        // Left Column (Gears)
        GameObject leftGears = CreateUIElement("LeftGears", controlPanelObj, new Vector2(0.04f, 0.3f), new Vector2(0.09f, 0.7f));
        CreateUIText(leftGears, "GearD", "D", new Vector2(0f, 0.75f), new Vector2(1f, 0.95f), 36, new Color(0.25f, 0.25f, 0.25f, 0.5f), TextAlignmentOptions.Center, FontStyles.Bold);
        CreateUIText(leftGears, "GearN", "N", new Vector2(0f, 0.5f), new Vector2(1f, 0.7f), 36, new Color(0.25f, 0.25f, 0.25f, 0.5f), TextAlignmentOptions.Center, FontStyles.Bold);
        CreateUIText(leftGears, "GearR", "R", new Vector2(0f, 0.25f), new Vector2(1f, 0.45f), 36, new Color(0.25f, 0.25f, 0.25f, 0.5f), TextAlignmentOptions.Center, FontStyles.Bold);
        CreateUIText(leftGears, "GearP", "P", new Vector2(0f, 0f), new Vector2(1f, 0.2f), 36, Color.cyan, TextAlignmentOptions.Center, FontStyles.Bold); // P lit on start

        // Right Column (Status)
        GameObject rightStatus = CreateUIElement("RightStatus", controlPanelObj, new Vector2(0.91f, 0.3f), new Vector2(0.96f, 0.7f));
        CreateUIText(rightStatus, "BrakeText", "(P)", new Vector2(0f, 0.75f), new Vector2(1f, 0.95f), 28, Color.red, TextAlignmentOptions.Center, FontStyles.Bold);
        CreateUIText(rightStatus, "BatteryText", "🔋", new Vector2(0f, 0.45f), new Vector2(1f, 0.65f), 28, Color.cyan, TextAlignmentOptions.Center, FontStyles.Normal);
        CreateUIText(rightStatus, "TempText", "🌡", new Vector2(0f, 0.15f), new Vector2(1f, 0.35f), 28, Color.cyan, TextAlignmentOptions.Center, FontStyles.Normal);

        // Left Metric Panel
        GameObject leftMetric = CreateUIElement("LeftMetricPanel", controlPanelObj, new Vector2(0.12f, 0.42f), new Vector2(0.33f, 0.72f));
        CreateUIText(leftMetric, "MetricTitle", "ACTIVE", new Vector2(0f, 0.78f), new Vector2(1f, 0.98f), 20, Color.cyan, TextAlignmentOptions.Center, FontStyles.Bold);
        CreateUIText(leftMetric, "ActiveCarsCountText", "00", new Vector2(0f, 0.2f), new Vector2(1f, 0.78f), 76, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        CreateUIText(leftMetric, "MetricSubtext", "NPC VEHICLES", new Vector2(0f, 0f), new Vector2(1f, 0.2f), 14, new Color(0.7f, 0.8f, 0.9f), TextAlignmentOptions.Center, FontStyles.Normal);

        // Right Metric Panel
        GameObject rightMetric = CreateUIElement("RightMetricPanel", controlPanelObj, new Vector2(0.67f, 0.42f), new Vector2(0.88f, 0.72f));
        CreateUIText(rightMetric, "MetricTitle", "PARKED", new Vector2(0f, 0.78f), new Vector2(1f, 0.98f), 20, Color.cyan, TextAlignmentOptions.Center, FontStyles.Bold);
        CreateUIText(rightMetric, "ParkedCarsCountText", "00", new Vector2(0f, 0.2f), new Vector2(1f, 0.78f), 76, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        CreateUIText(rightMetric, "MetricSubtext", "VALET SLOTS", new Vector2(0f, 0f), new Vector2(1f, 0.2f), 14, new Color(0.7f, 0.8f, 0.9f), TextAlignmentOptions.Center, FontStyles.Normal);

        // Center Visualizer Panel
        GameObject centerVisual = CreateUIElement("CenterVisualizer", controlPanelObj, new Vector2(0.36f, 0.30f), new Vector2(0.64f, 0.74f));
        
        GameObject roadBG = CreateUIElement("RoadBG", centerVisual, Vector2.zero, Vector2.one);
        Image roadBGImg = roadBG.AddComponent<Image>();
        roadBGImg.color = new Color(0.06f, 0.08f, 0.12f, 0.75f);
        
        GameObject roadStrip = CreateUIElement("RoadStrip", roadBG, new Vector2(0.1f, 0f), new Vector2(0.9f, 1f));
        Image roadStripImg = roadStrip.AddComponent<Image>();
        roadStripImg.color = new Color(0.02f, 0.03f, 0.05f, 0.9f);
        
        // Lane divider
        GameObject divider = CreateUIElement("LaneDivider", roadStrip, new Vector2(0.48f, 0f), new Vector2(0.52f, 1f));
        Image divImg = divider.AddComponent<Image>();
        divImg.color = new Color(1f, 1f, 1f, 0.15f);

        // Crosswalk stripes
        GameObject crosswalk = CreateUIElement("Crosswalk", roadStrip, new Vector2(0f, 0.52f), new Vector2(1f, 0.62f));
        for (int i = 0; i < 5; i++)
        {
            float xMin = 0.05f + i * 0.19f;
            GameObject stripe = CreateUIElement("Stripe_" + i, crosswalk, new Vector2(xMin, 0f), new Vector2(xMin + 0.12f, 1f));
            stripe.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.4f);
        }

        // Tapering sensor cone (simple projection trapezoid/rectangle)
        GameObject sensorCone = CreateUIElement("SensorCone", roadStrip, new Vector2(0.25f, 0.35f), new Vector2(0.75f, 0.52f));
        Image coneImg = sensorCone.AddComponent<Image>();
        coneImg.color = new Color(0f, 0.9f, 1f, 0.15f);
        
        // Pedestrian silhouette text indicator
        CreateUIText(roadStrip, "PedestrianSilhouette", "🚶", new Vector2(0.35f, 0.58f), new Vector2(0.65f, 0.78f), 30, Color.cyan);

        // Car Outline Panel
        GameObject carOutline = CreateUIElement("CarOutline", roadStrip, new Vector2(0.34f, 0.10f), new Vector2(0.66f, 0.35f));
        Image carImg = carOutline.AddComponent<Image>();
        carImg.color = new Color(0.55f, 0.6f, 0.68f, 0.9f);
        
        // Add windshield to car
        GameObject windshield = CreateUIElement("Windshield", carOutline, new Vector2(0.15f, 0.62f), new Vector2(0.85f, 0.78f));
        windshield.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.2f, 0.95f);
        
        // Add headlights to car
        GameObject headlightL = CreateUIElement("HeadlightL", carOutline, new Vector2(0.1f, 0.92f), new Vector2(0.28f, 1f));
        headlightL.AddComponent<Image>().color = Color.cyan;
        GameObject headlightR = CreateUIElement("HeadlightR", carOutline, new Vector2(0.72f, 0.92f), new Vector2(0.9f, 1f));
        headlightR.AddComponent<Image>().color = Color.cyan;

        // Visualizer Overlay Text
        CreateUIText(centerVisual, "OverlayText", "SAFE TO EXIT VEHICLE", 
            new Vector2(0f, 0.82f), new Vector2(1f, 0.95f), 18, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);

        // Aligned Minimalistic Row of Buttons
        CreateUIButton(controlPanelObj, "StartSimButton", "START SIM", new Vector2(0.08f, 0.15f), new Vector2(0.28f, 0.25f), new Color(0f, 0.85f, 1f, 0.85f), btnSprite);
        CreateUIButton(controlPanelObj, "StopSimButton", "STOP SIM", new Vector2(0.30f, 0.15f), new Vector2(0.50f, 0.25f), new Color(0f, 0.85f, 1f, 0.85f), btnSprite);
        CreateUIButton(controlPanelObj, "SpawnCarButton", "SPAWN CAR", new Vector2(0.52f, 0.15f), new Vector2(0.72f, 0.25f), new Color(0f, 0.85f, 1f, 0.85f), btnSprite);
        CreateUIButton(controlPanelObj, "HomeButton", "HOME", new Vector2(0.74f, 0.15f), new Vector2(0.92f, 0.25f), new Color(0f, 0.85f, 1f, 0.85f), btnSprite);
        
        // Confirmation/Status Text
        TextMeshProUGUI statusText = CreateUIText(controlPanelObj, "StatusText", "SYSTEM READY. SPAWNING IS PAUSED.", 
            new Vector2(0.1f, 0.07f), new Vector2(0.9f, 0.13f), 20, Color.yellow, TextAlignmentOptions.Center, FontStyles.Bold);
        statusText.fontStyle = FontStyles.Italic | FontStyles.Bold;

        // 7. Bottom Bar Layout
        GameObject bottomBar = CreateUIElement("BottomBar", canvasObj, new Vector2(0.05f, 0.015f), new Vector2(0.95f, 0.055f));
        CreateUIText(bottomBar, "BottomLeftText", "⚡ 321 KM    2146 KM", new Vector2(0f, 0f), new Vector2(0.33f, 1f), 16, Color.white, TextAlignmentOptions.Left, FontStyles.Normal);
        CreateUIText(bottomBar, "BottomCenterText", "◀ CONNECTION ONLINE ▶", new Vector2(0.33f, 0f), new Vector2(0.66f, 1f), 16, Color.cyan, TextAlignmentOptions.Center, FontStyles.Normal);
        CreateUIText(bottomBar, "BottomRightText", "28°C   12:00 AM", new Vector2(0.66f, 0f), new Vector2(1f, 1f), 16, Color.white, TextAlignmentOptions.Right, FontStyles.Normal);

        // 8. Save Prefab
        string prefabPath = "Assets/PreFabs/VRControlPanelCanvas.prefab";
        PrefabUtility.SaveAsPrefabAsset(canvasObj, prefabPath);
        Object.DestroyImmediate(canvasObj);
        
        Debug.Log("[CreateVRCanvasPrefab] Successfully created and saved VR UI Canvas Prefab with futuristic theme: " + prefabPath);
    }
    
    private static GameObject CreateUIElement(string name, GameObject parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return obj;
    }

    private static TextMeshProUGUI CreateUIText(GameObject parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center, FontStyles fontStyle = FontStyles.Normal)
    {
        GameObject obj = CreateUIElement(name, parent, anchorMin, anchorMax);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = fontStyle;
        return tmp;
    }

    private static GameObject CreateUIButton(GameObject parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Color normalColor, Sprite buttonSprite)
    {
        GameObject btnObj = CreateUIElement(name, parent, anchorMin, anchorMax);
        
        Image img = btnObj.AddComponent<Image>();
        img.sprite = buttonSprite;
        img.type = Image.Type.Sliced;
        img.color = normalColor;
        
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        
        ColorBlock colors = btn.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = normalColor * 1.15f;
        colors.pressedColor = normalColor * 0.85f;
        colors.disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.45f);
        colors.colorMultiplier = 1f;
        btn.colors = colors;
        
        // Add Text child
        GameObject txtObj = CreateUIElement("Text", btnObj, Vector2.zero, Vector2.one);
        TextMeshProUGUI tmpText = txtObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.fontSize = 20;
        tmpText.fontStyle = FontStyles.Bold;
        tmpText.color = Color.white;
        
        return btnObj;
    }

    private static void GenerateDashboardBG()
    {
        int width = 1000;
        int height = 800;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (x - width * 0.5f) / (width * 0.5f);
                float ny = (y - height * 0.5f) / (height * 0.5f);
                
                float ax = Mathf.Abs(nx);
                float ay = Mathf.Abs(ny);
                
                // Dashboard boundary: cut corners for sleek hexagonal shape
                float boundary = Mathf.Max(ax + ay * 0.35f, ay + ax * 0.15f);
                
                if (boundary > 0.98f)
                {
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                }
                else if (boundary > 0.965f)
                {
                    // Glowing cyan border outline
                    tex.SetPixel(x, y, new Color(0f, 0.85f, 1f, 0.95f));
                }
                else
                {
                    // Translucent dark-slate dashboard base with soft gradient
                    float dist = Mathf.Sqrt(nx*nx + ny*ny);
                    Color centerColor = new Color(0.04f, 0.06f, 0.10f, 0.95f);
                    Color edgeColor = new Color(0.01f, 0.02f, 0.04f, 0.98f);
                    tex.SetPixel(x, y, Color.Lerp(centerColor, edgeColor, dist));
                }
            }
        }
        tex.Apply();
        
        string folderPath = "Assets/PreFabs";
        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }
        
        byte[] bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(folderPath + "/DashboardBG.png", bytes);
        Object.DestroyImmediate(tex);
    }

    private static void GenerateButtonBG()
    {
        int width = 256;
        int height = 64;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        float radius = height * 0.5f; // 32
        float border = 3f;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dist;
                if (x < radius)
                {
                    float dx = x - radius;
                    float dy = y - radius;
                    dist = Mathf.Sqrt(dx*dx + dy*dy);
                }
                else if (x > width - radius)
                {
                    float dx = x - (width - radius);
                    float dy = y - radius;
                    dist = Mathf.Sqrt(dx*dx + dy*dy);
                }
                else
                {
                    dist = Mathf.Abs(y - radius);
                }
                
                if (dist > radius)
                {
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                }
                else if (dist >= radius - border)
                {
                    // White border outline (tintable in Unity)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.95f));
                }
                else
                {
                    // Semi-transparent interior (tintable in Unity)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.12f));
                }
            }
        }
        tex.Apply();
        
        byte[] bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes("Assets/PreFabs/ButtonBG.png", bytes);
        Object.DestroyImmediate(tex);
    }

    private static void ConfigureTextureImport(string filePath, Vector4 border)
    {
        AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            if (border != Vector4.zero)
            {
                importer.spriteBorder = border;
            }
            importer.SaveAndReimport();
        }
    }
}
