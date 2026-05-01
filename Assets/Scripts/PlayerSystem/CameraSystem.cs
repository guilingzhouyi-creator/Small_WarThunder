using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 摄像机绑定系统（执行层）。
/// 负责将场景中的 CameraPosition/ZoomCameraPosition/AimCameraPosition 绑定到玩家坦克的标记点。
/// 由 GameManager（总控层）在场景加载后调用 BindToPlayer()。
/// </summary>
public class CameraSystem : MonoBehaviour
{
    public static CameraSystem Instance { get; private set; }

    [SerializeField] private CameraPosition _cameraPosition;
    [SerializeField] private ZoomCameraPosition _zoomCameraPosition;
    [SerializeField] private AimCameraPosition _aimCameraPosition;

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

    /// <summary>
    /// 绑定场景中的摄像机控制器到玩家坦克的相机标记点。
    /// 如果 playerRoot 为 null，会自动从 PlayerSpawnSystem 获取当前玩家坦克。
    /// </summary>
    public void BindToPlayer(Transform playerRoot = null)
    {
        if (playerRoot == null)
        {
            var playerTank = PlayerSpawnSystem.Instance?.PlayerTank;
            if (playerTank == null)
            {
                Debug.LogWarning("[CameraSystem] 无法绑定摄像机：玩家坦克不存在。");
                return;
            }

            playerRoot = playerTank.transform;
        }

        if (playerRoot == null)
        {
            Debug.LogWarning("[CameraSystem] 无法绑定摄像机：playerRoot 为空。");
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
            issues.Add($"在 {tankRoot.name} 下没有找到 TankCameraBindMarker，将使用 tankRoot 作为回退目标");
        }

        Transform thirdPersonFollow = GetCameraTargetByRole(markers, TankCameraBindMarker.BindRole.ThirdPersonFollow, tankRoot);
        Transform thirdPersonLookAt = GetCameraTargetByRole(markers, TankCameraBindMarker.BindRole.ThirdPersonLookAt, thirdPersonFollow);

        Transform zoomFollow = GetCameraTargetByRole(markers, TankCameraBindMarker.BindRole.ZoomFollow, tankRoot);
        Transform zoomLookAt = GetCameraTargetByRole(markers, TankCameraBindMarker.BindRole.ZoomLookAt, zoomFollow);

        Transform aimFollow = GetCameraTargetByRole(markers, TankCameraBindMarker.BindRole.Aimfollow, tankRoot);
        Transform aimLookAt = GetCameraTargetByRole(markers, TankCameraBindMarker.BindRole.AimLookAt, aimFollow);

        CameraPosition resolvedCameraPosition = ResolveCameraPositionController();
        if (resolvedCameraPosition != null)
        {
            resolvedCameraPosition.BindTarget(thirdPersonFollow, thirdPersonLookAt);
            bindingSummaries.Add(BuildCameraBindingSummary("CameraPosition", thirdPersonFollow, thirdPersonLookAt));
        }
        else
        {
            issues.Add("未找到可绑定的 CameraPosition 控制器");
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
            issues.Add("未找到可绑定的 ZoomCameraPosition 控制器");
        }

        AimCameraPosition resolvedAimCameraPosition = ResolveAimCameraPositionController();
        if (resolvedAimCameraPosition != null)
        {
            resolvedAimCameraPosition.BindTarget(aimFollow, aimLookAt);
            bindingSummaries.Add(BuildCameraBindingSummary("AimCameraPosition", aimFollow, aimLookAt));
        }
        else
        {
            issues.Add("未找到可绑定的 AimCameraPosition 控制器");
        }

        if (bindingSummaries.Count > 0)
        {
            Debug.Log($"[CameraSystem] 相机绑定完成：{string.Join(" | ", bindingSummaries)}");
        }

        if (issues.Count > 0)
        {
            Debug.LogWarning($"[CameraSystem] 相机绑定存在回退或缺失：{string.Join(" | ", issues)}");
        }
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

    private string BuildCameraBindingSummary(string controllerName, Transform followTarget, Transform lookAtTarget)
    {
        string followName = followTarget != null ? followTarget.name : "null";
        string lookAtName = lookAtTarget != null ? lookAtTarget.name : "null";
        return $"{controllerName}: Follow = {followName}, LookAt = {lookAtName}";
    }
}
