using UnityEngine;

/// <summary>
/// Создаёт эффект полёта в космосе - летящие звёзды/частицы
/// Прикрепите этот скрипт к кораблю или к камере
/// </summary>
public class SpaceBackground : MonoBehaviour
{
    [Header("Настройки звёзд")]
    public int starCount = 200;              // Количество звёзд
    public float spawnRadius = 100f;         // Радиус спавна звёзд вокруг корабля
    public float starSize = 0.5f;            // Размер звёзд
    public float minBrightness = 0.3f;       // Минимальная яркость
    public float maxBrightness = 1f;         // Максимальная яркость
    
    [Header("Цвета звёзд")]
    public Color[] starColors = new Color[] {
        Color.white,
        new Color(0.9f, 0.9f, 1f),      // Голубоватый
        new Color(1f, 0.95f, 0.8f),     // Желтоватый
        new Color(1f, 0.8f, 0.8f),      // Красноватый
        new Color(0.8f, 0.8f, 1f)       // Синеватый
    };
    
    [Header("Настройки движения")]
    public bool followTarget = true;         // Следовать за целью
    public Transform target;                 // Цель для следования (корабль)
    
    private GameObject[] stars;
    private Material[] starMaterials;
    private Vector3 lastTargetPos;
    
    void Start()
    {
        // Если цель не назначена, ищем корабль
        if (target == null)
        {
            GameObject ship = GameObject.FindGameObjectWithTag("Player");
            if (ship == null)
            {
                ship = GameObject.Find("Ship");
            }
            if (ship != null)
            {
                target = ship.transform;
            }
        }
        
        if (target != null)
        {
            lastTargetPos = target.position;
        }
        
        CreateStars();
    }
    
    void CreateStars()
    {
        stars = new GameObject[starCount];
        starMaterials = new Material[starCount];
        
        // Создаём родительский объект для звёзд
        GameObject starsParent = new GameObject("Stars");
        starsParent.transform.SetParent(transform);
        
        for (int i = 0; i < starCount; i++)
        {
            // Создаём звезду
            GameObject star = GameObject.CreatePrimitive(PrimitiveType.Quad);
            star.name = $"Star_{i}";
            star.transform.SetParent(starsParent.transform);
            
            // Убираем коллайдер
            Destroy(star.GetComponent<Collider>());
            
            // Случайная позиция в сфере вокруг начальной точки
            Vector3 randomPos = Random.insideUnitSphere * spawnRadius;
            if (target != null)
            {
                randomPos += target.position;
            }
            star.transform.position = randomPos;
            
            // Случайный размер
            float size = starSize * Random.Range(0.3f, 1.5f);
            star.transform.localScale = Vector3.one * size;
            
            // Создаём материал
            Material mat = new Material(Shader.Find("Sprites/Default"));
            
            // Случайный цвет и яркость
            Color baseColor = starColors[Random.Range(0, starColors.Length)];
            float brightness = Random.Range(minBrightness, maxBrightness);
            mat.color = baseColor * brightness;
            
            star.GetComponent<Renderer>().material = mat;
            
            stars[i] = star;
            starMaterials[i] = mat;
            
            // Добавляем мерцание
            StarTwinkle twinkle = star.AddComponent<StarTwinkle>();
            twinkle.baseColor = mat.color;
            twinkle.material = mat;
        }
    }
    
    void Update()
    {
        if (!followTarget || target == null) return;
        
        // Перемещаем звёзды которые остались позади
        Vector3 movement = target.position - lastTargetPos;
        
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;
            
            // Звезда смотрит на камеру (billboard эффект)
            if (Camera.main != null)
            {
                stars[i].transform.LookAt(Camera.main.transform);
                stars[i].transform.Rotate(0, 180, 0);
            }
            
            // Проверяем расстояние до цели
            float dist = Vector3.Distance(stars[i].transform.position, target.position);
            
            // Если звезда слишком далеко позади - телепортируем вперёд
            Vector3 toStar = stars[i].transform.position - target.position;
            float dotForward = Vector3.Dot(toStar.normalized, target.forward);
            
            if (dist > spawnRadius * 1.5f || (dist > spawnRadius * 0.5f && dotForward < -0.5f))
            {
                // Телепортируем звезду вперёд
                Vector3 newPos = target.position + target.forward * spawnRadius;
                newPos += Random.insideUnitSphere * spawnRadius * 0.5f;
                stars[i].transform.position = newPos;
            }
        }
        
        lastTargetPos = target.position;
    }
}

/// <summary>
/// Эффект мерцания для звёзд
/// </summary>
public class StarTwinkle : MonoBehaviour
{
    public Color baseColor;
    public Material material;
    
    private float twinkleSpeed;
    private float twinkleOffset;
    
    void Start()
    {
        twinkleSpeed = Random.Range(1f, 4f);
        twinkleOffset = Random.Range(0f, Mathf.PI * 2f);
    }
    
    void Update()
    {
        if (material == null) return;
        
        float twinkle = 0.7f + 0.3f * Mathf.Sin(Time.time * twinkleSpeed + twinkleOffset);
        material.color = baseColor * twinkle;
    }
}

