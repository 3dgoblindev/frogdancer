using System.Collections;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;

/// <summary>
/// Muestra la puntuación de la ronda actual (GameManager.Score) y anima el
/// cambio: el número "cuenta" desde el valor viejo al nuevo en vez de saltar
/// de golpe. El conteo es una coroutine propia (sin depender de DOTween);
/// el MMFeedbacks es opcional y sirve para el "juice" extra (punch de escala,
/// color, sonido...), igual que croakSuccessFeedback en Frog. Si no le
/// asignas nada al feedback, simplemente no se dispara nada y solo cuenta.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TMP_Text scoreText;
    [Tooltip("Opcional: feedback de MoreMountains a disparar cada vez que la puntuación cambia.")]
    [SerializeField] private MMFeedbacks scoreChangeFeedback;

    [Header("Formato")]
    [Tooltip("Formato del texto mostrado. {0} se sustituye por el número.")]
    [SerializeField] private string format = "{0}";

    [Header("Animación de conteo")]
    [Tooltip("Cuánto tarda en llegar del valor viejo al nuevo.")]
    [SerializeField] private float countDuration = 0.4f;
    [SerializeField] private AnimationCurve countCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private int displayedScore;
    private Coroutine countRoutine;

    // ---------------------------------------------------------------
    // CICLO DE VIDA / SUSCRIPCIÓN
    // ---------------------------------------------------------------
    private void OnEnable()
    {
        GameManager.OnScoreChanged += HandleScoreChanged;

        // Al activarse (p.ej. al cargar la escena), sincroniza sin animar.
        displayedScore = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        UpdateText(displayedScore);
    }

    private void OnDisable()
    {
        GameManager.OnScoreChanged -= HandleScoreChanged;

        if (countRoutine != null)
        {
            StopCoroutine(countRoutine);
            countRoutine = null;
        }
    }

    // ---------------------------------------------------------------
    // REACCIÓN AL CAMBIO DE PUNTUACIÓN
    // ---------------------------------------------------------------
    private void HandleScoreChanged(int newScore)
    {
        if (countRoutine != null) StopCoroutine(countRoutine);
        countRoutine = StartCoroutine(CountTo(newScore));

        scoreChangeFeedback?.PlayFeedbacks();
    }

    private IEnumerator CountTo(int target)
    {
        int start = displayedScore;

        if (countDuration <= 0f || start == target)
        {
            displayedScore = target;
            UpdateText(displayedScore);
            yield break;
        }

        float t = 0f;
        while (t < countDuration)
        {
            t += Time.deltaTime;
            float progress = countCurve.Evaluate(Mathf.Clamp01(t / countDuration));
            displayedScore = Mathf.RoundToInt(Mathf.Lerp(start, target, progress));
            UpdateText(displayedScore);
            yield return null;
        }

        displayedScore = target;
        UpdateText(displayedScore);
        countRoutine = null;
    }

    private void UpdateText(int value)
    {
        if (scoreText != null) scoreText.text = string.Format(format, value);
    }
}
