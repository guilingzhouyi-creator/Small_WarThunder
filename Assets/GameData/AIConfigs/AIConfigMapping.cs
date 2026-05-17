using System;
using UnityEngine;

/// <summary>
/// AI配置映射条目，将Prefab与EnemyConfig、BehaviorConfig及AI类型关联
/// </summary>
namespace NGameData.NAIConfigs
{
    [Serializable]
    public class AIConfigMapping
    {
        [Tooltip("预制体Key")]
        public GameObject prefab;

        [Tooltip("敌人身份配置")]
        public EnemyConfig enemyConfig;

        [Tooltip("行为策略配置")]
        public BehaviorConfig behaviorConfig;

        [Tooltip("物理驱动配置（移动/悬挂参数）")]
        public AIMotionConfig motionConfig;

        [Tooltip("悬挂系统配置（悬挂几何/防翻滚/地形对齐/车身稳定）")]
        public AISuspensionConfig suspensionConfig;

        [Tooltip("AI类型")]
        public EAIType aiType = EAIType.Tank;
    }
}
