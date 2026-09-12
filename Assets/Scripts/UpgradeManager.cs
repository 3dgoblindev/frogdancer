using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona qué mejoras ha elegido el jugador en la partida actual y decide
/// qué opciones pueden salir en la tienda al final de cada ronda, según la
/// ronda actual y las mejoras ya elegidas (requisitos, repetición, maxStacks).
///
/// No sabe nada de rondas ni de timers: eso lo controla RoundManager, que es
/// quien le pide las opciones y le avisa de qué se ha elegido.
///
/// Las mejoras son SOLO de la partida actual: no se guardan en el save. Si
/// más adelante queréis mejoras permanentes entre partidas, este es el sitio
/// donde añadir esa persistencia (llamando a SavesManager desde ChooseUpgrade).
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Tooltip("Catálogo con todas las mejoras posibles del juego.")]
    [SerializeField] private UpgradePoolSO upgradePool;

    [Tooltip("Cuántas opciones distintas se ofrecen en cada tienda.")]
    [SerializeField] private int optionsPerShop = 3;

    // Cuántas veces se ha elegido cada mejora en esta partida (para respetar
    // maxStacks y para saber si una mejora no repetible ya se eligió).
    private readonly Dictionary<UpgradeSO, int> timesChosen = new Dictionary<UpgradeSO, int>();

    public static event Action<UpgradeSO> OnUpgradeChosen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Devuelve hasta "optionsPerShop" mejoras distintas, elegidas al azar entre
    /// todas las que están desbloqueadas para "currentRound" y cuyos requisitos
    /// ya se cumplen. Puede devolver menos si no hay suficientes elegibles.
    /// </summary>
    public List<UpgradeSO> GenerateShopOptions(int currentRound)
    {
        List<UpgradeSO> eligible = new List<UpgradeSO>();

        if (upgradePool != null)
        {
            foreach (UpgradeSO upgrade in upgradePool.upgrades)
            {
                if (IsEligible(upgrade, currentRound))
                {
                    eligible.Add(upgrade);
                }
            }
        }

        return PickRandomDistinct(eligible, optionsPerShop);
    }

    private bool IsEligible(UpgradeSO upgrade, int currentRound)
    {
        if (upgrade == null) return false;

        // Defensa extra: si el pool tiene un asset huérfano o mal configurado
        // (upgradeId vacío/por defecto, típico de restos de un catálogo
        // anterior), lo ignoramos en vez de ofrecerlo roto en la tienda.
        if (string.IsNullOrEmpty(upgrade.upgradeId) || upgrade.upgradeId == "mejora_nueva") return false;

        if (currentRound < upgrade.unlockRound) return false;

        int chosenSoFar = timesChosen.TryGetValue(upgrade, out int count) ? count : 0;

        if (!upgrade.isRepeatable && chosenSoFar > 0) return false;
        if (upgrade.isRepeatable && upgrade.maxStacks > 0 && chosenSoFar >= upgrade.maxStacks) return false;

        foreach (UpgradeSO required in upgrade.requiredUpgrades)
        {
            if (required == null) continue;
            if (!timesChosen.ContainsKey(required)) return false;
        }

        return true;
    }

    private List<UpgradeSO> PickRandomDistinct(List<UpgradeSO> source, int count)
    {
        List<UpgradeSO> pool = new List<UpgradeSO>(source);
        List<UpgradeSO> result = new List<UpgradeSO>();

        int picks = Mathf.Min(count, pool.Count);
        for (int i = 0; i < picks; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }

    /// <summary>
    /// Aplica el efecto de la mejora elegida y la registra como "elegida"
    /// para futuras comprobaciones de elegibilidad (requisitos, maxStacks...).
    /// </summary>
    public void ChooseUpgrade(UpgradeSO chosen)
    {
        if (chosen == null) return;

        chosen.Apply();

        timesChosen[chosen] = (timesChosen.TryGetValue(chosen, out int count) ? count : 0) + 1;

        OnUpgradeChosen?.Invoke(chosen);
    }

    public int TimesChosen(UpgradeSO upgrade)
    {
        return timesChosen.TryGetValue(upgrade, out int count) ? count : 0;
    }
}
