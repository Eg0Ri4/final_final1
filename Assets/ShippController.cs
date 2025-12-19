using UnityEngine;

public class ShipPadControl : MonoBehaviour
{
    [Header("Настройки полета")]
    public float flySpeed = 20f;       // Скорость движения вперед
    public float turnSpeed = 60f;      // Скорость поворота
    public float deadZone = 0.1f;      // "Мертвая зона" в центре (чтобы не дрожало)

    void Start()
    {
        // В этом режиме курсор ДОЛЖЕН быть виден, не блокируем его
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        // 1. Постоянный полет вперед
        transform.Translate(Vector3.forward * flySpeed * Time.deltaTime);

        // 2. Вычисляем положение мыши относительно центра экрана
        // (0,0) - это центр экрана. (-1, -1) - левый низ, (1, 1) - правый верх.
        Vector2 centerScreen = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 mousePos = (Vector2)Input.mousePosition - centerScreen;

        // Нормализуем значения от -1 до 1
        float inputX = mousePos.x / (Screen.width / 2f);
        float inputY = mousePos.y / (Screen.height / 2f);

        // 3. Поворот (если вышли за пределы мертвой зоны в центре)
        if (Mathf.Abs(inputX) > deadZone)
        {
            float rotAmount = inputX * turnSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up * rotAmount, Space.Self);
        }

        if (Mathf.Abs(inputY) > deadZone)
        {
            // Для инверсии (как в самолете) поставь знак минус перед inputY
            float rotAmount = -inputY * turnSpeed * Time.deltaTime; 
            transform.Rotate(Vector3.right * rotAmount, Space.Self);
        }
        
        // 4. (Опционально) Наклон корпуса при повороте (Roll)
        // Это добавляет эффект "виража"
        float targetRoll = -inputX * 30f; // Угол наклона
        float currentRoll = transform.localEulerAngles.z;
        // Преобразуем угол в формат -180...180 для плавной интерполяции
        if (currentRoll > 180) currentRoll -= 360;
        
        float newRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * 2f);
        transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, newRoll);
    }
    
    // Рисуем прицел в центре для удобства (видно только в редакторе Unity)
    void OnGUI()
    {
        GUI.Box(new Rect(Screen.width/2 - 2, Screen.height/2 - 2, 4, 4), "");
    }
}