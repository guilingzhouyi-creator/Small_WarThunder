using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 地图俯拍摄像机控制器。
/// 使用正交投影，从玩家上方俯拍，将画面输出到 RenderTexture 供 UGUI 的 RawImage 显示。
/// 激活时通过提高 Priority 切换到该摄像机，关闭时恢复低优先级。
/// 
/// 用法：场景中挂在一个带 CinemachineCamera 组件的空物体上，Inspector 中拖入 Camera。
/// 该 Camera 的 TargetTexture 由 MapUIController 在 Start 时分配。
/// </summary>
public class MapCameraPosition : MonoBehaviour
{
    [Header("俯拍参数")]
    [SerializeField] private float _overheadHeight = 500f;
    [SerializeField] private float _orthoSize = 250f;
    [SerializeField] private int _activePriority = 20;
    [SerializeField] private int _inactivePriority = 0;

    [Header("俯拍摄像机")]
    [SerializeField] private Camera _mapCamera;

    private CinemachineCamera _cinemachineCamera;

    public bool HasCameraReference => _cinemachineCamera != null;
    public string CameraName => _cinemachineCamera != null ? _cinemachineCamera.Name : string.Empty;
    public Camera MapCamera => _mapCamera;

    /// <summary>当前相机正交半高度（世界单位），供 MapRenderingEngine 坐标映射使用。</summary>
    public float OrthoSize => _orthoSize;

    private void Awake()
    {
        ResolveCameraReference();
        ResolveMapCamera();
        SetupOrthoCamera();
        SetInactive();
    }

    private void ResolveCameraReference()
    {
        _cinemachineCamera = GetComponent<CinemachineCamera>();
        if (_cinemachineCamera == null)
            _cinemachineCamera = GetComponentInChildren<CinemachineCamera>(true);
    }

    private void ResolveMapCamera()
    {
        if (_mapCamera != null) return;

        if (_cinemachineCamera != null)
            _mapCamera = _cinemachineCamera.GetComponent<Camera>();

        if (_mapCamera == null)
            _mapCamera = GetComponentInChildren<Camera>(true);
    }

    /// <summary>
    /// 配置正交投影（俯拍地图专用）。
    /// </summary>
    private void SetupOrthoCamera()
    {
        if (_cinemachineCamera == null) return;

        _cinemachineCamera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
        _cinemachineCamera.Lens.OrthographicSize = _orthoSize;
        _cinemachineCamera.Target.TrackingTarget = null;
    }

    /// <summary>
    /// 绑定俯拍跟随目标（通常是玩家坦克根节点）。
    /// </summary>
    public void BindTarget(Transform followTarget)
    {
        if (_cinemachineCamera == null || followTarget == null) return;

        _cinemachineCamera.Follow = followTarget;
        _cinemachineCamera.LookAt = null;
    }

    /// <summary>
    /// 设置相机的 TargetTexture（由 MapUIController 调用）。
    /// </summary>
    public void SetTargetTexture(RenderTexture rt)
    {
        if (_mapCamera != null)
            _mapCamera.targetTexture = rt;
    }

    /// <summary>
    /// 切换到地图俯拍视角。
    /// </summary>
    public void SetActive()
    {
        if (_cinemachineCamera == null) return;
        _cinemachineCamera.Priority = _activePriority;
    }

    /// <summary>
    /// 恢复非激活状态（第三人称相机接管）。
    /// </summary>
    public void SetInactive()
    {
        if (_cinemachineCamera == null) return;
        _cinemachineCamera.Priority = _inactivePriority;
    }
}
