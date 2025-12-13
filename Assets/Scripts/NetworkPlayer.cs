    using UnityEngine;
using Unity.Netcode;

namespace Friendslop
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(PlayerHealth))]
    public class NetworkPlayer : NetworkBehaviour
    {
        [Header("Player Info")]
        [SerializeField] private Material[] playerMaterials;
        [SerializeField] private Camera _playerCamera;

        private NetworkVariable<ulong> playerId = new NetworkVariable<ulong>(ulong.MaxValue);
        private Renderer _renderer;

        // reference to movement component
        private PlayerController _playerController;

        // track whether this instance created the camera so we only destroy cameras we created
        private bool _createdLocalCamera;

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

            // If the prefab contains a Camera as a child, make sure only the owner enables/uses it.
            Camera childCam = GetComponentInChildren<Camera>(true);
            if (childCam != null)
            {
                if (IsOwner)
                {
                    // Owner should use/enable the camera and register it as this player's camera.
                    _playerCamera = childCam;
                    _playerCamera.enabled = true;
                    _playerController?.SetCamera(_playerCamera);
                    _createdLocalCamera = false; // camera came from prefab
                }
                else
                {
                    // Non-owners should NOT have their player cameras enabled locally.
                    // Disable so it doesn't interfere with the local client's view.
                    childCam.enabled = false;

                    // If the prefab camera had the MainCamera tag (common mistake), remove it so it
                    // doesn't change Camera.main for the whole process (host/editor scenarios).
                    if (childCam.CompareTag("MainCamera"))
                    {
                        childCam.tag = "Untagged";
                    }
                }
            }

            if (IsOwner)
            {
                // owner-specific setup: request id and make camera if none found
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

            // If a prefab camera existed and was disabled on non-owner clients, enable it now.
            Camera childCam = GetComponentInChildren<Camera>(true);
            if (childCam != null)
            {
                _playerCamera = childCam;
                _playerCamera.enabled = true;
                _playerController?.SetCamera(_playerCamera);
                _createdLocalCamera = false;
            }
            else
            {
                // otherwise create one
                SetupPlayerCamera();
            }
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

            // destroy only the camera this instance created (owner only)
            if (_createdLocalCamera && _playerCamera != null)
            {
                Destroy(_playerCamera.gameObject);
                _playerCamera = null;
                _createdLocalCamera = false;
            }
            else if (_playerCamera != null && IsOwner == false)
            {
                // ensure we don't leave a disabled prefab camera tagged as MainCamera by accident
                if (_playerCamera.CompareTag("MainCamera"))
                {
                    _playerCamera.tag = "Untagged";
                }
                _playerCamera = null;
            }
        }
        #endregion

        #region PlayerSetupMethods
        private void SetupPlayerCamera()
        {
            // Only run for the local owner — every client should create/own its own camera.
            if (!IsOwner) return;

            // If we already have a cached reference (from prefab or earlier), don't create another
            if (_playerCamera != null) return;

            // Create a new camera and attach it to this player (local owner only)
            GameObject camObj = new GameObject($"PlayerCamera_{NetworkManager.Singleton.LocalClientId}");
            _playerCamera = camObj.AddComponent<Camera>();
            camObj.transform.SetParent(transform);
            camObj.transform.localPosition = new Vector3(0, 0.8f, 0.6f); // Adjust for FPS or 3rd person
            camObj.transform.localRotation = Quaternion.identity;

            // Do not set the "MainCamera" tag here. Camera.main is a global convenience and
            // will always resolve to the last camera tagged "MainCamera" in the same process.
            // For local ownership we only need the camera component enabled for this client.
            _playerCamera.enabled = true;

            // mark that we created this camera so we can clean it up on despawn
            _createdLocalCamera = true;

            _playerController?.SetCamera(_playerCamera);
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
