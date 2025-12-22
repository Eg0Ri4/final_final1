using Cynteract.InputDevices;
using UnityEngine;

public class CynteractShip : MonoBehaviour
{
    [Header("Настройки полета")]
    public float flySpeed = 30f;
    public float rotationSmoothness = 5f;
    
    [Header("Keyboard Controls (Fallback)")]
    public bool useKeyboardFallback = true;     // Enable keyboard when device not available
    public float keyboardRotationSpeed = 60f;   // Degrees per second for keyboard
    public float keyboardBoostSpeed = 60f;      // Speed when holding Shift
    
    [Header("Ускорение (качание влево-вправо)")]
    public float maxBoostSpeed = 80f;           // Максимальная скорость при ускорении
    public float boostBuildUpSpeed = 15f;       // Базовая скорость набора ускорения
    public float boostDecaySpeed = 20f;         // Как быстро падает скорость
    public float minRockAngle = 8f;             // Минимальный угол для засчитывания качания
    public float minAngularVelocity = 30f;      // Минимальная угловая скорость (градусов/сек)
    
    [Header("Настройки детекции качания")]
    public int rocksForBoost = 3;               // Сколько качаний нужно для начала ускорения
    public float rockWindowTime = 1.5f;         // Окно времени для подсчёта качаний (сек)
    public float frequencyBoostMultiplier = 2f; // Множитель скорости от частоты качаний

    // Cynteract device data
    private CushionData cushionData;
    private Quaternion targetRotation;
    private Quaternion currentRotation;
    private bool deviceConnected = false;
    private bool deviceConnectionAttempted = false;
    
    // Starting position for respawn
    private Vector3 startPosition;
    private Quaternion startRotation;
    
    // Improved boost system
    private float currentSpeed;
    private float lastRollAngle = 0f;
    private float angularVelocity = 0f;         // Угловая скорость
    private int lastRockDirection = 0;          // Предыдущее направление (-1 влево, 1 вправо)
    private float[] rockTimestamps;             // Время каждого качания
    private int rockIndex = 0;                  // Индекс в массиве
    private float lastPeakAngle = 0f;           // Последний пиковый угол
    private bool wasMovingRight = false;        // Двигался вправо?

