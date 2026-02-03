using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Клетка с брейнротом. При взаимодействии открывается и спавнит брейнрота.
/// Активность interaction по расстоянию — по образцу PlacementPanel (одна ближайшая Cell, кэш на кадр).
/// </summary>
public class Cell : InteractableObject
{
    [Header("Cell Settings")]
    [Tooltip("Префаб брейнрота, который появится после открытия клетки (или задаётся через SetBrainrotTemplate)")]
    [SerializeField] private GameObject brainrotPrefab;
    
    /// <summary> Шаблон брейнрота, заданный в runtime (приоритетнее brainrotPrefab). </summary>
    private GameObject brainrotTemplate;
    
    [Header("Время открытия клетки")]
    [Tooltip("Промежуток BaseIncome для множителя: центр даёт множитель 1 (например 0–1000 → 500 = 1)")]
    [SerializeField] private long baseIncomeMin = 0L;
    [SerializeField] private long baseIncomeMax = 1000L;
    
    [Tooltip("Базовое время открытия по редкости (сек): Common, Rare, Exclusive, Epic, Mythic, Legendary, Secret")]
    [SerializeField] private float[] baseTimeByRarity = new float[] { 2f, 3f, 4f, 5f, 6f, 7f, 8f };
    
    [Tooltip("Множитель от уровня прокачки скорости открытия: итог = (multiplier*baseTime) / (1 + levelMultiplier*LV)")]
    [SerializeField] private float levelMultiplier = 0.1f;
    
    [Tooltip("Текущий уровень прокачки скорости открытия (LV), в будущем — из прогресса")]
    [SerializeField] private int openingSpeedLevel = 0;
    
    [Tooltip("Минимальное итоговое время открытия (сек)")]
    [SerializeField] private float minOpenTime = 0.5f;
    
    [Tooltip("Максимальное итоговое время открытия (сек)")]
    [SerializeField] private float maxOpenTime = 10f;
    
    private static readonly string[] RarityNames = { "Common", "Rare", "Exclusive", "Epic", "Mythic", "Legendary", "Secret" };
    
    [Header("Interaction")]
    [Tooltip("Смещение точки взаимодействия относительно центра клетки (локальные X, Y, Z)")]
    [SerializeField] private Vector3 rangeOffset = Vector3.zero;
    
    [Header("Brainrot in Cell")]
    [Tooltip("Базовый масштаб брейнрота в клетке (умножается на cellScale из BrainrotObject)")]
    [SerializeField] private float brainrotBaseScale = 1f;
    
    [Tooltip("Смещение брейнрота внутри клетки относительно центра (локальные X, Y, Z)")]
    [SerializeField] private Vector3 brainrotCellOffset = Vector3.zero;
    
    [Tooltip("Дополнительное смещение по Y для спавна брейнрота (мировые координаты)")]
    [SerializeField] private float spawnBrOffsetY = 0f;
    
    [Tooltip("Дополнительное смещение по Y для BR_Info над клеткой (мировые координаты)")]
    [SerializeField] private float brInfoOffsetY = 0f;
    
    [Header("VFX on Open")]
    [Tooltip("Префаб VFX-эффекта при открытии клетки (например Particle System)")]
    [SerializeField] private GameObject vfxEffect;
    
    [Tooltip("Масштаб VFX-эффекта")]
    [SerializeField] private float vfxScale = 1f;
    
    [Tooltip("Смещение VFX относительно центра клетки (локальные X, Y, Z)")]
    [SerializeField] private Vector3 vfxOffset = Vector3.zero;
    
    // Кэш данных брейнрота
    private BrainrotObject cachedBrainrotData;
    private GameObject previewBrainrot;
    /// <summary> Экземпляр BR_Info, созданный и отображаемый самой Cell. </summary>
    private GameObject cellInfoInstance;
    
    // Аниматор игрока (параметр в Animator должен быть ровно "IsOpen" — учитывается регистр)
    private Animator playerAnimator;
    private static readonly int IsOpenHash = Animator.StringToHash("IsOpen");
    
    // Флаг для отслеживания состояния удержания
    private bool wasHolding = false;
    
    // По образцу PlacementPanel: список всех клеток и кэш ближайшей (один раз на кадр)
    private static readonly List<Cell> allCells = new List<Cell>();
    private static Cell cachedClosestCell;
    private static int lastClosestCellUpdateFrame = -1;
    private bool isClosestCell;
    
    private void OnEnable()
    {
        if (!allCells.Contains(this))
            allCells.Add(this);
        ResetClosestCellCache();
    }
    
    private void OnDisable()
    {
        allCells.Remove(this);
        ResetClosestCellCache();
        isClosestCell = false;
    }
    
    private static void ResetClosestCellCache()
    {
        cachedClosestCell = null;
        lastClosestCellUpdateFrame = -1;
    }
    
