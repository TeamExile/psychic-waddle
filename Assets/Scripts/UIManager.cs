using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Friendslop.UI
{
    /// <summary>
    /// UI Manager for handling game UI elements.
    /// Uses TextMeshPro for better text rendering (modern Unity standard).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UIManager>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("UIManager");
                        _instance = go.AddComponent<UIManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("Gameplay UI")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timerText;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            ShowMainMenu();
        }

        public void ShowMainMenu()
        {
            HideAllPanels();
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);
        }

        public void ShowGameplay()
        {
            HideAllPanels();
            if (gameplayPanel != null)
                gameplayPanel.SetActive(true);
        }

        public void ShowPause()
        {
            if (pausePanel != null)
                pausePanel.SetActive(true);
        }

        public void HidePause()
        {
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        public void ShowGameOver()
        {
            HideAllPanels();
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
        }

        private void HideAllPanels()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (gameplayPanel != null) gameplayPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }
        }

        public void UpdateTimer(float time)
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(time / 60f);
                int seconds = Mathf.FloorToInt(time % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        // Button callbacks
        public void OnStartGameButton()
        {
            GameManager.Instance?.StartGame();
            ShowGameplay();
        }

        public void OnPauseButton()
        {
            GameManager.Instance?.PauseGame();
            ShowPause();
        }

        public void OnResumeButton()
        {
            GameManager.Instance?.ResumeGame();
            HidePause();
        }

        public void OnQuitButton()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}
