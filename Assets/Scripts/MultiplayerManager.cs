using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Friendslop
{
    /// <summary>
    /// Manages multiplayer functionality including hosting, joining, and player spawning.
    /// Supports up to 4 players with basic player identification.
    /// </summary>
    public class MultiplayerManager : NetworkBehaviour
    {
        private static MultiplayerManager _instance;

        public static MultiplayerManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MultiplayerManager>();
                }
                return _instance;
            }
        }

        [Header("Network Settings")]
        [SerializeField] private int maxPlayers = 4;
        
        [Header("Player Spawning")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;

        // Track connected players
        private Dictionary<ulong, GameObject> _connectedPlayers = new Dictionary<ulong, GameObject>();
        private int _nextSpawnPointIndex = 0;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Setup spawn points if not configured
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                CreateDefaultSpawnPoints();
            }
        }

        private void Start()
        {
            // Subscribe to network events
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        /// <summary>
        /// Starts the game as a host (server + client).
        /// </summary>
        public void StartHost()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("NetworkManager.Singleton is null!");
                return;
            }

            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log("Host started successfully");
            }
            else
            {
                Debug.LogError("Failed to start host");
            }
        }

        /// <summary>
        /// Joins a game as a client.
        /// </summary>
        public void StartClient()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("NetworkManager.Singleton is null!");
                return;
            }

            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("Client started successfully");
            }
            else
            {
                Debug.LogError("Failed to start client");
            }
        }

        /// <summary>
        /// Stops the network session.
        /// </summary>
        public void Shutdown()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
                Debug.Log("Network shutdown");
            }

            _connectedPlayers.Clear();
            _nextSpawnPointIndex = 0;
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"Client {clientId} connected");

            // Only server spawns players
            if (IsServer)
            {
                // Check if we've reached max players
                if (_connectedPlayers.Count >= maxPlayers)
                {
                    Debug.LogWarning($"Max players ({maxPlayers}) reached. Client {clientId} cannot join.");
                    // Could disconnect the client here if needed
                    return;
                }

                SpawnPlayerForClient(clientId);
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"Client {clientId} disconnected");

            if (IsServer && _connectedPlayers.ContainsKey(clientId))
            {
                GameObject playerObject = _connectedPlayers[clientId];
                if (playerObject != null)
                {
                    Destroy(playerObject);
                }
                _connectedPlayers.Remove(clientId);
            }
        }

        private void SpawnPlayerForClient(ulong clientId)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("Player prefab is not assigned!");
                return;
            }

            // Get spawn position
            Vector3 spawnPosition = GetNextSpawnPosition();
            Quaternion spawnRotation = Quaternion.identity;

            // Instantiate player
            GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();

            if (networkObject != null)
            {
                networkObject.SpawnAsPlayerObject(clientId);
                _connectedPlayers[clientId] = playerInstance;
                Debug.Log($"Spawned player for client {clientId} at {spawnPosition}");
            }
            else
            {
                Debug.LogError("Player prefab does not have a NetworkObject component!");
                Destroy(playerInstance);
            }
        }

        private Vector3 GetNextSpawnPosition()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform spawnPoint = spawnPoints[_nextSpawnPointIndex];
                _nextSpawnPointIndex = (_nextSpawnPointIndex + 1) % spawnPoints.Length;
                return spawnPoint.position;
            }

            // Default spawn positions in a circle if no spawn points
            float angle = _nextSpawnPointIndex * (360f / maxPlayers) * Mathf.Deg2Rad;
            float radius = 5f;
            Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 1f, Mathf.Sin(angle) * radius);
            _nextSpawnPointIndex++;
            return position;
        }

        private void CreateDefaultSpawnPoints()
        {
            spawnPoints = new Transform[maxPlayers];
            
            for (int i = 0; i < maxPlayers; i++)
            {
                GameObject spawnPointObj = new GameObject($"SpawnPoint_{i}");
                spawnPointObj.transform.SetParent(transform);
                
                // Arrange spawn points in a circle
                float angle = i * (360f / maxPlayers) * Mathf.Deg2Rad;
                float radius = 5f;
                spawnPointObj.transform.position = new Vector3(
                    Mathf.Cos(angle) * radius, 
                    1f, 
                    Mathf.Sin(angle) * radius
                );
                
                spawnPoints[i] = spawnPointObj.transform;
            }

            Debug.Log($"Created {maxPlayers} default spawn points");
        }

        // Public properties
        public int MaxPlayers => maxPlayers;
        public int ConnectedPlayerCount => _connectedPlayers.Count;
        public bool IsLobbyFull => _connectedPlayers.Count >= maxPlayers;
    }
}
