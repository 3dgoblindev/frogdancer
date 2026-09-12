using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla el ciclo de "partida": una partida dura "totalRounds" rondas,
/// cada ronda dura "roundDuration" segundos. Al acabar una ronda se abre la
/// tienda de mejoras (salvo que fuera la última ronda, en cuyo caso termina
/// la partida directamente). El jugador elige una mejora y pulsa "empezar
/// siguiente ronda" desde la UI para continuar.
///
/// GameManager y TadpoleManager consultan IsRoundActive para saber si deben
/// seguir corriendo su lógica (ritmo de croar, spawn de renacuajos) o
/// quedarse en pausa mientras la tienda está abierta.
///
/// No lleva lógica propia de mejoras (eso es UpgradeManager) ni de ritmo de
/// croar (eso sigue siendo GameManager): solo orquesta cuándo pasa cada cosa.
/// </summary>
public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    private enum RoundState { Playing, ShopOpen, RunOver }

    [Header("Rondas")]
    [Tooltip("Duración de cada ronda, en segundos.")]
    [SerializeField] private float roundDuration = 30f;
    [Tooltip("Número total de rondas que dura una partida. Fijado a 7: T1 disponible siempre, T2 desde la ronda 3, T3 desde la ronda 5, dejando 2-3 rondas de margen para disfrutar de las mejoras de tier alto.")]
    [SerializeField] private int totalRounds = 7;

    [Header("Fin de partida")]
    [Tooltip("Nombre de la escena a cargar al terminar la partida (debe estar en Build Settings). Ahora que hay una escena de end screen dedicada, aquí va el nombre de ESA escena (no el del menú); el end screen lee los resultados de SavesManager y desde ahí se navega al menú.")]
    [SerializeField] private string menuSceneName = "Menu";

    private RoundState state;
    private float roundElapsed;

    public int CurrentRound { get; private set; }
    public int TotalRounds => totalRounds;

    // GameManager y TadpoleManager solo deben avanzar su lógica si esto es true.
    public bool IsRoundActive => state == RoundState.Playing;
    public bool IsShopOpen => state == RoundState.ShopOpen;
    public bool IsRunOver => state == RoundState.RunOver;

    // ---------------------------------------------------------------
    // EVENTOS (los escucha la UI de la tienda / HUD de rondas)
    // ---------------------------------------------------------------
    public static event Action<int> OnRoundStart;          // ronda que empieza (1-based)
    public static event Action<int> OnRoundEnd;            // ronda que acaba de terminar
    public static event Action<List<UpgradeSO>> OnShopOpened; // opciones a elegir en la tienda
    public static event Action<long> OnRunEnded;            // puntuación final de la partida

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        CurrentRound = 1;
        state = RoundState.Playing;
        roundElapsed = 0f;

        OnRoundStart?.Invoke(CurrentRound);
    }

    private void Update()
    {
        if (state != RoundState.Playing) return;

        roundElapsed += Time.deltaTime;
        if (roundElapsed >= roundDuration)
        {
            EndRound();
        }
    }

    /// <summary>Progreso normalizado (0-1) de la ronda actual, útil para una barra de tiempo en el HUD.</summary>
    public float GetRoundProgress01() => Mathf.Clamp01(roundElapsed / roundDuration);

    // ---------------------------------------------------------------
    // FIN DE RONDA / TIENDA
    // ---------------------------------------------------------------
    private void EndRound()
    {
        OnRoundEnd?.Invoke(CurrentRound);

        if (CurrentRound >= totalRounds)
        {
            EndRun();
            return;
        }

        OpenShop();
    }

    private void OpenShop()
    {
        state = RoundState.ShopOpen;

        List<UpgradeSO> options = UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GenerateShopOptions(CurrentRound)
            : new List<UpgradeSO>();

        OnShopOpened?.Invoke(options);
    }

    /// <summary>Llamado por la UI de la tienda cuando el jugador pulsa sobre una mejora.</summary>
    public void ChooseUpgrade(UpgradeSO chosen)
    {
        if (state != RoundState.ShopOpen) return;
        UpgradeManager.Instance?.ChooseUpgrade(chosen);
    }

    /// <summary>Llamado por la UI (botón "Siguiente ronda") una vez el jugador ha elegido su mejora.</summary>
    public void StartNextRound()
    {
        if (state != RoundState.ShopOpen) return;

        CurrentRound++;
        roundElapsed = 0f;
        state = RoundState.Playing;

        GameManager.Instance?.ResumeForNewRound();

        OnRoundStart?.Invoke(CurrentRound);
    }

    // ---------------------------------------------------------------
    // FIN DE PARTIDA
    // ---------------------------------------------------------------
    private void EndRun()
    {
        state = RoundState.RunOver;

        long finalScore = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        int tadpolesSaved = TadpoleManager.Instance != null ? TadpoleManager.Instance.TadpolesSaved : 0;

        bool isNewRecord = false;
        if (SavesManager.Instance != null)
        {
            SavesManager.Instance.SetLastRunResult(finalScore, tadpolesSaved);
            isNewRecord = SavesManager.Instance.LastRunWasNewRecord;
        }

        Debug.Log($"[RoundManager] Partida terminada tras {totalRounds} rondas. Puntuación: {finalScore}"
            + (isNewRecord ? " (¡nuevo récord!)" : "."));

        OnRunEnded?.Invoke(finalScore);

        // Placeholder de "pantalla de fin de partida": por ahora es directamente
        // el menú principal. Cuando haya una escena de resultados dedicada,
        // basta con cambiar menuSceneName (o cargar esa otra escena aquí).
        if (!string.IsNullOrEmpty(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            Debug.LogWarning("[RoundManager] menuSceneName está vacío: se recarga la escena actual en su lugar.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
