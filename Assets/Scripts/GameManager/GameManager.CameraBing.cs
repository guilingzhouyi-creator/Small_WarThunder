using UnityEngine;

public partial class GameManager : MonoBehaviour
{

    private void BindCameraTargets(Transform tankRoot)
    {
        if (tankRoot == null)
        {
            Debug.LogWarning("GameManager: 无法绑定相机目标，tankRoot 为空。");
            return;
        }

        TankCameraBindMarker[] markers = GetCameraBindMarkers(tankRoot);

        if (markers == null || markers.Length == 0)
        {
            Debug.LogWarning($"GameManager: 在 {tankRoot.name} 下没有找到 TankCameraBindMarker，将使用 tankRoot 作为相机回退目标。");
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

            LogCameraBinding("CameraPosition", thirdPersonFollow, thirdPersonLookAt);
        }
        else
        {
            Debug.LogWarning("GameManager: 未找到可绑定的 CameraPosition 控制器。");
        }


        ZoomCameraPosition resolvedZoomCameraPosition = ResolveZoomCameraPositionController();
        if (resolvedZoomCameraPosition != null)
        {
            resolvedZoomCameraPosition.BindTarget(zoomFollow, zoomLookAt);
            resolvedZoomCameraPosition.BindAimTarget(aimFollow, aimLookAt);

            LogCameraBinding("ZoomCameraPosition", zoomFollow, zoomLookAt);
            LogCameraBinding("ZoomCameraPosition(Aim)", aimFollow, aimLookAt);
        }
        else
        {
            Debug.LogWarning("GameManager: 未找到可绑定的 ZoomCameraPosition 控制器。");
        }


        AimCameraPosition resolvedAimCameraPosition = ResolveAimCameraPositionController();
        if (resolvedAimCameraPosition != null)
        {
            resolvedAimCameraPosition.BindTarget(aimFollow, aimLookAt);

            LogCameraBinding("AimCameraPosition", aimFollow, aimLookAt);
        }
        else
        {
            Debug.LogWarning("GameManager: 未找到可绑定的 AimCameraPosition 控制器。");
        }
    }


    private CameraPosition ResolveCameraPositionController()
    {
        if (cameraPosition != null && cameraPosition.HasCameraReference)
        {
            return cameraPosition;
        }

        //
        CameraPosition[] cameraPositions = FindObjectsByType<CameraPosition>(FindObjectsSortMode.None);
        for (int i = 0; i < cameraPositions.Length; i++)
        {
            CameraPosition candidate = cameraPositions[i];
            if (candidate != null && candidate.HasCameraReference)
            {
                cameraPosition = candidate;
                return candidate;
            }
        }

        return null;
    }

    private ZoomCameraPosition ResolveZoomCameraPositionController()
    {
        if (zoomCameraPosition != null && zoomCameraPosition.HasCameraReference)
        {
            return zoomCameraPosition;
        }

        ZoomCameraPosition[] zoomCameraPositions = FindObjectsByType<ZoomCameraPosition>(FindObjectsSortMode.None);

        for (int i = 0; i < zoomCameraPositions.Length; i++)
        {
            ZoomCameraPosition candidate = zoomCameraPositions[i];

            if (candidate != null && candidate.HasCameraReference)
            {
                zoomCameraPosition = candidate;
                return candidate;
            }
        }

        return null;
    }

    private AimCameraPosition ResolveAimCameraPositionController()
    {
        if (aimCameraPosition != null && aimCameraPosition.HasCameraReference)
        {
            return aimCameraPosition;
        }

        AimCameraPosition[] aimCameraPositions = FindObjectsByType<AimCameraPosition>(FindObjectsSortMode.None);

        for (int i = 0; i < aimCameraPositions.Length; i++)
        {
            AimCameraPosition candidate = aimCameraPositions[i];

            if (candidate != null && candidate.HasCameraReference)
            {
                aimCameraPosition = candidate;
                return candidate;
            }
        }

        return null;
    }


    /// <summary>
    /// 解析场景中的 CameraPosition 控制器，优先使用已分配的 cameraPosition 字段，如果无效则搜索场景中所有 CameraPosition 组件并选择第一个有效的
    /// </summary>
    private TankCameraBindMarker[] GetCameraBindMarkers(Transform tankRoot)
    {
        if (tankRoot == null)
        {
            return null;
        }

        return tankRoot.GetComponentsInChildren<TankCameraBindMarker>(true);
    }

    /// <summary>
    /// 根据指定的绑定角色从标记数组中获取相机目标，如果未找到则返回提供的回退目标
    /// </summary>
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



    /// <summary>
    /// 记录相机绑定信息的辅助方法，方便调试和验证相机目标的正确性
    /// </summary>
    private void LogCameraBinding(string controllerName, Transform followTarget, Transform lookAtTarget)
    {
        string followName = followTarget != null ? followTarget.name : "null";
        string lookAtName = lookAtTarget != null ? lookAtTarget.name : "null";
        Debug.Log($"GameManager: {controllerName} 已绑定 Follow = {followName}, LookAt = {lookAtName}");
    }

}
