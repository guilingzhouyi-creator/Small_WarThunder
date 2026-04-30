using UnityEngine;



/// <summary>
/// 火控系统核心渲染工具。
/// 负责将世界坐标转换为屏幕坐标，并计算物理弹道
/// 不具备画面绘制功能，仅提供数据转换和计算接口，供 HUD/准星系统调用。
/// </summary>
public static class FCSRenderingEngine
{
    public static Vector2 WorldToScreen(FCSSnapshot state, Vector3 worldPos)
    {
        Vector4 clipSpacePos = state.ProjectionMatrix * state.ViewMatrix * new Vector4(worldPos.x, worldPos.y, worldPos.z, 1f);

        if (clipSpacePos.w < 0.001f)
        {
            return new Vector2(-10000f, -10000f);
        }

        Vector3 ndc = new Vector3(clipSpacePos.x, clipSpacePos.y, clipSpacePos.z) / clipSpacePos.w;
        return new Vector2(
            (ndc.x + 1f) * 0.5f * state.ScreenWidth,
            (ndc.y + 1f) * 0.5f * state.ScreenHeight);
    }

    public static Vector2 ScreenToUIToolkit(Vector2 screenPos, float screenHeight)
    {
        return new Vector2(screenPos.x, screenHeight - screenPos.y);
    }

    public static Vector2 WorldToUIToolkit(FCSSnapshot state, Vector3 worldPos)
    {
        return ScreenToUIToolkit(WorldToScreen(state, worldPos), state.ScreenHeight);
    }

    public static Vector2 ProjectPhysicalImpactScreen(FCSSnapshot snapshot, NewAimConfigData config)
    {
        Vector3 impact = ResolvePhysicalImpact(snapshot, config);
        return WorldToScreen(snapshot, impact);
    }

    public static Vector2 ProjectPhysicalImpact(FCSSnapshot snapshot, NewAimConfigData config)
    {
        return ScreenToUIToolkit(ProjectPhysicalImpactScreen(snapshot, config), snapshot.ScreenHeight);
    }

    public static Vector2 CalculateTpsOffset(Vector2 currentPos, Vector2 mouseDelta, float sensitivity, float zoomModifier, Vector2 center, Vector2 maxOffset)
    {
        Vector2 target = currentPos + mouseDelta * sensitivity * zoomModifier;
        target.x = Mathf.Clamp(target.x, center.x - maxOffset.x, center.x + maxOffset.x);
        target.y = Mathf.Clamp(target.y, center.y - maxOffset.y, center.y + maxOffset.y);
        return target;
    }

    public static Vector3 ResolvePhysicalImpact(FCSSnapshot snapshot, NewAimConfigData config)
    {
        if (config != null && Physics.Raycast(snapshot.MuzzlePos, snapshot.BarrelForward, out RaycastHit hit, config.MaxDetectionRange, config.AimLayerMask))
        {
            return hit.point;
        }

        float maxDistance = config != null ? config.MaxDetectionRange : 2000f;
        return snapshot.MuzzlePos + snapshot.BarrelForward * maxDistance;
    }
}