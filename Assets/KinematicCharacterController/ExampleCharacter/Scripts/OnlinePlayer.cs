using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Offline;
using FishNet.Object;
using UnityEditor;
using Unity.VisualScripting;
using FishNet.Transporting;
using FishNet.Object.Prediction;
using FishNet.Component.Prediction;

namespace KinematicCharacterController.Online
{
    public struct ReconcileData : IReconcileData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 BaseVelocity;

        public bool MustUnground;
        public float MustUngroundTime;
        public bool LastMovementIterationFoundAnyGround;
        public CharacterTransientGroundingReport GroundingStatus;
        public Vector3 AttachedRigidbodyVelocity;

        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public class OnlinePlayer : NetworkBehaviour
    {
        // Ramyun: New input system member
        public bool isNewInputSystem;
        private OfflinePlayerInputHandler localInput;

        public OnlineCharacterController Character;
        public OfflineCharacterCamera CharacterCamera;
        public PredictedObject PlayerPredictedObject;

        // Ramyun: Old input system member
        private const string MouseXInput = "Mouse X";
        private const string MouseYInput = "Mouse Y";
        private const string MouseScrollInput = "Mouse ScrollWheel";
        private const string HorizontalInput = "Horizontal";
        private const string VerticalInput = "Vertical";

        // Ramyun: Subscription to timemanager tick event
        private bool _subscribed = false;

        // Ramyun: Server starts
        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("[OnStartServer] Your server is running.");
        }

        // Ramyun: Server stops
        public override void OnStopServer()
        {
            base.OnStopServer();
            Debug.Log("[OnStopServer] Your server was stopped.");
        }

        // Ramyun: Networking starts. This replace start()
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            Debug.Log("[OnStartNetwork] ----- Some Client entered into this Network. -----");

            //Quang: Using manual simultion instead of KCC auto simulation
            KinematicCharacterSystem.Settings.AutoSimulation = false;

            //Quang: Subcribe tick event, this will replace FixedUpdate
            SubscribeToEvents(true);

