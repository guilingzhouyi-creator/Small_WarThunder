using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 指示系统中央注册表：ScriptableObject 配置表，存储所有指示类型的视觉属性。
/// 提供通过类型快速查询对应配置的能力。
/// </summary>
[CreateAssetMenu(fileName = "IndicatorCentralRegistry", menuName = "SmallWarThunder/UI/指示器/中央注册表")]
public class IndicatorCentralRegistry : ScriptableObject
{
    [Serializable]
    public struct TypePropertiesEntry
    {
        [Tooltip("指示类型")]
        public EIndicatorType indicatorType;

        [Tooltip("该类型的视觉属性")]
        public IndicatorProperties properties;
    }

    [Header("类型配置列表")]
    [SerializeField]
    private List<TypePropertiesEntry> _typePropertiesList = new List<TypePropertiesEntry>();

    private Dictionary<EIndicatorType, IndicatorProperties> _lookup;

    /// <summary>
    /// 根据指示类型获取配置属性。若未找到则返回默认属性。
    /// </summary>
    public IndicatorProperties GetProperties(EIndicatorType type)
    {
        if (_lookup == null)
        {
            BuildLookup();
        }

        if (_lookup.TryGetValue(type, out IndicatorProperties props))
        {
            return props;
        }

        Debug.LogWarning($"[IndicatorCentralRegistry] 未找到类型 {type} 的配置，使用默认属性");
        return DefaultProperties(type);
    }

    /// <summary>
    /// 构建字典索引，加速查询。
    /// </summary>
    private void BuildLookup()
    {
        _lookup = new Dictionary<EIndicatorType, IndicatorProperties>();
        foreach (var entry in _typePropertiesList)
        {
            if (!_lookup.ContainsKey(entry.indicatorType))
            {
                _lookup[entry.indicatorType] = entry.properties;
            }
        }
    }

    /// <summary>
    /// 当配置缺失时，提供兜底默认属性。
    /// </summary>
    private IndicatorProperties DefaultProperties(EIndicatorType type)
    {
        Color color = type switch
        {
            EIndicatorType.TaskTarget => Color.yellow,
            EIndicatorType.MoveGuide => Color.green,
            EIndicatorType.AttackGuide => Color.red,
            EIndicatorType.Tutorial => Color.cyan,
            _ => Color.white,
        };

        return new IndicatorProperties
        {
            mainColor = color,
            edgeColor = Color.black,
            iconSize = 32f,
            arrowSize = 24f,
            iconSprite = null,
            showDistance = true,
            defaultPriority = 0,
            defaultDuration = 0f,
        };
    }

    private void OnValidate()
    {
        _lookup = null;
    }

    private void OnEnable()
    {
        _lookup = null;
    }
}
