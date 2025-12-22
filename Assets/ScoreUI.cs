using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
    [Header("UI Reference")]
    public Text scoreText;  // Assign in Inspector or auto-create
    
    [Header("Display Settings")]
    public string prefix = "Checkpoints: ";
    public Color textColor = Color.white;
    public int fontSize = 36;
    
    private Canvas canvas;
    
    void Start()
    {
        // If no UI assigned, create one automatically
        if (scoreText == null)
        {
            CreateUI();
        }
    }
    
    void CreateUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("ScoreCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create Text object
        GameObject textObj = new GameObject("ScoreText");
        textObj.transform.SetParent(canvasObj.transform, false);
        
        scoreText = textObj.AddComponent<Text>();
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.fontSize = fontSize;
        scoreText.color = textColor;
        scoreText.alignment = TextAnchor.UpperLeft;
        
        // Add outline for better visibility
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);
        
        // Position in top-left corner
        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(20, -20);
        rectTransform.sizeDelta = new Vector2(400, 100);
    }
    
    void Update()
    {
        if (scoreText != null && CheckpointManager.Instance != null)
        {
            int current = CheckpointManager.Instance.GetCheckpointsReached();
            int max = CheckpointManager.Instance.GetMaxCheckpoints();
            scoreText.text = prefix + current + " / " + max;
            
            // Check for win
            if (CheckpointManager.Instance.IsGameWon())
            {
                scoreText.text = "YOU WIN!\nAll " + max + " checkpoints collected!";
                scoreText.color = Color.green;
            }
        }
    }
}

