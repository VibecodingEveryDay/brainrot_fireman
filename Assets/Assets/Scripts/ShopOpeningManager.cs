using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

/// <summary>
/// Менеджер для управления покупками улучшений скорости открытия клеток.
/// Управляет отображением уровня открытия и покупками через Bar1, Bar2, Bar3.
/// </summary>
public class ShopOpeningManager : MonoBehaviour
{
    private const int MAX_LEVEL = 60; // Максимальный уровень скорости открытия
    
    [Header("Opening Upgrade Settings")]
    [Tooltip("Стартовый уровень скорости открытия (устанавливается при первом запуске, если текущий уровень = 0)")]
    [SerializeField] private int startingOpeningLevel = 0;
    
    [Tooltip("Множитель увеличения скорости открытия за уровень (отображается в UI для информации)")]
    [SerializeField] private float openingByLevelScaler = 0.1f;
    
    [Header("Price Settings")]
    [Tooltip("Цены за уровни от 0 до 60. Если цена не указана (-1), будет интерполирована между ближайшими указанными ценами")]
    [SerializeField] private long[] levelPrices;
    
    [Header("References")]
    [Tooltip("Transform, содержащий все Bar объекты (Bar1, Bar2, Bar3)")]
    [SerializeField] private Transform openingBarsContainer;
    
    [Header("Dev Mode")]
    [Tooltip("Режим разработчика: при старте принудительно устанавливает уровень открытия для тестирования")]
    [SerializeField] private bool devMode = false;
    
    [Tooltip("Номер уровня открытия для тестирования (0–60), используется при включённом devMode")]
    [SerializeField] [Range(0, 60)] private int devOpeningLevel = 10;
    
    [Header("Debug")]
    [Tooltip("Показывать отладочные сообщения в консоли")]
    [SerializeField] private bool debug = false;
    
    [System.Serializable]
    public class OpeningBar
    {
        public string barName;
        public Transform barTransform;
        public TextMeshProUGUI openingText1; // Текущий уровень
        public TextMeshProUGUI openingText2; // Будущий уровень
        public TextMeshProUGUI priceText;   // Цена
        public Button button;
        public int openingAmount; // Количество уровней, добавляемых при покупке (1, 5 или 10)
    }
    
    private List<OpeningBar> openingBars = new List<OpeningBar>();
    
    private GameStorage gameStorage;
    
    private double lastBalance = -1;
    private float balanceCheckInterval = 0.1f;
    private float balanceCheckTimer = 0f;
    
    private void Reset()
    {
        levelPrices = new long[61];
        for (int i = 0; i < 61; i++)
            levelPrices[i] = -1;
    }
    
    private void Awake()
    {
        if (openingBarsContainer == null)
        {
            GameObject openingModalContainer = GameObject.Find("OpeningModalContainer");
            if (openingModalContainer != null)
            {
                openingBarsContainer = openingModalContainer.transform;
                if (debug)
                    Debug.Log($"[ShopOpeningManager] OpeningModalContainer найден через GameObject.Find: {openingModalContainer.name}");
            }
            else
            {
                Transform parent = transform.parent;
                if (parent != null && (parent.name == "OpeningModalContainer" || parent.name.Contains("Opening")))
                {
                    openingBarsContainer = parent;
                    if (debug)
                        Debug.Log($"[ShopOpeningManager] Контейнер найден через родительский объект: {parent.name}");
                }
                else
                {
                    openingBarsContainer = transform;
                    if (debug)
                        Debug.Log($"[ShopOpeningManager] Используется текущий transform: {transform.name}");
                }
            }
        }
        
        if (levelPrices == null)
        {
            levelPrices = new long[61];
            for (int i = 0; i < levelPrices.Length; i++)
                levelPrices[i] = -1;
        }
        else if (levelPrices.Length != 61)
        {
            long[] oldPrices = levelPrices;
            levelPrices = new long[61];
            for (int i = 0; i < levelPrices.Length; i++)
                levelPrices[i] = i < oldPrices.Length ? oldPrices[i] : -1;
        }
        
        if (openingBars.Count == 0)
            SetupOpeningBars();
    }
    
