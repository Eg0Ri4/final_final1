using UnityEngine;

public class Asteroid : MonoBehaviour
{
    [Header("Rotation Only")]
    public float rotationSpeed = 20f;
    
    private Vector3 randomRotationAxis;
    
    void Start()
    {
        // Random slow rotation for visual effect only
        randomRotationAxis = Random.onUnitSphere;
        
        // Setup collider as trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        
        // Add Rigidbody for trigger detection (required for OnTriggerEnter)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;  // Don't move from physics
        rb.useGravity = false;   // No gravity
    }
    
    void Update()
    {
        // Only rotate slowly, don't move
        transform.Rotate(randomRotationAxis * rotationSpeed * Time.deltaTime);
    }
    
    void OnTriggerEnter(Collider other)
    {
        CheckCollision(other.gameObject);
    }
    
    void CheckCollision(GameObject other)
    {
        // Check if hit ship
        if (other.name == "Ship" || other.GetComponent<CynteractShip>() != null)
        {
            Debug.Log("ASTEROID HIT SHIP!");
            CynteractShip ship = other.GetComponent<CynteractShip>();
            if (ship != null)
            {
                ship.OnAsteroidCollision();
            }
        }
    }
}
