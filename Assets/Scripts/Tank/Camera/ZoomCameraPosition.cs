using UnityEngine;
using Unity.Cinemachine;
public class ZoomCameraPosition : MonoBehaviour
{
    private CinemachineCamera ZoomCamera;
    private Transform _normalFollowTarget;
    private Transform _normalLookAtTarget;
    private Transform _aimFollowTarget;
    private Transform _aimLookAtTarget;

    [Header("缩放参数")]
    [SerializeField] private float NormalFOV = 60f;

    private float targetFOV;
    private bool isZooming = false;
    private bool _cachedAimMode;
    private bool _targetsInitialized;

    public bool HasCameraReference => ZoomCamera != null;
    public string CameraName => ZoomCamera != null ? ZoomCamera.Name : string.Empty;

    private void Awake()
    {
        ResolveCameraReference();

        if (ZoomCamera == null)
        {
            return;
        }

    }

    private void ResolveCameraReference()
    {
        ZoomCamera = GetComponent<CinemachineCamera>();

        if (ZoomCamera == null)
        {
            ZoomCamera = GetComponentInChildren<CinemachineCamera>(true);
        }
    }

    public void BindTarget(Transform followTarget, Transform lookAtTarget)
    {
        if (ZoomCamera == null || followTarget == null || lookAtTarget == null)
        {
            return;
        }

        _normalFollowTarget = followTarget;
        _normalLookAtTarget = lookAtTarget;

        ApplyActiveTargets();
    }

    public void BindAimTarget(Transform followTarget, Transform lookAtTarget)
    {
        if (ZoomCamera == null || followTarget == null || lookAtTarget == null)
        {
            return;
        }

        _aimFollowTarget = followTarget;
        _aimLookAtTarget = lookAtTarget;

        ApplyActiveTargets();
    }

    void Start()
    {
        if (ZoomCamera == null)
        {
            return;
        }

        targetFOV = NormalFOV;
    }

    void Update()
    {
        if (ZoomCamera == null)
        {
            return;
        }

        bool isAimMode = UIManager.Instance != null && UIManager.Instance.IsAimMode;

        if (!_targetsInitialized || _cachedAimMode != isAimMode)
        {
            ApplyActiveTargets();
            _cachedAimMode = isAimMode;
            _targetsInitialized = true;
        }

        // 检测缩放输入（右键切换ZoomCamera）
        MIddleInputingController inputController = MIddleInputingController.Instance;
        if (inputController != null)
        {
            if (inputController.IsZoomFOVPressed())
            {
                if (!isZooming)
                {
                    int inactivePriority = GetInactivePriority();
                    ZoomCamera.Priority = isAimMode ? inactivePriority : GetActivePriority();
                    targetFOV = NormalFOV;
                    isZooming = true;
                }
            }
            else if (isZooming)
            {
                ZoomCamera.Priority = GetInactivePriority();
                targetFOV = NormalFOV;
                isZooming = false;
            }
        }


    }

    private int GetActivePriority()
    {
        CameraTransitionConfig config = FCSRegistry.CameraTransitionConfig;
        return config != null ? config.ZoomActivePriority : 20;
    }

    private int GetInactivePriority()
    {
        CameraTransitionConfig config = FCSRegistry.CameraTransitionConfig;
        return config != null ? config.ZoomInactivePriority : 0;
    }

    private void ApplyActiveTargets()
    {
        if (ZoomCamera == null)
        {
            return;
        }

        bool isAimMode = UIManager.Instance != null && UIManager.Instance.IsAimMode;
        Transform activeFollowTarget = isAimMode && _aimFollowTarget != null ? _aimFollowTarget : _normalFollowTarget;
        Transform activeLookAtTarget = isAimMode && _aimLookAtTarget != null ? _aimLookAtTarget : _normalLookAtTarget;

        if (activeFollowTarget != null)
        {
            ZoomCamera.Follow = activeFollowTarget;
        }

        if (activeLookAtTarget != null)
        {
            ZoomCamera.LookAt = activeLookAtTarget;
        }
    }



}
