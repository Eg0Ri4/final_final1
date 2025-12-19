using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    public static AsteroidSpawner Instance;  // Singleton для доступа из CheckpointManager
    
    [Header("Asteroid Prefab")]
    public GameObject asteroidPrefab;  // DRAG YOUR ASTEROID MODEL HERE!
    
    [Header("Spawn Settings")]
    public int asteroidsToSpawn = 50;     // How many asteroids (default 50)
    public float spawnRadius = 200f;       // Radius around ship
    public float minDistanceFromShip = 30f; // Don't spawn too close to ship
    
    [Header("Asteroid Size")]
    public float minSize = 2f;
    public float maxSize = 8f;
    
    private Transform shipTransform;
    private Vector3 spawnCenterPosition;  // Center of asteroid field (ship start pos)
    
    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        // Find ship - asteroids spawn around ship position
        GameObject ship = GameObject.Find("Ship");
        if (ship != null)
        {
            shipTransform = ship.transform;
            spawnCenterPosition = ship.transform.position;  // Save center position
        }
        
        // Spawn all asteroids at game start
        SpawnAllAsteroids();
    }
    
    void SpawnAllAsteroids()
    {
        if (shipTransform == null)
        {
            Debug.LogWarning("Ship not found! Spawning asteroids around spawner position.");
            spawnCenterPosition = transform.position;
        }
        
        for (int i = 0; i < asteroidsToSpawn; i++)
        {
            SpawnOneAsteroid();
        }
        Debug.Log("Spawned " + asteroidsToSpawn + " asteroids around ship!");
    }
    
    void SpawnOneAsteroid()
    {
        // Find valid spawn position
        Vector3 spawnPosition;
        int attempts = 0;
        
        do
        {
            // Random position in sphere around center
            spawnPosition = spawnCenterPosition + Random.insideUnitSphere * spawnRadius;
            attempts++;
        }
        while (Vector3.Distance(spawnPosition, spawnCenterPosition) < minDistanceFromShip && attempts < 10);
        
        // Create asteroid
        GameObject asteroid;
        
        if (asteroidPrefab != null)
        {
            asteroid = Instantiate(asteroidPrefab, spawnPosition, Random.rotation);
        }
        else
        {
            asteroid = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            asteroid.transform.position = spawnPosition;
            asteroid.transform.rotation = Random.rotation;
            
            Renderer renderer = asteroid.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = new Color(0.4f, 0.35f, 0.3f);
            }
        }
        
        asteroid.name = "Asteroid";
        
        // Random size
        float randomSize = Random.Range(minSize, maxSize);
        asteroid.transform.localScale = Vector3.one * randomSize;
        
        // Add Asteroid script
        if (asteroid.GetComponent<Asteroid>() == null)
        {
            asteroid.AddComponent<Asteroid>();
        }
        
        // Add collider
        if (asteroid.GetComponent<Collider>() == null)
        {
            asteroid.AddComponent<SphereCollider>();
        }
    }
    
    // Получить радиус спавна для CheckpointManager
    public float GetSpawnRadius()
    {
        return spawnRadius;
    }
    
    // Получить центр поля астероидов
    public Vector3 GetSpawnCenter()
    {
        return spawnCenterPosition;
    }
}
