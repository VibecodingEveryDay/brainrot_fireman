using UnityEngine;

/// <summary>
/// Триггер зоны башни. Вешается на объект с Collider (isTrigger).
/// При входе/выходе игрока уведомляет TeleportManager.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TowerZoneTrigger : MonoBehaviour
{
    [Tooltip("Тег игрока")]
    [SerializeField] private string playerTag = "Player";
    
    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        
        if (TeleportManager.Instance != null)
            TeleportManager.Instance.SetPlayerInTowerZone(true);
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetPlayerInFightZone(true);
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        
        string carriedBrainrotName = null;
        PlayerCarryController carry = other.GetComponent<PlayerCarryController>();
        if (carry == null)
            carry = other.GetComponentInChildren<PlayerCarryController>();
        if (carry != null)
        {
            BrainrotObject carried = carry.GetCurrentCarriedObject();
            if (carried != null)
                carriedBrainrotName = carried.GetObjectName();
        }
        
        if (TeleportManager.Instance != null)
        {
            TeleportManager.Instance.OnPlayerExitedTowerZoneWithBrainrot(carriedBrainrotName);
            TeleportManager.Instance.SetPlayerInTowerZone(false);
        }
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetPlayerInFightZone(false);
    }
}
