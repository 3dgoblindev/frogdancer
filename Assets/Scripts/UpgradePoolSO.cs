using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pool de todas las mejoras posibles del juego. Es solo el "catálogo":
/// no decide cuáles están desbloqueadas ni cuáles ya se eligieron, eso lo
/// resuelve UpgradeManager en tiempo de ejecución usando los datos de cada
/// UpgradeSO (unlockRound, requiredUpgrades, isRepeatable...).
/// Crear desde Assets > Create > Frogs > Mejoras > Upgrade Pool.
/// </summary>
[CreateAssetMenu(fileName = "UpgradePool", menuName = "Frogs/Mejoras/Upgrade Pool")]
public class UpgradePoolSO : ScriptableObject
{
    public List<UpgradeSO> upgrades = new List<UpgradeSO>();
}
