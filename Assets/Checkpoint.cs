using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [HideInInspector]
    public CheckpointManager manager;
    
    private bool collected = false;
    
    void OnTriggerEnter(Collider other)
    {
        // Проверяем что это корабль и чекпоинт ещё не собран
        if (collected) return;
        
        // Проверяем по тегу или по наличию компонента CynteractShip
        if (other.CompareTag("Player") || other.GetComponent<CynteractShip>() != null)
        {
            collected = true;
            
            if (manager != null)
            {
                manager.OnCheckpointReached();
            }
        }
    }
}

