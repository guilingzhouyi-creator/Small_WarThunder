using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    public static CameraSystem Instance { get; private set; }

    [SerializeField] private CameraPosition _cameraPosition;
    [SerializeField] private ZoomCameraPosition _zoomCameraPosition;
    [SerializeField] private AimCameraPosition _aimCameraPosition;
    [SerializeField] private MapCameraPosition _mapCameraPosition;

    private CinemachineBlenderSettings _aimCutBlends;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void BindToPlayer(Transform playerRoot = null)
    {
        if (playerRoot == null)
        {
            var playerTank = PlayerSpawnSystem.Instance?.PlayerTank;
            if (playerTank == null)
            {
                Debug.LogWarning("[CameraSystem] Cannot bind cameras because PlayerTank is missing.");
                return;
            }

            playerRoot = playerTank.transform;
        }

        if (playerRoot == null)
        {
            Debug.LogWarning("[CameraSystem] Cannot bind cameras because playerRoot is null.");
            return;
        }

        BindCameraTargets(playerRoot);
    }

    private void BindCameraTargets(Transform tankRoot)
    {
        List<string> bindingSummaries = new List<string>();
        List<string> issues = new List<string>();

        TankCameraBindMarker[] markers = GetCameraBindMarkers(tankRoot);
        if (markers == null || markers.Length == 0)
        {
            issues.Add($"No TankCameraBindMarker found under {tankRoot.name}, fallback to tank root.");
        }

        Transform thirdPersonFollow = GetCameraTargetByRole(markers, TankCameraBindMarker.BindRole.ThirdPersonFollow, tankRoot);
        Transform thirdPersonLookAt = GetCameraTargetByRole(markers, TankCameraBindMarker.BindRole.ThirdPersonLookAt, thirdPersonFollow);

        Transform zoomFollow = GetCameraTargetByRole(markers, TankCameraBindMarker.BindRole.ZoomFollow, tankRoot);
        Transform zoomLookAt = GetCameraTargetByRole(markers, TankCameraBindMarker.BindRole.ZoomLookAt, zoomFollow);

        Transform aimFollow = GetCameraTargetByRole(markers, TankCameraBindMarker.BindRole.Aimfollow, tankRoot);
        Transform aimLookAt = GetCameraTargetByRole(markers, TankCameraBindMarker.BindRole.AimLookAt, aimFollow);

        Transform mapOverheadTarget = GetCameraTargetByRole(markers, TankCameraBindMarker.BindRole.MapOverhead, tankRoot);

        CameraPosition resolvedCameraPosition = ResolveCameraPositionController();
        if (resolvedCameraPosition != null)
        {
            resolvedCameraPosition.BindTarget(thirdPersonFollow, thirdPersonLookAt);
            bindingSummaries.Add(BuildCameraBindingSummary("CameraPosition", thirdPersonFollow, thirdPersonLookAt));
        }
        else
        {
            issues.Add("CameraPosition controller not found.");
        }

        ZoomCameraPosition resolvedZoomCameraPosition = ResolveZoomCameraPositionController();
        if (resolvedZoomCameraPosition != null)
        {
            resolvedZoomCameraPosition.BindTarget(zoomFollow, zoomLookAt);
            resolvedZoomCameraPosition.BindAimTarget(aimFollow, aimLookAt);
            bindingSummaries.Add(BuildCameraBindingSummary("ZoomCameraPosition", zoomFollow, zoomLookAt));
            bindingSummaries.Add(BuildCameraBindingSummary("ZoomCameraPosition(Aim)", aimFollow, aimLookAt));
        }
        else
        {
            issues.Add("ZoomCameraPosition controller not found.");
        }

        AimCameraPosition resolvedAimCameraPosition = ResolveAimCameraPositionController();
        if (resolvedAimCameraPosition != null)
        {
            resolvedAimCameraPosition.BindTarget(aimFollow, aimLookAt);
            bindingSummaries.Add(BuildCameraBindingSummary("AimCameraPosition", aimFollow, aimLookAt));
        }
        else
        {
            issues.Add("AimCameraPosition controller not found.");
        }

        MapCameraPosition resolvedMapCameraPosition = ResolveMapCameraPositionController();
        if (resolvedMapCameraPosition != null)
        {
            resolvedMapCameraPosition.BindTarget(mapOverheadTarget);
            bindingSummaries.Add($"MapCameraPosition: Follow = {(mapOverheadTarget != null ? mapOverheadTarget.name : "null")}");
        }
        else
        {
            issues.Add("MapCameraPosition controller not found.");
        }

        if (bindingSummaries.Count > 0)
        {
            Debug.Log($"[CameraSystem] Camera binding complete: {string.Join(" | ", bindingSummaries)}");
        }

        if (issues.Count > 0)
        {
            Debug.LogWarning($"[CameraSystem] Camera binding fallback/issues: {string.Join(" | ", issues)}");
        }

        ConfigureAimCutBlend();
    }

    private CameraPosition ResolveCameraPositionController()
    {
        if (_cameraPosition != null && _cameraPosition.HasCameraReference)
        {
            return _cameraPosition;
        }

        CameraPosition[] cameraPositions = FindObjectsByType<CameraPosition>(FindObjectsSortMode.None);
        for (int i = 0; i < cameraPositions.Length; i++)
        {
            CameraPosition candidate = cameraPositions[i];
            if (candidate != null && candidate.HasCameraReference)
            {
                _cameraPosition = candidate;
                return candidate;
            }
        }

        return null;
    }

    private ZoomCameraPosition ResolveZoomCameraPositionController()
    {
        if (_zoomCameraPosition != null && _zoomCameraPosition.HasCameraReference)
        {
            return _zoomCameraPosition;
        }

        ZoomCameraPosition[] zoomCameraPositions = FindObjectsByType<ZoomCameraPosition>(FindObjectsSortMode.None);
        for (int i = 0; i < zoomCameraPositions.Length; i++)
        {
            ZoomCameraPosition candidate = zoomCameraPositions[i];
            if (candidate != null && candidate.HasCameraReference)
            {
                _zoomCameraPosition = candidate;
                return candidate;
            }
        }

        return null;
    }

    private AimCameraPosition ResolveAimCameraPositionController()
    {
        if (_aimCameraPosition != null && _aimCameraPosition.HasCameraReference)
        {
            return _aimCameraPosition;
        }

        AimCameraPosition[] aimCameraPositions = FindObjectsByType<AimCameraPosition>(FindObjectsSortMode.None);
        for (int i = 0; i < aimCameraPositions.Length; i++)
        {
            AimCameraPosition candidate = aimCameraPositions[i];
            if (candidate != null && candidate.HasCameraReference)
            {
                _aimCameraPosition = candidate;
                return candidate;
            }
        }

        return null;
    }

    private MapCameraPosition ResolveMapCameraPositionController()
    {
        if (_mapCameraPosition != null && _mapCameraPosition.HasCameraReference)
        {
            return _mapCameraPosition;
        }

        MapCameraPosition[] mapCameraPositions = FindObjectsByType<MapCameraPosition>(FindObjectsSortMode.None);
        for (int i = 0; i < mapCameraPositions.Length; i++)
        {
            MapCameraPosition candidate = mapCameraPositions[i];
            if (candidate != null && candidate.HasCameraReference)
            {
                _mapCameraPosition = candidate;
                return candidate;
            }
        }

        return null;
    }

    private TankCameraBindMarker[] GetCameraBindMarkers(Transform tankRoot)
    {
        if (tankRoot == null)
        {
            return null;
        }

        return tankRoot.GetComponentsInChildren<TankCameraBindMarker>(true);
    }

    private Transform GetCameraTargetByRole(TankCameraBindMarker[] markers, TankCameraBindMarker.BindRole role, Transform fallback)
    {
        if (markers != null)
        {
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i] != null && markers[i].Role == role)
                {
                    return markers[i].transform;
                }
            }
        }

        return fallback;
    }

    private void ConfigureAimCutBlend()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        CinemachineBrain brain = mainCamera.GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            return;
        }

        CameraTransitionConfig config = FCSRegistry.CameraTransitionConfig;
        if (config == null)
        {
            Debug.LogWarning("[CameraSystem] CameraTransitionConfig is null, skip applying custom blends.");
            return;
        }

        if (config.BlendRules == null || config.BlendRules.Length == 0)
        {
            Debug.LogWarning($"[CameraSystem] {config.name} has no blend rules, fallback to CinemachineBrain.DefaultBlend.");
            return;
        }

        if (_aimCutBlends == null)
        {
            _aimCutBlends = ScriptableObject.CreateInstance<CinemachineBlenderSettings>();
        }

        List<CinemachineBlenderSettings.CustomBlend> customBlends = new List<CinemachineBlenderSettings.CustomBlend>();
        List<string> summaries = new List<string>();

        for (int i = 0; i < config.BlendRules.Length; i++)
        {
            AimBlendRule rule = config.BlendRules[i];
            string fromCameraName = ResolveBlendCameraName(rule.FromCamera);
            string toCameraName = ResolveBlendCameraName(rule.ToCamera);

            if (string.IsNullOrEmpty(fromCameraName) || string.IsNullOrEmpty(toCameraName))
            {
                Debug.LogWarning($"[CameraSystem] Skip invalid blend rule: {rule.FromCamera} -> {rule.ToCamera}. Camera binding is missing.", this);
                continue;
            }

            customBlends.Add(new CinemachineBlenderSettings.CustomBlend
            {
                From = fromCameraName,
                To = toCameraName,
                Blend = rule.ToBlendDefinition()
            });

            summaries.Add($"{rule.FromCamera} -> {rule.ToCamera} => {fromCameraName} -> {toCameraName} ({rule.BlendStyle}, {rule.BlendDuration}s)");
        }

        _aimCutBlends.CustomBlends = customBlends.ToArray();
        brain.CustomBlends = _aimCutBlends;

        Debug.Log($"[CameraSystem] Applied camera blend rules: {string.Join(" | ", summaries)}");
    }

    private string ResolveBlendCameraName(CameraBlendTarget target)
    {
        switch (target)
        {
            case CameraBlendTarget.Any:
                return "**ANY CAMERA**";
            case CameraBlendTarget.TPS:
                return _cameraPosition != null ? _cameraPosition.CameraName : string.Empty;
            case CameraBlendTarget.Zoom:
                return _zoomCameraPosition != null ? _zoomCameraPosition.CameraName : string.Empty;
            case CameraBlendTarget.Aim:
                return _aimCameraPosition != null ? _aimCameraPosition.CameraName : string.Empty;
            case CameraBlendTarget.Map:
                return _mapCameraPosition != null ? _mapCameraPosition.CameraName : string.Empty;
            default:
                return string.Empty;
        }
    }

    private string BuildCameraBindingSummary(string controllerName, Transform followTarget, Transform lookAtTarget)
    {
        string followName = followTarget != null ? followTarget.name : "null";
        string lookAtName = lookAtTarget != null ? lookAtTarget.name : "null";
        return $"{controllerName}: Follow = {followName}, LookAt = {lookAtName}";
    }
}
