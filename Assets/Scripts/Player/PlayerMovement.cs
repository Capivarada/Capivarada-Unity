using UnityEngine;
using ArenadasAlmas.Systems;

namespace ArenadasAlmas.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerConfig playerConfig;
        [SerializeField] private float groundDrag = 5f;
        [SerializeField] private float airDrag = 2f;
        [SerializeField] private float gravity = -9.81f;

        private CharacterController controller;
        private Vector3 velocity = Vector3.zero;
        private Vector3 moveDirection = Vector3.zero;
        
        private float lastDashTime = -999f;
        private bool isDashing = false;
        private float dashStartTime = 0f;

        private void OnEnable()
        {
            if (playerConfig == null)
                playerConfig = GetComponent<PlayerConfig>();
            
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            HandleMovement();
            HandleDash();
            ApplyGravity();
            MoveCharacter();
            HandleRotation();
        }

        private void HandleMovement()
        {
            float moveX = 0;
            float moveZ = 0;

            if (Input.GetKey(KeyCode.W))
                moveZ += 1;
            if (Input.GetKey(KeyCode.S))
                moveZ -= 1;
            if (Input.GetKey(KeyCode.D))
                moveX += 1;
            if (Input.GetKey(KeyCode.A))
                moveX -= 1;

            Vector3 inputDirection = (transform.forward * moveZ + transform.right * moveX).normalized;
            float speed = playerConfig.GetMoveSpeed();
            
            moveDirection = inputDirection * speed;
        }

        private void HandleDash()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                float dashCooldown = playerConfig.GetDashCooldown();
                if (Time.time >= lastDashTime + dashCooldown)
                {
                    lastDashTime = Time.time;
                    isDashing = true;
                    dashStartTime = Time.time;
                }
            }

            if (isDashing)
            {
                float dashDuration = playerConfig.GetDashDuration();
                if (Time.time >= dashStartTime + dashDuration)
                {
                    isDashing = false;
                }
            }
        }

        private void ApplyGravity()
        {
            if (controller.isGrounded && velocity.y < 0)
            {
                velocity.y = 0f;
            }
            else
            {
                velocity.y += gravity * Time.deltaTime;
            }

            float drag = controller.isGrounded ? groundDrag : airDrag;
            velocity.x = Mathf.Lerp(velocity.x, 0, drag * Time.deltaTime);
            velocity.z = Mathf.Lerp(velocity.z, 0, drag * Time.deltaTime);
        }

        private void MoveCharacter()
        {
            Vector3 finalMove = moveDirection;

            if (isDashing)
            {
                float dashForce = playerConfig.GetDashForce();
                finalMove = transform.forward * dashForce;
            }

            finalMove.y = velocity.y;
            controller.Move(finalMove * Time.deltaTime);
        }

        private void HandleRotation()
        {
            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }
        }

        public bool IsDashing() => isDashing;
        public float GetDashCooldownRemaining() => Mathf.Max(0, playerConfig.GetDashCooldown() - (Time.time - lastDashTime));
    }
}
