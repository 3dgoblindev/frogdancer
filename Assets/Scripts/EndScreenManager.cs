using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Escena de end screen (separada de la escena de juego). Al arrancar, lee
/// los resultados de la última partida directamente de SavesManager
/// (LastRunScore / LastRunTadpolesSaved / LastRunWasNewRecord) en vez de
/// escuchar un evento: GameManager, TadpoleManager y RoundManager viven en
/// la escena de juego y se destruyen al cambiar de escena, así que no
/// pueden avisarnos aquí. SavesManager sí es DontDestroyOnLoad y sobrevive
/// al cambio, por eso es el puente entre las dos escenas.
///
/// Todo el resumen se pinta en un único TextMeshProUGUI, con el formato:
///   {tadpolesSaved} renacuajos salvados
///   {score} puntos de gratitud
///   [¡Nuevo récord!]
///
///   {mensaje de valoración}
/// </summary>
public class EndScreenManager : MonoBehaviour
{
    [Header("Referencia UI")]
    [SerializeField] private TextMeshProUGUI summaryText;

    [Header("Escena")]
    [SerializeField] private string menuSceneName = "Menu";

    [Header("Umbrales de valoración (renacuajos salvados)")]
    [Tooltip("Por debajo de este número: mensaje flojo/de ánimo.")]
    [SerializeField] private int lowThreshold = 5;
    [Tooltip("Por debajo de este número: mensaje intermedio.")]
    [SerializeField] private int midThreshold = 15;
    [Tooltip("Por debajo de este número: mensaje bueno. Igual o por encima: mensaje excelente.")]
    [SerializeField] private int highThreshold = 30;

    private void Start()
    {
        if (SavesManager.Instance == null)
        {
            Debug.LogWarning("[EndScreenManager] No hay SavesManager en escena; no se puede mostrar el resumen.");
            return;
        }

        long score = SavesManager.Instance.LastRunScore;
        int tadpolesSaved = SavesManager.Instance.LastRunTadpolesSaved;
        bool isNewRecord = SavesManager.Instance.LastRunWasNewRecord;

        if (summaryText != null)
        {
            summaryText.text = BuildSummary(tadpolesSaved, score, isNewRecord);
        }
    }

    private string BuildSummary(int tadpolesSaved, long score, bool isNewRecord)
    {
        string recordLine = isNewRecord ? "\n¡Nuevo récord!" : string.Empty;

        return $"{tadpolesSaved} saved tadpoles \n{score} gratitude points {recordLine}\n\n{GetRatingMessage(tadpolesSaved)}";
    }

    private string GetRatingMessage(int tadpolesSaved)
    {
        if (tadpolesSaved <= 0)
            return "IT'S ALL YOUR FAULT";
        if (tadpolesSaved < lowThreshold)
            return "Millions of tadpoles died. There won't be enough frogs in the next cycle. Your sacrifice was in vane.";
        if (tadpolesSaved < midThreshold)
            return "You could save some of them. Millions died. But, does it matter, if I'm going with them.";
        if (tadpolesSaved < highThreshold)
            return "You could save a lot of tadpoles. There will be frogs in the next earth cycle. But, does it matter, if I'm going with them.";
        return "You can rest with your frog heart in peace. The frog legacy is assured thanks to your sacrifice. Keep it funky my frinend.";
    }

    /// <summary>Engánchalo al OnClick del botón de menú desde el Inspector.</summary>
    public void GoToMenu()
    {
        if (!string.IsNullOrEmpty(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            Debug.LogWarning("[EndScreenManager] menuSceneName está vacío.");
        }
    }
}
