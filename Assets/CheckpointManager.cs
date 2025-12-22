using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    [Header("Checkpoint Settings")]
    public int maxCheckpoints = 5;
    public float checkpointSize = 10f;          // Visual size of the checkpoint model
    public float triggerRadius = 5f;            // How close ship needs to be to collect (can be smaller than visual)
    public float minDistanceFromShip = 40f;     // Минимальное расстояние от корабля
    
    [Header("Custom Checkpoint Visuals")]
    [Tooltip("Drag your checkpoint prefab here. If empty, a default sphere will be used.")]
    public GameObject checkpointPrefab;         // Your custom prefab
    
    [Tooltip("Optional: Custom material/texture for the checkpoint")]
    public Material checkpointMaterial;         // Custom material with your texture
    
    [Tooltip("Optional: Array of textures to randomly pick from")]
    public Texture2D[] checkpointTextures;      // Multiple textures to randomize
    
    [Header("Checkpoint Animation")]
    public float rotationSpeed = 50f;           // How fast checkpoint rotates
    public bool enablePulse = true;             // Enable pulsing effect
    public float pulseSpeed = 2f;               // Pulse speed
    public float pulseAmount = 0.1f;            // Pulse size (0.1 = 10%)
    
    [Header("Checkpoint Light")]
    public bool enableLight = true;             // Add a light to checkpoint
    public Color lightColor = Color.green;      // Light color
    public float lightIntensity = 2f;           // Light brightness
    public float lightRange = 30f;              // How far the light reaches

    private int checkpointsReached = 0;
    private GameObject currentCheckpoint;
    private Transform shipTransform;
    private bool gameWon = false;
    private bool hasSpawnedFirst = false;
    private Vector3 fieldCenter;  // Центр поля астероидов
    private float fieldRadius;    // Радиус поля астероидов

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        GameObject ship = GameObject.Find("Ship");
        if (ship != null)
        {
            shipTransform = ship.transform;
            fieldCenter = ship.transform.position;  // Центр = стартовая позиция корабля
        }
        
        // Получаем радиус от AsteroidSpawner
        if (AsteroidSpawner.Instance != null)
        {
            fieldRadius = AsteroidSpawner.Instance.GetSpawnRadius();
            fieldCenter = AsteroidSpawner.Instance.GetSpawnCenter();
        }
        else
        {
            fieldRadius = 200f;  // По умолчанию
        }
        
        // Spawn first checkpoint only once
        if (!hasSpawnedFirst)
        {
            hasSpawnedFirst = true;
            SpawnNextCheckpoint();
        }
    }

    void Update()
    {
        if (gameWon || shipTransform == null || currentCheckpoint == null) return;

        float distance = Vector3.Distance(shipTransform.position, currentCheckpoint.transform.position);
        if (distance < triggerRadius)
        {
            OnCheckpointReached();
        }
    }

    void SpawnNextCheckpoint()
    {
        // Destroy old checkpoint first
        if (currentCheckpoint != null)
        {
            Destroy(currentCheckpoint);
            currentCheckpoint = null;
        }
        
        if (shipTransform == null) return;

        // Спавним чекпоинт внутри поля астероидов
        Vector3 spawnPos = Vector3.zero;
        int attempts = 0;
        bool validPosition = false;
        
        while (!validPosition && attempts < 20)
        {
            // Случайная позиция внутри сферы астероидов
            spawnPos = fieldCenter + Random.insideUnitSphere * (fieldRadius * 0.8f);  // 80% от радиуса чтобы не на краю
            
            // Проверяем что не слишком близко к кораблю
            float distToShip = Vector3.Distance(spawnPos, shipTransform.position);
            if (distToShip > minDistanceFromShip)
            {
                validPosition = true;
            }
            attempts++;
        }

        // Create checkpoint - use prefab if available, otherwise create sphere
        if (checkpointPrefab != null)
        {
            // Use custom prefab
            currentCheckpoint = Instantiate(checkpointPrefab, spawnPos, Quaternion.identity);
            currentCheckpoint.name = "Checkpoint_" + (checkpointsReached + 1);
            currentCheckpoint.transform.localScale = Vector3.one * checkpointSize;
        }
        else
        {
            // Fallback to default sphere
            currentCheckpoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            currentCheckpoint.name = "Checkpoint_" + (checkpointsReached + 1);
            currentCheckpoint.transform.position = spawnPos;
            currentCheckpoint.transform.localScale = Vector3.one * checkpointSize;
        }
        
        // Apply custom material or texture
        Renderer renderer = currentCheckpoint.GetComponent<Renderer>();
        if (renderer == null)
        {
            // Try to find renderer in children (for prefabs)
            renderer = currentCheckpoint.GetComponentInChildren<Renderer>();
        }
        
        if (renderer != null)
        {
            if (checkpointMaterial != null)
            {
                // Use custom material
                renderer.material = checkpointMaterial;
            }
            else if (checkpointTextures != null && checkpointTextures.Length > 0)
            {
                // Pick random texture from array
                Texture2D randomTexture = checkpointTextures[Random.Range(0, checkpointTextures.Length)];
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.mainTexture = randomTexture;
                // Add some emission so it glows
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetTexture("_EmissionMap", randomTexture);
                renderer.material.SetColor("_EmissionColor", Color.white * 0.5f);
            }
            else if (checkpointPrefab == null)
            {
                // Default green color for generated sphere
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = Color.green;
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", Color.green * 0.5f);
            }
        }

        // Remove collider (we check distance manually) - only for generated spheres
        if (checkpointPrefab == null)
        {
            Collider col = currentCheckpoint.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }
        
        // Add animation component
        CheckpointAnimator animator = currentCheckpoint.AddComponent<CheckpointAnimator>();
        animator.rotationSpeed = rotationSpeed;
        animator.enablePulse = enablePulse;
        animator.pulseSpeed = pulseSpeed;
        animator.pulseAmount = pulseAmount;
        animator.baseScale = checkpointSize;
        
        // Add light source to checkpoint
        if (enableLight)
        {
            GameObject lightObj = new GameObject("CheckpointLight");
            lightObj.transform.SetParent(currentCheckpoint.transform);
            lightObj.transform.localPosition = Vector3.zero;
            
            Light pointLight = lightObj.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = lightColor;
            pointLight.intensity = lightIntensity;
            pointLight.range = lightRange;
            pointLight.shadows = LightShadows.None;  // No shadows for performance
        }

        Debug.Log("Checkpoint " + (checkpointsReached + 1) + " spawned inside asteroid field!");
    }

    public void OnCheckpointReached()
    {
        checkpointsReached++;
        Debug.Log("Checkpoint " + checkpointsReached + " reached!");

        // Destroy current checkpoint
        if (currentCheckpoint != null)
        {
            Destroy(currentCheckpoint);
            currentCheckpoint = null;
        }

        if (checkpointsReached >= maxCheckpoints)
        {
            gameWon = true;
            Debug.Log("YOU WIN!");
            Time.timeScale = 0f;
        }
        else
        {
            SpawnNextCheckpoint();
        }
    }

    public Vector3 GetCheckpointPosition()
    {
        if (currentCheckpoint != null)
            return currentCheckpoint.transform.position;
        return Vector3.zero;
    }

    public float GetCheckpointSize()
    {
        return checkpointSize;
    }
    
    public float GetTriggerRadius()
    {
        return triggerRadius;
    }
    
    public int GetCheckpointsReached()
    {
        return checkpointsReached;
    }
    
    public int GetMaxCheckpoints()
    {
        return maxCheckpoints;
    }
    
    public bool IsGameWon()
    {
        return gameWon;
    }
    
    // Reset score when player dies
    public void ResetScore()
    {
        checkpointsReached = 0;
        gameWon = false;
        
        // Destroy current checkpoint and spawn a new one
        if (currentCheckpoint != null)
        {
            Destroy(currentCheckpoint);
            currentCheckpoint = null;
        }
        
        SpawnNextCheckpoint();
        Debug.Log("Score reset to 0!");
    }
}

// Separate component for checkpoint animation
public class CheckpointAnimator : MonoBehaviour
{
    public float rotationSpeed = 50f;
    public bool enablePulse = true;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.1f;
    public float baseScale = 1f;
    
    private Vector3 rotationAxis;
    
    void Start()
    {
        // Random rotation axis for variety
        rotationAxis = Random.onUnitSphere;
    }
    
    void Update()
    {
        // Rotate
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
        
        // Pulse effect
        if (enablePulse)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = Vector3.one * baseScale * pulse;
        }
    }
}
