using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 单位运行时数据结构，维护单个单位的最小状态集。
/// 用于全局单位注册表中的序列化条目。
/// </summary>
[System.Serializable]
public struct GlobalUnitEntry
{
    [Tooltip("全局唯一标识符，格式：{UnitType}-{InstanceID}")]
    public string uid;

    [Tooltip("单位阵营")]
    public EUnitFaction faction;

    [Tooltip("单位类型标识")]
    public string unitType;

    [Tooltip("世界坐标位置")]
    public Vector3 worldPosition;

    [Tooltip("朝向角度（度）")]
    public float yaw;

    [Tooltip("生命状态")]
    public EUnitLifeStatus lifeStatus;

    [Tooltip("当前是否在侦测范围内")]
    public bool isDetected;

    [Tooltip("当前是否处于高亮状态")]
    public bool isHighlighted;

    [Tooltip("最后一次被侦测的时间（GameTime.time）")]
    public float lastDetectedTime;

    [Tooltip("跟踪状态")]
    public EUnitTrackStatus trackStatus;

    /// <summary>
    /// 创建默认条目。
    /// </summary>
    public static GlobalUnitEntry CreateDefault(string uid, EUnitFaction faction, string unitType, Vector3 position, float yaw)
    {
        return new GlobalUnitEntry
        {
            uid = uid,
            faction = faction,
            unitType = unitType,
            worldPosition = position,
            yaw = yaw,
            lifeStatus = EUnitLifeStatus.Alive,
            isDetected = false,
            isHighlighted = false,
            lastDetectedTime = 0f,
            trackStatus = EUnitTrackStatus.NotTracked
        };
    }
}

/// <summary>
/// 全局单位注册表 ScriptableObject。
/// 作为全局单位唯一真源，维护所有单位的 UID、阵营、类型、位置、生命状态等基础信息。
/// 允许侦测系统、敌方高亮系统、地图系统、任务系统只读查询。
/// 创建路径：右键 → SmallWarThunder → GlobalUnitTracking → Registry
/// </summary>
[CreateAssetMenu(menuName = "SmallWarThunder/GlobalUnitTracking/单位注册表")]
public class GlobalUnitRegistrySO : ScriptableObject
{
    [Header("注册表元数据")]
    [Tooltip("注册表版本号，用于数据迁移和兼容性检查。")]
    public string registryVersion = "1.0.0";

    [Tooltip("运行时注册表最大容量。超过则禁止新单位注册并输出警告。")]
    [Range(10, 1000)]
    public int maxRegistryCapacity = 500;

    [Header("默认显示规则")]
    [Tooltip("友军单位默认是否在大地图上显示。")]
    public bool allyDefaultShowOnFullMap = true;

    [Tooltip("友军单位默认是否在小地图上显示。")]
    public bool allyDefaultShowOnMiniMap = false;

    [Tooltip("敌方单位默认是否可被高亮。")]
    public bool enemyDefaultHighlightable = true;

    [Tooltip("中立单位默认是否可被跟踪。")]
    public bool neutralDefaultTrackable = false;

    [Header("初始化数据（可选）")]
    [Tooltip("预注册单位列表，用于场景加载时批量注册静态单位。")]
    public List<GlobalUnitEntry> preRegisteredUnits = new List<GlobalUnitEntry>();
}
