using UnityEngine;
using UnityEngine.UI;

public class CheckpointIndicator : MonoBehaviour
{
    [Header("Arrow Settings")]
    public Color arrowColor = Color.green;
    public float arrowSize = 50f;
    public float screenEdgeOffset = 50f;  // Distance from screen edge
    
    [Header("Distance Display")]
    public bool showDistance = true;
    public int distanceFontSize = 20;
    
    private Canvas canvas;
    private RectTransform arrowRect;
    private Image arrowImage;
    private Text distanceText;
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
        CreateIndicatorUI();
    }
    
    void CreateIndicatorUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("IndicatorCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create Arrow container
        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(canvasObj.transform, false);
        
        arrowRect = arrowObj.AddComponent<RectTransform>();
        arrowRect.sizeDelta = new Vector2(arrowSize, arrowSize);
        
        // Create arrow image
        arrowImage = arrowObj.AddComponent<Image>();
        arrowImage.color = arrowColor;
        
        // Create arrow texture (triangle pointing up)
        Texture2D arrowTex = CreateArrowTexture();
        arrowImage.sprite = Sprite.Create(arrowTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        
        // Create distance text
        if (showDistance)
        {
            GameObject textObj = new GameObject("DistanceText");
            textObj.transform.SetParent(arrowObj.transform, false);
            
            distanceText = textObj.AddComponent<Text>();
            distanceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            distanceText.fontSize = distanceFontSize;
            distanceText.color = Color.white;
            distanceText.alignment = TextAnchor.MiddleCenter;
            
            // Add outline
            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(0, -arrowSize * 0.8f);
            textRect.sizeDelta = new Vector2(100, 30);
        }
    }
    
    Texture2D CreateArrowTexture()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color transparent = new Color(0, 0, 0, 0);
        
        // Fill with transparent
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                tex.SetPixel(x, y, transparent);
            }
        }
        
        // Draw triangle pointing up
        int centerX = size / 2;
        int topY = size - 8;
        int bottomY = 8;
        int halfWidth = size / 2 - 8;
        
        for (int y = bottomY; y <= topY; y++)
        {
            float t = (float)(y - bottomY) / (topY - bottomY);
            int width = (int)(halfWidth * (1 - t));
            
            for (int x = centerX - width; x <= centerX + width; x++)
            {
                tex.SetPixel(x, y, Color.white);
            }
        }
        
        tex.Apply();
        return tex;
    }
    
    void Update()
    {
        if (mainCamera == null || CheckpointManager.Instance == null)
        {
            if (arrowImage != null) arrowImage.enabled = false;
            return;
        }
        
        Vector3 checkpointPos = CheckpointManager.Instance.GetCheckpointPosition();
        if (checkpointPos == Vector3.zero)
        {
            if (arrowImage != null) arrowImage.enabled = false;
            return;
        }
        
        // Convert checkpoint position to screen space
        Vector3 screenPos = mainCamera.WorldToScreenPoint(checkpointPos);
        
        // Check if checkpoint is behind camera
        bool isBehind = screenPos.z < 0;
        
        // Check if checkpoint is on screen
        bool isOnScreen = !isBehind && 
                          screenPos.x > screenEdgeOffset && 
                          screenPos.x < Screen.width - screenEdgeOffset &&
                          screenPos.y > screenEdgeOffset && 
                          screenPos.y < Screen.height - screenEdgeOffset;
        
        if (isOnScreen)
        {
            // Checkpoint is visible, hide arrow
            arrowImage.enabled = false;
            if (distanceText != null) distanceText.enabled = false;
        }
        else
        {
            // Checkpoint is off-screen, show arrow
            arrowImage.enabled = true;
            if (distanceText != null) distanceText.enabled = true;
            
            // If behind, flip the position
            if (isBehind)
            {
                screenPos.x = Screen.width - screenPos.x;
                screenPos.y = Screen.height - screenPos.y;
            }
            
            // Calculate screen center
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
            
            // Direction from center to checkpoint
            Vector3 direction = (screenPos - screenCenter).normalized;
            
            // Calculate angle for arrow rotation
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            arrowRect.rotation = Quaternion.Euler(0, 0, angle - 90); // -90 because arrow points up
            
            // Clamp position to screen edge
            float edgeX = Screen.width / 2f - screenEdgeOffset;
            float edgeY = Screen.height / 2f - screenEdgeOffset;
            
            // Find intersection with screen edge
            float absX = Mathf.Abs(direction.x);
            float absY = Mathf.Abs(direction.y);
            
            float scale;
            if (absX / edgeX > absY / edgeY)
            {
                scale = edgeX / absX;
            }
            else
            {
                scale = edgeY / absY;
            }
            
            Vector3 edgePosition = screenCenter + direction * scale;
            arrowRect.position = edgePosition;
            
            // Update distance text
            if (distanceText != null && showDistance)
            {
                float distance = Vector3.Distance(mainCamera.transform.position, checkpointPos);
                distanceText.text = Mathf.RoundToInt(distance) + "m";
                
                // Keep text upright
                distanceText.transform.rotation = Quaternion.identity;
            }
        }
    }
}
