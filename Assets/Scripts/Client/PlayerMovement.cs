using UnityEngine;
using UnityEngine.InputSystem;

namespace Clotzbergh.Client
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        public float initialMoveDelay = 1f;

        public Camera playerCamera;
        public float walkSpeed = 6f;
        public float runSpeed = 12f;
        public float jumpPower = 7f;
        public float gravity = 10f;
        public float lookSpeed = 2f;
        public float lookXLimit = 45f;
        public float defaultHeight = 2f;
        public float crouchHeight = 1f;
        public float crouchSpeed = 3f;

        private Vector3 moveDirection = Vector3.zero;
        private float rotationX = 0;
        private CharacterController characterController;

        private bool canMove = true;

        void Start()
        {
            characterController = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (Time.time < initialMoveDelay)
                return;

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);

            bool isRunning = keyboard?.leftShiftKey.isPressed ?? false;

            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                    horizontal -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                    horizontal += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                    vertical -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                    vertical += 1f;
            }

            float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * vertical : 0;
            float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * horizontal : 0;
            float movementDirectionY = moveDirection.y;
            moveDirection = (forward * curSpeedX) + (right * curSpeedY);

            if ((keyboard?.spaceKey.isPressed ?? false) && canMove && characterController.isGrounded)
            {
                moveDirection.y = jumpPower;
            }
            else
            {
                moveDirection.y = movementDirectionY;
            }

            if (!characterController.isGrounded)
            {
                moveDirection.y -= gravity * Time.deltaTime;
            }

            if ((keyboard?.rKey.isPressed ?? false) && canMove)
            {
                characterController.height = crouchHeight;
                walkSpeed = crouchSpeed;
                runSpeed = crouchSpeed;

            }
            else
            {
                characterController.height = defaultHeight;
                walkSpeed = 6f;
                runSpeed = 12f;
            }

            characterController.Move(moveDirection * Time.deltaTime);

            if (canMove)
            {
                Vector2 mouseDelta = mouse?.delta.ReadValue() ?? Vector2.zero;
                float mouseX = mouseDelta.x * 0.1f;
                float mouseY = mouseDelta.y * 0.1f;

                rotationX += -mouseY * lookSpeed;
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
                playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
                transform.rotation *= Quaternion.Euler(0, mouseX * lookSpeed, 0);
            }
        }
    }
}
