using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 中间输入控制器 - 作为输入系统和游戏逻辑之间的桥梁
/// 统一管理 InputAction 系统，提供清晰的输入检查接口
/// </summary>
public class MIddleInputingController : MonoBehaviour
{
    public static MIddleInputingController Instance { get; private set; }

    public event EventHandler OnPauseInputProcessed;
    public event EventHandler OnTabInputProcessed;

    private GameInputingSystem _inputActions;

    private void Awake()
    {
        // if (Instance == null)
        // {
        //     Instance = this;
        // }
        // else
        // {
        //     if (Application.isPlaying)
        //     {
        //         Destroy(gameObject);
        //     }

        //     return;
        // }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _inputActions = new GameInputingSystem();
        _inputActions.Enable();

        // 订阅输入事件 - 当玩家进行任何输入时触发 OnInputProcessed 事件
        // _inputActions.TankerDriver.Forward.performed += OnMovingInput;
        _inputActions.TankerDriver.Pause.performed += OnPauseInput;
        _inputActions.TankerDriver.MIssionLabShow.performed += OnTabInput;
    }


    private void OnPauseInput(InputAction.CallbackContext context)
    {
        OnPauseInputProcessed?.Invoke(this, EventArgs.Empty);
    }

    private void OnTabInput(InputAction.CallbackContext context)
    {
        OnTabInputProcessed?.Invoke(this, EventArgs.Empty);
    }



    /// <summary>
    /// 检查前进键是否按下
    /// </summary>
    public bool IsForwardPressed() => _inputActions.TankerDriver.Forward.IsPressed();

    /// <summary>
    /// 检查后退键是否按下
    /// </summary>
    public bool IsBackwardPressed() => _inputActions.TankerDriver.Back.IsPressed();

    /// <summary>
    /// 检查左转键是否按下
    /// </summary>
    public bool IsTurningLeftPressed() => _inputActions.TankerDriver.LeftTurn.IsPressed();

    /// <summary>
    /// 检查右转键是否按下
    /// </summary>
    public bool IsTurningRightPressed() => _inputActions.TankerDriver.RightTurn.IsPressed();

    /// <summary>
    /// 检查机枪开火键是否按下
    /// </summary>
    public bool IsMachineGunFirePressed() => _inputActions.TankerDriver.MachinegunFire.IsPressed();

    /// <summary>
    /// 检查主自由视角键是否按下
    /// </summary>
    public bool IsFreeLookPressed() => _inputActions.TankerDriver.FreeLooking.IsPressed();

    /// <summary>
    /// 检查变焦键是否按下
    /// </summary>
    public bool IsZoomFOVPressed() => _inputActions.TankerDriver.ZoomFOV.IsPressed();

    /// <summary>
    /// 检查主炮开火键是否按下
    /// </summary>
    public bool IsFirePressed() => _inputActions.TankerDriver.Fire.triggered;

    /// <summary>
    /// 检查换弹键是否按下
    /// </summary>
    public bool IsSwitchAmmoPressed() => _inputActions.TankerDriver.Reload.triggered;

    /// <summary>
    /// 检查引擎开关键是否按下
    /// </summary>
    public bool IsToggleEnginePressed() => _inputActions.TankerDriver.EngineSwitch.triggered;

    /// <summary> 
    /// 检查瞄准键是否按下
    /// </summary>
    public bool IsAimingPressed() => _inputActions.TankerDriver.GunAim.triggered;

    /// <summary>
    /// 检查测距键是否按下
    /// </summary>
    public bool IsRangeFinderPressed() => _inputActions.TankerDriver.Rangefinder.triggered;


    public Vector2 GetMouseScrollDelta() => Mouse.current != null ? Mouse.current.scroll.ReadValue() : Vector2.zero;// 获取鼠标滚轮输入，返回一个 Vector2，x 轴表示水平滚动，y 轴表示垂直滚动

    // 获取鼠标每帧位移，用于判断是否仍处于回归状态
    public Vector2 GetMouseDelta() => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;


}