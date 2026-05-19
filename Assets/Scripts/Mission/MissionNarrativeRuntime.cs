using System.Collections.Generic;

public static class MissionNarrativeRuntime
{
    private static GameLevelManager _activeOwner;
    private static string _activeRegionId;
    private static SubtitlePackage _currentPackage;
    private static readonly HashSet<string> _completedNarrativeKeys = new HashSet<string>();

    public static SubtitlePackage CurrentPackage => _currentPackage;
    public static string ActiveRegionId => _activeRegionId;
    public static bool HasCurrentPackage => _currentPackage != null;

    public static void PublishNarrative(GameLevelManager owner, string regionId, SubtitlePackage package)
    {
        if (owner == null || package == null)
        {
            return;
        }

        string narrativeKey = BuildNarrativeKey(owner, regionId);
        if (package.Channel == SubtitleChannel.Mission && _completedNarrativeKeys.Contains(narrativeKey))
        {
            return;
        }

        _activeOwner = owner;
        _activeRegionId = string.IsNullOrWhiteSpace(regionId) ? owner.gameObject.name : regionId;
        _currentPackage = package;

        if (package.Channel == SubtitleChannel.Mission)
        {
            MissionPannelUIController.Instance?.PresentNarrative(package);
        }
        else
        {
            GlobalSubtitleEngine.Instance?.RequestSubtitle(package);
        }
    }

    public static void RebindMissionPanel()
    {
        if (_currentPackage == null)
        {
            return;
        }

        MissionPannelUIController.Instance?.PresentNarrative(_currentPackage);
    }

    public static bool IsNarrativeCompleted(GameLevelManager owner, string regionId)
    {
        if (owner == null)
        {
            return false;
        }

        return _completedNarrativeKeys.Contains(BuildNarrativeKey(owner, regionId));
    }

    public static void MarkNarrativeCompleted(GameLevelManager owner, string regionId)
    {
        if (owner == null)
        {
            return;
        }

        _completedNarrativeKeys.Add(BuildNarrativeKey(owner, regionId));
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

    private static string BuildNarrativeKey(GameLevelManager owner, string regionId)
    {
        string resolvedRegionId = string.IsNullOrWhiteSpace(regionId) ? owner.gameObject.name : regionId;
        return owner.LevelIndex + ":" + resolvedRegionId;
    }
}
