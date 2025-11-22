using UnityEngine;
using Unity.Netcode;

namespace Friendslop
{
    /// <summary>
    /// Network-enabled player controller that extends PlayerController functionality.
    /// Synchronizes player movement and actions across the network.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkPlayer : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float rotationSpeed = 720f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.4f;
        [SerializeField] private LayerMask groundMask = 1;

        [Header("Shooting")]
        [SerializeField] private Gun gun;

        [Header("Player Info")]
        [SerializeField] private Material[] playerMaterials; // Different colors for different players

        // Network variables for synchronization
        private NetworkVariable<int> playerId = new NetworkVariable<int>(-1);
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();

        private Rigidbody _rb;
        private bool _isGrounded;
        private Vector3 _moveDirection;
        private Renderer _renderer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _renderer = GetComponentInChildren<Renderer>();

            // Create ground check if it doesn't exist
            if (groundCheck == null)
            {
                GameObject groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform);
                groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
                groundCheck = groundCheckObj.transform;
            }

            // Setup gun if not assigned
            if (gun == null)
            {
                gun = GetComponentInChildren<Gun>();
                if (gun == null)
                {
                    GameObject gunObj = new GameObject("Gun");
                    gunObj.transform.SetParent(transform);
                    gunObj.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);
                    gun = gunObj.AddComponent<Gun>();
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                // Set player ID for this client
                RequestPlayerIdServerRpc();
            }

            // Apply player material based on player ID
            playerId.OnValueChanged += OnPlayerIdChanged;
            if (playerId.Value >= 0)
            {
                ApplyPlayerMaterial(playerId.Value);
            }

            Debug.Log($"NetworkPlayer spawned. IsOwner: {IsOwner}, IsServer: {IsServer}, PlayerId: {playerId.Value}");
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            playerId.OnValueChanged -= OnPlayerIdChanged;
        }

        private void Update()
        {
            if (IsOwner)
            {
                HandleInput();
                CheckGround();
            }
            else
            {
                // Interpolate non-owner players to their network positions
                transform.position = Vector3.Lerp(transform.position, networkPosition.Value, Time.deltaTime * 10f);
                transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation.Value, Time.deltaTime * 10f);
            }
        }

        private void FixedUpdate()
        {
            if (IsOwner)
            {
                Move();
                
                // Update network variables
                UpdatePositionServerRpc(transform.position, transform.rotation);
            }
        }

        private void HandleInput()
        {
            // Movement input
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            _moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

            // Jump input
            if (Input.GetButtonDown("Jump") && _isGrounded)
            {
                Jump();
            }

            // Shooting input
            if (Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0))
            {
                if (gun != null && gun.TryShoot())
                {
                    // Notify server about shooting
                    ShootServerRpc();
                }
            }
        }

        private void CheckGround()
        {
            _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }

        private void Move()
        {
            if (_moveDirection.magnitude >= 0.1f)
            {
                Vector3 moveVelocity = _moveDirection * moveSpeed;
                _rb.linearVelocity = new Vector3(moveVelocity.x, _rb.linearVelocity.y, moveVelocity.z);

                Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.fixedDeltaTime
                );
            }
        }

        private void Jump()
        {
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        private void ApplyPlayerMaterial(int id)
        {
            if (_renderer != null && playerMaterials != null && playerMaterials.Length > 0)
            {
                int materialIndex = id % playerMaterials.Length;
                _renderer.material = playerMaterials[materialIndex];
            }
        }

        private void OnPlayerIdChanged(int oldValue, int newValue)
        {
            ApplyPlayerMaterial(newValue);
        }

        // Network RPCs
        [ServerRpc]
        private void RequestPlayerIdServerRpc(ServerRpcParams rpcParams = default)
        {
            // Assign player ID based on client ID
            playerId.Value = (int)rpcParams.Receive.SenderClientId;
        }

        [ServerRpc]
        private void UpdatePositionServerRpc(Vector3 position, Quaternion rotation)
        {
            networkPosition.Value = position;
            networkRotation.Value = rotation;
        }

        [ServerRpc]
        private void ShootServerRpc()
        {
            // Server authoritative shooting - notify all clients
            ShootClientRpc();
        }

        [ClientRpc]
        private void ShootClientRpc()
        {
            // Only execute for non-owners (owner already shot locally)
            if (!IsOwner && gun != null)
            {
                gun.TryShoot();
            }
        }

        // Visualize ground check in editor
        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = _isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
            }
        }

        // Public properties
        public int PlayerId => playerId.Value;
    }
}
