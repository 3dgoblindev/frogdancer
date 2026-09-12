using UnityEngine;

/// <summary>
/// Un renacuajo se gestiona a sí mismo: cuando hay un croar exitoso, comprueba
/// si hay alguna rana dentro de su rango de atracción y, si la hay, empieza a
/// moverse hacia la más cercana. La rana no sabe nada de esto: solo emite
/// eventos (GameManager.OnCroakSuccess) y se registra en Frog.ActiveFrogs.
/// </summary>
public class Tadpole : MonoBehaviour
{
    private enum State
    {
        Waiting,      // esperando a que alguna rana lo atraiga
        MovingToFrog, // yendo hacia la rana objetivo
        Saved         // llegó a la rana (rescatado)
    }

    [Header("Recompensa")]
    [Tooltip("Puntos que suma al marcador de la partida actual (GameManager.Score) cuando este renacuajo es salvado.")]
    [SerializeField] private int pointsValue = 10;

    // Ya no es un valor fijo propio: lo da GameManager (interpolado entre su
    // min/max según el combo actual) y se mantiene al día por evento.
    private float currentAttractionRange;

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 1.5f;
    [Tooltip("Distancia a la que consideramos que el renacuajo ha llegado a su destino (rana o punto idle).")]
    [SerializeField] private float arrivalThreshold = 0.15f;

    [Header("Idle (deambular mientras espera)")]
    [Tooltip("Velocidad al deambular en idle (normalmente más lenta que yendo hacia una rana).")]
    [SerializeField] private float idleMoveSpeed = 0.5f;
    [Tooltip("Radio máximo alrededor de su posición actual para elegir el siguiente punto idle.")]
    [SerializeField] private float idleMoveRadius = 1.5f;
    [Tooltip("Espera mínima y máxima (segundos) entre un movimiento idle y el siguiente.")]
    [SerializeField] private float idleMinWaitTime = 1f;
    [SerializeField] private float idleMaxWaitTime = 3f;

    private State state = State.Waiting;
    private Frog targetFrog;

    // Estado del deambular idle
    private bool hasIdleTarget;
    private Vector3 idleTargetPosition;
    private float idleWaitTimer;
    private bool firstIdleMoveTowardsCenter;

    // El tipo con el que se creó/spawneó este renacuajo. Lo necesita el
    // TadpoleManager para saber a qué cola de pool devolverlo.
    public TadpoleType Type { get; private set; }

    /// <summary>
    /// Configura este renacuajo con los stats de un tipo y resetea su estado
    /// para reutilizarlo desde el pool (o para un primer spawn).
    /// </summary>
    public void Initialize(TadpoleType type)
    {
        Type = type;
        pointsValue = type.pointsValue;
        moveSpeed = type.moveSpeed;

        // Las mejoras de velocidad global (TadpoleMoveSpeedMultiplier) se aplican
        // aquí, al (re)spawnear, para que afecten también a los renacuajos reciclados del pool.
        if (TadpoleManager.Instance != null)
        {
            moveSpeed *= TadpoleManager.Instance.GlobalMoveSpeedMultiplier;
        }

        state = State.Waiting;
        targetFrog = null;

        // Al (re)spawnear, el primer movimiento idle irá hacia el centro (si hay uno definido),
        // y no espera: puede arrancar a moverse en el próximo frame.
        hasIdleTarget = false;
        idleWaitTimer = 0f;
        firstIdleMoveTowardsCenter = true;
    }

    // ---------------------------------------------------------------
    // SUSCRIPCIÓN A EVENTOS
    // ---------------------------------------------------------------
    private void OnEnable()
    {
        GameManager.OnCroakSuccess += HandleCroakSuccess;
        GameManager.OnAttractionRangeChanged += HandleAttractionRangeChanged;

        // Por si el renacuajo se activa (o reutiliza de un pool) a mitad de partida,
        // coge el rango vigente en vez de esperar al próximo cambio de combo.
        if (GameManager.Instance != null)
        {
            currentAttractionRange = GameManager.Instance.CurrentAttractionRange;
        }
    }

    private void OnDisable()
    {
        GameManager.OnCroakSuccess -= HandleCroakSuccess;
        GameManager.OnAttractionRangeChanged -= HandleAttractionRangeChanged;
    }

    private void HandleAttractionRangeChanged(float newRange)
    {
        currentAttractionRange = newRange;
    }

