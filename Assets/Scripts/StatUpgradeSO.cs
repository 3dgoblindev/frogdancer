using UnityEngine;

/// <summary>
/// Mejora "genérica" de datos: sube (o multiplica) un stat concreto del
/// juego en una cantidad fija. Cubre la mayoría de mejoras normales sin
/// necesitar una subclase de código por cada una; para efectos más
/// especiales (desbloquear una rana nueva, un modo de juego, etc.) crea
/// otra subclase de UpgradeSO en vez de forzarla aquí.
/// </summary>
[CreateAssetMenu(fileName = "StatUpgrade", menuName = "Frogs/Mejoras/Stat Upgrade")]
public class StatUpgradeSO : UpgradeSO
{
    public enum Target
    {
        MinAttractionRange,            // suma "value" metros al rango mínimo
        MaxAttractionRange,            // suma "value" metros al rango máximo
        ScorePerSuccess,                // suma "value" puntos por croar bien
        WindowSizeSeconds,              // agranda la ventana válida "value" segundos (adelanta windowStart)
        TadpoleMoveSpeedMultiplier,     // multiplica la velocidad de los renacuajos por "value"
        SpawnIntervalMultiplier,        // multiplica el intervalo de spawn por "value" (<1 = spawnea más seguido)
        MaxActiveTadpoles,               // suma "value" (redondeado) al límite de renacuajos activos
        CroackCycleDuration,            // resta "value" segundos a la duración del ciclo de croar (adelanta el siguiente croar)
        LilypadMoveSpeed,                // suma "value" a la velocidad de crucero del nenúfar
        LilypadMaxSpeed,                 // suma "value" a la velocidad máxima del nenúfar
        LilypadAcceleration,             // suma "value" a la aceleración del nenúfar (respuesta más seca)
        LilypadMoveSpeedMultiplier,      // multiplica la velocidad de crucero del nenúfar por "value" (versión "tier 3" de LilypadMoveSpeed)
    }

    [Header("Efecto")]
    public Target target;
    [Tooltip("Cantidad a sumar, o multiplicador, según el target elegido.")]
    public float value = 1f;

    public override void Apply()
    {
        switch (target)
        {
            case Target.MinAttractionRange:
                GameManager.Instance?.IncreaseMinAttractionRange(value);
                break;

            case Target.MaxAttractionRange:
                GameManager.Instance?.IncreaseMaxAttractionRange(value);
                break;

            case Target.ScorePerSuccess:
                GameManager.Instance?.IncreaseScorePerSuccess(Mathf.RoundToInt(value));
                break;

            case Target.WindowSizeSeconds:
                GameManager.Instance?.WidenWindow(value);
                break;

            case Target.TadpoleMoveSpeedMultiplier:
                TadpoleManager.Instance?.MultiplyGlobalMoveSpeed(value);
                break;

            case Target.SpawnIntervalMultiplier:
                TadpoleManager.Instance?.MultiplySpawnInterval(value);
                break;

            case Target.MaxActiveTadpoles:
                TadpoleManager.Instance?.IncreaseMaxActiveTadpoles(Mathf.RoundToInt(value));
                break;

            case Target.CroackCycleDuration:
                GameManager.Instance?.IncreaseCycleDuration(-value);
                break;

            case Target.LilypadMoveSpeed:
                LilypadMovement.Instance?.IncreaseMoveSpeed(value);
                break;

            case Target.LilypadMaxSpeed:
                LilypadMovement.Instance?.IncreaseMaxSpeed(value);
                break;

            case Target.LilypadAcceleration:
                LilypadMovement.Instance?.IncreaseAcceleration(value);
                break;

            case Target.LilypadMoveSpeedMultiplier:
                LilypadMovement.Instance?.MultiplyMoveSpeed(value);
                break;
        }
    }
}
