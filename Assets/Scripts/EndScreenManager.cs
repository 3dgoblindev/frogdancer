using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
///   {tadpolesSaved} saved tadpoles
///   {score} gratitude points
///   [New record!]
///
///   {mensaje de valoración}
/// </summary>
public class EndScreenManager : MonoBehaviour
{
    [Header("Referencia UI")]
    [SerializeField] private TextMeshProUGUI summaryText;

    [Header("Escena")]
    [SerializeField] private string menuSceneName = "Menu";

    [Header("Botón de siguiente nivel")]
    [Tooltip("Botón que lleva a la siguiente partida/nivel. Se engancha su OnClick por código en Start(), así que no hace falta enlazarlo a mano desde el Inspector del botón.")]
    [SerializeField] private Button nextLevelButton;
    [Tooltip("Texto del botón (hijo del Button). Si se deja vacío, se busca automáticamente con GetComponentInChildren.")]
    [SerializeField] private TextMeshProUGUI nextLevelButtonLabel;
    [Tooltip("Texto a mostrar en el botón.")]
    [SerializeField] private string nextLevelButtonText = "Next Level";

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

        SetupNextLevelButton();
    }

    private void SetupNextLevelButton()
    {
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(GoToMenu);

            if (nextLevelButtonLabel == null)
            {
                nextLevelButtonLabel = nextLevelButton.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (nextLevelButtonLabel != null)
        {
            nextLevelButtonLabel.text = nextLevelButtonText;
        }
    }

    private string BuildSummary(int tadpolesSaved, long score, bool isNewRecord)
    {
        string recordLine = isNewRecord ? "\nNew record!" : string.Empty;

        return $"{tadpolesSaved} saved tadpoles \n{score} gratitude points {recordLine}\n\n{GetRatingMessage(tadpolesSaved)}";
    }

    private string GetRatingMessage(int tadpolesSaved)
    {
        if (tadpolesSaved <= 0)
            return "IT'S ALL YOUR FAULT";
        if (tadpolesSaved < lowThreshold)
            return "Millions of tadpoles died. There won't be enough frogs in the next cycle. Your sacrifice was in vain.";
        if (tadpolesSaved < midThreshold)
            return "You could save some of them. Millions died. But does it matter, if I'm going with them.";
        if (tadpolesSaved < highThreshold)
            return "You could save a lot of tadpoles. There will be frogs in the next earth cycle. But does it matter, if I'm going with them.";
        return "You can rest with your frog heart in peace. The frog legacy is assured thanks to your sacrifice. Keep it funky, my friend.";
    }

    /// <summary>
    /// Enganchado por código al botón de siguiente nivel en SetupNextLevelButton().
    /// También se puede seguir enganchando a mano desde el Inspector si hace
    /// falta llamarlo desde otro botón.
    /// </summary>
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