    void Start()
    {
        Cursor.visible = false;
        
        // Save starting position for respawn
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        currentRotation = transform.rotation;
        targetRotation = transform.rotation;
        currentSpeed = flySpeed;
        
        // Инициализируем массив для отслеживания качаний
        rockTimestamps = new float[20];  // Храним до 20 последних качаний

        // Setup collider for collision detection
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2f, 2f, 4f);
        }
        else
        {
            col.isTrigger = true;
        }

        // Connect to device (only try once)
        if (!deviceConnectionAttempted)
        {
            deviceConnectionAttempted = true;
            try
            {
                if (CynteractDeviceManager.Instance != null)
                {
                    CynteractDeviceManager.Instance.ListenOnReady(device =>
                    {
                        cushionData = new CushionData(device);
                        deviceConnected = true;
                        Debug.Log("Cynteract device connected!");
                    });
                }
                else
                {
                    Debug.Log("No Cynteract device found. Using keyboard controls (WASD/Arrow keys + Shift for boost)");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Cynteract device not available: " + e.Message + "\nUsing keyboard controls instead.");
            }
        }
    }

    void Update()
    {
        // Try Cynteract device first
        if (cushionData != null && deviceConnected)
        {
            try
            {
                targetRotation = cushionData.GetAbsoluteRotationOfPartOrDefault(FingerPart.palmCenter);
                currentRotation = Quaternion.Slerp(currentRotation, targetRotation, Time.deltaTime * rotationSmoothness);
                transform.rotation = currentRotation;
                
                // Проверяем качание влево-вправо для ускорения
                CheckRockingBoost();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Device read error: " + e.Message);
                deviceConnected = false;  // Disable device on error
            }
        }
        // Fallback to keyboard controls
        else if (useKeyboardFallback)
        {
            HandleKeyboardControls();
        }

        // Двигаем корабль с текущей скоростью
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
        
        // Check for asteroid collision
        CheckAsteroidCollision();
    }
    
    // Keyboard fallback controls
    void HandleKeyboardControls()
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal");  // A/D or Left/Right arrows
        float vertical = Input.GetAxis("Vertical");      // W/S or Up/Down arrows
        
        // Rotate ship based on input
        float pitchChange = -vertical * keyboardRotationSpeed * Time.deltaTime;  // Up/Down
        float yawChange = horizontal * keyboardRotationSpeed * Time.deltaTime;   // Left/Right
        float rollChange = 0f;
        
        // Q/E for roll
        if (Input.GetKey(KeyCode.Q)) rollChange = keyboardRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) rollChange = -keyboardRotationSpeed * Time.deltaTime;
        
        transform.Rotate(pitchChange, yawChange, rollChange, Space.Self);
        
        // Boost with Shift
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, keyboardBoostSpeed, boostBuildUpSpeed * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, flySpeed, boostDecaySpeed * Time.deltaTime);
        }
    }
    
    // Улучшенная система детекции качания
    void CheckRockingBoost()
    {
        // Получаем текущий угол наклона (roll) - Z ось
        float currentRollAngle = currentRotation.eulerAngles.z;
        
        // Конвертируем в -180 to 180
        if (currentRollAngle > 180f) currentRollAngle -= 360f;
        
        // Вычисляем угловую скорость (градусов в секунду)
        float deltaAngle = currentRollAngle - lastRollAngle;
        
        // Игнорируем скачки больше 180 (переход через границу)
        if (Mathf.Abs(deltaAngle) > 180f)
        {
            lastRollAngle = currentRollAngle;
            return;
        }
        
        angularVelocity = deltaAngle / Time.deltaTime;
        
        // Определяем текущее направление движения
        bool isMovingRight = angularVelocity > minAngularVelocity;
        bool isMovingLeft = angularVelocity < -minAngularVelocity;
        
        // Детектируем смену направления (пик качания)
        // Качание засчитывается когда:
        // 1. Сменилось направление движения
        // 2. Угол отклонения от последнего пика достаточный
        // 3. Угловая скорость достаточная
        
        bool directionChanged = false;
        
        if (isMovingRight && !wasMovingRight && lastRockDirection == -1)
        {
            // Начали двигаться вправо после движения влево
            float angleFromPeak = Mathf.Abs(currentRollAngle - lastPeakAngle);
            if (angleFromPeak >= minRockAngle)
            {
                directionChanged = true;
                lastRockDirection = 1;
                lastPeakAngle = currentRollAngle;
            }
        }
        else if (isMovingLeft && wasMovingRight && lastRockDirection == 1)
        {
            // Начали двигаться влево после движения вправо
            float angleFromPeak = Mathf.Abs(currentRollAngle - lastPeakAngle);
            if (angleFromPeak >= minRockAngle)
            {
                directionChanged = true;
                lastRockDirection = -1;
                lastPeakAngle = currentRollAngle;
            }
        }
        
        // Инициализация направления если ещё не задано
        if (lastRockDirection == 0)
        {
            if (isMovingRight) lastRockDirection = 1;
            else if (isMovingLeft) lastRockDirection = -1;
            lastPeakAngle = currentRollAngle;
        }
        
        // Записываем качание
        if (directionChanged)
        {
            rockTimestamps[rockIndex] = Time.time;
            rockIndex = (rockIndex + 1) % rockTimestamps.Length;
        }
        
        // Считаем количество качаний за последнее время
        float currentTime = Time.time;
        int recentRocks = 0;
        for (int i = 0; i < rockTimestamps.Length; i++)
        {
            if (currentTime - rockTimestamps[i] <= rockWindowTime && rockTimestamps[i] > 0)
            {
                recentRocks++;
            }
        }
        
        // Вычисляем частоту качаний (качаний в секунду)
        float rockFrequency = recentRocks / rockWindowTime;
        
        // Управление скоростью на основе частоты качаний
        if (recentRocks >= rocksForBoost)
        {
            // Чем чаще качаешь - тем быстрее ускоряешься!
            float frequencyMultiplier = 1f + (rockFrequency * frequencyBoostMultiplier);
            currentSpeed += boostBuildUpSpeed * frequencyMultiplier * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxBoostSpeed);
        }
        else
        {
            // Замедляемся до нормальной скорости
            if (currentSpeed > flySpeed)
            {
                currentSpeed -= boostDecaySpeed * Time.deltaTime;
                currentSpeed = Mathf.Max(currentSpeed, flySpeed);
            }
        }
        
        // Сохраняем состояние
        if (isMovingRight) wasMovingRight = true;
        else if (isMovingLeft) wasMovingRight = false;
        
        lastRollAngle = currentRollAngle;
    }
    
    // Получить текущую скорость (для UI или отладки)
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }
    
    // Получить процент ускорения (0-1)
    public float GetBoostPercent()
    {
        return Mathf.InverseLerp(flySpeed, maxBoostSpeed, currentSpeed);
    }
    
    // Manual collision check as backup
    void CheckAsteroidCollision()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2f);
        foreach (Collider hit in hitColliders)
        {
            if (hit.GetComponent<Asteroid>() != null)
            {
                OnAsteroidCollision();
                break;
            }
        }
    }

    // Called when asteroid hits ship - RESPAWN
    public void OnAsteroidCollision()
    {
        Debug.Log("Hit asteroid! Respawning...");
        Respawn();
    }
    
    // Respawn ship at starting position
    void Respawn()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
        currentRotation = startRotation;
        targetRotation = startRotation;
        
        // Полный сброс скорости и качания
        currentSpeed = flySpeed;
        lastRockDirection = 0;
        lastRollAngle = 0f;
        angularVelocity = 0f;
        lastPeakAngle = 0f;
        wasMovingRight = false;
        
        // Очищаем историю качаний
        for (int i = 0; i < rockTimestamps.Length; i++)
        {
            rockTimestamps[i] = 0f;
        }
        rockIndex = 0;
        
        // Reset checkpoint score to 0
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.ResetScore();
        }
        
        Debug.Log("Ship respawned at start position!");
    }
    
    // Detect collision from ship side
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Asteroid>() != null)
        {
            OnAsteroidCollision();
        }
    }
}
