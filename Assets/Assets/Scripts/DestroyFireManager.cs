using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Менеджер прогресса разрушения башни от огня.
/// Каждые n секунд (рандом) прибавляет x (рандом) к шкале прогресса.
/// Когда прогресс достигает 100% — башня разрушена.
/// </summary>
public class DestroyFireManager : MonoBehaviour
{
    [Header("Progress Bar")]
    [Tooltip("Image прогресс бара (Filled Horizontal)")]
    [SerializeField] private Image progressBar;
    
    [Tooltip("Длительность плавной анимации заполнения до целевого значения (сек). 0 = без анимации, мгновенное обновление")]
    [SerializeField] private float progressBarFillDuration = 0.3f;
    
    [Header("Time Settings")]
    [Tooltip("Минимальный интервал между добавлениями прогресса (секунды)")]
    [SerializeField] private float minInterval = 2f;
    
    [Tooltip("Максимальный интервал между добавлениями прогресса (секунды)")]
    [SerializeField] private float maxInterval = 4f;
    
    [Header("Progress Settings")]
    [Tooltip("Минимальное добавление к прогрессу за раз (0-1, где 1 = 100%)")]
    [SerializeField] private float minProgressAdd = 0.01f;
    
    [Tooltip("Максимальное добавление к прогрессу за раз (0-1, где 1 = 100%)")]
    [SerializeField] private float maxProgressAdd = 0.05f;
    
    [Header("Fire VFX Groups")]
    [Tooltip("Группа VFX огня для >20% прогресса")]
    [SerializeField] private GameObject fireGroup1;
    
    [Tooltip("Группа VFX огня для >40% прогресса")]
    [SerializeField] private GameObject fireGroup2;
    
    [Tooltip("Группа VFX огня для >60% прогресса")]
    [SerializeField] private GameObject fireGroup3;
    
    [Tooltip("Группа VFX огня для >80% прогресса")]
    [SerializeField] private GameObject fireGroup4;
    
    [Tooltip("Группа VFX огня для падения башни (100%)")]
    [SerializeField] private GameObject fireGroup5;
    
    [Header("Hide During Collapse")]
    [Tooltip("Группа текстов этажей (скрывается во время падения)")]
    [SerializeField] private GameObject floorNumbersTexts;
    
    [Header("Camera Shake Settings")]
    [Tooltip("Интенсивность тряски на 20%")]
    [SerializeField] private float shakeIntensity20 = 0.03f;
    
    [Tooltip("Интенсивность тряски на 40%")]
    [SerializeField] private float shakeIntensity40 = 0.05f;
    
    [Tooltip("Интенсивность тряски на 60%")]
    [SerializeField] private float shakeIntensity60 = 0.08f;
    
    [Tooltip("Интенсивность тряски на 80%")]
    [SerializeField] private float shakeIntensity80 = 0.12f;
    
    [Tooltip("Интенсивность тряски на 100%")]
    [SerializeField] private float shakeIntensity100 = 0.25f;
    
    [Tooltip("Интенсивность тряски на 2 этапе падения верхушки")]
    [SerializeField] private float shakeIntensityTopStage2 = 0.15f;
    
    [Tooltip("Интенсивность тряски на 3 этапе падения верхушки")]
    [SerializeField] private float shakeIntensityTopStage3 = 0.2f;
    
    [Tooltip("Длительность тряски")]
    [SerializeField] private float shakeDuration = 0.5f;
    
    [Header("Shake Sound")]
    [Tooltip("Звук при тряске камеры (проигрывается при каждом shake)")]
    [SerializeField] private AudioClip shakeSound;
    