    /// <summary>
    /// Определяет ближайшую к игроку Cell в радиусе (один раз на кадр, по образцу PlacementPanel).
    /// </summary>
    private void DetermineClosestCell()
    {
        if (lastClosestCellUpdateFrame == Time.frameCount)
        {
            isClosestCell = (cachedClosestCell == this);
            return;
        }
        Transform player = playerTransform;
        if (player == null)
        {
            isClosestCell = false;
            cachedClosestCell = null;
            lastClosestCellUpdateFrame = Time.frameCount;
            return;
        }
        Cell closest = null;
        float closestDist = float.MaxValue;
        float range = GetInteractionRange();
        Vector3 playerPos = player.position;
        for (int i = 0; i < allCells.Count; i++)
        {
            Cell c = allCells[i];
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            Vector3 pos = c.GetInteractionPositionPublic();
            float dist = Vector3.Distance(playerPos, pos);
            float r = c.GetInteractionRange();
            if (dist <= r && dist < closestDist)
            {
                closestDist = dist;
                closest = c;
            }
        }
        cachedClosestCell = closest;
        lastClosestCellUpdateFrame = Time.frameCount;
        isClosestCell = (closest == this);
    }
    
    protected override void Update()
    {
        DetermineClosestCell();
        base.Update();
        if (!isClosestCell)
        {
            FieldInfo isPlayerInRangeField = typeof(InteractableObject).GetField("isPlayerInRange",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (isPlayerInRangeField != null)
                isPlayerInRangeField.SetValue(this, false);
            if (HasUI())
                HideUI();
            return;
        }
    }
    
    private void Awake()
    {
        // Находим игрока и его аниматор
        FindPlayerAnimator();
    }
    
    private void Start()
    {
        // Уровень открытия из GameStorage (сохранения)
        if (GameStorage.Instance != null)
            SetOpeningSpeedLevel(GameStorage.Instance.GetOpeningLevel());
        
        // Не показывать предупреждение, если источник будет задан извне (SetBrainrotTemplate)
        if (GetBrainrotSource() != null)
        {
            InitializeBrainrotFromSource();
        }
    }
    
    /// <summary>
    /// Установить шаблон брейнрота в runtime (например, сгенерированный CellSpawnArea).
    /// Шаблон привязывается к клетке и скрывается — на карте не отображается.
    /// </summary>
    public void SetBrainrotTemplate(GameObject template)
    {
        brainrotTemplate = template;
        if (template != null)
        {
            template.SetActive(false);
            template.transform.SetParent(transform, false);
            template.transform.localPosition = Vector3.zero;
            template.transform.localRotation = Quaternion.identity;
            template.transform.localScale = Vector3.one;
        }
        if (gameObject.activeInHierarchy)
        {
            InitializeBrainrotFromSource();
        }
    }
    
    private void InitializeBrainrotFromSource()
    {
        CreateBrainrotPreview();
        CalculateInteractionTime();
    }
    
    /// <summary>
    /// Возвращает текущий источник брейнрота (template или prefab).
    /// </summary>
    private GameObject GetBrainrotSource()
    {
        return brainrotTemplate != null ? brainrotTemplate : brainrotPrefab;
    }
    
    /// <summary>
    /// Переопределение: точка взаимодействия = центр клетки + rangeOffset.
    /// </summary>
    protected override bool TryGetCustomInteractionPosition(out Vector3 worldPosition)
    {
        Vector3 center = GetCellCenter();
        worldPosition = center + transform.TransformDirection(rangeOffset);
        return true;
    }
    
    /// <summary>
    /// Получает центр клетки (использует центр коллайдера или bounds)
    /// </summary>
    private Vector3 GetCellCenter()
    {
        // Пробуем получить центр через коллайдер
        Collider cellCollider = GetComponent<Collider>();
        if (cellCollider != null)
        {
            return cellCollider.bounds.center;
        }
        
        // Если нет коллайдера, пробуем через Renderer
        Renderer cellRenderer = GetComponentInChildren<Renderer>();
        if (cellRenderer != null)
        {
            return cellRenderer.bounds.center;
        }
        
        // Если ничего не найдено, используем позицию transform
        return transform.position;
    }
    
    /// <summary>
    /// Находит аниматор игрока
    /// </summary>
    private void FindPlayerAnimator()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerAnimator = player.GetComponent<Animator>();
            if (playerAnimator == null)
            {
                playerAnimator = player.GetComponentInChildren<Animator>();
            }
        }
    }
    
    /// <summary>
    /// Создаёт превью брейнрота внутри клетки и BR_Info отображает сама Cell (префаб из брейнрота).
    /// </summary>
    private void CreateBrainrotPreview()
    {
        GameObject source = GetBrainrotSource();
        if (source == null) return;
        
        Vector3 centerPosition = GetCellCenter();
        Vector3 offsetWorld = transform.TransformDirection(brainrotCellOffset);
        previewBrainrot = Instantiate(source, centerPosition + offsetWorld, transform.rotation);
        previewBrainrot.name = "BrainrotPreview";
        previewBrainrot.transform.SetParent(transform, true);
        
        cachedBrainrotData = previewBrainrot.GetComponent<BrainrotObject>();
        if (cachedBrainrotData != null)
        {
            float scale = brainrotBaseScale * cachedBrainrotData.GetCellScale();
            previewBrainrot.transform.localScale = Vector3.one * scale;
            // Отключаем interaction у превью; BR_Info Cell создаёт и показывает сама
            cachedBrainrotData.enabled = false;
        }
        else
        {
            Debug.LogWarning($"[Cell] {gameObject.name}: Префаб не содержит компонент BrainrotObject!");
        }
        
        previewBrainrot.SetActive(true);
        
        // Cell сама берёт префаб BR_Info из брейнрота и отображает его
        if (cachedBrainrotData != null)
            CreateCellInfoFromBrainrot();
    }
    
    /// <summary>
    /// Создаёт экземпляр BR_Info из префаба брейнрота, заполняет данными и вешает на Cell.
    /// </summary>
    private void CreateCellInfoFromBrainrot()
    {
        if (cachedBrainrotData == null) return;
        GameObject prefab = cachedBrainrotData.GetInfoPrefab();
        if (prefab == null) return;
        
        if (cellInfoInstance != null)
        {
            Destroy(cellInfoInstance);
            cellInfoInstance = null;
        }
        
        Vector3 center = GetCellCenter();
        Vector3 pos = center + new Vector3(0f, brInfoOffsetY, 0f);
        cellInfoInstance = Instantiate(prefab, pos, Quaternion.identity);
        cellInfoInstance.name = "CellBR_Info";
        cellInfoInstance.transform.SetParent(transform, true);
        
        InfoPrefabBillboard billboard = cellInfoInstance.GetComponent<InfoPrefabBillboard>();
        if (billboard == null)
            cellInfoInstance.AddComponent<InfoPrefabBillboard>();
        
        cachedBrainrotData.FillInfoPrefabInstance(cellInfoInstance);
    }
    
    /// <summary>
    /// Установить уровень прокачки скорости открытия (из GameStorage или после покупки в магазине).
    /// </summary>
    public void SetOpeningSpeedLevel(int level)
    {
        openingSpeedLevel = Mathf.Max(0, level);
    }
    
    /// <summary>
    /// Обновляет уровень открытия у всех активных Cell в сцене из GameStorage (вызывается после покупки в ShopOpeningManager).
    /// </summary>
    public static void RefreshAllCellsOpeningLevel()
    {
        if (GameStorage.Instance == null) return;
        int level = GameStorage.Instance.GetOpeningLevel();
        Cell[] cells = Object.FindObjectsByType<Cell>(FindObjectsSortMode.None);
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != null)
            {
                cells[i].SetOpeningSpeedLevel(level);
                cells[i].RecalculateInteractionTimeIfInitialized();
            }
        }
    }
    
    /// <summary>
    /// Пересчитывает время взаимодействия, если брейнрот уже инициализирован (для вызова после смены уровня открытия).
    /// </summary>
    private void RecalculateInteractionTimeIfInitialized()
    {
        if (cachedBrainrotData != null)
            CalculateInteractionTime();
    }
    
    /// <summary>
    /// Рассчитывает время открытия: multiplier * baseTime / (1 + levelMultiplier * LV), затем clamp(min, max).
    /// multiplier = baseIncome / midPoint (midPoint = (baseIncomeMin + baseIncomeMax) / 2).
    /// baseTime берётся из baseTimeByRarity по редкости брейнрота.
    /// </summary>
    private void CalculateInteractionTime()
    {
        if (cachedBrainrotData == null)
        {
            Debug.LogWarning($"[Cell] {gameObject.name}: Нет данных брейнрота для расчёта времени!");
            return;
        }
        
        long baseIncome = cachedBrainrotData.GetBaseIncome();
        long midPoint = (baseIncomeMin + baseIncomeMax) / 2;
        if (midPoint <= 0) midPoint = 1;
        float multiplier = (float)baseIncome / midPoint;
        
        string rarity = cachedBrainrotData.GetRarity();
        int rarityIndex = GetRarityIndex(rarity);
        float baseTime = (baseTimeByRarity != null && rarityIndex >= 0 && rarityIndex < baseTimeByRarity.Length)
            ? baseTimeByRarity[rarityIndex]
            : baseTimeByRarity != null && baseTimeByRarity.Length > 0 ? baseTimeByRarity[0] : 2f;
        
        int lv = Mathf.Max(0, openingSpeedLevel);
        float denominator = 1f + levelMultiplier * lv;
        if (denominator <= 0f) denominator = 1f;
        float rawTime = (multiplier * baseTime) / denominator;
        float timeSeconds = Mathf.Clamp(rawTime, minOpenTime, maxOpenTime);
        
        SetInteractionTime(timeSeconds);
    }
    
    private static int GetRarityIndex(string rarity)
    {
        if (string.IsNullOrEmpty(rarity)) return 0;
        string r = rarity.Trim();
        for (int i = 0; i < RarityNames.Length; i++)
            if (string.Equals(r, RarityNames[i], System.StringComparison.OrdinalIgnoreCase))
                return i;
        return 0;
    }
    
    /// <summary>
    /// Переопределяем обработку ввода для управления анимацией IsOpen (клавиатура E и мобильная кнопка).
    /// </summary>
    protected override void HandleInput()
    {
        // Клавиатура E или мобильное удержание (GetCurrentHoldTime() > 0)
        bool isHolding = IsHoldingInteractionKey() || GetCurrentHoldTime() > 0f;
        
        // При начале удержания — включаем анимацию IsOpen
        if (isHolding && !wasHolding)
        {
            if (playerAnimator != null && isPlayerInRange)
            {
                playerAnimator.SetBool(IsOpenHash, true);
            }
        }
        // При отпускании — выключаем анимацию
        else if (!isHolding && wasHolding)
        {
            if (playerAnimator != null)
            {
                playerAnimator.SetBool(IsOpenHash, false);
            }
        }
        
        wasHolding = isHolding;
        
        base.HandleInput();
    }
    
    /// <summary>
    /// Проверяет, удерживается ли кнопка взаимодействия
    /// </summary>
    private bool IsHoldingInteractionKey()
    {
        // Используем тот же метод проверки, что и в базовом классе
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            // Проверяем клавишу E (стандартная клавиша взаимодействия)
            return UnityEngine.InputSystem.Keyboard.current.eKey.isPressed;
        }
        return false;
