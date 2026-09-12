using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra que representa el tiempo restante de la ronda actual: el fill se va
/// "encogiendo" (fillAmount de 1 a 0) según avanza
/// RoundManager.GetRoundProgress01(). Este script va en el bg de la barra;
/// el Image del fill (Image Type = Filled) se asigna a mano en el inspector.
/// </summary>
public class RoundTimerBar : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Image con Image Type = Filled (normalmente Horizontal) que representa el tiempo restante.")]
    [SerializeField] private Image fill;

    // ---------------------------------------------------------------
    // CICLO DE VIDA / SUSCRIPCIÓN
    // ---------------------------------------------------------------
    private void OnEnable()
    {
        RoundManager.OnRoundStart += HandleRoundStart;
    }

    private void OnDisable()
    {
        RoundManager.OnRoundStart -= HandleRoundStart;
    }

    private void HandleRoundStart(int roundNumber)
    {
        // Al empezar una ronda nueva la barra se llena de golpe: no hace
        // falta animarlo, el jugador ya ve la tienda cerrarse en ese momento.
        if (fill != null) fill.fillAmount = 1f;
    }

    // ---------------------------------------------------------------
    // ACTUALIZACIÓN DEL FILL
    // ---------------------------------------------------------------
    private void Update()
    {
        if (fill == null || RoundManager.Instance == null) return;

        // Con la tienda abierta el progreso se queda congelado en el valor
        // de fin de ronda (RoundManager pausa su Update ahí), así que no
        // merece la pena seguir escribiendo en el fill mientras tanto.
        if (!RoundManager.Instance.IsRoundActive) return;

        fill.fillAmount = 1f - RoundManager.Instance.GetRoundProgress01();
    }
}