    [Tooltip("Громкость звука тряски (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float shakeSoundVolume = 1f;
    
    [Tooltip("AudioSource для звука тряски (если не назначен — создаётся автоматически)")]
    [SerializeField] private AudioSource shakeAudioSource;
    
    [Header("Tower Collapse - Objects")]
    [Tooltip("Верхушка башни (группа мешей)")]
    [SerializeField] private Transform towerTop;
    
    [Tooltip("Тело башни (группа мешей)")]
    [SerializeField] private Transform towerBody;
    
    [Tooltip("Коллайдер зоны башни (TowerZoneTrigger). Отключается при падении, включается при рестарте — игрок не может войти в зону во время падения.")]
    [SerializeField] private Collider towerZoneTriggerCollider;
    
    [Tooltip("Невидимая стена (GameObject). По умолчанию выключена, включается только на время падения башни (100% до рестарта).")]
    [SerializeField] private GameObject invisibleWall;
    
    [Header("Tower Top Collapse Timing (seconds)")]
    [Tooltip("Время этапа 1 верхушки: к Pos(-19,-32,-10) Rot(0,0,-10)")]
    [SerializeField] private float topStage1Duration = 1.5f;
    
    [Tooltip("Время этапа 2 верхушки: к Pos(-99,-8,0) Rot(0,0,-41)")]
    [SerializeField] private float topStage2Duration = 2f;
    
    [Tooltip("Время этапа 3 верхушки: к Pos(-99,-200,0) Rot(0,0,-90)")]
    [SerializeField] private float topStage3Duration = 2.5f;
    
    [Header("Tower Body Collapse Timing (seconds)")]
    [Tooltip("Время этапа 1 тела: Rot Z=4.75")]
    [SerializeField] private float bodyStage1Duration = 1f;
    
    [Tooltip("Время этапа 2 тела: Rot Z=21, Pos Y=-80")]
    [SerializeField] private float bodyStage2Duration = 3f;
    
    [Header("Restart Settings")]
    [Tooltip("Время до сброса после падения башни (секунды). 0 = не сбрасывать автоматически")]
    [SerializeField] private float restartDelay = 5f;
    
    [Header("Debug")]
    [SerializeField] private bool debug = false;
    
    // Начальные позиции/ротации для сброса
    private Vector3 towerTopStartPos;
    private Quaternion towerTopStartRot;
    private Vector3 towerBodyStartPos;
    private Quaternion towerBodyStartRot;
    
    // Флаг запуска падения
    private bool collapseStarted = false;
    
    // Текущий прогресс (0-1)
    private float currentProgress = 0f;
    
    // Отображаемый прогресс для плавной анимации (0-1)
    private float displayedProgress = 0f;
    
    // Таймер до следующего добавления
    private float nextAddTime = 0f;
    
    // Флаг активности (можно приостановить)
    private bool isActive = true;
    
    // Отслеживание порогов для тряски
    private bool triggered20 = false;
    private bool triggered40 = false;
    private bool triggered60 = false;
    private bool triggered80 = false;
    private bool triggered100 = false;
    
    // Ссылка на камеру для тряски
    private ThirdPersonCamera thirdPersonCamera;
    
    // Событие при достижении 100%
    public event System.Action OnProgressComplete;
    
    // Событие при изменении прогресса (передаёт текущий прогресс 0-1)
    public event System.Action<float> OnProgressChanged;
    
    private void Start()
    {
        // Автоматически находим прогресс бар, если не назначен
        if (progressBar == null)
        {
            GameObject container = GameObject.Find("FireProgressBarContainer");
            if (container != null)
            {
                Transform progressTransform = container.transform.Find("Progress");
                if (progressTransform != null)
                {
                    progressBar = progressTransform.GetComponent<Image>();
                }
            }
            
            if (progressBar == null)
            {
                Debug.LogError("[DestroyFireManager] Progress bar не найден! Назначьте его в инспекторе.");
            }
        }
        
        // Находим камеру для тряски
        thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();
        
        // Отключаем все группы огня при старте
        DisableAllFireGroups();
        
        // Сохраняем начальные позиции башни для сброса
        SaveTowerInitialPositions();
        
        if (shakeAudioSource == null && shakeSound != null)
        {
            shakeAudioSource = gameObject.AddComponent<AudioSource>();
            shakeAudioSource.playOnAwake = false;
            shakeAudioSource.spatialBlend = 0f;
        }
        
        // Инициализируем прогресс бар
        displayedProgress = currentProgress;
        UpdateProgressBar();
        
        // Устанавливаем первый интервал
        SetNextInterval();
        
        // Невидимая стена по умолчанию выключена
        if (invisibleWall != null)
            invisibleWall.SetActive(false);
    }
    
    private void Update()
    {
        // Плавная анимация заполнения бара: за progressBarFillDuration сек догоняем целевой прогресс
        if (progressBar != null)
        {
            if (progressBarFillDuration > 0f)
            {
                float gap = currentProgress - displayedProgress;
                float step = gap * Mathf.Clamp01(Time.deltaTime / progressBarFillDuration);
                displayedProgress = Mathf.MoveTowards(displayedProgress, currentProgress, Mathf.Abs(step));
                progressBar.fillAmount = displayedProgress;
            }
            else
            {
                displayedProgress = currentProgress;
                progressBar.fillAmount = currentProgress;
            }
        }
        
        if (!isActive) return;
        if (currentProgress >= 1f) return; // Уже завершено
        
        // Проверяем таймер
        if (Time.time >= nextAddTime)
        {
            AddRandomProgress();
            SetNextInterval();
        }
    }
    
    /// <summary>
    /// Добавляет случайное количество прогресса
    /// </summary>
    private void AddRandomProgress()
    {
        float progressToAdd = Random.Range(minProgressAdd, maxProgressAdd);
        currentProgress = Mathf.Clamp01(currentProgress + progressToAdd);
        
        if (debug)
        {
            Debug.Log($"[DestroyFireManager] Прогресс добавлен: +{progressToAdd:P1}, текущий: {currentProgress:P1}");
        }
        
        UpdateProgressBar();
        OnProgressChanged?.Invoke(currentProgress);
        
        // Проверяем пороги для тряски камеры
        CheckShakeThresholds();
        
        // Проверяем завершение
        if (currentProgress >= 1f)
        {
            if (debug)
            {
                Debug.Log("[DestroyFireManager] Прогресс достиг 100%! Башня разрушена.");
            }
            OnProgressComplete?.Invoke();
        }
    }
    
    /// <summary>
    /// Проигрывает звук тряски (если назначен)
    /// </summary>
    private void PlayShakeSound()
    {
        if (shakeSound == null) return;
        float vol = Mathf.Clamp01(shakeSoundVolume);
        if (shakeAudioSource != null)
            shakeAudioSource.PlayOneShot(shakeSound, vol);
        else
            AudioSource.PlayClipAtPoint(shakeSound, Camera.main != null ? Camera.main.transform.position : Vector3.zero, vol);
    }
    
    /// <summary>
    /// Проверяет пороги прогресса и запускает тряску камеры
    /// </summary>
    private void CheckShakeThresholds()
    {
        if (thirdPersonCamera == null) return;
        
        // 100% — самая сильная тряска + падение башни
        if (currentProgress >= 1f && !triggered100)
        {
            triggered100 = true;
            thirdPersonCamera.Shake(shakeIntensity100, shakeDuration * 2f);
            PlayShakeSound();
            
            // Запускаем падение башни
            StartTowerCollapse();
            
            if (debug)
            {
                Debug.Log("[DestroyFireManager] Порог 100% — максимальная тряска + падение башни!");
            }
        }
        // > 80%
        else if (currentProgress > 0.8f && !triggered80)
        {
            triggered80 = true;
            thirdPersonCamera.Shake(shakeIntensity80, shakeDuration);
            PlayShakeSound();
            EnableFireGroup(fireGroup4);
            
            if (debug)
            {
                Debug.Log("[DestroyFireManager] Порог 80% — тряска + группа огня 4!");
            }
        }
        // > 60%
        else if (currentProgress > 0.6f && !triggered60)
        {
            triggered60 = true;
            thirdPersonCamera.Shake(shakeIntensity60, shakeDuration);
            PlayShakeSound();
            EnableFireGroup(fireGroup3);
            
            if (debug)
            {
                Debug.Log("[DestroyFireManager] Порог 60% — тряска + группа огня 3!");
            }
        }
        // > 40%
        else if (currentProgress > 0.4f && !triggered40)
        {
            triggered40 = true;
            thirdPersonCamera.Shake(shakeIntensity40, shakeDuration);
            PlayShakeSound();
            EnableFireGroup(fireGroup2);
            
            if (debug)
            {
                Debug.Log("[DestroyFireManager] Порог 40% — тряска + группа огня 2!");
            }
        }
        // > 20%
        else if (currentProgress > 0.2f && !triggered20)
        {
            triggered20 = true;
            thirdPersonCamera.Shake(shakeIntensity20, shakeDuration);
            PlayShakeSound();
            EnableFireGroup(fireGroup1);
            
            if (debug)
            {
                Debug.Log("[DestroyFireManager] Порог 20% — тряска + группа огня 1!");
            }
        }
    }
    
    /// <summary>
    /// Включает группу VFX огня
    /// </summary>
    private void EnableFireGroup(GameObject group)
    {
        if (group != null)
        {
            group.SetActive(true);
        }
    }
    
    /// <summary>
    /// Отключает все группы VFX огня
    /// </summary>
    private void DisableAllFireGroups()
    {
        if (fireGroup1 != null) fireGroup1.SetActive(false);
        if (fireGroup2 != null) fireGroup2.SetActive(false);
        if (fireGroup3 != null) fireGroup3.SetActive(false);
        if (fireGroup4 != null) fireGroup4.SetActive(false);
        if (fireGroup5 != null) fireGroup5.SetActive(false);
    }
    
    /// <summary>
    /// Отключает группы 1-4, включает группу 5 (для падения башни)
    /// </summary>
    private void SwitchToCollapseFireGroup()
    {
        if (fireGroup1 != null) fireGroup1.SetActive(false);
        if (fireGroup2 != null) fireGroup2.SetActive(false);
        if (fireGroup3 != null) fireGroup3.SetActive(false);
        if (fireGroup4 != null) fireGroup4.SetActive(false);
        if (fireGroup5 != null) fireGroup5.SetActive(true);
    }
    
    /// <summary>
    /// Устанавливает следующий случайный интервал
    /// </summary>
    private void SetNextInterval()
    {
        float interval = Random.Range(minInterval, maxInterval);
        nextAddTime = Time.time + interval;
        
        if (debug)
        {
            Debug.Log($"[DestroyFireManager] Следующее добавление через {interval:F1} сек");
        }
    }
    
    /// <summary>
    /// Обновляет отображение прогресс бара (displayedProgress анимируется к currentProgress в Update)
    /// </summary>
    private void UpdateProgressBar()
    {
        if (progressBar != null && progressBarFillDuration <= 0f)
        {
            displayedProgress = currentProgress;
            progressBar.fillAmount = currentProgress;
        }
    }
    
    /// <summary>
    /// Получить текущий прогресс (0-1)
    /// </summary>
    public float GetProgress()
    {
        return currentProgress;
    }
    
    /// <summary>
    /// Получить текущий прогресс в процентах (0-100)
    /// </summary>
    public float GetProgressPercent()
    {
        return currentProgress * 100f;
    }
    
    /// <summary>
    /// Проверить, завершён ли прогресс
    /// </summary>
    public bool IsComplete()
    {
        return currentProgress >= 1f;
    }
    
    /// <summary>
    /// Сбросить прогресс на 0
    /// </summary>
    public void ResetProgress()
    {
        currentProgress = 0f;
        
        // Сбрасываем флаги порогов
        triggered20 = false;
        triggered40 = false;
        triggered60 = false;
        triggered80 = false;
        triggered100 = false;
        
        // Отключаем все группы огня
        DisableAllFireGroups();
        
        // Сбрасываем башню
        ResetTower();
        
        // Разрешаем вход в зону башни снова
        if (towerZoneTriggerCollider != null)
            towerZoneTriggerCollider.enabled = true;
        
        // Выключаем невидимую стену
        if (invisibleWall != null)
            invisibleWall.SetActive(false);
        
        displayedProgress = 0f;
        UpdateProgressBar();
        SetNextInterval();
        
        if (debug)
        {
            Debug.Log("[DestroyFireManager] Прогресс сброшен");
        }
        
        OnProgressChanged?.Invoke(currentProgress);
    }
    
    /// <summary>
    /// Установить прогресс напрямую (0-1)
    /// </summary>
    public void SetProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);
        if (progressBarFillDuration <= 0f)
            displayedProgress = currentProgress;
        UpdateProgressBar();
        OnProgressChanged?.Invoke(currentProgress);
        
        if (currentProgress >= 1f)
        {
            OnProgressComplete?.Invoke();
        }
    }
    
