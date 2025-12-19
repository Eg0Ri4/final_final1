using Cynteract.InputDevices;
using UnityEngine;

/// <summary>
/// Controls a spaceship that flies forward automatically and is steered by cushion tilt
/// </summary>
public class ShipController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float forwardSpeed = 10f;
    [SerializeField] private float rotationSensitivity = 5f;
    [SerializeField] private float maxSpeed = 50f;
    
    [Header("Cushion Settings")]
    [SerializeField] private KeyCode resetKey = KeyCode.R;
    
    [Header("Debug/Testing")]
    [SerializeField] private bool useKeyboardFallback = true;
    [SerializeField] private float keyboardRotationSpeed = 90f;
    
    private CushionData cushionData;
    private Rigidbody shipRigidbody;
    private Quaternion baseRotation;
    private bool cushionConnected = false;

    void Start()
    {
        shipRigidbody = GetComponent<Rigidbody>();
        if (shipRigidbody == null)
        {
            Debug.LogError("ShipController requires a Rigidbody component!");
            enabled = false;
            return;
        }

        // Ensure Rigidbody is set up correctly for movement
        if (shipRigidbody.isKinematic)
        {
            Debug.LogWarning("ShipController: Rigidbody is kinematic! Setting to non-kinematic for physics movement.");
            shipRigidbody.isKinematic = false;
        }
        
        // Set reasonable drag if it's too high
        if (shipRigidbody.drag > 5f)
        {
            Debug.LogWarning($"ShipController: Rigidbody drag is {shipRigidbody.drag}, which might be too high. Consider lowering it.");
        }

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            if (renderer.sharedMaterial == null || 
                renderer.sharedMaterial.name.Contains("Default-Material") || 
                renderer.sharedMaterial.shader.name.Contains("Hidden/InternalErrorShader"))
            {
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
                    Material defaultMaterial = new Material(shader);
                    defaultMaterial.color = new Color(0.5f, 0.7f, 1f, 1f); // Light blue color
                    renderer.material = defaultMaterial;
                    Debug.Log("Ship material created successfully");
                }
            }
        }

        baseRotation = transform.rotation;

        // Check if CynteractDeviceManager exists in the scene
        if (CynteractDeviceManager.Instance == null)
        {
            Debug.LogWarning("ShipController: CynteractDeviceManager not found in scene! " +
                          "Please add a GameObject with CynteractDeviceManager script to your scene. " +
                          "Using keyboard controls as fallback.");
            return;
        }

        // Wait for the device to be ready (following the pattern from examples)
        CynteractDeviceManager.Instance.ListenOnReady(device =>
        {
            // Get the corresponding data - assumes the device is a cushion
            cushionData = new CushionData(device);
            // Reset cushion orientation on first connection
            cushionData.Reset();
            cushionConnected = true;
            baseRotation = transform.rotation;
            Debug.Log("Cushion connected and ready for ship control!");
        });
        
        Debug.Log("ShipController initialized. Waiting for cushion device to connect... " +
                 "The ship will move forward automatically. Press R to reset cushion orientation.");
    }

    void Update()
    {
        // Reset cushion orientation when R key is pressed
        if (Input.GetKeyDown(resetKey))
        {
            if (cushionData != null)
            {
                cushionData.Reset();
                baseRotation = transform.rotation;
                Debug.Log("Cushion orientation reset!");
            }
            else
            {
                baseRotation = transform.rotation;
                Debug.Log("Base rotation reset!");
            }
        }
    }

    void FixedUpdate()
    {
        if (shipRigidbody == null) return;

        // Apply forward velocity directly (more reliable than force for constant forward movement)
        Vector3 forwardVelocity = transform.forward * forwardSpeed;
        
        // Preserve any existing lateral velocity while maintaining forward speed
        Vector3 lateralVelocity = Vector3.ProjectOnPlane(shipRigidbody.velocity, transform.forward);
        Vector3 targetVelocity = forwardVelocity + lateralVelocity;
        
        // Clamp the total velocity to maxSpeed
        if (targetVelocity.magnitude > maxSpeed)
        {
            targetVelocity = targetVelocity.normalized * maxSpeed;
        }
        
        shipRigidbody.velocity = targetVelocity;

        // Control rotation based on cushion tilt or keyboard fallback
        if (cushionConnected && cushionData != null)
        {
            // Get the reset rotation of the cushion's palmCenter sensor
            // This gives rotation relative to when Reset() was called
            Quaternion cushionRotation = cushionData.GetResetRotationOfPartOrDefault(FingerPart.palmCenter);
            
            // Apply the cushion rotation to the base rotation
            Quaternion targetRotation = baseRotation * cushionRotation;
            
            // Smoothly rotate towards target rotation
            Quaternion desiredRotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSensitivity * Time.fixedDeltaTime
            );

            // Apply rotation using Rigidbody for physics-based movement
            shipRigidbody.MoveRotation(desiredRotation);
        }
        else if (useKeyboardFallback)
        {
            // Fallback keyboard controls for testing
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
            {
                Vector3 rotationDelta = new Vector3(-vertical, horizontal, 0) * keyboardRotationSpeed * Time.fixedDeltaTime;
                Quaternion deltaRotation = Quaternion.Euler(rotationDelta);
                Quaternion targetRotation = transform.rotation * deltaRotation;
                shipRigidbody.MoveRotation(targetRotation);
            }
        }
    }
}