    private void Update()
    {
        switch (state)
        {
            case State.MovingToFrog:
                if (targetFrog != null)
                {
                    MoveTowardsTarget();
                }
                break;

            case State.Waiting:
                UpdateIdle();
                break;

            case State.Saved:
                // Ya está de vuelta en el pool / desactivado; no hay nada que hacer.
                break;
        }
    }

    // ---------------------------------------------------------------
    // REACCIÓN AL CROAR
    // ---------------------------------------------------------------
    private void HandleCroakSuccess()
    {
        // Si ya está en camino o ya fue salvado, un nuevo croar no cambia nada
        // (más adelante aquí podría entrar lógica de "cambiar a una rana más cercana", etc.)
        if (state != State.Waiting)
        {
            return;
        }

        Frog nearestFrog = FindNearestFrogInRange();
        if (nearestFrog != null)
        {
            targetFrog = nearestFrog;
            state = State.MovingToFrog;
            Debug.Log($"[Renacuajo {name}] atraído por {nearestFrog.name}, empieza a nadar hacia ella.");
        }
    }

    private Frog FindNearestFrogInRange()
    {
        Frog nearest = null;
        float nearestSqrDistance = currentAttractionRange * currentAttractionRange;

        foreach (Frog frog in Frog.ActiveFrogs)
        {
            float sqrDistance = (frog.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance <= nearestSqrDistance)
            {
                nearest = frog;
                nearestSqrDistance = sqrDistance;
            }
        }

        return nearest;
    }

    // ---------------------------------------------------------------
    // IDLE (deambular mientras no hay ninguna rana atrayéndolo)
    // ---------------------------------------------------------------
    private void UpdateIdle()
    {
        if (hasIdleTarget)
        {
            transform.position = Vector3.MoveTowards(transform.position, idleTargetPosition, idleMoveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(Vector3.forward, idleTargetPosition - transform.position);
            float sqrDistance = (idleTargetPosition - transform.position).sqrMagnitude;
            if (sqrDistance <= arrivalThreshold * arrivalThreshold)
            {
                hasIdleTarget = false;
                idleWaitTimer = Random.Range(idleMinWaitTime, idleMaxWaitTime);
            }

            return;
        }

        idleWaitTimer -= Time.deltaTime;
        if (idleWaitTimer <= 0f)
        {
            PickNewIdleTarget();
        }
    }

    private void PickNewIdleTarget()
    {
        Vector3 offset;

        if (firstIdleMoveTowardsCenter && TadpoleManager.Instance != null && TadpoleManager.Instance.CenterPoint != null)
        {
            Vector3 towardsCenter = TadpoleManager.Instance.CenterPoint.position - transform.position;
            offset = towardsCenter.sqrMagnitude > 0.0001f
                ? towardsCenter.normalized * idleMoveRadius
                : (Vector3)(Random.insideUnitCircle * idleMoveRadius);
        }
        else
        {
            offset = Random.insideUnitCircle * idleMoveRadius;
        }

        firstIdleMoveTowardsCenter = false;
        idleTargetPosition = transform.position + offset;
        hasIdleTarget = true;
    }

    // ---------------------------------------------------------------
    // MOVIMIENTO HACIA LA RANA
    // ---------------------------------------------------------------
    private void MoveTowardsTarget()
    {
        Vector3 targetPosition = targetFrog.transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(Vector3.forward, targetPosition - transform.position);

        float sqrDistance = (targetPosition - transform.position).sqrMagnitude;
        if (sqrDistance <= arrivalThreshold * arrivalThreshold)
        {
            HandleSaved();
        }
    }

    private void HandleSaved()
    {
        state = State.Saved;
        Debug.Log($"[Renacuajo {name}] ¡a salvo! llegó junto a {targetFrog.name}. +{pointsValue} puntos.");

        // Los puntos ya no se escriben directo en el save: solo cuentan para
        // la puntuación de ESTA partida. RoundManager compara esa puntuación
        // final contra el highscore guardado cuando termina la partida.
        GameManager.Instance?.AddTadpoleScore(pointsValue);

        // El feel + el sonido armónico de "renacuajo salvado" viven en el
        // TadpoleManager (no aquí) porque, al haber pooling, la secuencia de
        // pitch debe ser una sola compartida por todos los renacuajos.
        TadpoleManager.Instance?.NotifyTadpoleSaved(this);

        if (TadpoleManager.Instance != null)
        {
            TadpoleManager.Instance.ReturnToPool(this);
        }
        else
        {
            // Fallback por si se está probando este objeto suelto en la escena, sin manager.
            gameObject.SetActive(false);
        }
    }
}
