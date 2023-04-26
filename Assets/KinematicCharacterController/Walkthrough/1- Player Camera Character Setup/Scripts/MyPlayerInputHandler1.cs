using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;

public partial class MyPlayerInputHandler1 : MonoBehaviour
{

    // Store our controls
    InputActions_1 primaryInputActions;
    InputActionMap player_ActionMap;
    InputAction look_Action;
    InputAction zoom_Action;
    InputAction cameraLockSwitch_Action;
    InputAction cameraModeSwitch_Action;

    [Header("Camera Movement Settings")]
    public bool cameraLockSwitcher;
    public bool cameraModeSwitcher;
    public Vector2 lookDelta;
    public float zoomScroll;

    private void Awake()
    {
        primaryInputActions = new InputActions_1();
        player_ActionMap = primaryInputActions.Player;
        // Quirka: Find ActionMap and Set InputAction References.
        look_Action = player_ActionMap.FindAction("Look");
        zoom_Action = player_ActionMap.FindAction("Zoom");
        cameraLockSwitch_Action = player_ActionMap.FindAction("CameraLockSwitch");
        cameraModeSwitch_Action = player_ActionMap.FindAction("CameraModeSwitch");
    }

    private void OnEnable()
    {
        primaryInputActions.Enable();
        look_Action.Enable();
        zoom_Action.Enable();
        cameraLockSwitch_Action.Enable();
        cameraModeSwitch_Action.Enable();
        Subscribe_Input();
    }
    private void OnDisable()
    {
        primaryInputActions.Disable();
        look_Action.Disable();
        zoom_Action.Disable();
        cameraLockSwitch_Action.Disable();
        cameraModeSwitch_Action.Disable();
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
    }

    private void UnSubscribe_Input()
    {
        look_Action.started -= GetLookInput;
        look_Action.canceled -= GetLookInput;

        zoom_Action.started -= GetZoomInput;
        zoom_Action.canceled -= GetZoomInput;

        cameraLockSwitch_Action.started -= GetCameraLockSwitchInput;

        cameraModeSwitch_Action.started -= GetCameraModeSwitchInput;
    }

}

// Quirka: Subscription Context
partial class MyPlayerInputHandler1
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
}

