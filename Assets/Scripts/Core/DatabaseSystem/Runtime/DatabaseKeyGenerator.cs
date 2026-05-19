using System;
using System.Text;
using UnityEngine;

/// <summary>
/// 数据库键生成器，负责构建和解析持久化记录的统一键。
/// 键格式：{实体类型}-{实体ID}-{状态类型}
/// 确保保存系统和重载系统使用同一套键规则对齐数据。
/// </summary>
public static class DatabaseKeyGenerator
{
    /// <summary>生成记录键</summary>
    public static string GenerateKey(string entityType, string entityId, string stateType)
    {
        if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(entityId) || string.IsNullOrEmpty(stateType))
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} GenerateKey failed: null or empty parameter. entityType={entityType}, entityId={entityId}, stateType={stateType}");
            return string.Empty;
        }
        return string.Format(DatabaseConstants.RecordKeyFormat, entityType, entityId, stateType);
    }

    /// <summary>从记录键解析出实体类型</summary>
    public static string ParseEntityType(string recordKey)
    {
        var parts = SplitKey(recordKey);
        return parts.Length >= 3 ? parts[0] : string.Empty;
    }

    /// <summary>从记录键解析出实体ID</summary>
    public static string ParseEntityId(string recordKey)
    {
        var parts = SplitKey(recordKey);
        return parts.Length >= 3 ? parts[1] : string.Empty;
    }

    /// <summary>从记录键解析出状态类型</summary>
    public static string ParseStateType(string recordKey)
    {
        var parts = SplitKey(recordKey);
        return parts.Length >= 3 ? parts[2] : string.Empty;
    }

    /// <summary>验证记录键格式是否合法</summary>
    public static bool IsValidKey(string recordKey)
    {
        if (string.IsNullOrEmpty(recordKey)) return false;
        var parts = recordKey.Split(new[] { DatabaseConstants.RecordKeySeparator }, StringSplitOptions.None);
        return parts.Length == 3 &&
               !string.IsNullOrEmpty(parts[0]) &&
               !string.IsNullOrEmpty(parts[1]) &&
               !string.IsNullOrEmpty(parts[2]);
    }

    /// <summary>生成批次ID</summary>
    public static string GenerateBatchId()
    {
        return DatabaseConstants.BatchIdPrefix + DateTime.UtcNow.Ticks.ToString("X");
    }

    private static string[] SplitKey(string recordKey)
    {
        if (string.IsNullOrEmpty(recordKey)) return new string[0];
        return recordKey.Split(new[] { DatabaseConstants.RecordKeySeparator }, StringSplitOptions.None);
    }
}
