using UnityEngine;
using UnityEngine.InputSystem; // Import the new Input System

namespace Friendslop
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        private const int DEFAULT_GROUND_LAYER = 1;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float rotationSpeed = 720f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.4f;
        [SerializeField] private LayerMask groundMask = DEFAULT_GROUND_LAYER;

        private Rigidbody _rb;
        private bool _isGrounded;
        private Vector3 _moveDirection;

        // Input Actions
        private InputAction _moveAction;
        private InputAction _jumpAction;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            if (groundCheck == null)
            {
                GameObject groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform);
                groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
                groundCheck = groundCheckObj.transform;
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
        }

        private void OnEnable()
        {
            _moveAction.Enable();
            _jumpAction.Enable();
            _jumpAction.performed += OnJump;
        }

        private void OnDisable()
        {
            _moveAction.Disable();
            _jumpAction.Disable();
            _jumpAction.performed -= OnJump;
        }

        private void Update()
        {
            HandleInput();
            CheckGround();
        }

        private void FixedUpdate()
        {
            Move();
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

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = _isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
            }
        }
    }
}
