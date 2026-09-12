using UnityEngine;

/// <summary>
/// Componente reusable para reproducir un pool de sonidos subiendo el pitch
/// en secuencia (paso a paso por un array de ratios armónicos) cada vez que
/// se llama a Play(), y disparando opcionalmente un sonido especial cada
/// "specialEvery" reproducciones en vez del normal.
///
/// Pensado para colgarlo tanto de Frog (croaks) como de Tadpole
/// (renacuajos): cada GameObject lleva su propia instancia con su propio
/// AudioSource, así que las secuencias de una rana y otra no se pisan entre
/// sí. Se llama desde fuera con harmonicAudio.Play(), por ejemplo desde
/// HandleCroakSuccess en Frog.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class HarmonicAudioPlayer : MonoBehaviour
{
    [Header("Sonidos normales")]
    [Tooltip("Pool de clips normales. Se elige uno al azar en cada Play() que no sea especial.")]
    [SerializeField] private AudioClip[] clips;

    [Header("Sonido especial")]
    [Tooltip("Pool de clips especiales. Se elige uno al azar cada 'specialEvery' reproducciones.")]
    [SerializeField] private AudioClip[] specialClips;
    [Tooltip("Cada cuántas reproducciones suena un especial en vez de uno normal. 0 o menos = desactivado.")]
    [SerializeField] private int specialEvery = 5;
    [Tooltip("Pitch con el que suena el especial (normalmente 1 = sin alterar, para que destaque del resto).")]
    [SerializeField] private float specialPitch = 1f;
    [Tooltip("Si está activo, al sonar el especial la secuencia de pitch vuelve a empezar desde el primer paso.")]
    [SerializeField] private bool resetSequenceOnSpecial = true;

    [Header("Secuencia armónica de pitch")]
    [Tooltip("Ratios de pitch por los que va pasando en orden en cada Play() normal (1 = pitch original). " +
             "Puedes meter una escala musical (1, 1.125, 1.25, 1.333...) o la serie armónica pura (1, 2, 3, 4...).")]
    [SerializeField]
    private float[] pitchSteps =
    {
        1f,     // tónica
        1.125f, // 2ª mayor
        1.25f,  // 3ª mayor
        1.333f, // 4ª justa
        1.5f,   // 5ª justa
        1.667f, // 6ª mayor
        1.875f, // 7ª mayor
        2f      // octava
    };

    private AudioSource audioSource;
    private int playCount;
    private int pitchIndex;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Reproduce el siguiente sonido de la secuencia. Sube el pitch un paso
    /// respecto a la llamada anterior (ciclando al llegar al final del
    /// array), salvo que toque especial, en cuyo caso suena ese en vez del
    /// normal y no consume/avanza el paso de pitch.
    /// </summary>
    public void Play()
    {
        playCount++;

        bool isSpecialTurn = specialEvery > 0 && playCount % specialEvery == 0;

        if (isSpecialTurn && specialClips != null && specialClips.Length > 0)
        {
            PlaySpecial();
            return;
        }

        PlayNormal();
    }

    private void PlayNormal()
    {
        if (clips == null || clips.Length == 0 || pitchSteps == null || pitchSteps.Length == 0) return;

        audioSource.pitch = pitchSteps[pitchIndex];
        audioSource.PlayOneShot(GetRandomClip(clips));

        pitchIndex = (pitchIndex + 1) % pitchSteps.Length;
    }

    private void PlaySpecial()
    {
        audioSource.pitch = specialPitch;
        audioSource.PlayOneShot(GetRandomClip(specialClips));

        if (resetSequenceOnSpecial) pitchIndex = 0;
    }

    private AudioClip GetRandomClip(AudioClip[] pool) => pool[Random.Range(0, pool.Length)];

    /// <summary>Reinicia el contador de reproducciones y el paso de pitch (p.ej. al empezar una ronda nueva).</summary>
    public void ResetSequence()
    {
        playCount = 0;
        pitchIndex = 0;
    }
}
