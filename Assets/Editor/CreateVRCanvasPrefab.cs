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
        bgImg.color = Color.white;
        
        // 5. Create Welcome Panel
        GameObject welcomePanelObj = CreateUIElement("WelcomePanel", canvasObj, Vector2.zero, Vector2.one);
        
        CreateUIText(welcomePanelObj, "WelcomeTitle", "WELCOME TO VALET SIMULATION", 
            new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.88f), 36, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            
        CreateUIText(welcomePanelObj, "WelcomeSubtitle", "SAFETY INTERACTION SYSTEM ONLINE", 
            new Vector2(0.1f, 0.66f), new Vector2(0.9f, 0.71f), 18, Color.cyan, TextAlignmentOptions.Center, FontStyles.Normal);

        CreateUIText(welcomePanelObj, "WelcomeBody", 
            "This simulation demonstrates an automated valet parking guidance system\nintegrated with active pedestrian crosswalk safety sensors.\n\n<b>Instructions:</b>\n• Press <b>'X'</b> on your Left Quest Controller to open/close this panel.\n• Point and click using your controller pointer ray to select options.", 
            new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.60f), 22, new Color(0.85f, 0.9f, 0.95f), TextAlignmentOptions.Center, FontStyles.Normal);
        
        CreateUIButton(welcomePanelObj, "EnterButton", "ENTER SIMULATION", new Vector2(0.38f, 0.18f), new Vector2(0.62f, 0.28f), new Color(0f, 0.85f, 1f, 0.85f), btnSprite);
        
        // 6. Create Control Panel
        GameObject controlPanelObj = CreateUIElement("ControlPanel", canvasObj, Vector2.zero, Vector2.one);
        controlPanelObj.SetActive(false); // Inactive on start
        
        // Titles
        CreateUIText(controlPanelObj, "ControlTitle", "SIMULATION CONTROL PANEL", 
            new Vector2(0.1f, 0.75f), new Vector2(0.9f, 0.90f), 36, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            
        CreateUIText(controlPanelObj, "ControlSubtitle", "SELECT COMMAND TO INTERACT", 
            new Vector2(0.1f, 0.69f), new Vector2(0.9f, 0.74f), 18, Color.cyan, TextAlignmentOptions.Center, FontStyles.Normal);
            
        // Minimalistic 2x2 Grid of Buttons
        // Row 1
        CreateUIButton(controlPanelObj, "StartSimButton", "START SIMULATION", new Vector2(0.18f, 0.45f), new Vector2(0.48f, 0.58f), new Color(0f, 0.85f, 1f, 0.85f), btnSprite);
        CreateUIButton(controlPanelObj, "StopSimButton", "STOP SIMULATION", new Vector2(0.52f, 0.45f), new Vector2(0.82f, 0.58f), new Color(0f, 0.85f, 1f, 0.85f), btnSprite);
        
        // Row 2
        CreateUIButton(controlPanelObj, "SpawnCarButton", "SPAWN TEST CAR", new Vector2(0.18f, 0.28f), new Vector2(0.48f, 0.41f), new Color(0f, 0.85f, 1f, 0.85f), btnSprite);
        CreateUIButton(controlPanelObj, "HomeButton", "HOME TELEPORT", new Vector2(0.52f, 0.28f), new Vector2(0.82f, 0.41f), new Color(0f, 0.85f, 1f, 0.85f), btnSprite);
        
        // Confirmation/Status Text
        TextMeshProUGUI statusText = CreateUIText(controlPanelObj, "StatusText", "SYSTEM READY. SPAWNING IS PAUSED.", 
            new Vector2(0.1f, 0.12f), new Vector2(0.9f, 0.20f), 22, Color.yellow, TextAlignmentOptions.Center, FontStyles.Bold);
        statusText.fontStyle = FontStyles.Italic | FontStyles.Bold;

        // 7. Save Prefab
        string prefabPath = "Assets/PreFabs/VRControlPanelCanvas.prefab";
        PrefabUtility.SaveAsPrefabAsset(canvasObj, prefabPath);
        Object.DestroyImmediate(canvasObj);
        
        Debug.Log("[CreateVRCanvasPrefab] Successfully created and saved VR UI Canvas Prefab with simplified dashboard style: " + prefabPath);
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
