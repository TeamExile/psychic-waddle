using UnityEngine;

namespace Friendslop
{
    /// <summary>
    /// Main game manager for Friendslop game.
    /// Uses the Singleton pattern with modern Unity best practices.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<GameManager>();

                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private GameSettings gameSettings;

        private GameState _currentState = GameState.MainMenu;

        private void Awake()
        {
            // Ensure only one instance exists and persist across scenes
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            _instance = this;

            Initialize();
        }

        private void Initialize()
        {
            Debug.Log("Friendslop Game Manager Initialized");

            if (gameSettings != null)
            {
                Debug.Log($"Game Settings Loaded: {gameSettings.gameName}");
            }
        }

        public void StartGame()
        {
            _currentState = GameState.Playing;
            Debug.Log("Game Started");
            // Add game start logic here
        }

        public void PauseGame()
        {
            _currentState = GameState.Paused;
            Time.timeScale = 0f;
            Debug.Log("Game Paused");
        }

        public void ResumeGame()
        {
            _currentState = GameState.Playing;
            Time.timeScale = 1f;
            Debug.Log("Game Resumed");
        }

        public void EndGame()
        {
            _currentState = GameState.GameOver;
            Time.timeScale = 1f; // Reset time scale in case game was paused
            Debug.Log("Game Over");
        }

        public GameState CurrentState => _currentState;
    }

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }
}