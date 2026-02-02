using UnityEngine;
using System.Collections;

/// <summary>
/// Скрипт для 3D кнопки открытия магазина разблокировки лестниц
/// При наступлении игрока на кнопку показывает LocksModalContainer
/// При уходе игрока скрывает модальное окно
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopLockButton : MonoBehaviour
{
    [Header("References")]
    [Tooltip("LocksModalContainer GameObject, который будет показан при наступлении на кнопку")]
    [SerializeField] private GameObject locksModalContainer;
    
    [Header("Settings")]
    [Tooltip("Скрывать LocksModalContainer при старте (если true, контейнер будет скрыт в Start)")]
    [SerializeField] private bool hideOnStart = true;
    
    [Header("Player Detection")]
    [Tooltip("Тег игрока (по умолчанию 'Player')")]
    [SerializeField] private string playerTag = "Player";
    
    // Флаг для отслеживания, находится ли игрок на кнопке
    private bool isPlayerOnButton = false;
    
    private void Awake()
    {
        // Проверяем наличие триггера
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (!col.isTrigger)
            {
                col.isTrigger = true;
            }
        }
    }
    
    private void Start()
    {
        // Автоматически находим LocksModalContainer, если не назначен
        if (locksModalContainer == null)
        {
            GameObject foundContainer = GameObject.Find("LocksModalContainer");
            
            if (foundContainer == null)
            {
                UnityEngine.SceneManagement.Scene currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                GameObject[] rootObjects = currentScene.GetRootGameObjects();
                
                foundContainer = FindGameObjectInHierarchy(rootObjects, "LocksModalContainer");
            }
            
            if (foundContainer == null)
            {
                ShopLockManager lockManager = FindFirstObjectByType<ShopLockManager>();
                if (lockManager != null)
                {
                    Transform parent = lockManager.transform;
                    while (parent != null)
                    {
                        if (parent.name.Contains("Lock") || parent.name.Contains("Modal") || parent.name.Contains("Container"))
                        {
                            foundContainer = parent.gameObject;
                            break;
                        }
                        parent = parent.parent;
                    }
                    
                    if (foundContainer == null && lockManager.transform.parent != null)
                    {
                        foundContainer = lockManager.transform.parent.gameObject;
                    }
                }
            }
            
            if (foundContainer != null)
            {
                locksModalContainer = foundContainer;
                Debug.Log($"[ShopLockButton] LocksModalContainer найден автоматически: {foundContainer.name}");
            }
            else
            {
                Debug.LogError("[ShopLockButton] LocksModalContainer не найден! Убедитесь, что объект существует в сцене.");
            }
        }
        
        // Скрываем контейнер при старте, если требуется
        if (locksModalContainer != null && hideOnStart)
        {
            locksModalContainer.SetActive(false);
        }
    }
    
    /// <summary>
    /// Вызывается когда объект входит в триггер
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerOnButton = true;
            OpenShop();
        }
    }
    
    /// <summary>
    /// Вызывается когда объект выходит из триггера
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerOnButton = false;
            CloseShop();
        }
    }
    
    /// <summary>
    /// Публичный метод для открытия магазина
    /// </summary>
    public void OpenShop()
    {
        if (locksModalContainer != null)
        {
            Transform parent = locksModalContainer.transform.parent;
            if (parent != null && !parent.gameObject.activeSelf)
            {
                parent.gameObject.SetActive(true);
            }
            
            locksModalContainer.SetActive(true);
            
            StartCoroutine(UpdateLocalizationDelayed());
        }
    }
    
    /// <summary>
    /// Обновляет все LocalizedText компоненты в модальном окне с небольшой задержкой
    /// </summary>
    private IEnumerator UpdateLocalizationDelayed()
    {
        yield return new WaitForEndOfFrame();
        
        if (locksModalContainer != null && locksModalContainer.activeSelf)
        {
            LocalizedText[] localizedTexts = locksModalContainer.GetComponentsInChildren<LocalizedText>(true);
            
            foreach (LocalizedText localizedText in localizedTexts)
            {
                if (localizedText != null)
                {
                    localizedText.UpdateText();
                }
            }
            
            ShopLockManager lockManager = locksModalContainer.GetComponentInChildren<ShopLockManager>();
            if (lockManager != null)
            {
                lockManager.UpdateAllLockBars();
            }
        }
    }
    
    /// <summary>
    /// Публичный метод для закрытия магазина
    /// </summary>
    public void CloseShop()
    {
        if (locksModalContainer != null)
        {
            locksModalContainer.SetActive(false);
        }
    }
    
    /// <summary>
    /// Рекурсивно ищет GameObject по имени в иерархии
    /// </summary>
    private GameObject FindGameObjectInHierarchy(GameObject[] rootObjects, string name)
    {
        foreach (GameObject root in rootObjects)
        {
            GameObject found = FindGameObjectInTransform(root.transform, name);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Рекурсивно ищет GameObject по имени в Transform и его дочерних объектах
    /// </summary>
    private GameObject FindGameObjectInTransform(Transform parent, string name)
    {
        if (parent.name == name)
        {
            return parent.gameObject;
        }
        
        foreach (Transform child in parent)
        {
            GameObject found = FindGameObjectInTransform(child, name);
            if (found != null)
            {
                return found;
            }
        }
        
        return null;
    }
}
