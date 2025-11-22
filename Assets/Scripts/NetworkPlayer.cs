using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem; // Import the new Input System

namespace Friendslop
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkPlayer : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float acceleration = 20f;
        [SerializeField] private float damping = 10f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.4f;
        [SerializeField] private LayerMask groundMask = 1;

        [Header("Shooting")]
        [SerializeField] private Gun gun;

        [Header("Player Info")]
        [SerializeField] private Material[] playerMaterials;

        [Header("Camera")]
        [SerializeField] private GameObject playerCameraPrefab; // Assign a camera prefab in the inspector

        private NetworkVariable<ulong> playerId = new NetworkVariable<ulong>(ulong.MaxValue);
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();

        private Rigidbody _rb;
        private bool _isGrounded;
        private Vector3 _moveDirection;
        private Renderer _renderer;
        private Camera _playerCamera;

        // Input Actions
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _shootAction;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _renderer = GetComponentInChildren<Renderer>();

            if (groundCheck == null)
            {
                GameObject groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform);
                groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
                groundCheck = groundCheckObj.transform;
            }

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

            // Setup Input Actions
            _moveAction = new InputAction("Move", InputActionType.Value, "<Gamepad>/leftStick");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            _jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            _jumpAction.AddBinding("<Gamepad>/buttonSouth");

            _shootAction = new InputAction("Shoot", InputActionType.Button, "<Mouse>/leftButton");
            _shootAction.AddBinding("<Keyboard>/leftCtrl");
            _shootAction.AddBinding("<Gamepad>/rightTrigger");
        }

        private void OnEnable()
        {
            _moveAction.Enable();
            _jumpAction.Enable();
            _shootAction.Enable();

            _jumpAction.performed += OnJump;
            _shootAction.performed += OnShoot;
        }

        private void OnDisable()
        {
            _moveAction.Disable();
            _jumpAction.Disable();
            _shootAction.Disable();

            _jumpAction.performed -= OnJump;
            _shootAction.performed -= OnShoot;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                RequestPlayerIdServerRpc();
            }

            playerId.OnValueChanged += OnPlayerIdChanged;
            if (playerId.Value != ulong.MaxValue)
            {
                ApplyPlayerMaterial((int)(playerId.Value % int.MaxValue));
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
                transform.position = Vector3.Lerp(transform.position, networkPosition.Value, Time.deltaTime * 10f);
                transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation.Value, Time.deltaTime * 10f);
            }
        }

        private void FixedUpdate()
        {
            if (IsOwner)
            {
                Move();
                UpdatePositionServerRpc(transform.position, transform.rotation);
            }
        }

        private void HandleInput()
        {
            Vector2 input = _moveAction.ReadValue<Vector2>();
            _moveDirection = new Vector3(input.x, 0f, input.y).normalized;
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            if (_isGrounded)
            {
                Jump();
            }
        }

        private void OnShoot(InputAction.CallbackContext context)
        {
            if (gun != null && gun.TryShoot())
            {
                ShootServerRpc();
            }
        }

        private void CheckGround()
        {
            _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }

        // Replace your Move() method with this:
        private void Move()
        {
            // Calculate desired velocity based on input
            Vector3 desiredVelocity = _moveDirection * moveSpeed;

            // Get current velocity (ignore vertical for movement)
            Vector3 currentVelocity = _rb.linearVelocity;
            Vector3 velocityChange = desiredVelocity - new Vector3(currentVelocity.x, 0f, currentVelocity.z);

            // Apply acceleration and damping for smooth movement
            Vector3 force = velocityChange * acceleration - new Vector3(currentVelocity.x, 0f, currentVelocity.z) * damping;
            _rb.AddForce(force, ForceMode.Acceleration);

            // Smoothly rotate player to face movement direction
            if (_moveDirection.magnitude >= 0.1f)
            {
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

        private void OnPlayerIdChanged(ulong oldValue, ulong newValue)
        {
            ApplyPlayerMaterial((int)(newValue % int.MaxValue));
        }

        [ServerRpc]
        private void RequestPlayerIdServerRpc(ServerRpcParams rpcParams = default)
        {
            playerId.Value = rpcParams.Receive.SenderClientId;
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
            ShootClientRpc();
        }

        [ClientRpc]
        private void ShootClientRpc()
        {
            if (!IsOwner && gun != null)
            {
                gun.TryShoot();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = _isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
            }
        }

        public ulong PlayerId => playerId.Value;
    }
}
