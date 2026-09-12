using System.Collections;
using UnityEngine;
using MoreMountains.Feedbacks;

public class Frog : MonoBehaviour
{
    public static readonly System.Collections.Generic.List<Frog> ActiveFrogs = new System.Collections.Generic.List<Frog>();

    [Header("Identidad")]
    [SerializeField] private string frogName = "Rana";

    [Header("Feedbacks")]
    [SerializeField] private MMFeedbacks croakSuccessFeedback;
    [SerializeField] private MMFeedbacks croakFailFeedback;

    [Header("Audio")]
    [SerializeField] private HarmonicAudioPlayer croakSuccessAudio;
    [SerializeField] private AudioSource croakFailAudioSource;
    [SerializeField] private AudioClip[] croakFailClips;

    [Header("Sprites y Animación")]
    [Tooltip("Se autoasigna si se deja vacío.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Lista única de sprites. Se organizan en pares: [0]=Idle A, [1]=Idle B, [2]=Idle C, [3]=Idle D...")]
    [SerializeField] private Sprite[] frogSprites;

    [Header("Tiempos y Rotación")]
    [SerializeField] private float idleFrameDuration = 0.3f;
    [SerializeField] private float idleResumeDelay = 0.3f;
    [SerializeField] private float minCroakRotation = -15f;
    [SerializeField] private float maxCroakRotation = 15f;

    private Coroutine idleAnimationCoroutine;
    private float idleResumeTime;

    // Estado de la animación
    private int currentBaseIndex = 0;
    private bool toggleFrame = false;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        ActiveFrogs.Add(this);

        GameManager.OnCycleStart += HandleCycleStart;
        GameManager.OnWindowOpen += HandleWindowOpen;
        GameManager.OnWindowClose += HandleWindowClose;

        GameManager.OnCroakSuccess += HandleCroakSuccess;
        GameManager.OnCroakFail += HandleCroakFail;
        GameManager.OnCroakLate += HandleCroakLate;

        GameManager.OnComboChanged += HandleComboChanged;
        GameManager.OnComboBroken += HandleComboBroken;

        idleAnimationCoroutine = StartCoroutine(IdleAnimationLoop());
    }

    private void OnDisable()
    {
        ActiveFrogs.Remove(this);

        GameManager.OnCycleStart -= HandleCycleStart;
        GameManager.OnWindowOpen -= HandleWindowOpen;
        GameManager.OnWindowClose -= HandleWindowClose;

        GameManager.OnCroakSuccess -= HandleCroakSuccess;
        GameManager.OnCroakFail -= HandleCroakFail;
        GameManager.OnCroakLate -= HandleCroakLate;

        GameManager.OnComboChanged -= HandleComboChanged;
        GameManager.OnComboBroken -= HandleComboBroken;

        if (idleAnimationCoroutine != null)
        {
            StopCoroutine(idleAnimationCoroutine);
            idleAnimationCoroutine = null;
        }
    }

    private void Start()
    {
        Debug.Log($"[{frogName}] lista en posición {transform.position}.");
    }

    // ---------------------------------------------------------------
    // HANDLERS DE RITMO Y CROAR
    // ---------------------------------------------------------------
    private void HandleCycleStart() => Debug.Log($"[{frogName}] empieza un nuevo ciclo de croar.");
    private void HandleWindowOpen() => Debug.Log($"[{frogName}] ventana de croar ABIERTA.");
    private void HandleWindowClose() => Debug.Log($"[{frogName}] ventana de croar CERRADA.");

    private void HandleCroakSuccess()
    {
        Debug.Log($"[{frogName}] ¡CROAR BONITO!");
        ApplyRandomCroakVisual();
        croakSuccessFeedback.PlayFeedbacks();
        croakSuccessAudio.Play();
    }

    private void HandleCroakFail()
    {
        Debug.Log($"[{frogName}] croar FEO");
        ApplyRandomCroakVisual();
        croakFailFeedback.PlayFeedbacks();
        PlayCroakFailClip();
    }

    private void HandleCroakLate()
    {
        Debug.Log($"[{frogName}] croar TARDE");
        ApplyRandomCroakVisual();
        croakFailFeedback.PlayFeedbacks();
        PlayCroakFailClip();
    }

    private void PlayCroakFailClip()
    {
        if (croakFailAudioSource == null || croakFailClips == null || croakFailClips.Length == 0) return;

        AudioClip clip = croakFailClips[Random.Range(0, croakFailClips.Length)];
        croakFailAudioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Cambia el ID base del conjunto de sprites al croar y asigna rotación aleatoria.
    /// </summary>
    private void ApplyRandomCroakVisual()
    {
        if (frogSprites != null && frogSprites.Length >= 2)
        {
            // Selecciona un índice par (0, 2, 4...) de la lista de pares disponibles
            int totalPairs = frogSprites.Length / 2;
            currentBaseIndex = Random.Range(0, totalPairs) * 2;

            // Muestra inmediatamente el segundo sprite del par (ID + 1) como impacto visual
            int croakFrame = Mathf.Min(currentBaseIndex + 1, frogSprites.Length - 1);
            spriteRenderer.sprite = frogSprites[croakFrame];
            toggleFrame = true;
        }

        float randomZRotation = Random.Range(minCroakRotation, maxCroakRotation);
        transform.rotation = Quaternion.Euler(0f, 0f, randomZRotation);

        idleResumeTime = Time.time + idleResumeDelay;
    }

    /// <summary>
    /// Alterna en bucle entre el sprite ID e ID + 1.
    /// </summary>
    private IEnumerator IdleAnimationLoop()
    {
        if (frogSprites == null || frogSprites.Length < 2) yield break;

        while (true)
        {
            if (spriteRenderer != null && Time.time >= idleResumeTime)
            {
                int frameOffset = toggleFrame ? 1 : 0;
                int targetIndex = Mathf.Clamp(currentBaseIndex + frameOffset, 0, frogSprites.Length - 1);

                spriteRenderer.sprite = frogSprites[targetIndex];
                toggleFrame = !toggleFrame;
            }

            yield return new WaitForSeconds(idleFrameDuration);
        }
    }

    // ---------------------------------------------------------------
    // HANDLERS DE COMBO
    // ---------------------------------------------------------------
    private void HandleComboChanged(int newCombo) => Debug.Log($"[{frogName}] combo actual: {newCombo}");
    private void HandleComboBroken(int comboAntes) => Debug.Log($"[{frogName}] combo ROTO (tenía {comboAntes})");
}