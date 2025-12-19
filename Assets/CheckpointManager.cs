using UnityEngine;
<<<<<<< HEAD

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
=======
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour
{
    [Header("Настройки чекпоинтов")]
    public int totalCheckpoints = 5;           // Сколько всего чекпоинтов нужно пройти
    public float spawnDistance = 100f;         // На каком расстоянии спавнится чекпоинт
    public float spawnSpread = 40f;            // Разброс по сторонам (случайность позиции)
    public float checkpointSize = 8f;          // Размер чекпоинта
    
    [Header("Визуализация")]
    public Color checkpointColor = new Color(0f, 1f, 0.5f, 0.7f);  // Цвет чекпоинта
    public Color lineColor = new Color(0f, 1f, 0.5f, 0.3f);        // Цвет линии к чекпоинту
    
    [Header("Ссылки")]
    public Transform ship;                     // Ссылка на корабль
    
    [Header("UI")]
    public bool showUI = true;
    
    // Приватные переменные
    private GameObject currentCheckpoint;
    private int checkpointsCollected = 0;
    private bool gameFinished = false;
    private LineRenderer guideLine;
    
    // Материалы
    private Material checkpointMaterial;
    private Material lineMaterial;
    
    void Start()
    {
        // Если корабль не назначен, ищем по тегу или имени
        if (ship == null)
        {
            GameObject shipObj = GameObject.FindGameObjectWithTag("Player");
            if (shipObj == null)
            {
                shipObj = GameObject.Find("Ship");
            }
            if (shipObj != null)
            {
                ship = shipObj.transform;
            }
            else
            {
                Debug.LogError("CheckpointManager: Корабль не найден! Назначьте его вручную или добавьте тег 'Player'");
                return;
            }
        }
        
        // Создаём материалы
        CreateMaterials();
        
        // Создаём линию-указатель
        CreateGuideLine();
        
        // Спавним первый чекпоинт
        SpawnNextCheckpoint();
    }
    
    void CreateMaterials()
    {
        // Материал для чекпоинта (полупрозрачный светящийся)
        checkpointMaterial = new Material(Shader.Find("Standard"));
        checkpointMaterial.SetFloat("_Mode", 3); // Transparent mode
        checkpointMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        checkpointMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        checkpointMaterial.SetInt("_ZWrite", 0);
        checkpointMaterial.DisableKeyword("_ALPHATEST_ON");
        checkpointMaterial.EnableKeyword("_ALPHABLEND_ON");
        checkpointMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        checkpointMaterial.renderQueue = 3000;
        checkpointMaterial.color = checkpointColor;
        checkpointMaterial.EnableKeyword("_EMISSION");
        checkpointMaterial.SetColor("_EmissionColor", checkpointColor * 2f);
    }
    
    void CreateGuideLine()
    {
        // Создаём объект для линии
        GameObject lineObj = new GameObject("GuideLine");
        lineObj.transform.SetParent(transform);
        guideLine = lineObj.AddComponent<LineRenderer>();
        
        // Настраиваем линию
        guideLine.startWidth = 0.3f;
        guideLine.endWidth = 0.1f;
        guideLine.positionCount = 2;
        guideLine.material = new Material(Shader.Find("Sprites/Default"));
        guideLine.startColor = lineColor;
        guideLine.endColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0f);
    }
    
    void Update()
    {
        if (gameFinished || ship == null) return;
        
        // Обновляем линию-указатель
        if (currentCheckpoint != null && guideLine != null)
        {
            guideLine.SetPosition(0, ship.position);
            guideLine.SetPosition(1, currentCheckpoint.transform.position);
        }
    }
    
    void SpawnNextCheckpoint()
    {
        if (checkpointsCollected >= totalCheckpoints)
        {
            FinishGame();
            return;
        }
        
        // Удаляем старый чекпоинт если есть
        if (currentCheckpoint != null)
        {
            Destroy(currentCheckpoint);
        }
        
        // Вычисляем позицию нового чекпоинта
        // Спавним впереди корабля с небольшим случайным отклонением
        Vector3 spawnPos = ship.position + ship.forward * spawnDistance;
        spawnPos += ship.right * Random.Range(-spawnSpread, spawnSpread);
        spawnPos += ship.up * Random.Range(-spawnSpread * 0.5f, spawnSpread * 0.5f);
        
        // Создаём чекпоинт
        currentCheckpoint = CreateCheckpointObject(spawnPos);
    }
    
    GameObject CreateCheckpointObject(Vector3 position)
    {
        // Создаём основной объект
        GameObject checkpoint = new GameObject($"Checkpoint_{checkpointsCollected + 1}");
        checkpoint.transform.position = position;
        checkpoint.layer = LayerMask.NameToLayer("Default");
        
        // Добавляем визуальную сферу (кольцо было бы лучше, но сфера проще)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(checkpoint.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * checkpointSize;
        
        // Убираем коллайдер с визуала
        Destroy(visual.GetComponent<Collider>());
        
        // Применяем материал
        visual.GetComponent<Renderer>().material = checkpointMaterial;
        
        // Добавляем триггер-коллайдер на основной объект
        SphereCollider trigger = checkpoint.AddComponent<SphereCollider>();
        trigger.radius = checkpointSize / 2f;
        trigger.isTrigger = true;
        
        // Добавляем скрипт чекпоинта
        Checkpoint cpScript = checkpoint.AddComponent<Checkpoint>();
        cpScript.manager = this;
        
        // Добавляем вращение для красоты
        CheckpointRotator rotator = checkpoint.AddComponent<CheckpointRotator>();
        
        // Добавляем пульсацию
        CheckpointPulse pulse = visual.AddComponent<CheckpointPulse>();
        pulse.baseScale = checkpointSize;
        
        return checkpoint;
    }
    
    public void OnCheckpointReached()
    {
        checkpointsCollected++;
        Debug.Log($"Чекпоинт собран! {checkpointsCollected}/{totalCheckpoints}");
        
        // Эффект при сборе (можно добавить партиклы)
        if (currentCheckpoint != null)
        {
            CreateCollectEffect(currentCheckpoint.transform.position);
        }
        
        // Спавним следующий
        SpawnNextCheckpoint();
    }
    
    void CreateCollectEffect(Vector3 position)
    {
        // Простой эффект - расширяющаяся сфера
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.transform.position = position;
        Destroy(effect.GetComponent<Collider>());
        
        Material effectMat = new Material(Shader.Find("Sprites/Default"));
        effectMat.color = new Color(checkpointColor.r, checkpointColor.g, checkpointColor.b, 0.5f);
        effect.GetComponent<Renderer>().material = effectMat;
        
        // Добавляем скрипт для анимации и уничтожения
        CollectEffect ce = effect.AddComponent<CollectEffect>();
        ce.startScale = checkpointSize;
    }
    
    void FinishGame()
    {
        gameFinished = true;
        Debug.Log("=== ПОЗДРАВЛЯЕМ! Все чекпоинты собраны! ===");
        
        // Скрываем линию
        if (guideLine != null)
        {
            guideLine.enabled = false;
        }
    }
    
    // UI для отображения прогресса
    void OnGUI()
    {
        if (!showUI) return;
        
        // Стиль для текста
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.fontStyle = FontStyle.Bold;
        
        if (gameFinished)
        {
            style.normal.textColor = Color.green;
            style.fontSize = 36;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(Screen.width/2 - 200, Screen.height/2 - 50, 400, 100), 
                "МИССИЯ ВЫПОЛНЕНА!", style);
        }
        else
        {
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(20, 20, 300, 40), 
                $"Чекпоинты: {checkpointsCollected} / {totalCheckpoints}", style);
            
            // Расстояние до чекпоинта
            if (currentCheckpoint != null && ship != null)
            {
                float dist = Vector3.Distance(ship.position, currentCheckpoint.transform.position);
                style.fontSize = 18;
                GUI.Label(new Rect(20, 55, 300, 30), $"Расстояние: {dist:F0} м", style);
            }
        }
    }
}

