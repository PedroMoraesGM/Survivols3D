using DG.Tweening;
using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseWindow : MonoBehaviour
{
    [SerializeField] private CanvasGroup confirmCanvas;
    [SerializeField] private CanvasGroup pauseCanvas;
    private PauseController pauseController;

    private void Awake()
    {
        pauseController = FindFirstObjectByType<PauseController>();
        pauseController.GamePauseChanged += OnGamePauseChanged;
    }   

    public void ToggleConfirmCanvas(bool open)
    {
        if (open)
        {
            confirmCanvas.gameObject.SetActive(true);
            confirmCanvas.DOFade(1, 0.3f);
        }
        else
            confirmCanvas.DOFade(0, 0.3f).OnComplete(() => confirmCanvas.gameObject.SetActive(false));
    }

    public void CloseUI()
    {
        confirmCanvas.DOFade(0, 0.3f).OnComplete(() => confirmCanvas.gameObject.SetActive(false));
        pauseCanvas.DOFade(0, 0.3f).OnComplete(() => pauseCanvas.gameObject.SetActive(false));

        pauseController.Resume();
    }

    public void DisconnectButton()
    {
        QuantumRunner.Default.Game.RemovePlayer();
        // Stop Quantum runner
        if (QuantumRunner.Default != null && QuantumRunner.Default.Game != null)
        {
            QuantumRunner.Default.Shutdown();
        }
        // Load Menu scene (index 0 or name)
        SceneManager.LoadScene("Menu");
    }

    private void OnGamePauseChanged(bool obj)
    {
        if (obj)
        {
            pauseCanvas.gameObject.SetActive(true);
            pauseCanvas.DOFade(1, 0.3f);
        }
        else
            CloseUI();
    }
}
