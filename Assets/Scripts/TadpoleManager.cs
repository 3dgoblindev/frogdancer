using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

/// <summary>
/// Genera renacuajos periódicamente usando pooling dinámico (reutiliza
/// instancias en vez de Instantiate/Destroy constantemente) y elige qué
/// tipo aparece consultando la TadpoleSpawnTable con el nivel actual del save.
///
/// El pool es dinámico: si no hay ninguno libre de un tipo, se instancia uno
/// nuevo (no hay un tamaño fijo pre-creado); una vez creado, se reutiliza
/// siempre a través de ReturnToPool en vez de destruirse.
///
/// Si hay un RoundManager en la escena, deja de generar renacuajos nuevos
/// mientras RoundManager.IsRoundActive sea false (tienda abierta o partida
/// terminada); los renacuajos ya activos siguen su comportamiento normal.
/// </summary>
public class TadpoleManager : MonoBehaviour
{
    public static TadpoleManager Instance { get; private set; }

    [Header("Configuración de aparición")]
    [SerializeField] private TadpoleSpawnTable spawnTable;
    [Tooltip("Puntos donde pueden aparecer los renacuajos (defínelos tú desde el editor, p.ej. 4 esquinas).")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 3f;
    [Tooltip("Límite de renacuajos activos a la vez, para no descontrolar el pooling.")]
    [SerializeField] private int maxActiveTadpoles = 20;

    [Header("Comportamiento idle")]
    [Tooltip("Objeto de referencia hacia el que cada renacuajo da su primer paso al aparecer (p.ej. el centro de la escena/pantalla). Puede dejarse vacío.")]
    [SerializeField] private Transform centerPoint;
    public Transform CenterPoint => centerPoint;

    [Header("Feedback de renacuajo salvado")]
    [Tooltip("Placeholder: MMFeedbacks a disparar en la posición del renacuajo cada vez que llega a su rana. Vive aquí (no en cada Tadpole) para configurarlo una sola vez para todos los tipos/pools.")]
    [SerializeField] private MMFeedbacks tadpoleSavedFeedback;
    [Tooltip("Reproduce el sonido armónico al salvar un renacuajo. Vive en el manager y NO en cada Tadpole a propósito: al haber pooling, la secuencia de pitch (armonía ascendente) debe ser UNA sola compartida por todos los rescates, en vez de reiniciarse cada vez que se recicla una instancia.")]
    [SerializeField] private HarmonicAudioPlayer tadpoleSavedAudio;

    // Multiplicador global de velocidad aplicado a los renacuajos al spawnear
    // (lo suben las mejoras tipo TadpoleMoveSpeedMultiplier). 1 = sin cambios.
    public float GlobalMoveSpeedMultiplier { get; private set; } = 1f;

    // Cuántos renacuajos se han salvado en la partida actual. Como TadpoleManager
    // vive en la escena de juego (no es DontDestroyOnLoad), se reinicia solo al
    // recargar la escena; no necesita reset manual entre partidas.
    public int TadpolesSaved { get; private set; }

    // Un pool (cola) por cada tipo de renacuajo, ya que cada tipo usa un prefab distinto.
    private readonly Dictionary<TadpoleType, Queue<Tadpole>> pools = new Dictionary<TadpoleType, Queue<Tadpole>>();
    private readonly List<Tadpole> activeTadpoles = new List<Tadpole>();

    private Transform poolContainer;
    private float spawnTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Contenedor solo para mantener limpia la jerarquía con los renacuajos "dormidos".
        poolContainer = new GameObject("TadpolePool (inactivos)").transform;
        poolContainer.SetParent(transform);
    }

