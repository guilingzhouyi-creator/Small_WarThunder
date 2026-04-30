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
    // [SerializeField] private float ZoomedFOV = 35f;
    [SerializeField] private float ZoomSpeed = 10f;

    private int _ActiveZoomPriority = 20;//缩放摄像机的优先级，当按下缩放键时切换到该摄像机
    private int _ZoomCameraNormalPriority = 0;//默认摄像机的优先级，当松开缩放键时切换回该摄像机




    private float targetFOV;
    private bool isZooming = false;

    public bool HasCameraReference => ZoomCamera != null;

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

        ApplyActiveTargets();

        bool isAimMode = UIManager.Instance != null && UIManager.Instance.IsAimMode;

        // 检测缩放输入（右键切换ZoomCamera）
        MIddleInputingController inputController = MIddleInputingController.Instance;
        if (inputController != null)
        {
            if (inputController.IsZoomFOVPressed())
            {
                // TPS 下按住右键：切换到 ZoomCamera 改变视点；Aim 下由炮圈局部缩放层处理
                if (!isZooming)
                {
                    ZoomCamera.Priority = isAimMode ? _ZoomCameraNormalPriority : _ActiveZoomPriority;
                    targetFOV = NormalFOV;
                    isZooming = true;
                    Debug.Log("🔍 切换到缩放摄像机");
                }
            }
            else if (isZooming)
            {
                // 松开右键：恢复原摄像机（降低优先级）
                ZoomCamera.Priority = _ZoomCameraNormalPriority;  // 降低优先级
                targetFOV = NormalFOV;
                isZooming = false;
                Debug.Log("📷 恢复原摄像机");
            }
        }


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