using UnityEngine;

/// <summary>
/// 轻量级火控注册中心。
/// 载具侧负责每帧上报快照，HUD/准星系统从这里读取统一状态。
/// </summary>
public static class FCSRegistry
{
    private static FCSSnapshot _playerSnapshot;
    private static NewAimConfigData _currentConfig;
    private static CameraTransitionConfig _cameraTransitionConfig;
    private static bool _hasSnapshot;

    public static void RegisterPlayerFCS(FCSSnapshot snapshot, NewAimConfigData config)
    {
        _playerSnapshot = snapshot;
        _currentConfig = config;
        _hasSnapshot = true;
    }

    public static void RegisterCameraTransitionConfig(CameraTransitionConfig config)
    {
        _cameraTransitionConfig = config;
    }

    public static bool TryGetPlayerState(out FCSSnapshot snapshot, out NewAimConfigData config)
    {
        snapshot = _playerSnapshot;
        config = _currentConfig;
        return _hasSnapshot && config != null;
    }

    public static CameraTransitionConfig CameraTransitionConfig => _cameraTransitionConfig;

    public static bool HasPlayerState => _hasSnapshot;

    public static void ClearPlayerState()
    {
        _playerSnapshot = default;
        _currentConfig = null;
        _cameraTransitionConfig = null;
        _hasSnapshot = false;
    }
}
