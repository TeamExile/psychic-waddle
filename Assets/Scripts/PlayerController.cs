using UnityEngine;

namespace Friendslop
{
    /// <summary>
    /// Player controller using modern Unity practices.
    /// Can be extended to use the new Input System (com.unity.inputsystem).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float rotationSpeed = 720f;
        
        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.4f;
        [SerializeField] private LayerMask groundMask = 1; // Default layer
        
        private Rigidbody _rb;
        private bool _isGrounded;
        private Vector3 _moveDirection;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            
            // Create ground check if it doesn't exist
            if (groundCheck == null)
            {
                GameObject groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform);
                groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
                groundCheck = groundCheckObj.transform;
            }
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
            // Using legacy input system for compatibility
            // Can be replaced with new Input System
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            
            _moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
            
            // Jump
            if (Input.GetButtonDown("Jump") && _isGrounded)
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
                // Move the player
                Vector3 moveVelocity = _moveDirection * moveSpeed;
                _rb.velocity = new Vector3(moveVelocity.x, _rb.velocity.y, moveVelocity.z);
                
                // Rotate player to face movement direction
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

        // Visualize ground check in editor
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