    private void Start()
    {
        gameStorage = GameStorage.Instance;
        if (gameStorage == null)
            Debug.LogError("[ShopOpeningManager] GameStorage.Instance не найден!");
        
        if (gameStorage != null)
        {
            if (devMode)
            {
                int level = Mathf.Clamp(devOpeningLevel, 0, MAX_LEVEL);
                gameStorage.SetOpeningLevel(level);
                gameStorage.Save();
                Cell.RefreshAllCellsOpeningLevel();
                if (debug)
                    Debug.Log($"[ShopOpeningManager] DevMode: установлен уровень открытия для теста: {level}");
            }
            else
            {
                int currentLevel = gameStorage.GetOpeningLevel();
                if (currentLevel == 0 && startingOpeningLevel > 0)
                {
                    gameStorage.SetOpeningLevel(startingOpeningLevel);
                    gameStorage.Save();
                    if (debug)
                        Debug.Log($"[ShopOpeningManager] Установлен стартовый уровень открытия: {startingOpeningLevel}");
                    Cell.RefreshAllCellsOpeningLevel();
                }
            }
        }
        
        UpdateAllOpeningBars();
        if (gameStorage != null)
            lastBalance = gameStorage.GetBalanceDouble();
    }
    
    private void OnEnable()
    {
        if (gameStorage != null)
            UpdateAllOpeningBars();
    }
    
    private void Update()
    {
        if (openingBarsContainer != null && openingBarsContainer.gameObject.activeInHierarchy)
        {
            balanceCheckTimer += Time.deltaTime;
            if (balanceCheckTimer >= balanceCheckInterval)
            {
                balanceCheckTimer = 0f;
                if (gameStorage == null)
                    gameStorage = GameStorage.Instance;
                if (gameStorage != null)
                {
                    double currentBalance = gameStorage.GetBalanceDouble();
                    if (lastBalance < 0 || Math.Abs(currentBalance - lastBalance) > 0.0001)
                    {
                        lastBalance = currentBalance;
                        UpdateAllOpeningBars();
                    }
                }
            }
        }
        else
            balanceCheckTimer = 0f;
    }
    
    private void SetupOpeningBars()
    {
        Transform bar1 = FindChildByName(openingBarsContainer, "Bar1") ?? FindChildByName(openingBarsContainer, "bar1");
        Transform bar2 = FindChildByName(openingBarsContainer, "Bar2") ?? FindChildByName(openingBarsContainer, "bar2");
        Transform bar3 = FindChildByName(openingBarsContainer, "Bar3") ?? FindChildByName(openingBarsContainer, "bar3");
        
        openingBars.Clear();
        
        if (bar1 != null)
            openingBars.Add(CreateOpeningBar(bar1, "Bar1", 1));
        if (bar2 != null)
            openingBars.Add(CreateOpeningBar(bar2, "Bar2", 5));
        if (bar3 != null)
            openingBars.Add(CreateOpeningBar(bar3, "Bar3", 10));
        
        if (openingBars.Count == 0 && debug)
            Debug.LogWarning("[ShopOpeningManager] Не найдено ни одного Bar объекта (Bar1, Bar2, Bar3)!");
    }
    
    private OpeningBar CreateOpeningBar(Transform barTransform, string name, int openingAmount)
    {
        OpeningBar bar = new OpeningBar
        {
            barName = name,
            barTransform = barTransform,
            openingAmount = openingAmount
        };
        
        bar.openingText1 = FindChildComponent<TextMeshProUGUI>(barTransform, "OpeningText1");
        bar.openingText2 = FindChildComponent<TextMeshProUGUI>(barTransform, "OpeningText2");
        bar.priceText = FindChildComponent<TextMeshProUGUI>(barTransform, "Price") ??
                        FindChildComponent<TextMeshProUGUI>(barTransform, "price");
        
        bar.button = FindChildComponent<Button>(barTransform, "Button");
        if (bar.button != null)
        {
            bar.button.onClick.RemoveAllListeners();
            int amountCopy = openingAmount;
            bar.button.onClick.AddListener(() => OnBuyOpeningButtonClicked(amountCopy));
        }
        
        return bar;
    }
    
