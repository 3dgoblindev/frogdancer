using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI de la tienda de mejoras: se suscribe a los eventos de RoundManager,
/// rellena los botones con las opciones recibidas (leyendo directamente
/// displayName/description/icon de cada UpgradeSO) y delega en RoundManager
/// tanto la elección como el paso a la siguiente ronda.
///
/// También pinta cada botón según el tier de la mejora que representa
/// (blanco/azul/morado por defecto), para que el jugador distinga de un
/// vistazo la potencia de cada opción.
/// </summary>
public class ShopUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;

    [Header("Opciones (uno por hueco de la tienda)")]
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TMP_Text[] optionTitles;
    [SerializeField] private TMP_Text[] optionDescriptions;
    [SerializeField] private Image[] optionIcons; // opcional: deja el array vacío si no usas iconos todavía

    [Header("Colores por tier")]
    [Tooltip("Índice = (int)UpgradeSO.UpgradeTier. Por defecto: Tier1 blanco, Tier2 azul, Tier3 morado.")]
    [SerializeField]
    private Color[] tierColors =
    {
        Color.white,
        new Color(0.35f, 0.65f, 1f),   // azul
        new Color(0.65f, 0.35f, 0.95f) // morado
    };

    [SerializeField] private Button nextRoundButton;

    private bool hasChosenThisShop;

    private void OnEnable()
    {
        RoundManager.OnShopOpened += HandleShopOpened;
        RoundManager.OnRoundStart += HandleRoundStart;
    }

    private void OnDisable()
    {
        RoundManager.OnShopOpened -= HandleShopOpened;
        RoundManager.OnRoundStart -= HandleRoundStart;
    }

    private void Start()
    {
        panelRoot.SetActive(false); // oculto hasta que termine la primera ronda

        // Los botones también se ocultan uno a uno (no solo el panel), para
        // no depender de que ya estén desactivados a mano en la escena.
        foreach (Button button in optionButtons)
        {
            button.gameObject.SetActive(false);
        }
    }

    private void HandleShopOpened(List<UpgradeSO> options)
    {
        hasChosenThisShop = false;
        panelRoot.SetActive(true);

        nextRoundButton.interactable = true;
        nextRoundButton.onClick.RemoveAllListeners();
        nextRoundButton.onClick.AddListener(() =>
        {
            RoundManager.Instance.StartNextRound();
            panelRoot.SetActive(false);
        });

        for (int i = 0; i < optionButtons.Length; i++)
        {
            bool hasOption = i < options.Count;
            optionButtons[i].gameObject.SetActive(hasOption);
            if (!hasOption) continue;

            UpgradeSO upgrade = options[i];

            Debug.Log($"[ShopUI] Opción {i}: \"{upgrade.displayName}\" (id={upgrade.upgradeId}, tier={upgrade.tier}, unlockRound={upgrade.unlockRound})");

            optionTitles[i].text = upgrade.displayName;
            optionDescriptions[i].text = upgrade.description;

            if (optionIcons != null && i < optionIcons.Length && optionIcons[i] != null)
            {
                optionIcons[i].sprite = upgrade.icon;
                optionIcons[i].enabled = upgrade.icon != null;
            }

            // OJO: interactable primero. Cambiar "interactable" de false a
            // true dispara la transición de color por defecto del Button
            // (Selectable.DoStateTransition), que pisa targetGraphic.color
            // con el normalColor del botón. Si lo hacemos antes de pintar el
            // color de tier, el color de tier queda como última escritura y
            // no se pierde.
            optionButtons[i].interactable = true;

            if (optionButtons[i].targetGraphic != null)
            {
                optionButtons[i].targetGraphic.color = GetTierColor(upgrade.tier);
            }
            else
            {
                Debug.LogWarning($"[ShopUI] optionButtons[{i}] no tiene Target Graphic asignado: no se puede pintar el color de tier.");
            }

            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => SelectUpgrade(upgrade));
        }
    }

    private Color GetTierColor(UpgradeSO.UpgradeTier tier)
    {
        int index = (int)tier;
        if (tierColors != null && index >= 0 && index < tierColors.Length)
        {
            return tierColors[index];
        }

        return Color.white;
    }

    private void SelectUpgrade(UpgradeSO upgrade)
    {
        if (hasChosenThisShop) return; // solo se puede elegir una mejora por tienda
        hasChosenThisShop = true;

        RoundManager.Instance.ChooseUpgrade(upgrade);

        // Bloquea visualmente el resto de opciones para dejar claro cuál se eligió.
        foreach (Button button in optionButtons)
        {
            if (button.gameObject.activeSelf)
            {
                button.interactable = false;
            }
        }
    }

    private void HandleRoundStart(int round)
    {
        panelRoot.SetActive(false);
    }
}
