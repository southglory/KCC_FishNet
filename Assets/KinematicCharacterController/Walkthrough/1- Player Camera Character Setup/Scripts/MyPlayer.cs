using UnityEngine;
using KinematicCharacterController.Examples;
using System.Linq;
using UnityEngine.InputSystem;

namespace KinematicCharacterController.Walkthrough.PlayerCameraCharacterSetup
{
    public class MyPlayer : MonoBehaviour
    {

        public bool isNewInputSystem;
        private MyPlayerInputHandler1 localInput;

        public ExampleCharacterCamera OrbitCamera;
        public Transform CameraFollowPoint;
        public MyCharacterController Character;
        
        private Vector3 _lookInputVector = Vector3.zero;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            // Tell camera to follow transform
            OrbitCamera.SetFollowTransform(CameraFollowPoint);

            // Ignore the character's collider(s) for camera obstruction checks
            OrbitCamera.IgnoredColliders = Character.GetComponentsInChildren<Collider>().ToList();

            if (isNewInputSystem )
            {
                localInput = GetComponentInChildren<MyPlayerInputHandler1>();
                localInput.enabled = true;

            }
        }

        private void Update()
        {
            if (isNewInputSystem)
            {
                if (localInput.cameraLockSwitcher)
                {
                    Debug.Log("leftButton_newInputSystem");
                    Cursor.lockState = CursorLockMode.Locked;

                    localInput.cameraLockSwitcher = false;
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Debug.Log("leftButton");
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }

        private void LateUpdate()
        {
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
            float mouseLookAxisUp = localInput.lookDelta.y; //Input.GetAxisRaw("Mouse Y");
            float mouseLookAxisRight = localInput.lookDelta.x; //Input.GetAxisRaw("Mouse X");
            _lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

            // Prevent moving the camera while the cursor isn't locked
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                _lookInputVector = Vector3.zero;
            }

            // Input for zooming the camera (disabled in WebGL because it can cause problems)
            float scrollInput = -localInput.zoomScroll;//-Input.GetAxis("Mouse ScrollWheel");
#if UNITY_WEBGL
            scrollInput = 0f;
#endif

            // Apply inputs to the camera
            OrbitCamera.UpdateWithInput(Time.deltaTime, scrollInput, _lookInputVector);

            // Handle toggling zoom level
            if (localInput.cameraModeSwitcher)
            {
                Debug.Log("RightButton_newInputSystem");
                OrbitCamera.TargetDistance = (OrbitCamera.TargetDistance == 0f) ? OrbitCamera.DefaultDistance : 0f;

                localInput.cameraModeSwitcher = false;
            }
        }

        private void HandleCameraInput()
        {
            // Create the look input vector for the camera
            float mouseLookAxisUp = Input.GetAxisRaw("Mouse Y");
            float mouseLookAxisRight = Input.GetAxisRaw("Mouse X");
            _lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

            // Prevent moving the camera while the cursor isn't locked
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                _lookInputVector = Vector3.zero;
            }

            // Input for zooming the camera (disabled in WebGL because it can cause problems)
            float scrollInput = -Input.GetAxis("Mouse ScrollWheel");
    #if UNITY_WEBGL
            scrollInput = 0f;
    #endif

            // Apply inputs to the camera
            OrbitCamera.UpdateWithInput(Time.deltaTime, scrollInput, _lookInputVector);

            // Handle toggling zoom level
            if (Input.GetMouseButtonDown(1))
            {
                OrbitCamera.TargetDistance = (OrbitCamera.TargetDistance == 0f) ? OrbitCamera.DefaultDistance : 0f;
            }
        }
    }
}