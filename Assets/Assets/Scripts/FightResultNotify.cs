using UnityEngine;
using TMPro;
using System.Collections;
#if Localization_yg
using YG;
#endif

/// <summary>
/// Уведомление о результате боя с боссом.
/// Показывает анимированный текст победы или поражения.
/// </summary>
public class FightResultNotify : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ссылка на TextMeshProUGUI для отображения текста")]
    [SerializeField] private TextMeshProUGUI resultText;
    
    [Header("Animation Settings")]
    [Tooltip("Время до начала исчезновения (секунды)")]
    [SerializeField] private float displayDuration = 1.7f;
    
    [Tooltip("Длительность анимации пульсации (секунды)")]
    [SerializeField] private float pulseDuration = 0.3f;
    
    [Tooltip("Максимальный масштаб при пульсации")]
    [SerializeField] private float pulseScale = 1.3f;
    
    [Tooltip("Длительность плавного исчезновения (секунды)")]
    [SerializeField] private float fadeDuration = 0.5f;
    
    [Header("Localization")]
    [Tooltip("Текст победы (русский)")]
    [SerializeField] private string victoryTextRu = "ВЫ ПОБЕДИЛИ БОССА!";
    
    [Tooltip("Текст поражения (русский)")]
    [SerializeField] private string defeatTextRu = "БОСС ПОБЕДИЛ ВАС!";
    
    [Tooltip("Текст победы (английский)")]
    [SerializeField] private string victoryTextEn = "YOU DEFEATED THE BOSS!";
    
    [Tooltip("Текст поражения (английский)")]
    [SerializeField] private string defeatTextEn = "THE BOSS DEFEATED YOU!";
    
    [Header("Colors")]
    [Tooltip("Цвет текста победы")]
    [SerializeField] private Color victoryColor = Color.white;
    
    [Tooltip("Цвет текста поражения")]
    [SerializeField] private Color defeatColor = Color.red;
    
    // Кэшированные компоненты
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Coroutine currentAnimation;
    
    private void Awake()
    {
        // Находим TextMeshProUGUI если не назначен
        if (resultText == null)
        {
            resultText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
        
        // Получаем или создаём CanvasGroup для плавного исчезновения
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Кэшируем RectTransform
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null && resultText != null)
        {
            rectTransform = resultText.GetComponent<RectTransform>();
        }
        
        // Сохраняем оригинальный масштаб
        if (rectTransform != null)
        {
            originalScale = rectTransform.localScale;
        }
        else
        {
            originalScale = Vector3.one;
        }
        
        // Скрываем элемент по умолчанию (только alpha, не деактивируем GameObject)
        HideImmediate();
    }
    
    private void Start()
    {
        // Подписываемся на события в Start (BattleManager уже должен быть инициализирован)
        SubscribeToEvents();
    }
    
    private void OnEnable()
    {
        // Пытаемся подписаться при включении
        SubscribeToEvents();
    }
    
    private void OnDisable()
    {
        // Отписываемся от событий
        UnsubscribeFromEvents();
    }
    
    private void OnDestroy()
    {
        // Отписываемся при уничтожении
        UnsubscribeFromEvents();
    }
    
    /// <summary>
    /// Подписывается на события (BattleManager удалён из проекта)
    /// </summary>
    private void SubscribeToEvents()
    {
        // BattleManager удалён из проекта
        // Методы ShowVictory() и ShowDefeat() можно вызывать напрямую
    }
    
    /// <summary>
    /// Отписывается от событий (BattleManager удалён из проекта)
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        // BattleManager удалён из проекта
    }
    
    /// <summary>
    /// Показывает уведомление о победе
    /// </summary>
    public void ShowVictory()
    {
        string text = GetLocalizedVictoryText();
        ShowNotification(text, victoryColor);
    }
    
    /// <summary>
    /// Показывает уведомление о поражении
    /// </summary>
    public void ShowDefeat()
    {
        string text = GetLocalizedDefeatText();
        ShowNotification(text, defeatColor);
    }
    
    /// <summary>
    /// Показывает уведомление с указанным текстом и цветом
    /// </summary>
    private void ShowNotification(string text, Color color)
    {
        // Останавливаем предыдущую анимацию
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        // Устанавливаем текст и цвет
        if (resultText != null)
        {
            resultText.text = text;
            resultText.color = color;
        }
        
        // Запускаем анимацию
        currentAnimation = StartCoroutine(AnimateNotification());
    }
    
    /// <summary>
    /// Корутина анимации уведомления
    /// </summary>
    private IEnumerator AnimateNotification()
    {
        // Показываем элемент
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        
        // Сбрасываем масштаб к начальному
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale * 0.7f;
        }
        
        // Фаза 1: Плавная пульсация с elastic easing
        float elapsed = 0f;
        
        // Единая плавная анимация: 0.7 -> pulseScale -> 1.0
        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pulseDuration);
            
            // Elastic ease out для естественной пульсации
            float scale = EaseOutElastic(t, 0.7f, 1f, pulseScale);
            
            if (rectTransform != null)
            {
                rectTransform.localScale = originalScale * scale;
            }
            
            yield return null;
        }
        
        // Финальный масштаб
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }
        
        // Фаза 2: Ожидание перед исчезновением
        yield return new WaitForSeconds(displayDuration);
        
        // Фаза 3: Плавное исчезновение с ease out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float easeT = EaseOutQuad(t);
            canvasGroup.alpha = 1f - easeT;
            
            yield return null;
        }
        
        // Скрываем элемент
        canvasGroup.alpha = 0f;
        Hide();
        currentAnimation = null;
    }
    
    /// <summary>
    /// Elastic ease out для плавной пульсации
    /// </summary>
    private float EaseOutElastic(float t, float startScale, float endScale, float overshoot)
    {
        if (t <= 0f) return startScale;
        if (t >= 1f) return endScale;
        
        // Упрощённый elastic: быстро к overshoot, затем плавно к endScale
        if (t < 0.4f)
        {
            // Быстрое увеличение до overshoot (ease out)
            float localT = t / 0.4f;
            float eased = 1f - (1f - localT) * (1f - localT);
            return Mathf.Lerp(startScale, overshoot, eased);
        }
        else
        {
            // Плавное уменьшение до endScale (ease in out)
            float localT = (t - 0.4f) / 0.6f;
            float eased = localT < 0.5f 
                ? 2f * localT * localT 
                : 1f - Mathf.Pow(-2f * localT + 2f, 2f) / 2f;
            return Mathf.Lerp(overshoot, endScale, eased);
        }
    }
    
    /// <summary>
    /// Ease out quad
    /// </summary>
    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
    
    /// <summary>
    /// Скрывает уведомление (с сбросом масштаба)
    /// </summary>
    private void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }
    }
    
    /// <summary>
    /// Мгновенно скрывает уведомление (для инициализации)
    /// </summary>
    private void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }
    
    /// <summary>
    /// Получает локализованный текст победы
    /// </summary>
    private string GetLocalizedVictoryText()
    {
        string lang = GetCurrentLanguage();
        
        if (lang == "ru")
        {
            return victoryTextRu;
        }
        
        return victoryTextEn;
    }
    
    /// <summary>
    /// Получает локализованный текст поражения
    /// </summary>
    private string GetLocalizedDefeatText()
    {
        string lang = GetCurrentLanguage();
        
        if (lang == "ru")
        {
            return defeatTextRu;
        }
        
        return defeatTextEn;
    }
    
    /// <summary>
    /// Получает текущий язык
    /// </summary>
    private string GetCurrentLanguage()
    {
#if Localization_yg
        if (YG2.lang != null)
        {
            return YG2.lang;
        }
#endif
        // Пытаемся использовать LocalizationManager если есть
        try
        {
            return LocalizationManager.GetCurrentLanguage();
        }
        catch
        {
            return "ru"; // По умолчанию русский
        }
    }
}
