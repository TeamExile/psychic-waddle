using UnityEngine;
using Unity.Netcode;

namespace Friendslop
{
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkPlayer : NetworkBehaviour
    {
        [Header("Player Info")]
        [SerializeField] private Material[] playerMaterials;
        [SerializeField] private Camera _playerCamera;

        private NetworkVariable<ulong> playerId = new NetworkVariable<ulong>(ulong.MaxValue);
        private Renderer _renderer;

        private void Awake()
        {
            Debug.Log($"NetworkPlayer Awake on client {NetworkManager.Singleton.LocalClientId}");
            _renderer = GetComponentInChildren<Renderer>();
        }

        #region NetworkSetupMethods
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log($"NetworkPlayer OnNetworkSpawn on client {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}");
            if (IsOwner)
            {
                RequestPlayerIdServerRpc();
                Debug.Log($"Ownership gained by client {NetworkManager.Singleton.LocalClientId}");
                SetupPlayerCamera();
            }

            playerId.OnValueChanged += OnPlayerIdChanged;
            if (playerId.Value != ulong.MaxValue)
            {
                ApplyPlayerMaterial((int)(playerId.Value % int.MaxValue));
            }
        }

        //public override void OnGainedOwnership()
        //{
        //    base.OnGainedOwnership();
        //    Debug.Log($"Ownership gained by client {NetworkManager.Singleton.LocalClientId}");
        //    SetupPlayerCamera();
        //}

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            playerId.OnValueChanged -= OnPlayerIdChanged;
        }
        #endregion

        #region PlayerSetupMethods
        private void SetupPlayerCamera()
        {
            // Destroy any existing main camera (optional, for single-player scenes)
            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.gameObject != null)
            {
                Destroy(mainCam.gameObject);
            }

            // Create a new camera and attach it to this player
            GameObject camObj = new GameObject("PlayerCamera");
            _playerCamera = camObj.AddComponent<Camera>();
            camObj.transform.SetParent(transform);
            camObj.transform.localPosition = new Vector3(0, 1.6f, 0); // Adjust for FPS or 3rd person
            camObj.transform.localRotation = Quaternion.identity;
            _playerCamera.tag = "MainCamera";
        }

        private void ApplyPlayerMaterial(int id)
        {
            if (_renderer != null && playerMaterials != null && playerMaterials.Length > 0)
            {
                int materialIndex = id % playerMaterials.Length;
                _renderer.material = playerMaterials[materialIndex];
            }
        }
        #endregion

        #region ServerRPCs
        private void OnPlayerIdChanged(ulong oldValue, ulong newValue)
        {
            ApplyPlayerMaterial((int)(newValue % int.MaxValue));
        }

        [ServerRpc]
        private void RequestPlayerIdServerRpc(ServerRpcParams rpcParams = default)
        {
            playerId.Value = rpcParams.Receive.SenderClientId;
        }

        public ulong PlayerId => playerId.Value;
        #endregion
    }
}