// Вспомогательный класс для вращения чекпоинта
public class CheckpointRotator : MonoBehaviour
{
    public float rotationSpeed = 30f;
    
    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        transform.Rotate(Vector3.right * rotationSpeed * 0.5f * Time.deltaTime);
    }
}

// Пульсация чекпоинта
public class CheckpointPulse : MonoBehaviour
{
    public float baseScale = 8f;
    public float pulseAmount = 0.1f;
    public float pulseSpeed = 2f;
    
    void Update()
    {
        float scale = baseScale * (1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
        transform.localScale = Vector3.one * scale;
    }
}

// Эффект при сборе чекпоинта
public class CollectEffect : MonoBehaviour
{
    public float startScale = 8f;
    public float expandSpeed = 50f;
    public float fadeSpeed = 3f;
    
    private float currentScale;
    private Material mat;
    private float alpha = 0.5f;
    
    void Start()
    {
        currentScale = startScale;
        mat = GetComponent<Renderer>().material;
    }
    
    void Update()
    {
        currentScale += expandSpeed * Time.deltaTime;
        transform.localScale = Vector3.one * currentScale;
        
        alpha -= fadeSpeed * Time.deltaTime;
        Color c = mat.color;
        c.a = alpha;
        mat.color = c;
        
        if (alpha <= 0)
        {
            Destroy(gameObject);
        }
    }
}

>>>>>>> 0e7259d3d6a5ed82554feecc436b076242c27e7c
