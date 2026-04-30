using UnityEngine;
using TMPro;
using SmallWar.Data;

public class MissionPannelUIController : MonoBehaviour
{
    // [SerializeField] private TextMeshProUGUI MissionTextMeshLab;
    [SerializeField] private MissionRegistrySystem registrysystem;

    // public MissionCategory currentCat = MissionCategory.Frontline;
    // public int currentSubID = 201;

    public void TriggerLevelStartNarrative()
    {
        // 从中央注册表提取 101 到 105 的任务序列，设为 Dialogue 优先级
        SubtitlePackage narrative = registrysystem.GetPackageSequence
        (
            MissionCategory.Training, 101, 105, SubtitleChannel.Dialogue
        );

        if (narrative != null)
        {
            GlobalSubtitleEngine.Instance.RequestSubtitle(narrative);
        }
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