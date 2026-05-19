using UnityEngine;

/// <summary>
/// 行为策略配置ScriptableObject，定义AI的战斗行为参数
/// </summary>
namespace NGameData.NAIConfigs
{
    [CreateAssetMenu(fileName = "BehaviorConfig", menuName = "SmallWarThunder/AI/配置/行为配置", order = 2)]
    public class BehaviorConfig : ScriptableObject
    {
        [Header("可疑态")]
        [Tooltip("可疑态持续时间（秒）")]
        public float suspiciousDuration = 5f;

        [Header("攻击")]
        [Tooltip("开火冷却时间（秒）")]
        public float attackInterval = 3f;

        [Tooltip("预测精度系数（0-1），越高越精准")]
        [Range(0f, 1f)]
        public float predictionAccuracy = 0.7f;
    }
}
