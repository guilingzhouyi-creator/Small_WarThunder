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

        [Tooltip("AI类型")]
        public EAIType aiType = EAIType.Tank;
    }
}
