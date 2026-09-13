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
        LilypadMaxSpeedMultiplier,        // multiplica la velocidad máxima del nenúfar por "value" (pareja de LilypadMoveSpeedMultiplier, para que el tope no se quede corto al multiplicar el crucero)
    }

    [Header("Efecto")]
    public Target target;
    [Tooltip("Cantidad a sumar, o multiplicador, según el target elegido.")]
    public float value = 1f;

    [System.Serializable]
    public class SecondaryEffect
    {
        public Target target;
        public float value;
    }

    [Header("Efectos secundarios (opcional)")]
    [Tooltip("Pensado para mejoras que, para tener sentido de verdad, deben mover más de un stat a la vez " +
             "(p. ej. subir la velocidad de crucero del nenúfar sin tocar su velocidad máxima apenas se nota, " +
             "porque el clamp de LilypadMovement recorta el extra). Cada entrada se aplica igual que el efecto " +
             "principal, en el mismo Apply() y con la misma llamada a elegir la mejora.")]
    public System.Collections.Generic.List<SecondaryEffect> secondaryEffects = new System.Collections.Generic.List<SecondaryEffect>();

    public override void Apply()
    {
        ApplySingle(target, value);

        foreach (SecondaryEffect effect in secondaryEffects)
        {
            ApplySingle(effect.target, effect.value);
        }
    }

    private void ApplySingle(Target appliedTarget, float appliedValue)
    {
        switch (appliedTarget)
        {
            case Target.MinAttractionRange:
                GameManager.Instance?.IncreaseMinAttractionRange(appliedValue);
                break;

            case Target.MaxAttractionRange:
                GameManager.Instance?.IncreaseMaxAttractionRange(appliedValue);
                break;

            case Target.ScorePerSuccess:
                GameManager.Instance?.IncreaseScorePerSuccess(Mathf.RoundToInt(appliedValue));
                break;

            case Target.WindowSizeSeconds:
                GameManager.Instance?.WidenWindow(appliedValue);
                break;

            case Target.TadpoleMoveSpeedMultiplier:
                TadpoleManager.Instance?.MultiplyGlobalMoveSpeed(appliedValue);
                break;

            case Target.SpawnIntervalMultiplier:
                TadpoleManager.Instance?.MultiplySpawnInterval(appliedValue);
                break;

            case Target.MaxActiveTadpoles:
                TadpoleManager.Instance?.IncreaseMaxActiveTadpoles(Mathf.RoundToInt(appliedValue));
                break;

            case Target.CroackCycleDuration:
                GameManager.Instance?.IncreaseCycleDuration(-appliedValue);
                break;

            case Target.LilypadMoveSpeed:
                LilypadMovement.Instance?.IncreaseMoveSpeed(appliedValue);
                break;

            case Target.LilypadMaxSpeed:
                LilypadMovement.Instance?.IncreaseMaxSpeed(appliedValue);
                break;

            case Target.LilypadAcceleration:
                LilypadMovement.Instance?.IncreaseAcceleration(appliedValue);
                break;

            case Target.LilypadMoveSpeedMultiplier:
                LilypadMovement.Instance?.MultiplyMoveSpeed(appliedValue);
                break;

            case Target.LilypadMaxSpeedMultiplier:
                LilypadMovement.Instance?.MultiplyMaxSpeed(appliedValue);
                break;
        }
    }
}
