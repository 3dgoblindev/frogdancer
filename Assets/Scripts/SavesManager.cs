using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Datos que se guardan entre partidas. Según crezca la progresión, aquí
/// irán más campos (ranas desbloqueadas, rango de renacuajos, mejoras, etc.).
/// Debe ser [Serializable] y usar solo tipos simples/listas para que
/// JsonUtility lo pueda convertir a JSON sin problemas.
/// </summary>
[Serializable]
public class SaveData
{
    public long totalScore;

    [Tooltip("Mejor puntuación conseguida en UNA partida (no acumulada entre partidas). Es el highscore que se le muestra al jugador al terminar una partida.")]
    public long bestScore;

    // Rango de atracción base (con combo 0) y máximo (con combo alto).
    // Estos valores son los que se mostrarán/subirán en una futura tienda de mejoras;
    // GameManager los lee al arrancar e interpola entre ellos según el combo actual.
    public float minAttractionRange = 2f;
    public float maxAttractionRange = 8f;

    [Tooltip("Nivel actual del jugador; determina qué tipos de renacuajo pueden aparecer y con qué probabilidad.")]
    public int currentLevel = 1;
}

/// <summary>
/// Guarda y carga el progreso del jugador en un fichero JSON dentro de
/// Application.persistentDataPath. Cualquier sistema que necesite sumar
/// puntos llama a SavesManager.Instance.AddScore(...).
/// </summary>
public class SavesManager : MonoBehaviour
{
    public static SavesManager Instance { get; private set; }

    private const string SaveFileName = "save.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public SaveData Data { get; private set; } = new SaveData();

    // Datos de la última partida jugada. NO se persisten en el JSON (son solo
    // para pasar información a la escena de end screen, que no puede leer
    // GameManager/TadpoleManager porque esos viven en la escena de juego y
    // se destruyen al cambiar de escena). SavesManager sí es DontDestroyOnLoad,
    // así que sobreviven al cambio de escena sin necesidad de guardarlos a disco.
    public long LastRunScore { get; private set; }
    public int LastRunTadpolesSaved { get; private set; }
    public bool LastRunWasNewRecord { get; private set; }

    // Para que la UI (marcador de puntos, etc.) pueda escuchar cambios sin hacer polling.
    public static event Action<long> OnScoreChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // el save persiste aunque cambies de escena
        LoadGame();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // En móvil, cerrar la app no siempre dispara OnApplicationQuit,
        // así que guardamos también al pausar/perder foco.
        if (pauseStatus)
        {
            SaveGame();
        }
    }

    // ---------------------------------------------------------------
    // API PÚBLICA
    // ---------------------------------------------------------------
    public void AddScore(long amount)
    {
        Data.totalScore += amount;
        OnScoreChanged?.Invoke(Data.totalScore);

        // Simplicidad por ahora: guardar en cada cambio de puntos.
        // Si más adelante esto se vuelve muy frecuente (progresión activa),
        // se puede cambiar fácilmente a un autosave cada X segundos.
        SaveGame();
    }

    /// <summary>
    /// Llamado por RoundManager al terminar una partida con la puntuación final
    /// de esa partida (GameManager.Score) y los renacuajos salvados
    /// (TadpoleManager.TadpolesSaved). Actualiza el highscore si procede y deja
    /// los datos en LastRunScore/LastRunTadpolesSaved/LastRunWasNewRecord para
    /// que la escena de end screen los lea al arrancar.
    /// </summary>
    public void SetLastRunResult(long runScore, int tadpolesSaved)
    {
        LastRunScore = runScore;
        LastRunTadpolesSaved = tadpolesSaved;
        LastRunWasNewRecord = ReportRunScore(runScore);
    }

    /// <summary>
    /// Llamado por RoundManager al terminar una partida con la puntuación final
    /// de esa partida (GameManager.Score). Si supera el highscore guardado, lo
    /// actualiza y persiste. Devuelve true si fue un nuevo récord.
    /// </summary>
    public bool ReportRunScore(long runScore)
    {
        if (runScore > Data.bestScore)
        {
            Data.bestScore = runScore;
            SaveGame();
            return true;
        }

        return false;
    }

    // Placeholders para cuando montes la tienda de mejoras: suben el rango
    // guardado y lo persisten. GameManager solo lee estos valores al arrancar,
    // así que una mejora comprada en mitad de partida se aplicará en la siguiente carga/escena
    // (si quieres que se aplique al instante, se puede exponer un evento aquí también).
    public void UpgradeMinAttractionRange(float newValue)
    {
        Data.minAttractionRange = newValue;
        SaveGame();
    }

    public void UpgradeMaxAttractionRange(float newValue)
    {
        Data.maxAttractionRange = newValue;
        SaveGame();
    }

    public void SetLevel(int newLevel)
    {
        Data.currentLevel = newLevel;
        SaveGame();
    }

    public void SaveGame()
    {
        string json = JsonUtility.ToJson(Data, prettyPrint: true);
        File.WriteAllText(SavePath, json);
    }

    public void LoadGame()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            Data = JsonUtility.FromJson<SaveData>(json);
        }
        else
        {
            Data = new SaveData();
        }
    }

    [ContextMenu("Borrar guardado")]
    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }

        Data = new SaveData();
        OnScoreChanged?.Invoke(Data.totalScore);
    }
}
