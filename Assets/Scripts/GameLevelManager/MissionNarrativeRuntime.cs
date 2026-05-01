public static class MissionNarrativeRuntime
{
    private static GameLevelManager _activeOwner;
    private static string _activeRegionId;
    private static SubtitlePackage _currentPackage;

    public static SubtitlePackage CurrentPackage => _currentPackage;
    public static string ActiveRegionId => _activeRegionId;
    public static bool HasCurrentPackage => _currentPackage != null;

    public static void PublishNarrative(GameLevelManager owner, string regionId, SubtitlePackage package)
    {
        if (owner == null || package == null)
        {
            return;
        }

        _activeOwner = owner;
        _activeRegionId = string.IsNullOrWhiteSpace(regionId) ? owner.gameObject.name : regionId;
        _currentPackage = package;

        MissionPannelUIController.Instance?.PresentNarrative(package);
    }

    public static void RebindMissionPanel()
    {
        if (_currentPackage == null)
        {
            return;
        }

        MissionPannelUIController.Instance?.PresentNarrative(_currentPackage);
    }

    public static void DetachOwner(GameLevelManager owner)
    {
        if (_activeOwner != owner)
        {
            return;
        }

        _activeOwner = null;
    }

    public static void StopNarrative(GameLevelManager owner, bool clearPlayback = true)
    {
        if (_activeOwner != owner)
        {
            return;
        }

        _activeOwner = null;
        _activeRegionId = null;
        _currentPackage = null;
        MissionPannelUIController.Instance?.ClearNarrative();

        if (clearPlayback)
        {
            GlobalSubtitleEngine.Instance?.ResetPlayback();
        }
    }

    public static void ResetAll(bool clearPlayback = true)
    {
        _activeOwner = null;
        _activeRegionId = null;
        _currentPackage = null;

        if (clearPlayback)
        {
            GlobalSubtitleEngine.Instance?.ResetPlayback();
        }
    }
}