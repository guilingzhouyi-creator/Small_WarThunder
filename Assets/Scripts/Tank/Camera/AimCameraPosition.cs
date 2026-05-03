using UnityEngine;
using Unity.Cinemachine;

public class AimCameraPosition : MonoBehaviour
{
    [SerializeField] private bool useHardLock = true;
    private CinemachineCamera cinemachineCamera;
    private Transform hardLockTarget;
    private bool _lastAimState = false;

    public bool HasCameraReference => cinemachineCamera != null;
    public string CameraName => cinemachineCamera != null ? cinemachineCamera.Name : string.Empty;

    private void Awake()
    {
        if (cinemachineCamera == null) cinemachineCamera = GetComponent<CinemachineCamera>();
        cinemachineCamera.Priority = GetInactivePriority();
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
            CameraTransitionConfig config = FCSRegistry.CameraTransitionConfig;
            int activePriority = config != null ? config.AimActivePriority : 15;
            int inactivePriority = config != null ? config.AimInactivePriority : 0;
            cinemachineCamera.Priority = isAimMode ? activePriority : inactivePriority;
            _lastAimState = isAimMode;


            if (useHardLock && hardLockTarget != null)
            {
                Transform cameraTransform = cinemachineCamera.transform;
                cameraTransform.SetPositionAndRotation(hardLockTarget.position, hardLockTarget.rotation);
            }
        }
        else
        {
            cinemachineCamera.Priority = isAimMode ? GetActivePriority() : GetInactivePriority();
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

    private int GetActivePriority()
    {
        CameraTransitionConfig config = FCSRegistry.CameraTransitionConfig;
        return config != null ? config.AimActivePriority : 15;
    }

    private int GetInactivePriority()
    {
        CameraTransitionConfig config = FCSRegistry.CameraTransitionConfig;
        return config != null ? config.AimInactivePriority : 0;
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
