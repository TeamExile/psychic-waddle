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

        // reference to movement component
        private PlayerController _playerController;

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            // cache movement component if present
            _playerController = GetComponent<PlayerController>();
            if (_playerController != null)
            {
                // default to disabled until ownership is confirmed
                _playerController.SetControlActive(false);
            }
        }

        #region NetworkSetupMethods
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // enable/disable local controls immediately based on ownership
            if (_playerController != null)
            {
                _playerController.SetControlActive(IsOwner);
            }

            if (IsOwner)
            {
                // owner-specific setup: request id and make camera
                RequestPlayerIdServerRpc();

                // ensure camera is created for owner (OnGainedOwnership may not fire reliably on all netcode versions)
                SetupPlayerCamera();
            }

            playerId.OnValueChanged += OnPlayerIdChanged;
            if (playerId.Value != ulong.MaxValue)
            {
                ApplyPlayerMaterial((int)(playerId.Value % int.MaxValue));
            }

            Debug.Log($"NetworkPlayer OnNetworkSpawn. LocalClientId: {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}");
        }

        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();
            Debug.Log($"Ownership gained by client {NetworkManager.Singleton.LocalClientId}");

            // double-safety: enable controls and camera when ownership is gained
            if (_playerController != null)
            {
                _playerController.SetControlActive(true);
            }

            SetupPlayerCamera();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            // disable controls on despawn
            if (_playerController != null)
            {
                _playerController.SetControlActive(false);
            }

            // remove handler
            playerId.OnValueChanged -= OnPlayerIdChanged;

            // destroy local camera instance (only the owner created it)
            if (_playerCamera != null)
            {
                Destroy(_playerCamera.gameObject);
                _playerCamera = null;
            }
        }
        #endregion

        #region PlayerSetupMethods
        private void SetupPlayerCamera()
        {
            // If camera already exists (reconnect scenarios) don't create another
            if (_playerCamera != null) return;

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
            camObj.transform.localPosition = new Vector3(0, 0.8f, 0.6f); // Adjust for FPS or 3rd person
            camObj.transform.localRotation = Quaternion.identity;
            _playerCamera.tag = "MainCamera";
            _playerController.SetCamera(_playerCamera);
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
