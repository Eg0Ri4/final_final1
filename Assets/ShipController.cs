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
    
    private CushionData cushionData;
    private Rigidbody shipRigidbody;
    private Quaternion baseRotation;

    void Start()
    {
        shipRigidbody = GetComponent<Rigidbody>();
        if (shipRigidbody == null)
        {
            Debug.LogError("ShipController requires a Rigidbody component!");
            enabled = false;
            return;
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
            Debug.LogError("ShipController: CynteractDeviceManager not found in scene! " +
                          "Please add a GameObject with CynteractDeviceManager script to your scene.");
            return;
        }

        // Wait for the device to be ready (following the pattern from examples)
        CynteractDeviceManager.Instance.ListenOnReady(device =>
        {
            // Get the corresponding data - assumes the device is a cushion
            cushionData = new CushionData(device);
            // Reset cushion orientation on first connection
            cushionData.Reset();
            Debug.Log("Cushion connected and ready for ship control!");
        });
        
        Debug.Log("ShipController initialized. Waiting for cushion device to connect... " +
                 "The ship will move forward automatically, but rotation requires a connected cushion device.");
    }

    void Update()
    {
        // Reset cushion orientation when R key is pressed
        if (Input.GetKeyDown(resetKey) && cushionData != null)
        {
            cushionData.Reset();
            baseRotation = transform.rotation;
            Debug.Log("Cushion orientation reset!");
        }
    }

    void FixedUpdate()
    {
        if (shipRigidbody == null) return;

        // Apply forward force continuously
        Vector3 forwardForce = transform.forward * forwardSpeed;
        shipRigidbody.AddForce(forwardForce, ForceMode.Force);

        // Limit maximum speed
        if (shipRigidbody.velocity.magnitude > maxSpeed)
        {
            shipRigidbody.velocity = shipRigidbody.velocity.normalized * maxSpeed;
        }

        // Control rotation based on cushion tilt
        if (cushionData != null)
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
    }
}
