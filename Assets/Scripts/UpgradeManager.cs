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

    [Header("Bandas de tier por tienda")]
    [Tooltip("El sorteo ponderado por peso seguía dejando que salieran Tier1 en tiendas avanzadas (solo bajaba la probabilidad, no la eliminaba). Ahora cada tienda ofrece ÚNICAMENTE mejoras del tier que le toca según su posición: las primeras 'tier1ShopCount' tiendas son 100% Tier1, las siguientes 'tier2ShopCount' son 100% Tier2, y el resto (hasta la última tienda de la partida) son 100% Tier3.")]
    [SerializeField] private int tier1ShopCount = 2;
    [SerializeField] private int tier2ShopCount = 2;

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
    /// Devuelve hasta "optionsPerShop" mejoras distintas, elegidas al azar
    /// SOLO entre las mejoras del tier que le toca a "currentRound" (ver
    /// GetTierForShop) y cuyos requisitos ya se cumplen. Puede devolver menos
    /// si no hay suficientes elegibles de ese tier.
    /// </summary>
    public List<UpgradeSO> GenerateShopOptions(int currentRound)
    {
        UpgradeSO.UpgradeTier shopTier = GetTierForShop(currentRound);

        List<UpgradeSO> eligible = new List<UpgradeSO>();

        if (upgradePool != null)
        {
            foreach (UpgradeSO upgrade in upgradePool.upgrades)
            {
                if (upgrade != null && upgrade.tier == shopTier && IsEligible(upgrade, currentRound))
                {
                    eligible.Add(upgrade);
                }
            }
        }

        return PickRandomDistinct(eligible, optionsPerShop);
    }

    /// <summary>
    /// "currentRound" aquí es la ronda que ACABA de terminar (RoundManager
    /// llama a esto antes de incrementar CurrentRound), así que la tienda
    /// tras la ronda 1 usa currentRound=1, tras la ronda 2 usa currentRound=2, etc.
    /// Con los valores por defecto (2/2): tiendas 1-2 -> Tier1, 3-4 -> Tier2,
    /// 5 en adelante -> Tier3.
    /// </summary>
    private UpgradeSO.UpgradeTier GetTierForShop(int currentRound)
    {
        if (currentRound <= tier1ShopCount) return UpgradeSO.UpgradeTier.Tier1;
        if (currentRound <= tier1ShopCount + tier2ShopCount) return UpgradeSO.UpgradeTier.Tier2;
        return UpgradeSO.UpgradeTier.Tier3;
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
