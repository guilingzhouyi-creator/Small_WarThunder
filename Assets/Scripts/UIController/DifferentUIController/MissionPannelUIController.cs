using UnityEngine;
using TMPro;
using SmallWar.Data;

public class MissionPannelUIController : MonoBehaviour
{
    // [SerializeField] private TextMeshProUGUI MissionTextMeshLab;
    [SerializeField] private MissionRegistrySystem registrysystem;
    private SubtitlePackage _pendingNarrative;

    // public MissionCategory currentCat = MissionCategory.Frontline;
    // public int currentSubID = 201;

    private void OnEnable()
    {
        if (GlobalSubtitleEngine.Instance != null
            && GlobalSubtitleEngine.Instance.HasActivePackage
            && !GlobalSubtitleEngine.Instance.IsPlaying)
        {
            GlobalSubtitleEngine.Instance.ResumePlayback();
            return;
        }

        if (_pendingNarrative == null)
        {
            _pendingNarrative = CreateLevelStartNarrative();
        }

        TryPlayPendingNarrative();
    }

    private void OnDisable()
    {
        if (GlobalSubtitleEngine.Instance != null)
        {
            GlobalSubtitleEngine.Instance.PausePlayback();
        }
    }

    public void TriggerLevelStartNarrative()
    {
        // 先缓存开场叙事，等任务面板真正显示时再播放。
        _pendingNarrative = CreateLevelStartNarrative();

        TryPlayPendingNarrative();
    }

    private SubtitlePackage CreateLevelStartNarrative()
    {
        if (registrysystem == null)
        {
            return null;
        }

        return registrysystem.GetPackageSequence
        (
            MissionCategory.Training, 100, 105, SubtitleChannel.Dialogue
        );
    }

    private void TryPlayPendingNarrative()
    {
        if (!isActiveAndEnabled || _pendingNarrative == null)
        {
            return;
        }

        if (GlobalSubtitleEngine.Instance == null)
        {
            return;
        }

        GlobalSubtitleEngine.Instance.ResetPlayback();
        GlobalSubtitleEngine.Instance.RequestSubtitle(_pendingNarrative);
        _pendingNarrative = null;
    }


    // private void Awake()
    // {
    //     if (registrysystem != null) registrysystem.Initialize();

    // }

    // private void OnEnable()
    // {
    //     var data = registrysystem.Get(currentCat, currentSubID);

    //     if (data != null)
    //     {
    //         // 如果是加密数据，此处应调用 Decrypt 方法

    //         SubtitleEngine.Render(MissionTextMeshLab, data.content);
    //     }
    //     else
    //     {
    //         Debug.LogWarning($"[MissionSystem] 找不到任务数据: {currentCat} - {currentSubID}");
    //     }
    // }
}