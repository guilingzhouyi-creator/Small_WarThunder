using UnityEngine;
using Unity.Cinemachine;

public class AimCameraPosition : MonoBehaviour
{
    [SerializeField] private bool useHardLock = true;
    private CinemachineCamera cinemachineCamera;
    private Transform hardLockTarget;
    private bool _lastAimState = false;

    // 优先级设定：确保 Aim(15) 高于普通(10)，但低于 Zoom(20)
    private int _ActivePriority = 15;
    private int _InactivePriority = 0;

    public bool HasCameraReference => cinemachineCamera != null;

    private void Awake()
    {
        if (cinemachineCamera == null) cinemachineCamera = GetComponent<CinemachineCamera>();
        cinemachineCamera.Priority = _InactivePriority;
    }

    private void Update()
    {
        if (cinemachineCamera == null)
        {
            return;
        }

        bool isAimMode = UIManager.Instance != null && UIManager.Instance.IsAimMode;


        if (isAimMode != _lastAimState)
        {
            cinemachineCamera.Priority = isAimMode ? _ActivePriority : _InactivePriority;
            _lastAimState = isAimMode;

            if (useHardLock && hardLockTarget != null)
            {
                // 切换到瞄准模式时，立即将摄像机位置锁定到目标，避免过渡抖动。
                Transform cameraTransform = cinemachineCamera.transform;
                cameraTransform.SetPositionAndRotation(hardLockTarget.position, hardLockTarget.rotation);
            }
        }
        else
        {
            cinemachineCamera.Priority = isAimMode ? _ActivePriority : _InactivePriority;
        }
    }

    private void LateUpdate()
    {
        if (!useHardLock || cinemachineCamera == null || hardLockTarget == null)
        {
            return;
        }

        bool isAimMode = UIManager.Instance != null && UIManager.Instance.IsAimMode;
        if (!isAimMode)
        {
            return;
        }

        // 硬锁到炮镜挂点，避免 Follow/LookAt 插值导致的抖动。
        Transform cameraTransform = cinemachineCamera.transform;
        cameraTransform.SetPositionAndRotation(hardLockTarget.position, hardLockTarget.rotation);
    }

    public void BindTarget(Transform followTarget, Transform lookAtTarget)
    {
        if (cinemachineCamera == null || followTarget == null)
        {
            return;
        }

        hardLockTarget = followTarget;

        if (useHardLock)
        {
            // 使用硬锁时不依赖 Cinemachine 的目标跟随，避免阻尼/轨道解算干扰。
            cinemachineCamera.Follow = null;
            cinemachineCamera.LookAt = null;
            return;
        }

        if (lookAtTarget == null)
        {
            return;
        }

        cinemachineCamera.Follow = followTarget;
        cinemachineCamera.LookAt = lookAtTarget;
    }
}