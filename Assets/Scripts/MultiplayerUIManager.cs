using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace Friendslop.UI
{
    /// <summary>
    /// UI Manager for multiplayer lobby functionality.
    /// Handles host/join buttons and connection status display.
    /// </summary>
    public class MultiplayerUIManager : MonoBehaviour
    {
        private const int DEFAULT_MAX_PLAYERS = 4;

        [Header("Lobby UI")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI playerCountText;

        [Header("Gameplay UI")]
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private TextMeshProUGUI currentPlayerIdText;

        private void Start()
        {
            // Setup button listeners
            if (hostButton != null)
            {
                hostButton.onClick.AddListener(OnHostButtonClicked);
            }

            if (joinButton != null)
            {
                joinButton.onClick.AddListener(OnJoinButtonClicked);
            }

            if (disconnectButton != null)
            {
                disconnectButton.onClick.AddListener(OnDisconnectButtonClicked);
                disconnectButton.gameObject.SetActive(false);
            }

            ShowLobby();
            UpdateStatusText("Ready to connect");
        }

        private void Update()
        {
            // Update player count and status
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                UpdatePlayerCount();
                UpdateCurrentPlayerId();
            }
        }

        private void OnHostButtonClicked()
        {
            if (MultiplayerManager.Instance != null)
            {
                UpdateStatusText("Starting host...");
                MultiplayerManager.Instance.StartHost();
                
                // Update UI state
                if (hostButton != null) hostButton.interactable = false;
                if (joinButton != null) joinButton.interactable = false;
                if (disconnectButton != null) disconnectButton.gameObject.SetActive(true);
                
                ShowGameplay();
                UpdateStatusText("Hosting - Waiting for players...");
            }
            else
            {
                UpdateStatusText("Error: MultiplayerManager not found!");
            }
        }

        private void OnJoinButtonClicked()
        {
            if (MultiplayerManager.Instance != null)
            {
                UpdateStatusText("Joining game...");
                MultiplayerManager.Instance.StartClient();
                
                // Update UI state
                if (hostButton != null) hostButton.interactable = false;
                if (joinButton != null) joinButton.interactable = false;
                if (disconnectButton != null) disconnectButton.gameObject.SetActive(true);
                
                ShowGameplay();
                UpdateStatusText("Connected to host");
            }
            else
            {
                UpdateStatusText("Error: MultiplayerManager not found!");
            }
        }

        private void OnDisconnectButtonClicked()
        {
            if (MultiplayerManager.Instance != null)
            {
                MultiplayerManager.Instance.Shutdown();
            }

            // Reset UI state
            if (hostButton != null) hostButton.interactable = true;
            if (joinButton != null) joinButton.interactable = true;
            if (disconnectButton != null) disconnectButton.gameObject.SetActive(false);

            ShowLobby();
            UpdateStatusText("Disconnected");
            UpdatePlayerCount(0);
        }

        private void ShowLobby()
        {
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
            if (gameplayPanel != null) gameplayPanel.SetActive(false);
        }

        private void ShowGameplay()
        {
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            if (gameplayPanel != null) gameplayPanel.SetActive(true);
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void UpdatePlayerCount(int? count = null)
        {
            if (playerCountText != null)
            {
                int playerCount = count ?? (MultiplayerManager.Instance?.ConnectedPlayerCount ?? 0);
                int maxPlayers = MultiplayerManager.Instance?.MaxPlayers ?? DEFAULT_MAX_PLAYERS;
                playerCountText.text = $"Players: {playerCount}/{maxPlayers}";
            }
        }

        private void UpdateCurrentPlayerId()
        {
            if (currentPlayerIdText != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            {
                ulong clientId = NetworkManager.Singleton.LocalClientId;
                currentPlayerIdText.text = $"Player ID: {clientId}";
            }
        }
    }
}
