#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Herramienta de Editor: genera el catálogo completo de mejoras en 3 tiers
/// (11 familias x 3 tiers = 33 StatUpgradeSO) y los añade a un UpgradePoolSO.
///
/// Convenciones aplicadas automáticamente:
/// - unlockRound: Tier1 = ronda 1, Tier2 = ronda 3, Tier3 = ronda 5.
/// - Cada T2 requiere (requiredUpgrades) haber elegido su T1, y cada T3 su T2:
///   así la progresión de potencia también es una progresión de elección.
/// - isRepeatable = true en todas, con maxStacks decreciente por tier
///   (T1 más veces apilable, T3 más limitado por ser más potente).
/// - Nombres y descripciones en inglés (placeholder de tono neutro,
///   pensados para retocar luego a mano con más personalidad/humor).
///
/// Uso: menú "Frogs/Mejoras/Generar catálogo de mejoras (3 tiers)".
/// Es seguro volver a ejecutarlo: si el asset de una mejora ya existe, se
/// actualiza en vez de duplicarse.
///
/// NOTA: si ya generaste antes un catálogo con ids distintos (p.ej. de una
/// versión anterior de esta herramienta), esos assets viejos se quedan sueltos
/// en la carpeta y en el pool, y como su upgradeId/displayName nunca se
/// actualizan, pueden aparecer en la tienda como mejoras "rotas" sin nombre.
/// Usa el menú "Frogs/Mejoras/Limpiar mejoras huérfanas" para quitarlos del
/// pool y borrarlos de disco automáticamente, o borra toda la carpeta
/// Assets/Mejoras antes de ejecutar esto para partir de cero.
/// </summary>
public static class UpgradeAssetGenerator
{
    private const string FolderPath = "Assets/Mejoras";
    private const string PoolAssetPath = FolderPath + "/UpgradePool.asset";

    private struct UpgradeDef
    {
        public string id;
        public string displayName;
        public string description;
        public UpgradeSO.UpgradeTier tier;
        public StatUpgradeSO.Target target;
        public float value;
        public int maxStacks;
        public string requiresId; // id de otro UpgradeDef de esta misma lista, o null

