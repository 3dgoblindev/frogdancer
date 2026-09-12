using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mejora que el jugador puede elegir en la tienda al final de cada ronda.
/// Es un ScriptableObject abstracto: cada tipo concreto de mejora define su
/// propio efecto en Apply(). Así se pueden añadir nuevos tipos de mejora
/// (por ejemplo, algo más especial que subir un stat) sin tocar el
/// UpgradeManager ni la tienda.
///
/// No existe un asset aparte de "tabla de progresión": cada mejora lleva su
/// propia condición de desbloqueo (unlockRound + requiredUpgrades), y es el
/// UpgradeManager quien usa esos datos en tiempo de ejecución para decidir
/// qué mejoras pueden salir en cada tienda.
/// </summary>
public abstract class UpgradeSO : ScriptableObject
{
    /// <summary>
    /// Tier/rareza de la mejora. Por convención: Tier1 disponible desde la
    /// primera tienda, Tier2 a partir de la ronda 3, Tier3 a partir de la
    /// ronda 5 (ver UpgradeAssetGenerator). El valor real de desbloqueo sigue
    /// estando en "unlockRound" por si algún asset concreto quiere salirse
    /// de esa convención.
    /// </summary>
    public enum UpgradeTier
    {
        Tier1, // blanco: mejoras base
        Tier2, // azul: mejoras intermedias
        Tier3, // morado: mejoras potentes
    }

    [Header("Identidad (para UI)")]
    public string upgradeId = "mejora_nueva";
    public string displayName = "Mejora sin nombre";
    [TextArea] public string description;
    public Sprite icon;

    [Header("Tier (rareza / potencia)")]
    public UpgradeTier tier = UpgradeTier.Tier1;

    [Header("Progresión / desbloqueo")]
    [Tooltip("Ronda mínima (1 = primera ronda) a partir de la cual esta mejora puede salir en la tienda.")]
    [Min(1)] public int unlockRound = 1;

    [Tooltip("Mejoras que el jugador debe tener ya elegidas en esta partida para que ésta pueda aparecer. Vacío = sin requisito.")]
    public List<UpgradeSO> requiredUpgrades = new List<UpgradeSO>();

    [Header("Repetición")]
    [Tooltip("Si está marcado, esta mejora puede volver a salir (y elegirse) varias veces en la misma partida, acumulando su efecto.")]
    public bool isRepeatable = false;

    [Tooltip("Solo si isRepeatable: número máximo de veces que puede elegirse en una partida. 0 = sin límite.")]
    [Min(0)] public int maxStacks = 0;

    /// <summary>
    /// Aplica el efecto de esta mejora sobre los sistemas del juego
    /// (GameManager, TadpoleManager, etc.). Se llama una vez cada vez que
    /// el jugador la elige (incluidas repeticiones, si isRepeatable).
    /// </summary>
    public abstract void Apply();
}
