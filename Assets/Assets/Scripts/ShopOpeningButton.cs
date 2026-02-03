using UnityEngine;
using System.Collections;

/// <summary>
/// Скрипт для 3D кнопки открытия магазина скорости открытия клеток.
/// При наступлении игрока на кнопку показывает OpeningModalContainer.
/// При уходе игрока скрывает модальное окно.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShopOpeningButton : MonoBehaviour
{
    [Header("References")]
    [Tooltip("OpeningModalContainer GameObject, который будет показан при наступлении на кнопку")]
    [SerializeField] private GameObject openingModalContainer;
    
    [Header("Settings")]
    [Tooltip("Скрывать OpeningModalContainer при старте (если true, контейнер будет скрыт в Start)")]
    [SerializeField] private bool hideOnStart = true;
    
    [Header("Player Detection")]
    [Tooltip("Тег игрока (по умолчанию 'Player')")]
    [SerializeField] private string playerTag = "Player";
    
    private bool isPlayerOnButton = false;
    
    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }
    
    private void Start()
    {
        if (openingModalContainer == null)
        {
            GameObject foundContainer = GameObject.Find("OpeningModalContainer");
            if (foundContainer == null)
            {
                var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                GameObject[] rootObjects = currentScene.GetRootGameObjects();
                foundContainer = FindGameObjectInHierarchy(rootObjects, "OpeningModalContainer");
            }
            if (foundContainer == null)
            {
                ShopOpeningManager openingManager = FindFirstObjectByType<ShopOpeningManager>();
                if (openingManager != null)
                {
                    Transform parent = openingManager.transform;
                    while (parent != null)
                    {
                        if (parent.name.Contains("Opening") || parent.name.Contains("Modal") || parent.name.Contains("Container"))
                        {
                            foundContainer = parent.gameObject;
                            break;
                        }
                        parent = parent.parent;
                    }
                    if (foundContainer == null && openingManager.transform.parent != null)
                        foundContainer = openingManager.transform.parent.gameObject;
                }
            }
            if (foundContainer != null)
                openingModalContainer = foundContainer;
            else
                Debug.LogError("[ShopOpeningButton] OpeningModalContainer не найден! Назначьте его вручную в инспекторе или создайте объект с именем 'OpeningModalContainer'.");
        }
        
        if (openingModalContainer != null && hideOnStart)
            openingModalContainer.SetActive(false);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerOnButton = true;
            OpenShop();
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerOnButton = false;
            CloseShop();
        }
    }
    
    public void OpenShop()
    {
        if (openingModalContainer == null) return;
        
        Transform parent = openingModalContainer.transform.parent;
        if (parent != null && !parent.gameObject.activeSelf)
            parent.gameObject.SetActive(true);
        
        openingModalContainer.SetActive(true);
        StartCoroutine(UpdateLocalizationDelayed());
    }
    
    private IEnumerator UpdateLocalizationDelayed()
    {
        yield return new WaitForEndOfFrame();
        
        if (openingModalContainer != null && openingModalContainer.activeSelf)
        {
            LocalizedText[] localizedTexts = openingModalContainer.GetComponentsInChildren<LocalizedText>(true);
            foreach (LocalizedText lt in localizedTexts)
            {
                if (lt != null)
                    lt.UpdateText();
            }
            
            ShopOpeningManager openingManager = openingModalContainer.GetComponentInChildren<ShopOpeningManager>();
            if (openingManager != null)
                openingManager.UpdateAllOpeningBars();
        }
    }
    
    public void CloseShop()
    {
        if (openingModalContainer != null)
            openingModalContainer.SetActive(false);
    }
    
    private GameObject FindGameObjectInHierarchy(GameObject[] rootObjects, string name)
    {
        foreach (GameObject root in rootObjects)
        {
            GameObject found = FindGameObjectInTransform(root.transform, name);
            if (found != null) return found;
        }
        return null;
    }
    
    private GameObject FindGameObjectInTransform(Transform parent, string name)
    {
        if (parent.name == name) return parent.gameObject;
        foreach (Transform child in parent)
        {
            GameObject found = FindGameObjectInTransform(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