    private Transform FindChildByName(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindChildByName(child, name);
            if (found != null) return found;
        }
        return null;
    }
    
    private T FindChildComponent<T>(Transform parent, string name) where T : Component
    {
        Transform child = FindChildByName(parent, name);
        return child != null ? child.GetComponent<T>() : null;
    }
    
    public void UpdateAllOpeningBars()
    {
        if (gameStorage == null)
            gameStorage = GameStorage.Instance;
        if (gameStorage == null) return;
        
        int currentLevel = gameStorage.GetOpeningLevel();
        float currentOpening = CalculateOpeningSpeed(currentLevel);
        
        foreach (OpeningBar bar in openingBars)
            UpdateOpeningBar(bar, currentLevel, currentOpening);
    }
    
    private void UpdateOpeningBar(OpeningBar bar, int currentLevel, float currentOpening)
    {
        bool isMaxLevel = currentLevel >= MAX_LEVEL;
        int futureLevel = currentLevel + bar.openingAmount;
        int actualLevelsToAdd = bar.openingAmount;
        if (futureLevel > MAX_LEVEL)
        {
            futureLevel = MAX_LEVEL;
            actualLevelsToAdd = MAX_LEVEL - currentLevel;
        }
        
        long price = 0;
        if (!isMaxLevel && actualLevelsToAdd > 0)
            price = CalculatePriceForLevels(currentLevel, actualLevelsToAdd);
        
        if (bar.openingText1 != null)
            bar.openingText1.text = currentLevel.ToString();
        if (bar.openingText2 != null)
            bar.openingText2.text = futureLevel.ToString();
        
        if (bar.priceText != null)
        {
            if (isMaxLevel)
                bar.priceText.text = "Макс. ур";
            else
            {
                bar.priceText.text = gameStorage != null ? gameStorage.FormatBalance((double)price) : price.ToString();
                if (gameStorage != null && price > 0)
                {
                    double balance = gameStorage.GetBalanceDouble();
                    bar.priceText.color = balance < (double)price ? Color.red : Color.white;
                }
                else if (price == 0)
                    bar.priceText.color = Color.white;
            }
        }
        
        if (bar.button != null)
        {
            if (isMaxLevel)
                bar.button.interactable = false;
            else if (gameStorage != null && price > 0)
            {
                double balance = gameStorage.GetBalanceDouble();
                bar.button.interactable = balance >= (double)price && actualLevelsToAdd > 0;
            }
            else
                bar.button.interactable = false;
        }
    }
    
    private long GetPriceForLevel(int level)
    {
        if (levelPrices == null || levelPrices.Length == 0) return 0;
        if (level < 0) level = 0;
        if (level >= levelPrices.Length) level = levelPrices.Length - 1;
        
        if (levelPrices[level] >= 0) return levelPrices[level];
        
        int lowerLevel = -1, upperLevel = -1;
        for (int i = level - 1; i >= 0; i--)
            if (levelPrices[i] >= 0) { lowerLevel = i; break; }
        for (int i = level + 1; i < levelPrices.Length; i++)
            if (levelPrices[i] >= 0) { upperLevel = i; break; }
        
        if (lowerLevel >= 0 && upperLevel >= 0)
        {
            double t = (double)(level - lowerLevel) / (upperLevel - lowerLevel);
            double interpolated = (double)levelPrices[lowerLevel] + ((double)levelPrices[upperLevel] - levelPrices[lowerLevel]) * t;
            return (long)Math.Round(interpolated);
        }
        if (lowerLevel >= 0) return levelPrices[lowerLevel];
        if (upperLevel >= 0) return levelPrices[upperLevel];
        return 0;
    }
    
    private long CalculatePriceForLevels(int currentLevel, int levelsToAdd)
    {
        long totalPrice = 0;
        for (int i = 0; i < levelsToAdd; i++)
            totalPrice += GetPriceForLevel(currentLevel + i);
        return totalPrice;
    }
    
    /// <summary>
    /// Вычисляет множитель скорости открытия на основе уровня (для отображения в UI при необходимости).
    /// </summary>
    private float CalculateOpeningSpeed(int level)
    {
        return 1f + openingByLevelScaler * Mathf.Max(0, level);
    }
    
    private void OnBuyOpeningButtonClicked(int openingAmount)
    {
        if (gameStorage == null)
        {
            Debug.LogError("[ShopOpeningManager] GameStorage недоступен!");
            return;
        }
        
        int currentLevel = gameStorage.GetOpeningLevel();
        if (currentLevel >= MAX_LEVEL) return;
        
        int actualLevelsToAdd = openingAmount;
        int futureLevel = currentLevel + openingAmount;
        if (futureLevel > MAX_LEVEL)
            actualLevelsToAdd = MAX_LEVEL - currentLevel;
        
        long price = CalculatePriceForLevels(currentLevel, actualLevelsToAdd);
        double balance = gameStorage.GetBalanceDouble();
        if (balance < (double)price) return;
        
        bool success = gameStorage.SubtractBalanceLong(price);
        if (success)
        {
            gameStorage.IncreaseOpeningLevel(actualLevelsToAdd);
            gameStorage.Save();
            Cell.RefreshAllCellsOpeningLevel();
            UpdateAllOpeningBars();
            if (debug)
                Debug.Log($"[ShopOpeningManager] Уровень открытия увеличен на {actualLevelsToAdd}, новый уровень: {gameStorage.GetOpeningLevel()}");
        }
    }
}
