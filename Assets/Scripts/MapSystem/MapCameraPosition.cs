using Unity.Cinemachine;
using UnityEngine;

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
    private Transform _followTarget;

    public bool HasCameraReference => _cinemachineCamera != null || _mapCamera != null;
    public string CameraName => _cinemachineCamera != null ? _cinemachineCamera.Name : (_mapCamera != null ? _mapCamera.name : string.Empty);
    public Camera MapCamera => _mapCamera;
    public float OrthoSize => _orthoSize;

    private void Awake()
    {
        ResolveCameraReference();
        ResolveMapCamera();
        SetupOrthoCamera();
        SetInactive();
    }

    private void LateUpdate()
    {
        SyncPhysicalCameraToTarget();
    }

    private void ResolveCameraReference()
    {
        _cinemachineCamera = GetComponent<CinemachineCamera>();
        if (_cinemachineCamera == null)
        {
            _cinemachineCamera = GetComponentInChildren<CinemachineCamera>(true);
        }
    }

    private void ResolveMapCamera()
    {
        if (_mapCamera != null)
        {
            return;
        }

        _mapCamera = GetComponent<Camera>();
        if (_mapCamera == null)
        {
            _mapCamera = GetComponentInChildren<Camera>(true);
        }
    }

    private void SetupOrthoCamera()
    {
        if (_cinemachineCamera != null)
        {
            _cinemachineCamera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            _cinemachineCamera.Lens.OrthographicSize = _orthoSize;
            _cinemachineCamera.Target.TrackingTarget = null;
        }

        if (_mapCamera != null)
        {
            _mapCamera.orthographic = true;
            _mapCamera.orthographicSize = _orthoSize;
        }
    }

    public void BindTarget(Transform followTarget)
    {
        if (followTarget == null)
        {
            return;
        }

        _followTarget = followTarget;

        if (_cinemachineCamera != null)
        {
            _cinemachineCamera.Follow = followTarget;
            _cinemachineCamera.LookAt = null;
        }

        SyncPhysicalCameraToTarget();
    }

    public void SetTargetTexture(RenderTexture rt)
    {
        if (_mapCamera != null)
        {
            _mapCamera.targetTexture = rt;
        }
    }

    public void SetActive()
    {
        if (_cinemachineCamera != null)
        {
            _cinemachineCamera.Priority = _activePriority;
        }
    }

    public void SetInactive()
    {
        if (_cinemachineCamera != null)
        {
            _cinemachineCamera.Priority = _inactivePriority;
        }
    }

    private void SyncPhysicalCameraToTarget()
    {
        if (_mapCamera == null || _followTarget == null)
        {
            return;
        }

        Vector3 followPosition = _followTarget.position;
        Transform cameraTransform = _mapCamera.transform;
        cameraTransform.position = new Vector3(followPosition.x, followPosition.y + _overheadHeight, followPosition.z);
        cameraTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
        _mapCamera.orthographic = true;
        _mapCamera.orthographicSize = _orthoSize;
    }
}