    /// <summary>
    /// Приостановить/возобновить накопление прогресса
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;
        
        if (active)
        {
            // При возобновлении — устанавливаем новый интервал от текущего момента
            SetNextInterval();
        }
        
        if (debug)
        {
            Debug.Log($"[DestroyFireManager] Активность: {active}");
        }
    }
    
    /// <summary>
    /// Проверить, активен ли менеджер
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }
    
    #region Tower Collapse
    
    /// <summary>
    /// Сохраняет начальные позиции и ротации башни
    /// </summary>
    private void SaveTowerInitialPositions()
    {
        if (towerTop != null)
        {
            towerTopStartPos = towerTop.localPosition;
            towerTopStartRot = towerTop.localRotation;
        }
        
        if (towerBody != null)
        {
            towerBodyStartPos = towerBody.localPosition;
            towerBodyStartRot = towerBody.localRotation;
        }
    }
    
    /// <summary>
    /// Запускает анимацию падения башни
    /// </summary>
    public void StartTowerCollapse()
    {
        if (collapseStarted) return;
        collapseStarted = true;
        
        if (debug)
        {
            Debug.Log("[DestroyFireManager] Запуск падения башни!");
        }
        
        // Переключаем VFX: отключаем группы 1-4, включаем группу 5
        SwitchToCollapseFireGroup();
        
        // Скрываем тексты этажей
        if (floorNumbersTexts != null)
        {
            floorNumbersTexts.SetActive(false);
        }
        
        // Блокируем вход в зону башни до рестарта
        if (towerZoneTriggerCollider != null)
            towerZoneTriggerCollider.enabled = false;
        
        // Включаем невидимую стену на время падения
        if (invisibleWall != null)
            invisibleWall.SetActive(true);
        
        // Запускаем ДВЕ независимые корутины - для верхушки и тела
        if (towerTop != null)
        {
            StartCoroutine(AnimateTowerTop());
        }
        if (towerBody != null)
        {
            StartCoroutine(AnimateTowerBody());
        }
        
        // Запускаем таймер автосброса, если задан
        if (restartDelay > 0)
        {
            StartCoroutine(RestartAfterCollapse());
        }
    }
    
    /// <summary>
    /// Ждёт завершения падения + restartDelay секунд, затем сбрасывает всё
    /// </summary>
    private IEnumerator RestartAfterCollapse()
    {
        // Считаем максимальное время падения
        float topTotalTime = topStage1Duration + topStage2Duration + topStage3Duration;
        float bodyTotalTime = bodyStage1Duration + bodyStage2Duration;
        float maxCollapseTime = Mathf.Max(topTotalTime, bodyTotalTime);
        
        // Ждём завершения падения + restartDelay
        float totalWait = maxCollapseTime + restartDelay;
        
        if (debug)
        {
            Debug.Log($"[DestroyFireManager] Автосброс через {totalWait:F1} сек (падение: {maxCollapseTime:F1} + задержка: {restartDelay:F1})");
        }
        
        yield return new WaitForSeconds(totalWait);
        
        if (debug)
        {
            Debug.Log("[DestroyFireManager] Автосброс - возврат в начальное состояние");
        }
        
        // Сбрасываем всё
        ResetProgress();
    }
    
    /// <summary>
    /// Независимая анимация падения верхушки (3 этапа)
    /// </summary>
    private IEnumerator AnimateTowerTop()
    {
        if (debug) Debug.Log("[DestroyFireManager] Начало падения верхушки");
        
        // === ЭТАП 1 верхушки ===
        Vector3 startPos = towerTop.localPosition;
        Quaternion startRot = towerTop.localRotation;
        Vector3 targetPos = new Vector3(-19f, -32f, 0f);
        Quaternion targetRot = Quaternion.Euler(0f, 0f, -10f);
        
        float elapsed = 0f;
        while (elapsed < topStage1Duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / topStage1Duration));
            towerTop.localPosition = Vector3.Lerp(startPos, targetPos, t);
            towerTop.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
        towerTop.localPosition = targetPos;
        towerTop.localRotation = targetRot;
        
        if (debug) Debug.Log("[DestroyFireManager] Верхушка: этап 1 завершён");
        
        // === ЭТАП 2 верхушки ===
        // Тряска камеры в начале этапа 2
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.Shake(shakeIntensityTopStage2, shakeDuration);
            PlayShakeSound();
            if (debug) Debug.Log("[DestroyFireManager] Тряска: начало этапа 2 верхушки");
        }
        
        startPos = towerTop.localPosition;
        startRot = towerTop.localRotation;
        targetPos = new Vector3(-39f, -62f, 0f);
        targetRot = Quaternion.Euler(-3f, 0f, -20f);
        
        elapsed = 0f;
        while (elapsed < topStage2Duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / topStage2Duration));
            towerTop.localPosition = Vector3.Lerp(startPos, targetPos, t);
            towerTop.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
        towerTop.localPosition = targetPos;
        towerTop.localRotation = targetRot;
        
        if (debug) Debug.Log("[DestroyFireManager] Верхушка: этап 2 завершён");
        
        // === ЭТАП 3 верхушки ===
        // Тряска камеры в начале этапа 3
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.Shake(shakeIntensityTopStage3, shakeDuration);
            PlayShakeSound();
            if (debug) Debug.Log("[DestroyFireManager] Тряска: начало этапа 3 верхушки");
        }
        
        startPos = towerTop.localPosition;
        startRot = towerTop.localRotation;
        targetPos = new Vector3(-150f, -200f, 0f);
        targetRot = Quaternion.Euler(-10f, 0f, -90f);
        
        elapsed = 0f;
        while (elapsed < topStage3Duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / topStage3Duration));
            towerTop.localPosition = Vector3.Lerp(startPos, targetPos, t);
            towerTop.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
        towerTop.localPosition = targetPos;
        towerTop.localRotation = targetRot;
        
        if (debug) Debug.Log("[DestroyFireManager] Верхушка: падение завершено");
    }
    
    /// <summary>
    /// Независимая анимация падения тела (2 этапа)
    /// </summary>
    private IEnumerator AnimateTowerBody()
    {
        if (debug) Debug.Log("[DestroyFireManager] Начало падения тела");
        
        Vector3 bodyStartPos = towerBody.localPosition;
        Vector3 bodyStartRotEuler = towerBody.localRotation.eulerAngles;
        
        // === ЭТАП 1 тела ===
        Vector3 startPos = towerBody.localPosition;
        Quaternion startRot = towerBody.localRotation;
        Vector3 targetPos = bodyStartPos; // позиция не меняется
        Quaternion targetRot = Quaternion.Euler(bodyStartRotEuler.x, bodyStartRotEuler.y, 4.75f);
        
        float elapsed = 0f;
        while (elapsed < bodyStage1Duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / bodyStage1Duration));
            towerBody.localPosition = Vector3.Lerp(startPos, targetPos, t);
            towerBody.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
        towerBody.localPosition = targetPos;
        towerBody.localRotation = targetRot;
        
        if (debug) Debug.Log("[DestroyFireManager] Тело: этап 1 завершён");
        
        // === ЭТАП 2 тела ===
        startPos = towerBody.localPosition;
        startRot = towerBody.localRotation;
        targetPos = new Vector3(bodyStartPos.x, -80f, bodyStartPos.z);
        targetRot = Quaternion.Euler(bodyStartRotEuler.x, bodyStartRotEuler.y, 21f);
        
        elapsed = 0f;
        while (elapsed < bodyStage2Duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / bodyStage2Duration));
            towerBody.localPosition = Vector3.Lerp(startPos, targetPos, t);
            towerBody.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
        towerBody.localPosition = targetPos;
        towerBody.localRotation = targetRot;
        
        if (debug) Debug.Log("[DestroyFireManager] Тело: падение завершено");
    }
    
    
    /// <summary>
    /// Сбрасывает башню в начальное положение
    /// </summary>
    public void ResetTower()
    {
        // Останавливаем все корутины
        StopAllCoroutines();
        
        collapseStarted = false;
        
        // Возвращаем башню в начальное положение
        if (towerTop != null)
        {
            towerTop.localPosition = towerTopStartPos;
            towerTop.localRotation = towerTopStartRot;
        }
        
        if (towerBody != null)
        {
            towerBody.localPosition = towerBodyStartPos;
            towerBody.localRotation = towerBodyStartRot;
        }
        
        // Восстанавливаем тексты этажей
        if (floorNumbersTexts != null)
        {
            floorNumbersTexts.SetActive(true);
        }
        
        if (debug)
        {
            Debug.Log("[DestroyFireManager] Башня сброшена в начальное положение");
        }
    }
    
    #endregion
}
