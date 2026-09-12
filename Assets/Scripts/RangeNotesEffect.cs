using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa el rango de atracción de la rana instanciando notas
/// musicales (sprites) alrededor de ella: aparecen a la distancia del
/// rango, suben un poco y se desvanecen. Usa pooling (todas las notas se
/// crean una vez en Awake y se reciclan) para no instanciar/destruir en
/// cada croar.
/// </summary>
public class RangeNotesEffect : MonoBehaviour
{
    [Header("Prefab y pool")]
    [SerializeField] private SpriteRenderer notePrefab;
    [SerializeField] private int poolSize = 20;

    [Header("Emisión")]
    [Tooltip("Cuántas notas salen cada vez que se llama a Play().")]
    [SerializeField] private int notesPerBurst = 6;
    [Tooltip("Variación en el radio al que aparecen, para que no salgan todas pegadas al borde exacto del círculo.")]
    [SerializeField] private float radiusJitter = 0.3f;

    [Header("Animación de cada nota")]
    [SerializeField] private float lifetime = 1f;
    [Tooltip("Cuánto sube (eje Y de mundo) a lo largo de su vida.")]
    [SerializeField] private float riseDistance = 1f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly Queue<SpriteRenderer> pool = new Queue<SpriteRenderer>();

    // ---------------------------------------------------------------
    // POOLING Y CICLO DE VIDA
    // ---------------------------------------------------------------
    private void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            SpriteRenderer note = Instantiate(notePrefab, transform);
            note.gameObject.SetActive(false);
            pool.Enqueue(note);
        }
    }

    private void OnEnable()
    {
        GameManager.OnCroakSuccess += HandleCroakSuccess;
    }

    private void OnDisable()
    {
        GameManager.OnCroakSuccess -= HandleCroakSuccess;
    }

    private SpriteRenderer GetFromPool()
    {
        // Si la pool se queda sin notas libres (ráfagas muy seguidas), simplemente
        // no salen más hasta que alguna termine su animación y vuelva a la cola.
        return pool.Count > 0 ? pool.Dequeue() : null;
    }

    // ---------------------------------------------------------------
    // HANDLERS DE EVENTOS
    // ---------------------------------------------------------------
    private void HandleCroakSuccess()
    {
        if (GameManager.Instance != null)
        {
            Play(GameManager.Instance.CurrentAttractionRange);
        }
    }

    // ---------------------------------------------------------------
    // API PÚBLICA
    // ---------------------------------------------------------------
    /// <summary>Lanza una tanda de notas alrededor de la rana, a una distancia basada en "range".</summary>
    public void Play(float range)
    {
        for (int i = 0; i < notesPerBurst; i++)
        {
            SpriteRenderer note = GetFromPool();
            if (note == null) break;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Mathf.Max(0f, range + Random.Range(-radiusJitter, radiusJitter));
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

            note.transform.position = transform.position + offset;

            Color c = note.color;
            c.a = 1f;
            note.color = c;

            note.gameObject.SetActive(true);

            StartCoroutine(AnimateNote(note));
        }
    }

    // ---------------------------------------------------------------
    // ANIMACIÓN
    // ---------------------------------------------------------------
    private IEnumerator AnimateNote(SpriteRenderer note)
    {
        Vector3 startPos = note.transform.position;

        float t = 0f;
        while (t < lifetime)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / lifetime);

            float rise = riseCurve.Evaluate(progress) * riseDistance;
            note.transform.position = startPos + Vector3.up * rise;

            Color c = note.color;
            c.a = fadeCurve.Evaluate(progress);
            note.color = c;

            yield return null;
        }

        note.gameObject.SetActive(false);
        pool.Enqueue(note);
    }
}