            CommonPreSetting(kinematic: true);
        }

        // Ramyun: Networking stops
        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            Debug.Log("[OnStopNetwork]  -----  Some Client exited from this Network.  ----- ");

            //Ramyun: UnSubcribe tick event.
            SubscribeToEvents(false);
        }

        private void SubscribeToEvents(bool subscribe)
        {
            if (subscribe == _subscribed)
                return;
            if (base.TimeManager == null)
                return;

            _subscribed = subscribe;
            if (subscribe)
            {
                base.TimeManager.OnTick += TimeManager_OnTick;
                base.TimeManager.OnPostTick += TimeManager_OnPostTick;
            }
            else
            {
                base.TimeManager.OnTick -= TimeManager_OnTick;
                base.TimeManager.OnPostTick -= TimeManager_OnPostTick;
            }
        }

        public struct DebugSentences
        {
            public string _msg;
            public string _(string msg) => _msg = msg;
            public string S(string msg) => _msg += msg;
            public void _() => Debug.Log(_msg);
        }
        DebugSentences notice = new DebugSentences();

        public override void OnStartClient()
        {
            base.OnStartClient();
            notice._("[OnStartClient]");

            //Cursor.lockState = CursorLockMode.Locked;


            // Ramyun: Host(Server + Client)
            if (base.IsHost)
            {
                notice.S("You are a Host.");
                if (base.IsOwner)
                {
                    notice.S("You entered into this Network.");
                    OnlinePlayerSetting(isNewInputSystem);
                }
                else
                {
                    notice.S("Other Client entered into this Network.");
                }
            }
            else
            {
                // Ramyun: Server Only. This passed
                if (base.IsServer){}
                // Ramyun: Client Only
                else
                {
                    
                    if (base.IsOwner)
                    {
                        notice.S("You entered into this network.");
                        OnlinePlayerSetting(isNewInputSystem);
                    }
                    else
                    {
                        notice.S("Other Client entered into this network.");
                        ExclusivePlayerSetting(motor:false);
                    }
                }
            }
            notice._();
        }

        #region OnStartClient Settings
        // Ramyun: Player settings(camera, input)
        private void OnlinePlayerSetting(bool isNewInputSystem)
        {


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

        // Ramyun: Common settings
        private void CommonPreSetting(bool kinematic)
        {
            // Ramyun: Character Settings
            Character = GetComponentInChildren<OnlineCharacterController>();
            if (kinematic) Character.GetComponent<Rigidbody>().isKinematic = true;
            // Ramyun: Camera Settings
            CharacterCamera = GetComponentInChildren<OfflineCharacterCamera>();
            // Ramyun: Player Settings
            PlayerPredictedObject = GetComponent<PredictedObject>();
            PlayerPredictedObject.SetGraphicalObject(Character.transform.Find("GraphicalObject"));
            PlayerPredictedObject.SetRigidbody(Character.GetComponent<Rigidbody>());
        }

        // Ramyun: Exlusive Player settings for other client instances
        private void ExclusivePlayerSetting(bool motor)
        {
            // Ramyun: Character Setting
            if (!motor) Destroy(GetComponent<KinematicCharacterMotor>());
        }
        #endregion

        public override void OnStopClient()
        {
            base.OnStopClient();
            Debug.Log("[OnStopClient] You are out ot server.");
        }


        #region TimeManager Tick events
        // Ramyun: TimeManager_OnTick() replace the update()
        private void TimeManager_OnTick()
        {
            if (base.IsOwner)
            {
                // Ramyun: Character Owner get reconciled state from Server, and Apply it into his own Motor.State
                Reconciliation(default, false);
                PlayerCharacterInputs inputData = HandleCharacterInput();
                HandleCameraInput();
                // Ramyun: Replicate the inputData to Server
                Move(inputData, false);
            }
            else
            {

            }
            if (base.IsServer)
            {
                // Ramyun: Replicate to client
                Move(default, true);
            }
        }
        private void TimeManager_OnPostTick()
        {
            /* Reconcile is sent during PostTick because we
            * want to send the rb data AFTER the simulation. */
            if (base.IsServer)
            {
                // Ramyun: Server get his own Motor.State and translate it into ReconcileData.
                //  Because server is delayed than client, Server send delayed state to Client.
                KinematicCharacterMotorState state = Character.Motor.GetState();
                ReconcileData past_rd = TranslateIntoReconcileData(state);
                Reconciliation(past_rd, true);
            }
        }

        [Reconcile]
        private void Reconciliation(ReconcileData rd, bool asServer, Channel channel = Channel.Unreliable)
        {
            //Quang: Note - KCCMotorState has Rigidbody field, this component is not serialized, 
            // and doesn't have to be reconciled, so we build a new Reconcile data that exclude Rigidbody field
            Character.Motor.ApplyState(TranslateIntoStateData(rd));
        }

        [Replicate]
        private void Move(PlayerCharacterInputs input, bool asServer, Channel channel = Channel.Unreliable, bool replaying = false)
        {
            Character.SetInputs(ref input);

            //Quang: When Fishnet reconcile, it will run this Replicate method as replay in order for redo missing input,
            // so we have to simulate KCC tick manually here, but only when replaying
            if (!asServer)
            {
                if (replaying)
                {
                    KinematicCharacterSystem.SimulateThisTick((float)base.TimeManager.TickDelta);
                }
                else
                {
                    // Ramyun: One time events like Sound Effect, VFX
                }
            }
        }

        private ReconcileData TranslateIntoReconcileData(KinematicCharacterMotorState state)
        {
            ReconcileData rd = new ReconcileData();
            rd.Position = state.Position;
            rd.Rotation = state.Rotation;
            rd.BaseVelocity = state.BaseVelocity;

            rd.MustUnground = state.MustUnground;
            rd.MustUngroundTime = state.MustUngroundTime;
            rd.LastMovementIterationFoundAnyGround = state.LastMovementIterationFoundAnyGround;
            rd.GroundingStatus = state.GroundingStatus;
            rd.AttachedRigidbodyVelocity = state.AttachedRigidbodyVelocity;

            return rd;
        }

        // Ramyun: After ReconcileData is sended to Server, Server give back to client 
        private KinematicCharacterMotorState TranslateIntoStateData(ReconcileData rd)
        {
            KinematicCharacterMotorState state = new KinematicCharacterMotorState();
            state.Position = rd.Position;
            state.Rotation = rd.Rotation;
            state.BaseVelocity = rd.BaseVelocity;

            state.MustUnground = rd.MustUnground;
            state.MustUngroundTime = rd.MustUngroundTime;
            state.LastMovementIterationFoundAnyGround = rd.LastMovementIterationFoundAnyGround;
            state.GroundingStatus = rd.GroundingStatus;
            state.AttachedRigidbodyVelocity = rd.AttachedRigidbodyVelocity;

            return state;
        }

        #endregion

        private void LateUpdate()
        {
            // Handle rotating the camera along with physics movers
            if (CharacterCamera.RotateWithPhysicsMover && Character.Motor.AttachedRigidbody != null)
            {
                CharacterCamera.PlanarDirection = Character.Motor.AttachedRigidbody.GetComponent<PhysicsMover>().RotationDeltaFromInterpolation * CharacterCamera.PlanarDirection;
                CharacterCamera.PlanarDirection = Vector3.ProjectOnPlane(CharacterCamera.PlanarDirection, Character.Motor.CharacterUp).normalized;
            }

        }

        
        // Ramyun: Auto InputSystem switch by wrapping
        private void HandleCameraInput()
        {
            if (isNewInputSystem) { HandleCameraNewInput(); } else { HandleCameraOldInput(); }
        }
        private PlayerCharacterInputs HandleCharacterInput()
        {
            if (isNewInputSystem) { HandleCharacterNewInput(out PlayerCharacterInputs inputData); return inputData; } 
            else  { HandleCharacterOldInput(out PlayerCharacterInputs inputData); return inputData; }
        }

        private void HandleCameraNewInput()
        {
            if (localInput.cameraLockSwitcher)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
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



        private void HandleCameraOldInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
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

        private void HandleCharacterNewInput(out PlayerCharacterInputs characterInputs)
        {
            characterInputs = default;
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

        private void HandleCharacterOldInput(out PlayerCharacterInputs characterInputs)
        {
            characterInputs = default;
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