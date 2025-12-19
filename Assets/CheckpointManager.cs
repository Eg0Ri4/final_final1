using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    [Header("Checkpoint Settings")]
    public int maxCheckpoints = 5;
    public float checkpointSize = 10f;
    public float minDistanceFromShip = 40f;  // Минимальное расстояние от корабля

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
        if (distance < checkpointSize)
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

        // Create checkpoint
        currentCheckpoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        currentCheckpoint.name = "Checkpoint_" + (checkpointsReached + 1);
        currentCheckpoint.transform.position = spawnPos;
        currentCheckpoint.transform.localScale = Vector3.one * checkpointSize;

        // Green color
        Renderer renderer = currentCheckpoint.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.green;
        }

        // Remove collider (we check distance manually)
        Collider col = currentCheckpoint.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Debug.Log("Checkpoint " + (checkpointsReached + 1) + " spawned inside asteroid field!");
    }

    void OnCheckpointReached()
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
}
