using Cynteract.InputDevices;
using UnityEngine;

public class CynteractShip : MonoBehaviour
{
    [Header("Настройки полета")]
    public float flySpeed = 30f;
    public float rotationSmoothness = 5f;
    
    [Header("Ускорение (качание влево-вправо)")]
    public float maxBoostSpeed = 60f;       // Максимальная скорость при ускорении
    public float boostBuildUpSpeed = 5f;    // Как быстро набирается скорость
    public float boostDecaySpeed = 10f;     // Как быстро падает скорость (когда не качаешь)
    public float rockingThreshold = 5f;     // Минимальный угол качания для ускорения

    // Cynteract device data
    private CushionData cushionData;
    private bool deviceError = false;
    private Quaternion targetRotation;
    private Quaternion currentRotation;
    
    // Starting position for respawn
    private Vector3 startPosition;
    private Quaternion startRotation;
    
    // Boost system
    private float currentSpeed;
    private float lastRollAngle = 0f;
    private float timeSinceLastRock = 0f;   // Время с последнего качания
    private float rockTimeout = 0.3f;        // Сколько времени без качания = замедление
    private int rockDirection = 0;           // Направление качания (-1 влево, 1 вправо)
    private int lastRockDirection = 0;       // Предыдущее направление
    private bool isRocking = false;          // Сейчас качает?

    void Start()
    {
<<<<<<< HEAD
        Cursor.visible = false;
        
        // Save starting position for respawn
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        currentRotation = transform.rotation;
        targetRotation = transform.rotation;
        currentSpeed = flySpeed;

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

        // Connect to device
        try
        {
            if (CynteractDeviceManager.Instance != null)
            {
                CynteractDeviceManager.Instance.ListenOnReady(device =>
                {
                    cushionData = new CushionData(device);
                    Debug.Log("Cynteract device connected!");
                });
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Cynteract device not available: " + e.Message);
            deviceError = true;
=======
        // В этом режиме курсор ДОЛЖЕН быть виден, не блокируем его
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Добавляем тег Player если его нет (для системы чекпоинтов)
        if (string.IsNullOrEmpty(gameObject.tag) || gameObject.tag == "Untagged")
        {
            // Тег должен быть создан в Unity Editor: Edit -> Project Settings -> Tags
            // Или можно использовать проверку по компоненту ShipPadControl
        }
        
        // Добавляем Rigidbody если его нет (нужен для триггеров)
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true; // Kinematic чтобы не влияла физика
        }
        
        // Добавляем коллайдер если его нет
        if (GetComponent<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.size = Vector3.one * 2f; // Размер можно настроить под модель
>>>>>>> 0e7259d3d6a5ed82554feecc436b076242c27e7c
        }
    }

    void Update()
    {
        if (cushionData == null)
        {
            return;
        }

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
        }

        // Двигаем корабль с текущей скоростью
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
        
        // Check for asteroid collision
        CheckAsteroidCollision();
    }
    
    // Проверяем качание влево-вправо для ускорения
    void CheckRockingBoost()
    {
        // Получаем текущий угол наклона (roll) - Z ось
        float currentRollAngle = currentRotation.eulerAngles.z;
        
        // Конвертируем в -180 to 180
        if (currentRollAngle > 180f) currentRollAngle -= 360f;
        
        // Вычисляем изменение угла
        float rollChange = currentRollAngle - lastRollAngle;
        
        // Игнорируем скачки больше 180 (переход через границу)
        if (Mathf.Abs(rollChange) > 180f)
        {
            lastRollAngle = currentRollAngle;
            return;
        }
        
        // Определяем направление качания
        if (rollChange > rockingThreshold)
        {
            rockDirection = 1;  // Качаем вправо
        }
        else if (rollChange < -rockingThreshold)
        {
            rockDirection = -1; // Качаем влево
        }
        else
        {
            rockDirection = 0;  // Не качаем
        }
        
        // Проверяем смену направления (реальное качание туда-сюда)
        if (rockDirection != 0 && rockDirection != lastRockDirection && lastRockDirection != 0)
        {
            // Сменили направление = качаем!
            isRocking = true;
            timeSinceLastRock = 0f;
        }
        
        // Обновляем таймер
        timeSinceLastRock += Time.deltaTime;
        
        // Если долго не качали - перестали качать
        if (timeSinceLastRock > rockTimeout)
        {
            isRocking = false;
        }
        
        // Управление скоростью
        if (isRocking)
        {
            // Ускоряемся!
            currentSpeed += boostBuildUpSpeed * Time.deltaTime * 10f;
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
        
        // Сохраняем значения
        if (rockDirection != 0)
        {
            lastRockDirection = rockDirection;
        }
        lastRollAngle = currentRollAngle;
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
        isRocking = false;
        timeSinceLastRock = 0f;
        lastRockDirection = 0;
        rockDirection = 0;
        lastRollAngle = 0f;
        
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