#else
        return Input.GetKey(KeyCode.E);
#endif
    }
    
    /// <summary>
    /// Вызывается при завершении взаимодействия (клетка открыта)
    /// </summary>
    protected override void OnInteractionComplete()
    {
        // Сбрасываем анимацию
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(IsOpenHash, false);
        }
        
        // Спавним VFX при открытии
        SpawnOpenVFX();
        
        // Спавним свободного брейнрота на месте клетки
        SpawnFreeBrainrot();
        
        // Уничтожаем клетку
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Спавнит VFX-эффект в результате открытия клетки (позиция: центр клетки + vfxOffset, масштаб: vfxScale).
    /// </summary>
    private void SpawnOpenVFX()
    {
        if (vfxEffect == null) return;
        
        Vector3 center = GetCellCenter();
        Vector3 offsetWorld = transform.TransformDirection(vfxOffset);
        Vector3 vfxPosition = center + offsetWorld;
        Quaternion vfxRotation = transform.rotation;
        
        GameObject vfxInstance = Instantiate(vfxEffect, vfxPosition, vfxRotation);
        vfxInstance.transform.localScale = Vector3.one * vfxScale;
        vfxInstance.SetActive(true);
        
        ParticleSystem[] particleSystems = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].Play(true);
        }
    }
    
    /// <summary>
    /// Спавнит свободного брейнрота на месте клетки
    /// </summary>
    private void SpawnFreeBrainrot()
    {
        GameObject source = GetBrainrotSource();
        if (source == null)
        {
            Debug.LogWarning($"[Cell] {gameObject.name}: Префаб/шаблон брейнрота не назначен!");
            return;
        }
        
        Vector3 centerPosition = GetCellCenter();
        Vector3 offsetWorld = transform.TransformDirection(brainrotCellOffset);
        Vector3 spawnPosition = centerPosition + offsetWorld;
        spawnPosition.y += spawnBrOffsetY;
        Quaternion spawnRotation = transform.rotation;
        
        GameObject spawnedBrainrot = Instantiate(source, spawnPosition, spawnRotation);
        spawnedBrainrot.SetActive(true);
        
        // Получаем компонент BrainrotObject
        BrainrotObject brainrotComponent = spawnedBrainrot.GetComponent<BrainrotObject>();
        
        if (brainrotComponent != null)
        {
            // Включаем interaction
            brainrotComponent.enabled = true;
            
            // Копируем данные из превью (если нужно сохранить изменения)
            if (cachedBrainrotData != null)
            {
                // Данные уже в префабе, дополнительное копирование не требуется
            }
            
        }
    }
    
    private void OnDestroy()
    {
        // Убеждаемся, что анимация сброшена при уничтожении
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(IsOpenHash, false);
        }
    }
}
