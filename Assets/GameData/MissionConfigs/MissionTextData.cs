using UnityEngine;

//资产配置的栏数据显示（Scriptable Objects//SCRIPTNAME//是可该目录部分）
[CreateAssetMenu(fileName = "MissionTextData", menuName = "MissionSystem/MissionTextData")]
public class MissionEntryAsset : ScriptableObject
{
    public SmallWar.Data.MissionCategory category;
    public int subID;
    public string title;

    // 任务文本内容，支持多行输入，编辑器中显示为 TextArea 以方便编辑
    [TextArea(10, 20)] public string content;
}