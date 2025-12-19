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

        // Make sure CynteractDeviceManager exists in the scene
        if (CynteractDeviceManager.Instance == null)
        {
            Debug.LogWarning("CynteractDeviceManager not found in scene. Waiting for device connection...");
        }

        // Wait for the device to be ready
        CynteractDeviceManager.Instance?.ListenOnReady(device =>
        {
            // Check if device is a cushion
            if (device.DeviceType == DeviceType.Cushion)
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

        // Control rotation based on cushion tilt
        if (isCushionReady && cushionData != null)
        {
            Quaternion targetRotation;
            
            if (useResetRotation)
            {
                // Get reset rotation (relative to initial orientation)
                targetRotation = cushionData.GetResetRotationOfPartOrDefault(FingerPart.palmCenter);
            }
            else
            {
                // Get absolute rotation
                targetRotation = cushionData.GetAbsoluteRotationOfPartOrDefault(FingerPart.palmCenter);
            }

            // Apply rotation with sensitivity and smooth rotation
            Quaternion desiredRotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation * baseRotation,
                rotationSensitivity * Time.fixedDeltaTime
            );

            // Apply rotation using Rigidbody for physics-based movement
            shipRigidbody.MoveRotation(desiredRotation);
        }
    }
}


