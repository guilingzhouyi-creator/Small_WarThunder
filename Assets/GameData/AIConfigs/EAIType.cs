using UnityEngine;

/// <summary>
/// AI类型枚举，定义不同的AI单位类型
/// </summary>
namespace NGameData.NAIConfigs
{
    public enum EAIType
    {
        /// <summary>坦克类型AI</summary>
        Tank = 0,

        /// <summary>炮塔/固定武器类型AI</summary>
        Turret = 1,

        /// <summary>其他类型AI</summary>
        Other = 2
    }
}
