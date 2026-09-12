using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Representación visual del ciclo de ritmo del GameManager, como reloj RADIAL:
/// - "progressFill" es un Image tipo Filled > Radial 360 que da la vuelta completa
///   según avanza el ciclo (la "aguja" del reloj de croar).
/// - "windowZoneHighlight" + "windowZoneMask" dibujan el arco de la ventana válida
///   como una franja fija sobre el círculo de fondo (truco de dos radiales superpuestos,
///   ver ConfigureWindowZone()).
/// - Feedback de color instantáneo en progressFill según el resultado del croar.
///
/// No contiene lógica de ritmo propia: todo sale de GameManager.Instance,
/// este script solo "pinta" ese estado. Si una mejora cambia windowStart/windowEnd
/// en tiempo real (GameManager.OnWindowValuesChanged), la franja se recalcula sola.
///
/// CONFIGURACIÓN EN EL INSPECTOR DE CADA IMAGE:
/// - progressFill, windowZoneHighlight, windowZoneMask deben ser Image > Image Type: Filled,
///   Fill Method: Radial 360, MISMO Fill Origin (p.ej. Top) y MISMA dirección (Clockwise),
///   para que los tres círculos coincidan.
/// - Orden en la jerarquía (de atrás hacia adelante): fondo circular estático (opcional) →
///   windowZoneHighlight → windowZoneMask → progressFill.
/// </summary>
public class CroakTimer : MonoBehaviour
{
    [Header("Referencias UI (todas Image > Filled > Radial 360)")]
    [Tooltip("Aguja radial que representa el progreso del ciclo actual (0 a 1 = una vuelta completa).")]
    [SerializeField] private Image progressFill;

    [Tooltip("Radial que pinta desde el origen hasta el FINAL de la ventana válida, con el color de ventana.")]
    [SerializeField] private Image windowZoneHighlight;

    [Tooltip("Radial del mismo color que el fondo, que tapa desde el origen hasta el INICIO de la ventana, dejando solo visible la franja [start, end] de windowZoneHighlight.")]
    [SerializeField] private Image windowZoneMask;

    [Header("Colores de feedback (progressFill)")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color windowOpenColor = Color.yellow;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failColor = Color.red;
    [SerializeField] private Color lateColor = new Color(1f, 0.5f, 0f); // naranja

    [Header("Colores de la franja de ventana (círculo de fondo)")]
    [Tooltip("Color de fondo del reloj (debe coincidir con windowZoneMask para que 'tape' bien).")]
    [SerializeField] private Color trackColor = new Color(0.2f, 0.2f, 0.2f);
    [SerializeField] private Color windowZoneColor = new Color(1f, 1f, 0.6f);

    [Tooltip("Cuánto se mantiene el color de feedback (éxito/fallo/tarde) antes de volver al color base.")]
    [SerializeField] private float feedbackDuration = 0.2f;

    private float feedbackTimer;
    private bool windowIsOpen;

    // ---------------------------------------------------------------
    // SUSCRIPCIÓN A EVENTOS
    // ---------------------------------------------------------------
    private void OnEnable()
    {
        GameManager.OnCycleStart += HandleCycleStart;
        GameManager.OnWindowOpen += HandleWindowOpen;
        GameManager.OnWindowClose += HandleWindowClose;

        GameManager.OnCroakSuccess += HandleCroakSuccess;
        GameManager.OnCroakFail += HandleCroakFail;
        GameManager.OnCroakLate += HandleCroakLate;

        GameManager.OnWindowValuesChanged += HandleWindowValuesChanged;
    }

    private void OnDisable()
    {
        GameManager.OnCycleStart -= HandleCycleStart;
        GameManager.OnWindowOpen -= HandleWindowOpen;
        GameManager.OnWindowClose -= HandleWindowClose;

        GameManager.OnCroakSuccess -= HandleCroakSuccess;
        GameManager.OnCroakFail -= HandleCroakFail;
        GameManager.OnCroakLate -= HandleCroakLate;

        GameManager.OnWindowValuesChanged -= HandleWindowValuesChanged;
    }

    private void Start()
    {
        if (progressFill != null)
        {
            progressFill.color = defaultColor;
        }

        ConfigureWindowZone();
    }

    private void Update()
    {
        if (GameManager.Instance == null || progressFill == null) return;

        progressFill.fillAmount = GameManager.Instance.GetCycleProgress01();

        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0f)
            {
                // Al acabar el feedback, vuelve al color base (o al de "ventana abierta" si sigue abierta)
                progressFill.color = windowIsOpen ? windowOpenColor : defaultColor;
            }
        }
    }

    // ---------------------------------------------------------------
    // CONFIGURAR LA FRANJA DE VENTANA VÁLIDA (al iniciar, y cada vez que
    // una mejora cambia windowStart/windowEnd en tiempo real)
    // ---------------------------------------------------------------
    private void ConfigureWindowZone()
    {
        if (GameManager.Instance == null) return;

        float cycleDuration = GameManager.Instance.CycleDuration;
        float windowStart = GameManager.Instance.WindowStart;
        float windowEnd = GameManager.Instance.WindowEnd;

        if (cycleDuration <= 0f) return;

        float startFraction = windowStart / cycleDuration;
        float endFraction = windowEnd / cycleDuration;

        // windowZoneHighlight pinta el color de ventana desde el origen (0) hasta endFraction.
        if (windowZoneHighlight != null)
        {
            windowZoneHighlight.color = windowZoneColor;
            windowZoneHighlight.fillAmount = endFraction;
        }

        // windowZoneMask, del color de fondo, tapa desde el origen (0) hasta startFraction,
        // encima de windowZoneHighlight. El resultado visible es solo la franja [start, end].
        if (windowZoneMask != null)
        {
            windowZoneMask.color = trackColor;
            windowZoneMask.fillAmount = startFraction;
        }
    }

    // ---------------------------------------------------------------
    // HANDLERS
    // ---------------------------------------------------------------
    private void HandleCycleStart()
    {
        windowIsOpen = false;
        feedbackTimer = 0f;
        if (progressFill != null) progressFill.color = defaultColor;
    }

    private void HandleWindowOpen()
    {
        windowIsOpen = true;
        if (feedbackTimer <= 0f && progressFill != null)
        {
            progressFill.color = windowOpenColor;
        }
    }

    private void HandleWindowClose()
    {
        windowIsOpen = false;
        if (feedbackTimer <= 0f && progressFill != null)
        {
            progressFill.color = defaultColor;
        }
    }

    private void HandleCroakSuccess() => FlashColor(successColor);
    private void HandleCroakFail() => FlashColor(failColor);
    private void HandleCroakLate() => FlashColor(lateColor);

    private void HandleWindowValuesChanged() => ConfigureWindowZone();

    private void FlashColor(Color color)
    {
        if (progressFill == null) return;
        progressFill.color = color;
        feedbackTimer = feedbackDuration;
    }
}
