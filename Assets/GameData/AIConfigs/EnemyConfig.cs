using UnityEngine;

/// <summary>
/// 敌人身份配置ScriptableObject，定义单一敌人类的属性
/// </summary>
namespace NGameData.NAIConfigs
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "SmallWarThunder/AI/配置/敌方配置", order = 1)]
    public class EnemyConfig : ScriptableObject
    {
        [Header("感知")]
        [Tooltip("基础感知距离（米），实际有效检测范围为1.15倍此值")]
        public float maxDetectionRange = 150f;

        [Tooltip("进入可疑态的感知概率阈值（0.0-1.0）")]
        [Range(0f, 1f)]
        public float awarenessThreshold = 0.35f;

        [Header("战斗")]
        [Tooltip("实际打击距离（米）")]
        public float attackRange = 120f;

        [Header("属性")]
        [Tooltip("最大生命值")]
        public float maxHealth = 1000f;

        [Tooltip("移动速度（米/秒）")]
        public float moveSpeed = 8f;

        [Tooltip("转向速度（度/秒）")]
        public float turnSpeed = 30f;
    }
}
