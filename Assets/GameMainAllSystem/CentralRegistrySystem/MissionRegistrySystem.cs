using System.Collections.Generic;
using UnityEngine;
using SmallWar.Data;

//资产配置的栏数据显示（Scriptable Objects//SCRIPTNAME//是可该目录部分）
[CreateAssetMenu(fileName = "MissionRegistrySystem", menuName = "MissionSystem/MissionRegistrySystem")]
public class MissionRegistrySystem : ScriptableObject
{
    public List<MissionEntryAsset> allMissions;

    private Dictionary<MissionKey, MissionEntryAsset> _lookup;

    public void Initialize()
    {
        _lookup = new Dictionary<MissionKey, MissionEntryAsset>();

        // 将所有任务数据资产加载到字典中，使用 MissionCategory 作为键，以便快速检索
        foreach (var asset in allMissions)
        {

            if (asset != null)
            {
                // 构建复合键：大类 + 子ID
                MissionKey key = new MissionKey { category = asset.category, subID = asset.subID };
                if (!_lookup.ContainsKey(key))
                    _lookup.Add(key, asset);
            }
        }
    }

    public SubtitlePackage GetPackageSequence(MissionCategory cat, int startID, int endID, SubtitleChannel channel)
    {
        if (_lookup == null) Initialize(); // 确保字典已初始化

        List<string> contents = new List<string>();

        for (int i = startID; i <= endID; i++)
        {
            var asset = Get(cat, i);

            if (asset != null) contents.Add(asset.content);
            else Debug.LogWarning($"[MissionSystem] 找不到任务数据: {cat} - {i}");
        }

        // 如果找到了至少一个有效的任务文本，返回一个新的 SubtitlePackage；否则返回 null
        return contents.Count > 0 ? new SubtitlePackage(channel, contents) : null;
    }



    /// <summary>
    /// 根据任务类别和子ID检索对应的任务文本数据资产，返回一个轻量化的 MissionKey 结构体作为键值
    /// </summary>
    public MissionEntryAsset Get(MissionCategory cat, int id)
    {

        if (_lookup == null) Initialize(); // 确保字典已初始化



        var searchKey = new MissionKey { category = cat, subID = id };


        return _lookup.TryGetValue(searchKey, out MissionEntryAsset asset) ? asset : null;
    }
}