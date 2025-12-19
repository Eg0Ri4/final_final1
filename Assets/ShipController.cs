using Cynteract.InputDevices;
using UnityEngine;

/// <summary>
/// Controls a spaceship that flies forward automatically and is steered by cushion tilt
/// </summary>
public class ShipController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float forwardSpeed = 10f;
    [SerializeField] private float rotationSensitivity = 2f;
    [SerializeField] private float maxSpeed = 50f;
    [SerializeField] private float rotationSpeed = 90f; // degrees per second
    
    [Header("Cushion Settings")]
    [SerializeField] private bool useResetRotation = true;
    [SerializeField] private KeyCode resetKey = KeyCode.R; // Key to reset cushion orientation
    
    private CushionData cushionData;
    private Rigidbody shipRigidbody;
    private Quaternion baseRotation;
    private bool isCushionReady = false;

    void Start()
    {
        shipRigidbody = GetComponent<Rigidbody>();
        if (shipRigidbody == null)
        {
            Debug.LogError("ShipController requires a Rigidbody component!");
            return;
        }

        // Fix material if it's missing (pink cube issue)
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // Check if material is missing or invalid
            if (renderer.sharedMaterial == null || renderer.sharedMaterial.name.Contains("Default-Material") || 
                renderer.sharedMaterial.shader.name.Contains("Hidden/InternalErrorShader"))
            {
                // Try to find a suitable shader
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }
                
                if (shader != null)
                {
                    // Create a simple default material
                    Material defaultMaterial = new Material(shader);
                    defaultMaterial.color = new Color(0.5f, 0.7f, 1f, 1f); // Light blue color
                    renderer.material = defaultMaterial;
                    Debug.Log("Ship material created successfully");
                }
                else
                {
                    Debug.LogWarning("Could not find a suitable shader for ship material");
                }
            }
        }

        // Make sure CynteractDeviceManager exists in the scene
        if (CynteractDeviceManager.Instance == null)
        {
            Debug.LogWarning("CynteractDeviceManager not found in scene. Waiting for device connection...");
        }

        // Wait for the device to be ready
        CynteractDeviceManager.Instance?.ListenOnReady(device =>
        {
            // Check if device is a cushion
            if (device.DeviceType == Cynteract.InputDevices.DeviceType.Cushion)
            {
                cushionData = new CushionData(device);
                isCushionReady = true;
                // Reset cushion orientation on first connection
                cushionData.Reset();
                baseRotation = Quaternion.identity;
                Debug.Log("Cushion connected and ready for ship control!");
            }
        });
    }

    void Update()
    {
        // Reset cushion orientation when R key is pressed
        if (Input.GetKeyDown(resetKey) && cushionData != null)
        {
            cushionData.Reset();
            Debug.Log("Cushion orientation reset!");
        }
    }

    void FixedUpdate()
    {
        if (shipRigidbody == null) return;

        // Apply forward force
        Vector3 forwardForce = transform.forward * forwardSpeed;
        shipRigidbody.AddForce(forwardForce, ForceMode.Force);

        // Limit maximum speed
        if (shipRigidbody.velocity.magnitude > maxSpeed)
        {
            shipRigidbody.velocity = shipRigidbody.velocity.normalized * maxSpeed;
        }

        // Control rotation based on cushion tilt or keyboard input
        Quaternion targetRotation = transform.rotation;
        bool hasInput = false;

        if (isCushionReady && cushionData != null)
        {
            // Use cushion input
            if (useResetRotation)
            {
                targetRotation = cushionData.GetResetRotationOfPartOrDefault(FingerPart.palmCenter);
            }
            else
            {
                targetRotation = cushionData.GetAbsoluteRotationOfPartOrDefault(FingerPart.palmCenter);
            }
            targetRotation = targetRotation * baseRotation;
            hasInput = true;
        }
        else
        {
            // Fallback to keyboard controls
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
            {
                // Rotate based on arrow keys or WASD
                float rotationY = horizontal * rotationSpeed * Time.fixedDeltaTime;
                float rotationX = -vertical * rotationSpeed * Time.fixedDeltaTime;
                
                targetRotation = transform.rotation * Quaternion.Euler(rotationX, rotationY, 0);
                hasInput = true;
            }
        }

        if (hasInput)
        {
            // Apply rotation with sensitivity and smooth rotation
            Quaternion desiredRotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSensitivity * Time.fixedDeltaTime
            );

            // Apply rotation using Rigidbody for physics-based movement
            shipRigidbody.MoveRotation(desiredRotation);
        }
    }
}


