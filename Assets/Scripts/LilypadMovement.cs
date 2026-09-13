using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla el movimiento del nenúfar (el mismo GameObject que lleva Frog)
/// al hacer click con el ratón: calcula el punto del mundo bajo el cursor
/// y mueve el Rigidbody2D hacia ahí por físicas (velocity), no por
/// transform.position ni MovePosition, para que las colisiones con otros
/// nenúfares/objetos respondan de forma natural (empujones, rebotes, etc.).
///
/// Solo debe existir UNA instancia activa (el nenúfar del jugador), así que
/// sigue el mismo patrón singleton que GameManager/SavesManager para que
/// las mejoras (StatUpgradeSO) puedan llamar a LilypadMovement.Instance.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class LilypadMovement : MonoBehaviour
{
    public static LilypadMovement Instance { get; private set; }

    [Header("Movimiento (integrable con el sistema de mejoras)")]
    [Tooltip("Velocidad 'de crucero' hacia el objetivo. Sube con mejoras (StatUpgradeSO).")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("Velocidad máxima absoluta que puede alcanzar el nenúfar (por si un empujón físico lo acelera más).")]
    [SerializeField] private float maxSpeed = 8f;
    [Tooltip("Con qué rapidez se acerca la velocidad actual a la velocidad deseada (unidades/seg^2). Más alto = respuesta más 'seca'.")]
    [SerializeField] private float acceleration = 20f;
    [Tooltip("Distancia al objetivo por debajo de la cual se considera 'llegado' y frena.")]
    [SerializeField] private float stoppingDistance = 0.15f;

    [Header("Input / Cámara")]
    [Tooltip("Cámara usada para pasar de posición de pantalla a posición de mundo. Si se deja vacío, usa Camera.main.")]
    [SerializeField] private Camera cam;

    private Rigidbody2D rb;
    private Vector2 targetPosition;
    private bool hasTarget;

    // ---------------------------------------------------------------
    // CICLO DE VIDA
    // ---------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        if (cam == null) cam = Camera.main;

        // Movimiento 2D "de mesa": sin gravedad y sin drag propio, porque
        // el frenado lo controlamos a mano en FixedUpdate.
        rb.gravityScale = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------------------------------------------------------------
    // INPUT (Input System nuevo, igual que GameManager con Keyboard.current)
    // ---------------------------------------------------------------
    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SetTargetFromMouse();
        }
    }

    private void SetTargetFromMouse()
    {
        if (cam == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));

        targetPosition = worldPos;
        hasTarget = true;
    }

    // ---------------------------------------------------------------
    // MOVIMIENTO POR FÍSICAS
    // ---------------------------------------------------------------
    private void FixedUpdate()
    {
        if (!hasTarget) return;

        Vector2 toTarget = targetPosition - rb.position;
        float distance = toTarget.magnitude;

        if (distance <= stoppingDistance)
        {
            // Frenado suave hasta parar del todo al llegar, en vez de un stop brusco.
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, acceleration * Time.fixedDeltaTime);

            if (rb.linearVelocity.sqrMagnitude < 0.0001f)
            {
                rb.linearVelocity = Vector2.zero;
                hasTarget = false;
            }
            return;
        }

        Vector2 desiredVelocity = Vector2.ClampMagnitude(toTarget.normalized * moveSpeed, maxSpeed);
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desiredVelocity, acceleration * Time.fixedDeltaTime);
    }

    // ---------------------------------------------------------------
    // HOOKS PARA MEJORAS (llamados desde StatUpgradeSO.Apply(), igual que
    // GameManager.IncreaseXxx). El propio GameManager no sabe nada de esto,
    // así que no hay que tocarlo.
    // ---------------------------------------------------------------
    public void IncreaseMoveSpeed(float amount) => moveSpeed = Mathf.Max(0f, moveSpeed + amount);

    public void IncreaseMaxSpeed(float amount) => maxSpeed = Mathf.Max(moveSpeed, maxSpeed + amount);

    public void IncreaseAcceleration(float amount) => acceleration = Mathf.Max(0.01f, acceleration + amount);

    /// <summary>
    /// Versión "tier 3" de IncreaseMoveSpeed: multiplica en vez de sumar, para
    /// que la mejora más potente de esta familia cambie de escala (efecto
    /// mucho más notable que seguir sumando un valor fijo).
    /// </summary>
    public void MultiplyMoveSpeed(float multiplier) => moveSpeed = Mathf.Max(0f, moveSpeed * multiplier);

    /// <summary>
    /// Pareja de MultiplyMoveSpeed: sube el tope de velocidad en la misma
    /// proporción. Sin esto, multiplicar solo moveSpeed puede no notarse en
    /// absoluto si el resultado sigue quedando por debajo de maxSpeed, o
    /// notarse solo a medias si lo supera y el clamp de FixedUpdate lo recorta.
    /// </summary>
    public void MultiplyMaxSpeed(float multiplier) => maxSpeed = Mathf.Max(moveSpeed, maxSpeed * multiplier);
}
