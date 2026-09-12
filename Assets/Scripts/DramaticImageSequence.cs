using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla una secuencia activando solo el hijo actual (SetActive true) 
/// y desactivando el resto (SetActive false). 
/// Avanza al siguiente con cualquier tecla/click. 
/// Al finalizar la lista carga la escena indicada.
/// </summary>
public class DramaticImageSequence : MonoBehaviour
{
    [Header("Comportamiento")]
    [Tooltip("Al llegar al último hijo, si está activo vuelve al primero; si no, cambia de escena.")]
    [SerializeField] private bool loop = false;

    [Header("Fin de secuencia (solo si loop está desactivado)")]
    [Tooltip("Nombre de la escena a cargar al terminar. Debe estar añadida en Build Settings.")]
    [SerializeField] private string nextSceneName;

    private readonly List<GameObject> slides = new List<GameObject>();
    private int currentIndex = 0;

    private void Awake()
    {
        CollectChildren();
        ShowSlide(0);
    }

    private void CollectChildren()
    {
        slides.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            slides.Add(transform.GetChild(i).gameObject);
        }
    }

    private void ShowSlide(int index)
    {
        if (slides.Count == 0) return;

        currentIndex = index;
        for (int i = 0; i < slides.Count; i++)
        {
            slides[i].SetActive(i == currentIndex);
        }
    }

    private void Update()
    {
        bool anyKey = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool anyClick = Mouse.current != null &&
            (Mouse.current.leftButton.wasPressedThisFrame
             || Mouse.current.rightButton.wasPressedThisFrame
             || Mouse.current.middleButton.wasPressedThisFrame);

        if (anyKey || anyClick)
        {
            AdvanceNext();
        }
    }

    /// <summary>Avanza al siguiente hijo o cambia de escena.</summary>
    public void AdvanceNext()
    {
        if (slides.Count == 0) return;

        int nextIndex = currentIndex + 1;

        if (nextIndex >= slides.Count)
        {
            if (loop)
            {
                ShowSlide(0);
            }
            else
            {
                LoadNextScene();
            }
        }
        else
        {
            ShowSlide(nextIndex);
        }
    }

    private void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("[DramaticImageSequence] nextSceneName está vacío: la secuencia terminó pero no hay escena que cargar.");
        }
    }
}