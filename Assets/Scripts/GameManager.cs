using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla el ciclo de ritmo del croar: cada ciclo dura "cycleDuration" segundos
/// y dentro de ese ciclo hay una ventana [windowStart, windowEnd] en la que
/// pulsar Espacio cuenta como croar bien (mantiene combo).
///
/// - Pulsar ANTES de la ventana  -> croar feo / fallo (rompe combo)
/// - Pulsar DENTRO de la ventana -> croar bien (suma combo)
/// - Pulsar DESPUÉS de la ventana -> croa igualmente pero tarde (rompe combo)
///
/// Las ranas (y cualquier otro sistema: UI, audio, progresión...) se suscriben
/// a los eventos estáticos en su propio Start()/OnEnable() y reaccionan sin que
/// el GameManager necesite conocerlas directamente.
///
/// Si hay un RoundManager en la escena, este script se pausa por completo
/// (no avanza el ciclo ni lee input) mientras RoundManager.IsRoundActive sea
/// false, es decir, mientras la tienda de mejoras está abierta o la partida
/// ya ha terminado.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ---------------------------------------------------------------
    // PARÁMETROS DE RITMO (ajustables desde el Inspector)
    // ---------------------------------------------------------------
    [Header("Ritmo")]
    [Tooltip("Duración total de un ciclo de croar, en segundos.")]
    [SerializeField] private float cycleDuration = 2f;

    [Tooltip("Segundo del ciclo en el que se ABRE la ventana válida.")]
    [SerializeField] private float windowStart = 1.5f;

    [Tooltip("Segundo del ciclo en el que se CIERRA la ventana válida.")]
    [SerializeField] private float windowEnd = 2f;

    [Header("Input")]
    [Tooltip("Tecla del nuevo Input System (por defecto: barra espaciadora).")]
    [SerializeField] private Key croakKey = Key.Space;

    [Header("Combo")]
    [Tooltip("Puntos que suma cada croar exitoso (placeholder para progresión).")]
    [SerializeField] private int scorePerSuccess = 10;

    [Header("Rango de atracción (crece con el combo)")]
    [Tooltip("Rango de atracción con combo 0. Si hay un save, se sobreescribe con el valor guardado.")]
    [SerializeField] private float minAttractionRange = 2f;
    [Tooltip("Rango de atracción al llegar a 'comboForMaxRange'. Si hay un save, se sobreescribe con el valor guardado.")]
    [SerializeField] private float maxAttractionRange = 8f;
    [Tooltip("Combo necesario para alcanzar el rango máximo. Por encima de este combo, el rango no sigue creciendo.")]
    [SerializeField] private int comboForMaxRange = 20;

    // ---------------------------------------------------------------
    // ESTADO INTERNO
    // ---------------------------------------------------------------
    private float elapsed;
    private bool hasCroakedThisCycle;

    public int CurrentCombo { get; private set; }
    public int MaxCombo { get; private set; }
    public int Score { get; private set; }

    // Rango de atracción vigente ahora mismo (interpola entre min y max según el combo).
    // Los renacuajos leen esto (o escuchan OnAttractionRangeChanged) en vez de tener su propio valor fijo.
    public float CurrentAttractionRange { get; private set; }

    // Solo lectura hacia fuera: el timer visual (CroakTimer) los necesita
    // para saber dónde dibujar la zona de ventana válida, pero no debe poder tocarlos.
    public float CycleDuration => cycleDuration;
    public float WindowStart => windowStart;
    public float WindowEnd => windowEnd;

    private enum CycleZone { TooEarly, InWindow, TooLate }

    // ---------------------------------------------------------------
    // EVENTOS (se suscriben las ranas, UI, audio, etc.)
    // ---------------------------------------------------------------
    public static event Action OnCycleStart;          // arranca un nuevo ciclo (para animar "se acerca el ritmo")
    public static event Action OnWindowOpen;           // la ventana válida se abre
    public static event Action OnWindowClose;          // la ventana válida se cierra sin haber croado

    public static event Action OnCroakSuccess;         // croó dentro de la ventana
    public static event Action OnCroakFail;             // croó demasiado pronto (croaido feo)
    public static event Action OnCroakLate;             // croó demasiado tarde

    public static event Action<int> OnComboChanged;    // valor nuevo del combo (tras cada croar)
    public static event Action<int> OnComboBroken;      // valor del combo justo antes de romperse
    public static event Action<int> OnScoreChanged;
    public static event Action<float> OnAttractionRangeChanged; // nuevo rango de atracción (tras cada cambio de combo)
    public static event Action OnWindowValuesChanged;   // windowStart/windowEnd cambiaron (p.ej. por una mejora): CroakTimer debe redibujar su franja

    private bool windowOpenFired;
    private bool windowCloseFired;

    // ---------------------------------------------------------------
    // CICLO DE VIDA
    // ---------------------------------------------------------------
    private void Awake()
    {
        // Singleton simple para que las ranas puedan hacer GameManager.Instance.X
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        LoadAttractionRangeFromSave();
        ValidateWindowValues();
        RecalculateAttractionRange();
        StartNewCycle();
    }

    private void LoadAttractionRangeFromSave()
    {
        // Si hay un SavesManager con datos cargados, esos valores (potencialmente
        // mejorados por el jugador) pisan los del Inspector. Si no hay save
        // (por ejemplo probando esta escena suelta), se usan los valores por defecto.
        if (SavesManager.Instance != null && SavesManager.Instance.Data != null)
        {
            minAttractionRange = SavesManager.Instance.Data.minAttractionRange;
            maxAttractionRange = SavesManager.Instance.Data.maxAttractionRange;
        }
    }

    private void Update()
    {
        // Si hay un RoundManager y dice que la ronda no está activa (tienda
        // abierta o partida terminada), el ritmo de croar se congela aquí.
        if (RoundManager.Instance != null && !RoundManager.Instance.IsRoundActive) return;

        elapsed += Time.deltaTime;

        CheckWindowEvents();

        if (Keyboard.current != null
            && Keyboard.current[croakKey].wasPressedThisFrame
            && !hasCroakedThisCycle)
        {
            TryCroak();
        }

        if (elapsed >= cycleDuration)
        {
            StartNewCycle();
        }
    }

    // ---------------------------------------------------------------
    // LÓGICA DE RITMO
    // ---------------------------------------------------------------
    private void StartNewCycle()
    {
        elapsed = 0f;
        hasCroakedThisCycle = false;
        windowOpenFired = false;
        windowCloseFired = false;

        OnCycleStart?.Invoke();
    }

    private void CheckWindowEvents()
    {
        if (!windowOpenFired && elapsed >= windowStart)
        {
            windowOpenFired = true;
            OnWindowOpen?.Invoke();
        }

        if (!windowCloseFired && elapsed >= windowEnd)
        {
            windowCloseFired = true;
            OnWindowClose?.Invoke();
        }
    }

    private void TryCroak()
    {
        hasCroakedThisCycle = true;
        CycleZone zone = GetCurrentZone();

        switch (zone)
        {
            case CycleZone.TooEarly:
                HandleFail();
                break;

            case CycleZone.InWindow:
                HandleSuccess();
                break;

            case CycleZone.TooLate:
                HandleLate();
                break;
        }
    }

    private CycleZone GetCurrentZone()
    {
        if (elapsed < windowStart) return CycleZone.TooEarly;
        if (elapsed <= windowEnd) return CycleZone.InWindow;
        return CycleZone.TooLate;
    }

    // ---------------------------------------------------------------
    // RESULTADOS DE CADA CROAR
    // ---------------------------------------------------------------
    private void HandleSuccess()
    {
        CurrentCombo++;
        MaxCombo = Mathf.Max(MaxCombo, CurrentCombo);

        AddScore(scorePerSuccess);

        OnCroakSuccess?.Invoke();
        OnComboChanged?.Invoke(CurrentCombo);
        RecalculateAttractionRange();
    }

    private void HandleFail()
    {
        BreakCombo();
        OnCroakFail?.Invoke();
    }

    private void HandleLate()
    {
        BreakCombo();
        OnCroakLate?.Invoke();
    }

    private void BreakCombo()
    {
        if (CurrentCombo > 0)
        {
            OnComboBroken?.Invoke(CurrentCombo);
        }
        CurrentCombo = 0;
        OnComboChanged?.Invoke(CurrentCombo);
        RecalculateAttractionRange();
    }

    private void AddScore(int amount)
    {
        Score += amount;
        OnScoreChanged?.Invoke(Score);
    }

    private void RecalculateAttractionRange()
    {
        float comboFraction = comboForMaxRange > 0
            ? Mathf.Clamp01((float)CurrentCombo / comboForMaxRange)
            : 1f;

        CurrentAttractionRange = Mathf.Lerp(minAttractionRange, maxAttractionRange, comboFraction);
        OnAttractionRangeChanged?.Invoke(CurrentAttractionRange);
    }

    // ---------------------------------------------------------------
    // UTILIDADES
    // ---------------------------------------------------------------
    private void ValidateWindowValues()
    {
        windowStart = Mathf.Clamp(windowStart, 0f, cycleDuration);
        windowEnd = Mathf.Clamp(windowEnd, windowStart, cycleDuration);
    }

    /// <summary>
    /// Progreso normalizado (0-1) del ciclo actual. Útil para que el timer visual
    /// (barra, aguja, etc.) se enganche sin duplicar lógica de tiempo.
    /// </summary>
    public float GetCycleProgress01() => Mathf.Clamp01(elapsed / cycleDuration);

    // ---------------------------------------------------------------
    // MEJORAS (llamadas desde UpgradeSO.Apply()) y control de rondas
    // (llamado desde RoundManager). GameManager no sabe nada de tiendas
    // ni de rondas: solo expone estos hooks para que otros lo modifiquen.
    // ---------------------------------------------------------------
    public void IncreaseMinAttractionRange(float amount)
    {
        minAttractionRange = Mathf.Max(0f, minAttractionRange + amount);
        RecalculateAttractionRange();
    }

    public void IncreaseMaxAttractionRange(float amount)
    {
        maxAttractionRange = Mathf.Max(minAttractionRange, maxAttractionRange + amount);
        RecalculateAttractionRange();
    }

    public void IncreaseScorePerSuccess(int amount)
    {
        scorePerSuccess = Mathf.Max(0, scorePerSuccess + amount);
    }

    /// <summary>Agranda la ventana válida "amountSeconds" adelantando su inicio (windowEnd no cambia).</summary>
    public void WidenWindow(float amountSeconds)
    {
        windowStart = Mathf.Max(0f, windowStart - amountSeconds);
        ValidateWindowValues();
        OnWindowValuesChanged?.Invoke();
    }

    /// <summary>Suma puntos de fuera del ciclo de croar (p.ej. renacuajos rescatados) al marcador de la partida actual.</summary>
    public void AddTadpoleScore(int amount) => AddScore(amount);

    /// <summary>Llamado por RoundManager al empezar una ronda nueva: resetea el ciclo de croar en curso.</summary>
    public void ResumeForNewRound()
    {
        BreakCombo();
        StartNewCycle();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void IncreaseCycleDuration(float amount)
    {
        cycleDuration = Mathf.Max(0.1f, cycleDuration + amount);
        ValidateWindowValues();
        OnWindowValuesChanged?.Invoke();
    }
}
