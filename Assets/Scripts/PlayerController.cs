using UnityEngine;
using UnityEngine.InputSystem;

namespace Friendslop
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerCamera = null;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float airControl = 0.5f;

        [Header("Jump & Gravity")]
        [SerializeField] private float jumpForce = 6f;
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private float terminalVelocity = 20f;

        [Header("Crouch")]
        [SerializeField] private bool enableCrouch = true;
        [SerializeField] private float crouchHeight = 1f;
        [SerializeField] private float standHeight = 1.8f;
        [SerializeField] private float crouchSpeed = 2f;

        [Header("Look")]
        [SerializeField] private float lookSensitivity = 1.5f;
        [SerializeField] private float lookSmoothing = 0.05f;
        [SerializeField] private float maxLookAngle = 90f;

        private CharacterController _controller;
        private Vector3 _velocity; // vertical velocity Y tracked here
        private Vector3 _moveVelocity; // current horizontal velocity used for smoothing
        private float _currentSpeed;
        private float _targetSpeed;

        private float _xRotation; // camera pitch
        private Vector2 _lookSmoothVelocity;
        private Vector2 _currentLook;

        // Input Actions
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _crouchAction;

        // State
        private bool _isSprinting;
        private bool _isCrouching;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (playerCamera == null)
            {
                Debug.LogWarning("PlayerController: Player Camera not assigned. Assign a child camera for proper look control.");
            }

            // Build input actions in code for simple, inspector-free setup
            _moveAction = new InputAction("Move", InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d")
                .With("Up", "<Gamepad>/leftStick/up")
                .With("Down", "<Gamepad>/leftStick/down")
                .With("Left", "<Gamepad>/leftStick/left")
                .With("Right", "<Gamepad>/leftStick/right");

            _lookAction = new InputAction("Look", InputActionType.Value);
            _lookAction.AddBinding("<Mouse>/delta");
            _lookAction.AddBinding("<Gamepad>/rightStick");

            _jumpAction = new InputAction("Jump", InputActionType.Button);
            _jumpAction.AddBinding("<Keyboard>/space");
            _jumpAction.AddBinding("<Gamepad>/buttonSouth");

            _sprintAction = new InputAction("Sprint", InputActionType.Button);
            _sprintAction.AddBinding("<Keyboard>/leftShift");
            _sprintAction.AddBinding("<Gamepad>/leftStickPress");

            _crouchAction = new InputAction("Crouch", InputActionType.Button);
            _crouchAction.AddBinding("<Keyboard>/leftCtrl");
            _crouchAction.AddBinding("<Gamepad>/buttonWest"); // optional
        }

        private void OnEnable()
        {
            _moveAction.Enable();
            _lookAction.Enable();
            _jumpAction.Enable();
            _sprintAction.Enable();
            _crouchAction.Enable();

            // keep cursor locked for FPS feel; caller (network/menus) can change it
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            _moveAction.Disable();
            _lookAction.Disable();
            _jumpAction.Disable();
            _sprintAction.Disable();
            _crouchAction.Disable();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            HandleLook();
            HandleMovementInput();
            HandleCrouch();
        }

        public void SetCamera(Camera cam)
        {
            if (cam != null)
            {
                playerCamera = cam.transform;
            }
        }

        private void HandleLook()
        {
            if (playerCamera == null) return;

            Vector2 rawLook = _lookAction.ReadValue<Vector2>() * lookSensitivity;
            // smoothing
            _currentLook.x = Mathf.SmoothDamp(_currentLook.x, rawLook.x, ref _lookSmoothVelocity.x, lookSmoothing);
            _currentLook.y = Mathf.SmoothDamp(_currentLook.y, rawLook.y, ref _lookSmoothVelocity.y, lookSmoothing);

            _xRotation -= _currentLook.y;
            _xRotation = Mathf.Clamp(_xRotation, -maxLookAngle, maxLookAngle);

            playerCamera.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * _currentLook.x);
        }

        private void HandleMovementInput()
        {
            Vector2 input = _moveAction.ReadValue<Vector2>();
            Vector3 inputDir = new Vector3(input.x, 0f, input.y);
            inputDir = Vector3.ClampMagnitude(inputDir, 1f);

            // Determine target speed
            _isSprinting = _sprintAction.ReadValue<float>() > 0.5f;
            _targetSpeed = _isSprinting ? sprintSpeed : walkSpeed;
            if (_isCrouching) _targetSpeed = crouchSpeed;

            // Horizontal target velocity relative to player orientation
            Vector3 targetVelocity = transform.TransformDirection(inputDir) * _targetSpeed;

            // Smooth acceleration; air control is reduced
            float accel = (_controller.isGrounded ? acceleration : acceleration * airControl);
            _moveVelocity = Vector3.MoveTowards(_moveVelocity, targetVelocity, accel * Time.deltaTime);

            // Apply horizontal movement
            Vector3 horizontalMove = _moveVelocity;
            _controller.Move(horizontalMove * Time.deltaTime);

            // Gravity and jump handling
            if (_controller.isGrounded)
            {
                if (_velocity.y < 0f)
                    _velocity.y = -2f; // small downward force to keep grounded

                if (_jumpAction.triggered && !_isCrouching)
                {
                    _velocity.y = jumpForce;
                }
            }
            else
            {
                _velocity.y = Mathf.Max(_velocity.y - gravity * Time.deltaTime, -terminalVelocity);
            }

            _controller.Move(new Vector3(0f, _velocity.y, 0f) * Time.deltaTime);
        }

        private void HandleCrouch()
        {
            if (!enableCrouch) return;

            bool crouchPressed = _crouchAction.ReadValue<float>() > 0.5f;
            // simple: hold to crouch
            if (crouchPressed && !_isCrouching)
            {
                StartCrouch();
            }
            else if (!crouchPressed && _isCrouching)
            {
                TryStand();
            }
        }

        private void StartCrouch()
        {
            _isCrouching = true;
            _controller.height = crouchHeight;
            // lower center to match height
            _controller.center = new Vector3(0f, crouchHeight / 2f, 0f);
            // adjust camera
            if (playerCamera != null)
            {
                playerCamera.localPosition = new Vector3(0f, crouchHeight - 0.2f, 0f);
            }
        }

        private void TryStand()
        {
            // check if there is room to stand (raycast up)
            float headClearance = standHeight - _controller.height;
            Vector3 origin = transform.position + Vector3.up * _controller.height;
            if (Physics.SphereCast(origin, _controller.radius, Vector3.up, out RaycastHit hit, headClearance))
            {
                // can't stand
                return;
            }

            // stand up
            _isCrouching = false;
            _controller.height = standHeight;
            _controller.center = new Vector3(0f, standHeight / 2f, 0f);
            if (playerCamera != null)
            {
                playerCamera.localPosition = new Vector3(0f, standHeight - 0.2f, 0f);
            }
        }

        // Public helper to enable/disable controls (useful for NetworkPlayer ownership)
        public void SetControlActive(bool active)
        {
            enabled = active;
            if (active)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}