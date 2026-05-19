using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI中央配置ScriptableObject，持有全局默认配置及按Prefab映射的配置列表
/// </summary>
namespace NGameData.NAIConfigs
{
    [CreateAssetMenu(fileName = "AICentralConfig", menuName = "SmallWarThunder/AI/总配置/AI中央配置", order = 0)]
    public class AICentralConfig : ScriptableObject
    {
        [Header("全局默认配置")]
        [Tooltip("默认敌人身份配置，未在映射列表中找到时使用")]
        public EnemyConfig defaultEnemyConfig;

        [Tooltip("默认行为策略配置，未在映射列表中找到时使用")]
        public BehaviorConfig defaultBehaviorConfig;

        [Tooltip("默认物理驱动配置（移动/悬挂），未在映射列表中找到时使用")]
        public AIMotionConfig defaultMotionConfig;

        [Tooltip("默认悬挂系统配置，未在映射列表中找到时使用")]
        public AISuspensionConfig defaultSuspensionConfig;

        [Header("映射列表")]
        [Tooltip("按Prefab匹配的配置列表，优先于默认配置")]
        public List<AIConfigMapping> configMappingList = new List<AIConfigMapping>();
    }
}