        public UpgradeDef(string id, string displayName, string description, UpgradeSO.UpgradeTier tier,
            StatUpgradeSO.Target target, float value, int maxStacks, string requiresId = null)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.tier = tier;
            this.target = target;
            this.value = value;
            this.maxStacks = maxStacks;
            this.requiresId = requiresId;
        }
    }

    private static int UnlockRoundForTier(UpgradeSO.UpgradeTier tier)
    {
        switch (tier)
        {
            case UpgradeSO.UpgradeTier.Tier1: return 1;
            case UpgradeSO.UpgradeTier.Tier2: return 3;
            case UpgradeSO.UpgradeTier.Tier3: return 5;
            default: return 1;
        }
    }

    private static List<UpgradeDef> BuildDefinitions()
    {
        var T1 = UpgradeSO.UpgradeTier.Tier1;
        var T2 = UpgradeSO.UpgradeTier.Tier2;
        var T3 = UpgradeSO.UpgradeTier.Tier3;

        var defs = new List<UpgradeDef>();

        // --- Minimum attraction range ---
        defs.Add(new UpgradeDef("range_min_t1", "Wider Ripple", "Increases the minimum attraction range.",
            T1, StatUpgradeSO.Target.MinAttractionRange, 0.3f, 6));
        defs.Add(new UpgradeDef("range_min_t2", "Booming Call", "Increases the minimum attraction range further.",
            T2, StatUpgradeSO.Target.MinAttractionRange, 0.6f, 4, "range_min_t1"));
        defs.Add(new UpgradeDef("range_min_t3", "Thunderous Voice", "Massively increases the minimum attraction range.",
            T3, StatUpgradeSO.Target.MinAttractionRange, 1.2f, 2, "range_min_t2"));

        // --- Maximum attraction range ---
        defs.Add(new UpgradeDef("range_max_t1", "Longer Reach", "Increases the maximum attraction range.",
            T1, StatUpgradeSO.Target.MaxAttractionRange, 0.5f, 6));
        defs.Add(new UpgradeDef("range_max_t2", "Far-Carrying Song", "Increases the maximum attraction range further.",
            T2, StatUpgradeSO.Target.MaxAttractionRange, 1.0f, 4, "range_max_t1"));
        defs.Add(new UpgradeDef("range_max_t3", "Legendary Croak", "Massively increases the maximum attraction range.",
            T3, StatUpgradeSO.Target.MaxAttractionRange, 2.0f, 2, "range_max_t2"));

        // --- Score per success ---
        defs.Add(new UpgradeDef("score_t1", "Confident Croak", "Earn more points per successful croak.",
            T1, StatUpgradeSO.Target.ScorePerSuccess, 1f, 6));
        defs.Add(new UpgradeDef("score_t2", "Show-Off Croak", "Earn even more points per successful croak.",
            T2, StatUpgradeSO.Target.ScorePerSuccess, 3f, 4, "score_t1"));
        defs.Add(new UpgradeDef("score_t3", "Rockstar Croak", "Earn a lot more points per successful croak.",
            T3, StatUpgradeSO.Target.ScorePerSuccess, 6f, 2, "score_t2"));

        // --- Valid window size ---
        defs.Add(new UpgradeDef("window_t1", "Good Ear", "Widens the valid croak window slightly.",
            T1, StatUpgradeSO.Target.WindowSizeSeconds, 0.05f, 5));
        defs.Add(new UpgradeDef("window_t2", "Perfect Pitch", "Widens the valid croak window further.",
            T2, StatUpgradeSO.Target.WindowSizeSeconds, 0.1f, 3, "window_t1"));
        defs.Add(new UpgradeDef("window_t3", "One With The Rhythm", "Widens the valid croak window a lot.",
            T3, StatUpgradeSO.Target.WindowSizeSeconds, 0.2f, 2, "window_t2"));

        // --- Tadpole move speed ---
        defs.Add(new UpgradeDef("tad_speed_t1", "Eager Tadpoles", "Tadpoles swim a bit faster.",
            T1, StatUpgradeSO.Target.TadpoleMoveSpeedMultiplier, 1.1f, 5));
        defs.Add(new UpgradeDef("tad_speed_t2", "Determined Tadpoles", "Tadpoles swim noticeably faster.",
            T2, StatUpgradeSO.Target.TadpoleMoveSpeedMultiplier, 1.25f, 3, "tad_speed_t1"));
        defs.Add(new UpgradeDef("tad_speed_t3", "Torpedo Tadpoles", "Tadpoles swim a lot faster.",
            T3, StatUpgradeSO.Target.TadpoleMoveSpeedMultiplier, 1.5f, 2, "tad_speed_t2"));

        // --- Spawn interval (lower multiplier = tadpoles appear more often) ---
        defs.Add(new UpgradeDef("spawn_t1", "Busy Pond", "Tadpoles appear a bit more often.",
            T1, StatUpgradeSO.Target.SpawnIntervalMultiplier, 0.95f, 5));
        defs.Add(new UpgradeDef("spawn_t2", "Crowded Pond", "Tadpoles appear noticeably more often.",
            T2, StatUpgradeSO.Target.SpawnIntervalMultiplier, 0.85f, 3, "spawn_t1"));
        defs.Add(new UpgradeDef("spawn_t3", "Tadpole Rush", "Tadpoles appear a lot more often.",
            T3, StatUpgradeSO.Target.SpawnIntervalMultiplier, 0.7f, 2, "spawn_t2"));

        // --- Max active tadpoles ---
        defs.Add(new UpgradeDef("cap_t1", "More Room", "Increases the max number of active tadpoles.",
            T1, StatUpgradeSO.Target.MaxActiveTadpoles, 3f, 5));
        defs.Add(new UpgradeDef("cap_t2", "Bigger Pond", "Increases the max number of active tadpoles further.",
            T2, StatUpgradeSO.Target.MaxActiveTadpoles, 6f, 3, "cap_t1"));
        defs.Add(new UpgradeDef("cap_t3", "Endless Pond", "Massively increases the max number of active tadpoles.",
            T3, StatUpgradeSO.Target.MaxActiveTadpoles, 12f, 2, "cap_t2"));

        // --- Croak cycle duration (faster rhythm) ---
        defs.Add(new UpgradeDef("cycle_t1", "Quicker Beat", "Slightly speeds up the croak rhythm.",
            T1, StatUpgradeSO.Target.CroackCycleDuration, 0.05f, 4));
        defs.Add(new UpgradeDef("cycle_t2", "Fast Beat", "Speeds up the croak rhythm further.",
            T2, StatUpgradeSO.Target.CroackCycleDuration, 0.1f, 3, "cycle_t1"));
        defs.Add(new UpgradeDef("cycle_t3", "Frantic Beat", "Speeds up the croak rhythm a lot.",
            T3, StatUpgradeSO.Target.CroackCycleDuration, 0.2f, 1, "cycle_t2"));

        // --- Lilypad move speed: T1/T2 add flat speed, T3 switches to a multiplier ---
        defs.Add(new UpgradeDef("lily_speed_t1", "Steady Paddle", "Increases lilypad cruise speed.",
            T1, StatUpgradeSO.Target.LilypadMoveSpeed, 0.5f, 6));
        defs.Add(new UpgradeDef("lily_speed_t2", "Strong Paddle", "Increases lilypad cruise speed further.",
            T2, StatUpgradeSO.Target.LilypadMoveSpeed, 1.0f, 4, "lily_speed_t1"));
        defs.Add(new UpgradeDef("lily_speed_t3", "Turbo Paddle", "Multiplies lilypad cruise speed.",
            T3, StatUpgradeSO.Target.LilypadMoveSpeedMultiplier, 1.3f, 2, "lily_speed_t2"));

        // --- Lilypad max speed ---
        defs.Add(new UpgradeDef("lily_max_t1", "Higher Gear", "Increases lilypad max speed.",
            T1, StatUpgradeSO.Target.LilypadMaxSpeed, 0.75f, 6));
        defs.Add(new UpgradeDef("lily_max_t2", "Overdrive", "Increases lilypad max speed further.",
            T2, StatUpgradeSO.Target.LilypadMaxSpeed, 1.5f, 4, "lily_max_t1"));
        defs.Add(new UpgradeDef("lily_max_t3", "Ludicrous Speed", "Massively increases lilypad max speed.",
            T3, StatUpgradeSO.Target.LilypadMaxSpeed, 3.0f, 2, "lily_max_t2"));

        // --- Lilypad acceleration ---
        defs.Add(new UpgradeDef("lily_accel_t1", "Snappy Response", "Increases lilypad acceleration.",
            T1, StatUpgradeSO.Target.LilypadAcceleration, 3f, 6));
        defs.Add(new UpgradeDef("lily_accel_t2", "Sharp Response", "Increases lilypad acceleration further.",
            T2, StatUpgradeSO.Target.LilypadAcceleration, 6f, 4, "lily_accel_t1"));
        defs.Add(new UpgradeDef("lily_accel_t3", "Instant Response", "Massively increases lilypad acceleration.",
            T3, StatUpgradeSO.Target.LilypadAcceleration, 12f, 2, "lily_accel_t2"));

        return defs;
    }

    /// <summary>
    /// Limpieza compartida: quita del pool y borra de disco cualquier
    /// StatUpgradeSO cuyo upgradeId no esté entre los ids actuales de
    /// BuildDefinitions(). A diferencia de una versión anterior de esta
    /// limpieza, escanea TODOS los assets físicos de la carpeta (no solo los
    /// que ya estaban en pool.upgrades), para pillar también archivos sueltos
    /// que nunca se llegaron a añadir al pool.
    /// </summary>
    private static void CleanOrphans(UpgradePoolSO pool, HashSet<string> validIds, out int removedFromPool, out int deletedAssets)
    {
        removedFromPool = 0;
        deletedAssets = 0;

        // 1) Quitar del pool referencias rotas (null) o con un id que ya no existe.
        for (int i = pool.upgrades.Count - 1; i >= 0; i--)
        {
            UpgradeSO upgrade = pool.upgrades[i];
            bool isBrokenReference = upgrade == null;
            bool isOrphanId = upgrade != null && (string.IsNullOrEmpty(upgrade.upgradeId) || !validIds.Contains(upgrade.upgradeId));

            if (isBrokenReference || isOrphanId)
            {
                pool.upgrades.RemoveAt(i);
                removedFromPool++;
            }
        }

        // 2) Recorrer TODOS los StatUpgradeSO físicos de la carpeta, estén o
        // no en el pool, y borrar los que no correspondan a ningún id actual.
        string[] guids = AssetDatabase.FindAssets("t:StatUpgradeSO", new[] { FolderPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StatUpgradeSO asset = AssetDatabase.LoadAssetAtPath<StatUpgradeSO>(path);
            if (asset == null) continue;

            if (!string.IsNullOrEmpty(asset.upgradeId) && validIds.Contains(asset.upgradeId)) continue;

            pool.upgrades.Remove(asset);
            AssetDatabase.DeleteAsset(path);
            deletedAssets++;
        }
    }

    [MenuItem("Frogs/Mejoras/Limpiar mejoras huérfanas")]
    public static void CleanOrphanedUpgrades()
    {
        UpgradePoolSO pool = AssetDatabase.LoadAssetAtPath<UpgradePoolSO>(PoolAssetPath);
        if (pool == null)
        {
            Debug.LogWarning($"[UpgradeAssetGenerator] No hay ningún UpgradePool en {PoolAssetPath}. Nada que limpiar.");
            return;
        }

        HashSet<string> validIds = new HashSet<string>();
        foreach (UpgradeDef def in BuildDefinitions())
        {
            validIds.Add(def.id);
        }

        CleanOrphans(pool, validIds, out int removedFromPool, out int deletedAssets);

        EditorUtility.SetDirty(pool);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[UpgradeAssetGenerator] Limpieza completa: {removedFromPool} entradas huérfanas quitadas del pool, {deletedAssets} assets borrados de disco.");
    }

    /// <summary>
    /// No borra ni cambia nada: solo imprime en la consola, para cada
    /// StatUpgradeSO físico en la carpeta, el nombre real del archivo junto a
    /// su upgradeId/displayName interno. Útil cuando algo "sale roto" en la
    /// tienda y quieres ver a qué archivo concreto corresponde antes de
    /// borrar nada.
    /// </summary>
    [MenuItem("Frogs/Mejoras/Diagnosticar mejoras")]
    public static void DiagnoseUpgrades()
    {
        HashSet<string> validIds = new HashSet<string>();
        foreach (UpgradeDef def in BuildDefinitions())
        {
            validIds.Add(def.id);
        }

        string[] guids = AssetDatabase.FindAssets("t:StatUpgradeSO", new[] { FolderPath });
        Debug.Log($"[UpgradeAssetGenerator] {guids.Length} StatUpgradeSO encontrados en {FolderPath}:");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StatUpgradeSO asset = AssetDatabase.LoadAssetAtPath<StatUpgradeSO>(path);
            if (asset == null)
            {
                Debug.LogWarning($"  - {path} -> no se pudo cargar como StatUpgradeSO.");
                continue;
            }

            bool ok = !string.IsNullOrEmpty(asset.upgradeId) && validIds.Contains(asset.upgradeId);
            string tag = ok ? "OK" : "HUÉRFANO";
            Debug.Log($"  - [{tag}] archivo={path} | upgradeId=\"{asset.upgradeId}\" | displayName=\"{asset.displayName}\"");
        }

        UpgradePoolSO pool = AssetDatabase.LoadAssetAtPath<UpgradePoolSO>(PoolAssetPath);
        if (pool != null)
        {
            for (int i = 0; i < pool.upgrades.Count; i++)
            {
                if (pool.upgrades[i] == null)
                {
                    Debug.LogWarning($"  - pool.upgrades[{i}] es una referencia rota (null).");
                }
            }
        }
    }

    [MenuItem("Frogs/Mejoras/Generar catálogo de mejoras (3 tiers)")]
    public static void GenerateAll()
    {
        if (!AssetDatabase.IsValidFolder(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
            AssetDatabase.Refresh();
        }

        UpgradePoolSO pool = AssetDatabase.LoadAssetAtPath<UpgradePoolSO>(PoolAssetPath);
        if (pool == null)
        {
            pool = ScriptableObject.CreateInstance<UpgradePoolSO>();
            AssetDatabase.CreateAsset(pool, PoolAssetPath);
        }

        List<UpgradeDef> definitions = BuildDefinitions();
        Dictionary<string, StatUpgradeSO> createdById = new Dictionary<string, StatUpgradeSO>();

        int created = 0;

        // Primera pasada: crear (o actualizar) cada asset, sin resolver
        // requiredUpgrades todavía (puede que el asset requerido no exista aún).
        foreach (UpgradeDef def in definitions)
        {
            string assetPath = $"{FolderPath}/StatUpgrade_{def.id}.asset";
            StatUpgradeSO upgrade = AssetDatabase.LoadAssetAtPath<StatUpgradeSO>(assetPath);

            if (upgrade == null)
            {
                upgrade = ScriptableObject.CreateInstance<StatUpgradeSO>();
                AssetDatabase.CreateAsset(upgrade, assetPath);
                created++;
            }

            upgrade.upgradeId = def.id;
            upgrade.displayName = def.displayName;
            upgrade.description = def.description;
            upgrade.tier = def.tier;
            upgrade.target = def.target;
            upgrade.value = def.value;
            upgrade.unlockRound = UnlockRoundForTier(def.tier);
            upgrade.isRepeatable = true;
            upgrade.maxStacks = def.maxStacks;

            EditorUtility.SetDirty(upgrade);
            createdById[def.id] = upgrade;

            if (!pool.upgrades.Contains(upgrade))
            {
                pool.upgrades.Add(upgrade);
            }
        }

        // Segunda pasada: ahora que todos los assets existen, enlazar
        // requiredUpgrades (T2 requiere su T1, T3 requiere su T2).
        foreach (UpgradeDef def in definitions)
        {
            if (string.IsNullOrEmpty(def.requiresId)) continue;
            if (!createdById.TryGetValue(def.requiresId, out StatUpgradeSO required)) continue;

            StatUpgradeSO upgrade = createdById[def.id];
            upgrade.requiredUpgrades.Clear();
            upgrade.requiredUpgrades.Add(required);
            EditorUtility.SetDirty(upgrade);
        }

        // Tercera pasada: autolimpieza. Cualquier asset físico en la carpeta
        // (esté o no en el pool) cuyo upgradeId no sea uno de los ids que
        // acabamos de generar se considera huérfano y se borra. Así no hace
        // falta acordarse de ejecutar "Limpiar mejoras huérfanas" a mano.
        HashSet<string> validIds = new HashSet<string>();
        foreach (UpgradeDef def in definitions)
        {
            validIds.Add(def.id);
        }
        CleanOrphans(pool, validIds, out int removedFromPool, out int deletedAssets);

        EditorUtility.SetDirty(pool);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[UpgradeAssetGenerator] Catálogo listo en {FolderPath}. Creadas {created} mejoras nuevas, {removedFromPool} huérfanas quitadas del pool, {deletedAssets} assets huérfanos borrados. Pool con {pool.upgrades.Count} en total.");
        Selection.activeObject = pool;
    }
}
#endif
