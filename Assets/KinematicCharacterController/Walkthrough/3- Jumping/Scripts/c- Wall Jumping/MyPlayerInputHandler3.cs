using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;

public partial class MyPlayerInputHandler3 : MonoBehaviour
{

    // Store our controls
    InputActions_3 primaryInputActions;
    InputActionMap player_ActionMap;
    InputAction look_Action;
    InputAction zoom_Action;
    InputAction cameraLockSwitch_Action;
    InputAction cameraModeSwitch_Action;
    InputAction move_Action;
    InputAction jump_Action;

    [Header("Camera Movement Settings")]
    public Vector2 lookDelta;
    public float zoomScroll;
    public bool cameraLockSwitcher;
    public bool cameraModeSwitcher;

    [Header("Character Movement Settings")]
    public bool moving;
    public Vector2 moving_Input;
    public bool jumping;

    private void Awake()
    {
        primaryInputActions = new InputActions_3();
        player_ActionMap = primaryInputActions.Player;
        // Quirka: Find ActionMap and Set InputAction References.
        look_Action = player_ActionMap.FindAction("Look");
        zoom_Action = player_ActionMap.FindAction("Zoom");
        cameraLockSwitch_Action = player_ActionMap.FindAction("CameraLockSwitch");
        cameraModeSwitch_Action = player_ActionMap.FindAction("CameraModeSwitch");
        move_Action = player_ActionMap.FindAction("Move");
        jump_Action = player_ActionMap.FindAction("Jump");
    }

    private void OnEnable()
    {
        primaryInputActions.Enable();
        look_Action.Enable();
        zoom_Action.Enable();
        cameraLockSwitch_Action.Enable();
        cameraModeSwitch_Action.Enable();
        move_Action.Enable();
        jump_Action.Enable();
        Subscribe_Input();
    }
    private void OnDisable()
    {
        primaryInputActions.Disable();
        look_Action.Disable();
        zoom_Action.Disable();
        cameraLockSwitch_Action.Disable();
        cameraModeSwitch_Action.Disable();
        move_Action.Disable();
        jump_Action.Disable();
        UnSubscribe_Input();
    }

    private void Subscribe_Input()
    {
        look_Action.started += GetLookInput;
        look_Action.canceled += GetLookInput;

        zoom_Action.started += GetZoomInput;
        zoom_Action.canceled += GetZoomInput;

        cameraLockSwitch_Action.started += GetCameraLockSwitchInput;

        cameraModeSwitch_Action.started += GetCameraModeSwitchInput;

        move_Action.performed += GetMoveInput;
        move_Action.canceled += GetMoveInput;

        jump_Action.started += GetJumpInput;

    }

    private void UnSubscribe_Input()
    {
        look_Action.started -= GetLookInput;
        look_Action.canceled -= GetLookInput;

        zoom_Action.started -= GetZoomInput;
        zoom_Action.canceled -= GetZoomInput;

        cameraLockSwitch_Action.started -= GetCameraLockSwitchInput;

        cameraModeSwitch_Action.started -= GetCameraModeSwitchInput;

        move_Action.performed -= GetMoveInput;
        move_Action.canceled -= GetMoveInput;

        jump_Action.started -= GetJumpInput;
    }

}

// Quirka: Subscription Context
partial class MyPlayerInputHandler3
{
    private void GetLookInput(InputAction.CallbackContext ctx)
    {
        lookDelta = ctx.ReadValue<Vector2>() * (float)0.05;
    }

    private void GetZoomInput(InputAction.CallbackContext ctx)
    {
        zoomScroll = ctx.ReadValue<float>() * (float)0.001;
    }

    private void GetCameraLockSwitchInput(InputAction.CallbackContext ctx)
    {
        if (ctx.started) { cameraLockSwitcher = true; }
    }

    private void GetCameraModeSwitchInput(InputAction.CallbackContext ctx)
    {
        if (ctx.started) { cameraModeSwitcher = true; }
    }

    private void GetMoveInput(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) { moving = true;  moving_Input = ctx.ReadValue<Vector2>(); }
        if (ctx.canceled) { moving = false; moving_Input = ctx.ReadValue<Vector2>(); }
    }

    private void GetJumpInput(InputAction.CallbackContext ctx)
    {
        if (ctx.started) { jumping = true; }
    }
}