    private void Update()
    {
        // Mientras la tienda está abierta o la partida terminó, no spawneamos más.
        if (RoundManager.Instance != null && !RoundManager.Instance.IsRoundActive) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            TrySpawnTadpole();
        }
    }

    // ---------------------------------------------------------------
    // SPAWN
    // ---------------------------------------------------------------
    private void TrySpawnTadpole()
    {
        if (spawnTable == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        // Limpieza defensiva: si algún renacuajo se destruyó por fuera del flujo normal
        // (p. ej. al cargar otra escena), que no se quede contando para el límite.
        activeTadpoles.RemoveAll(t => t == null);
        if (activeTadpoles.Count >= maxActiveTadpoles)
        {
            return;
        }

        int currentLevel = SavesManager.Instance != null ? SavesManager.Instance.Data.currentLevel : 1;
        TadpoleType type = spawnTable.GetRandomType(currentLevel);
        if (type == null)
        {
            return; // ningún tipo desbloqueado todavía a este nivel
        }

        Tadpole tadpole = GetFromPool(type);
        if (tadpole == null)
        {
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        tadpole.transform.SetParent(null);
        tadpole.transform.position = spawnPoint.position;

        tadpole.Initialize(type);
        tadpole.gameObject.SetActive(true);

        activeTadpoles.Add(tadpole);
    }

    // ---------------------------------------------------------------
    // POOLING
    // ---------------------------------------------------------------
    private Tadpole GetFromPool(TadpoleType type)
    {
        Queue<Tadpole> queue = GetOrCreateQueue(type);

        if (queue.Count > 0)
        {
            return queue.Dequeue();
        }

        // No había ninguno libre de este tipo: se crea uno nuevo (pooling dinámico).
        if (type.tadpolePrefab == null)
        {
            Debug.LogWarning($"[TadpoleManager] El tipo '{type.typeId}' no tiene prefab asignado.");
            return null;
        }

        GameObject instance = Instantiate(type.tadpolePrefab);
        Tadpole tadpole = instance.GetComponent<Tadpole>();

        if (tadpole == null)
        {
            Debug.LogError($"[TadpoleManager] El prefab de '{type.typeId}' no tiene el componente Tadpole.");
            Destroy(instance);
            return null;
        }

        return tadpole;
    }

    /// <summary>
    /// Devuelve un renacuajo al pool para reutilizarlo más adelante. Llamado
    /// por el propio Tadpole cuando termina su ciclo de vida (p. ej. al ser salvado).
    /// </summary>
    public void ReturnToPool(Tadpole tadpole)
    {
        activeTadpoles.Remove(tadpole);

        tadpole.gameObject.SetActive(false);
        tadpole.transform.SetParent(poolContainer);

        if (tadpole.Type == null)
        {
            // No debería pasar si siempre se spawnea vía este manager, pero por seguridad no lo perdemos:
            Debug.LogWarning($"[TadpoleManager] Se devolvió un renacuajo sin tipo asignado ({tadpole.name}); no se pudo poolear correctamente.");
            return;
        }

        GetOrCreateQueue(tadpole.Type).Enqueue(tadpole);
    }

    /// <summary>
    /// Llamado por Tadpole.HandleSaved() justo antes de volver al pool.
    /// Centraliza aquí el feedback/sonido de "renacuajo salvado" porque, al
    /// haber pooling, el HarmonicAudioPlayer debe ser uno solo compartido por
    /// todos los renacuajos (si viviera en cada Tadpole, cada instancia
    /// llevaría su propia cuenta de pitch y no sonaría como una secuencia
    /// armónica conjunta a lo largo de la ronda).
    /// </summary>
    public void NotifyTadpoleSaved(Tadpole tadpole)
    {
        TadpolesSaved++;

        tadpoleSavedFeedback?.PlayFeedbacks(tadpole.transform.position);
        tadpoleSavedAudio?.Play();
    }

    private Queue<Tadpole> GetOrCreateQueue(TadpoleType type)
    {
        if (!pools.TryGetValue(type, out Queue<Tadpole> queue))
        {
            queue = new Queue<Tadpole>();
            pools[type] = queue;
        }

        return queue;
    }

    // ---------------------------------------------------------------
    // MEJORAS (llamadas desde UpgradeSO.Apply())
    // ---------------------------------------------------------------
    /// <summary>Multiplica el intervalo de spawn (p.ej. 0.85 = spawnea un 15% más seguido). Se acumula entre mejoras.</summary>
    public void MultiplySpawnInterval(float multiplier)
    {
        spawnInterval = Mathf.Max(0.1f, spawnInterval * multiplier);
    }

    public void IncreaseMaxActiveTadpoles(int amount)
    {
        maxActiveTadpoles = Mathf.Max(1, maxActiveTadpoles + amount);
    }

    /// <summary>Multiplica la velocidad de los renacuajos que spawneen a partir de ahora. Se acumula entre mejoras.</summary>
    public void MultiplyGlobalMoveSpeed(float multiplier)
    {
        GlobalMoveSpeedMultiplier *= multiplier;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
