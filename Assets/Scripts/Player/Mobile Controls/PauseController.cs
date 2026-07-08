using UnityEngine;

public class PauseController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseUI;

    [Header("Estados de pausa")]
    [SerializeField] private bool canUsePause = true;
    [SerializeField] private bool isPaused = false;

    private void Start()
    {
        if (pauseUI != null)
        {
            pauseUI.SetActive(false);
        }

        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.OnPauseTriggered += TogglePause;
        }

        else
        {
            Debug.LogError("No se encontró el MobileInputManager");
        }
    }

    private void OnDestroy()
    {
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.OnPauseTriggered -= TogglePause;
        }
    }

    private void TogglePause()
    {
        if (!canUsePause) return;

        isPaused = !isPaused;

        if (isPaused)
        {
            ActivatePause();
        }

        else
        {
            DeactivatePause();
        }
    }

    private void ActivatePause()
    {
        if (pauseUI != null) pauseUI.SetActive(true);
        Time.timeScale = 0f;
        Debug.Log("Juego pausado");
    }
    public void DeactivatePause()
    {
        if (pauseUI != null) pauseUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        Debug.Log("Juego reanudado");
    }
}