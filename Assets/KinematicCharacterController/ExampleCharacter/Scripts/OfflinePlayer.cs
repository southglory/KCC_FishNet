using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;

namespace KinematicCharacterController.Offline
{
    public class OfflinePlayer : MonoBehaviour
    {
        // Ramyun: New input system member
        public bool isNewInputSystem;
        private OfflinePlayerInputHandler localInput;

        public OfflineCharacterController Character;
        public OfflineCharacterCamera CharacterCamera;

        // Ramyun: Old input system member
        private const string MouseXInput = "Mouse X";
        private const string MouseYInput = "Mouse Y";
        private const string MouseScrollInput = "Mouse ScrollWheel";
        private const string HorizontalInput = "Horizontal";
        private const string VerticalInput = "Vertical";

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            // Tell camera to follow transform
            CharacterCamera.SetFollowTransform(Character.CameraFollowPoint);

            // Ignore the character's collider(s) for camera obstruction checks
            CharacterCamera.IgnoredColliders.Clear();
            CharacterCamera.IgnoredColliders.AddRange(Character.GetComponentsInChildren<Collider>());

            if (isNewInputSystem)
            {
                localInput = GetComponentInChildren<OfflinePlayerInputHandler>();
                localInput.enabled = true;
            }
        }

        private void Update()
        {
            if (isNewInputSystem)
            {
                if (localInput.cameraLockSwitcher)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
                HandleCharacterNewInput();
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }

                HandleCharacterInput();
            }
        }

        private void LateUpdate()
        {
            // Handle rotating the camera along with physics movers
            if (CharacterCamera.RotateWithPhysicsMover && Character.Motor.AttachedRigidbody != null)
            {
                CharacterCamera.PlanarDirection = Character.Motor.AttachedRigidbody.GetComponent<PhysicsMover>().RotationDeltaFromInterpolation * CharacterCamera.PlanarDirection;
                CharacterCamera.PlanarDirection = Vector3.ProjectOnPlane(CharacterCamera.PlanarDirection, Character.Motor.CharacterUp).normalized;
            }

            if (isNewInputSystem)
            {
                HandleCameraNewInput();
            }
            else
            {
                HandleCameraInput();
            }
        }

        private void HandleCameraNewInput()
        {
            // Create the look input vector for the camera
            float mouseLookAxisUp = localInput.lookDelta.y;
            float mouseLookAxisRight = localInput.lookDelta.x;
            Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

            // Prevent moving the camera while the cursor isn't locked
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                lookInputVector = Vector3.zero;
            }

            // Input for zooming the camera (disabled in WebGL because it can cause problems)
            float scrollInput = -localInput.zoomScroll;
#if UNITY_WEBGL
        scrollInput = 0f;
#endif

            // Apply inputs to the camera
            CharacterCamera.UpdateWithInput(Time.deltaTime, scrollInput, lookInputVector);

            // Handle toggling zoom level
            if (localInput.cameraModeSwitcher)
            {
                Debug.Log("RightButton");
                CharacterCamera.TargetDistance = (CharacterCamera.TargetDistance == 0f) ? CharacterCamera.DefaultDistance : 0f;

                localInput.cameraModeSwitcher = false;
            }
        }

        private void HandleCharacterNewInput()
        {
            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

            // Build the CharacterInputs struct
            characterInputs.MoveAxisForward = localInput.moving_Input.y;
            characterInputs.MoveAxisRight = localInput.moving_Input.x;
            characterInputs.CameraRotation = CharacterCamera.Transform.rotation;
            characterInputs.JumpDown = localInput.jumping; localInput.jumping = false;

            characterInputs.CrouchDown = localInput.crouchingDown;
            characterInputs.CrouchUp = !localInput.crouchingDown;


            // Apply inputs to character
            Character.SetInputs(ref characterInputs);
        }

        private void HandleCameraInput()
        {
            // Create the look input vector for the camera
            float mouseLookAxisUp = Input.GetAxisRaw(MouseYInput);
            float mouseLookAxisRight = Input.GetAxisRaw(MouseXInput);
            Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

            // Prevent moving the camera while the cursor isn't locked
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                lookInputVector = Vector3.zero;
            }

            // Input for zooming the camera (disabled in WebGL because it can cause problems)
            float scrollInput = -Input.GetAxis(MouseScrollInput);
#if UNITY_WEBGL
        scrollInput = 0f;
#endif

            // Apply inputs to the camera
            CharacterCamera.UpdateWithInput(Time.deltaTime, scrollInput, lookInputVector);

            // Handle toggling zoom level
            if (Input.GetMouseButtonDown(1))
            {
                CharacterCamera.TargetDistance = (CharacterCamera.TargetDistance == 0f) ? CharacterCamera.DefaultDistance : 0f;
            }
        }

        private void HandleCharacterInput()
        {
            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

            // Build the CharacterInputs struct
            characterInputs.MoveAxisForward = Input.GetAxisRaw(VerticalInput);
            characterInputs.MoveAxisRight = Input.GetAxisRaw(HorizontalInput);
            characterInputs.CameraRotation = CharacterCamera.Transform.rotation;
            characterInputs.JumpDown = Input.GetKeyDown(KeyCode.Space);
            characterInputs.CrouchDown = Input.GetKeyDown(KeyCode.C);
            characterInputs.CrouchUp = Input.GetKeyUp(KeyCode.C);

            // Apply inputs to character
            Character.SetInputs(ref characterInputs);
        }
    }
}