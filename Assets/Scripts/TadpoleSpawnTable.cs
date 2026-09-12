using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Entrada de la tabla de aparición: asocia un TadpoleType con su peso de
/// probabilidad, y cómo ese peso escala con el nivel actual. Es el "objeto
/// intermedio" entre el tipo de renacuajo (qué es) y el manager (cuándo/con
/// qué probabilidad aparece).
/// </summary>
[System.Serializable]
public class TadpoleSpawnEntry
{
    public TadpoleType type;

    [Tooltip("Peso de aparición justo al alcanzar 'unlockLevel'.")]
    public float baseWeight = 1f;

    [Tooltip("Cuánto sube (o baja, si es negativo) el peso por cada nivel por encima de 'unlockLevel'.")]
    public float weightPerLevel = 0f;

    [Tooltip("Nivel mínimo en el que este tipo puede empezar a aparecer. Por debajo, peso = 0.")]
    public int unlockLevel = 1;

    public float GetWeightForLevel(int level)
    {
        if (level < unlockLevel)
        {
            return 0f;
        }

        float weight = baseWeight + weightPerLevel * (level - unlockLevel);
        return Mathf.Max(0f, weight);
    }
}

/// <summary>
/// Tabla de probabilidades de todos los tipos de renacuajo, en función del
/// nivel actual del jugador. Es un ScriptableObject (preset de diseño) para
/// que puedas ajustar las probabilidades sin tocar código.
/// Crear desde Assets > Create > Frogs > Tadpole Spawn Table.
/// </summary>
[CreateAssetMenu(fileName = "TadpoleSpawnTable", menuName = "Frogs/Tadpole Spawn Table")]
public class TadpoleSpawnTable : ScriptableObject
{
    public List<TadpoleSpawnEntry> entries = new List<TadpoleSpawnEntry>();

    /// <summary>
    /// Elige un TadpoleType al azar, ponderado según los pesos de cada entrada
    /// para el nivel dado. Devuelve null si ningún tipo está desbloqueado a ese nivel.
    /// </summary>
    public TadpoleType GetRandomType(int level)
    {
        float totalWeight = 0f;
        foreach (TadpoleSpawnEntry entry in entries)
        {
            totalWeight += entry.GetWeightForLevel(level);
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (TadpoleSpawnEntry entry in entries)
        {
            float weight = entry.GetWeightForLevel(level);
            if (weight <= 0f) continue;

            cumulative += weight;
            if (roll <= cumulative)
            {
                return entry.type;
            }
        }

        // Fallback de seguridad por si el redondeo flotante deja algo fuera.
        return entries.Count > 0 ? entries[entries.Count - 1].type : null;
    }
}