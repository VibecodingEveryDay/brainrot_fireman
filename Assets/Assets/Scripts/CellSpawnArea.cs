using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Спавнит N клеток с сгенерированными брейнротами в зоне вокруг spawnCellAreaPos.
/// Брейнроты загружаются из Resources/game/Brainrots/. Редкость и baseIncome задаются параметрами.
/// </summary>
public class CellSpawnArea : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Префаб Cell (например, Assets/Assets/Prefabs/Cell/Cell.prefab — перетащить в инспектор)")]
    [SerializeField] private GameObject cellPrefab;
    
    [Header("Spawn Area")]
    [Tooltip("Центр зоны спавна в МИРОВЫХ координатах (X,Y,Z). Клетки спавнятся на плоскости через эту точку. Настраивается визуально по Gizmos.")]
    [SerializeField] private Vector3 spawnCellAreaPos = Vector3.zero;
    
    [Tooltip("Половина размера зоны по X (мировые единицы, вдоль Right объекта)")]
    [SerializeField] private float spawnAreaHalfExtentX = 2.5f;
    
    [Tooltip("Половина размера зоны по Z (мировые единицы, вдоль Forward объекта)")]
    [SerializeField] private float spawnAreaHalfExtentZ = 2.5f;
    
    [Header("Spawn")]
    [Tooltip("Количество клеток для спавна")]
    [SerializeField] private int cellCount = 5;
    
    [Tooltip("Минимальное расстояние между клетками (в мировых единицах)")]
    [SerializeField] private float minSpacing = 1f;
    
    [Tooltip("Максимум попыток подобрать позицию с отступом от других клеток")]
    [SerializeField] private int maxPlacementAttempts = 50;
    
    [Header("Base Income (для сгенерированных брейнротов)")]
    [SerializeField] private long baseIncomeMin = 100;
    [SerializeField] private long baseIncomeMax = 1000;
    
    [Header("Rarity Chances (0 = 0%, -1 = уравнять остаток до 100% поровну)")]
    [Tooltip("Порядок: Common, Rare, Exclusive, Epic, Mythic, Legendary, Secret")]
    [SerializeField] private float[] rarityChances = new float[] { 70f, 20f, 7f, 2f, 0.5f, 0.4f, 0.1f };
    
    private static readonly string[] RarityNames = { "Common", "Rare", "Exclusive", "Epic", "Mythic", "Legendary", "Secret" };
    
    private GameObject[] brainrotPrefabs;
    private DestroyFireManager destroyFireManager;
    
    private void Awake()
    {
        LoadBrainrotPrefabs();
    }
    
    private void OnDrawGizmos()
    {
        Vector3 centerWorld = spawnCellAreaPos;
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.6f);
        Gizmos.DrawSphere(centerWorld, 0.15f);
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        Vector3 size = new Vector3(spawnAreaHalfExtentX * 2f, 0.01f, spawnAreaHalfExtentZ * 2f);
        Gizmos.matrix = Matrix4x4.TRS(centerWorld, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.matrix = Matrix4x4.identity;
    }
    
    private void Start()
    {
        destroyFireManager = FindFirstObjectByType<DestroyFireManager>();
        if (destroyFireManager != null)
        {
            destroyFireManager.OnProgressComplete += OnDestroyFireProgressComplete;
            destroyFireManager.OnProgressChanged += OnDestroyFireProgressChanged;
        }
        SpawnCells();
    }
    
    private void OnDestroy()
    {
        if (destroyFireManager != null)
        {
            destroyFireManager.OnProgressComplete -= OnDestroyFireProgressComplete;
            destroyFireManager.OnProgressChanged -= OnDestroyFireProgressChanged;
        }
    }
    
    private void OnDestroyFireProgressComplete()
    {
        RemoveAllCells();
    }
    
    private void OnDestroyFireProgressChanged(float progress)
    {
        if (progress <= 0f)
            SpawnCells();
    }
    
    /// <summary>
    /// Удаляет все Cell на карте (вызывается при 100% прогресса огня).
    /// </summary>
    private void RemoveAllCells()
    {
        Cell[] cells = FindObjectsByType<Cell>(FindObjectsSortMode.None);
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != null && cells[i].gameObject != null)
                Destroy(cells[i].gameObject);
        }
    }
    
    /// <summary>
    /// Загружает префабы брейнротов из Resources/game/Brainrots/
    /// </summary>
    private void LoadBrainrotPrefabs()
    {
        GameObject[] all = Resources.LoadAll<GameObject>("game/Brainrots");
        List<GameObject> list = new List<GameObject>();
        for (int i = 0; i < (all != null ? all.Length : 0); i++)
        {
            GameObject go = all[i];
            if (go != null && go.GetComponent<BrainrotObject>() != null)
            {
                list.Add(go);
            }
        }
        brainrotPrefabs = list.ToArray();
        
        if (brainrotPrefabs == null || brainrotPrefabs.Length == 0)
        {
            Debug.LogWarning("[CellSpawnArea] Не найдены префабы брейнротов в Resources/game/Brainrots/");
        }
    }
    
    /// <summary>
    /// Интерполирует шансы редкостей: 0 остаётся 0, -1 заменяется на долю недостающего до 100%.
    /// </summary>
    private float[] GetInterpolatedRarityChances()
    {
        int n = Mathf.Min(7, rarityChances != null ? rarityChances.Length : 0);
        float[] result = new float[7];
        float sumPositive = 0f;
        int minusOneCount = 0;
        
        for (int i = 0; i < 7; i++)
        {
            float v = (i < n && rarityChances != null) ? rarityChances[i] : 0f;
            if (v > 0f)
            {
                result[i] = v;
                sumPositive += v;
            }
            else if (v < 0f) // -1
            {
                result[i] = -1f;
                minusOneCount++;
            }
            else
            {
                result[i] = 0f;
            }
        }
        
        float remainder = 100f - sumPositive;
        float share = (minusOneCount > 0 && remainder > 0f) ? (remainder / minusOneCount) : 0f;
        
        for (int i = 0; i < 7; i++)
        {
            if (result[i] < 0f)
            {
                result[i] = share;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Выбирает редкость по взвешенному случайному (массив шансов в %).
    /// </summary>
    private string PickRarityByChances(float[] chances)
    {
        float sum = 0f;
        foreach (float c in chances)
        {
            sum += Mathf.Max(0f, c);
        }
        if (sum <= 0f)
        {
            return RarityNames[0];
        }
        float roll = Random.Range(0f, sum);
        for (int i = 0; i < 7; i++)
        {
            float w = Mathf.Max(0f, chances[i]);
            if (roll < w)
            {
                return RarityNames[i];
            }
            roll -= w;
        }
        return RarityNames[6];
    }
    
    /// <summary>
    /// Случайная точка в зоне спавна. Центр = spawnCellAreaPos (мировые координаты).
    /// Смещение по плоскости (Right/Forward объекта) в пределах halfExtent.
    /// </summary>
    private Vector3 GetRandomPointInSpawnArea()
    {
        Vector3 centerWorld = spawnCellAreaPos;
        Vector3 offset = transform.right * Random.Range(-spawnAreaHalfExtentX, spawnAreaHalfExtentX)
                       + transform.forward * Random.Range(-spawnAreaHalfExtentZ, spawnAreaHalfExtentZ);
        return centerWorld + offset;
    }
    
    /// <summary>
    /// Пытается получить случайную позицию на плоскости с отступом от уже размещённых клеток.
    /// </summary>
    private bool TryGetSpawnPosition(List<Vector3> placedWorldPositions, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            Vector3 candidate = GetRandomPointInSpawnArea();
            bool tooClose = false;
            for (int i = 0; i < placedWorldPositions.Count; i++)
            {
                if (Vector3.Distance(candidate, placedWorldPositions[i]) < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose)
            {
                placedWorldPositions.Add(candidate);
                worldPosition = candidate;
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Создаёт один сгенерированный брейнрот (шаблон) и возвращает GameObject.
    /// </summary>
    private GameObject CreateBrainrotTemplate()
    {
        if (brainrotPrefabs == null || brainrotPrefabs.Length == 0)
        {
            return null;
        }
        
        GameObject prefab = brainrotPrefabs[Random.Range(0, brainrotPrefabs.Length)];
        GameObject template = Instantiate(prefab);
        template.name = "BrainrotTemplate_" + prefab.name;
        
        BrainrotObject br = template.GetComponent<BrainrotObject>();
        if (br == null)
        {
            Destroy(template);
            return null;
        }
        
        long baseIncome = (long)Random.Range((float)baseIncomeMin, (float)baseIncomeMax + 1f);
        br.SetBaseIncome(baseIncome);
        br.SetLevel(0);
        
        float[] chances = GetInterpolatedRarityChances();
        string rarity = PickRarityByChances(chances);
        br.SetRarity(rarity);
        
        template.SetActive(false);
        return template;
    }
    
    /// <summary>
    /// Спавнит N клеток на площади.
    /// </summary>
    private void SpawnCells()
    {
        if (cellPrefab == null)
        {
            Debug.LogWarning("[CellSpawnArea] Префаб Cell не назначен!");
            return;
        }
        
        if (brainrotPrefabs == null || brainrotPrefabs.Length == 0)
        {
            Debug.LogWarning("[CellSpawnArea] Нет префабов брейнротов — клетки не созданы.");
            return;
        }
        
        List<Vector3> placedWorldPositions = new List<Vector3>(cellCount);
        
        for (int i = 0; i < cellCount; i++)
        {
            GameObject brainrotTemplate = CreateBrainrotTemplate();
            if (brainrotTemplate == null)
            {
                continue;
            }
            
            if (!TryGetSpawnPosition(placedWorldPositions, out Vector3 worldPos))
            {
                Destroy(brainrotTemplate);
                continue;
            }
            
            Quaternion worldRot = transform.rotation;
            GameObject cellGo = Instantiate(cellPrefab, worldPos, worldRot);
            cellGo.name = "Cell_" + i;
            cellGo.SetActive(false);
            
            Cell cell = cellGo.GetComponent<Cell>();
            if (cell != null)
            {
                cell.SetBrainrotTemplate(brainrotTemplate);
            }
            else
            {
                Destroy(brainrotTemplate);
            }
            
            cellGo.SetActive(true);
        }
    }
}
