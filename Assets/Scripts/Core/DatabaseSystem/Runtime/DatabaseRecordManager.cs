using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 数据库记录管理器，负责记录项的创建、更新、查找和文件落地。
/// 是持久化记录构建层的核心运行时组件。
/// </summary>
public class DatabaseRecordManager
{
    private const string RECORD_FILE_EXTENSION = ".json";
    private const string RECORD_DIRECTORY = "DatabaseRecords";

    private readonly Dictionary<string, DatabaseRecordSO> _recordCache = new Dictionary<string, DatabaseRecordSO>();
    private string _currentBatchId;

    /// <summary>初始化记录管理器</summary>
    public DatabaseRecordManager()
    {
        DatabaseFileStore.EnsureDirectory(RECORD_DIRECTORY);
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseRecordManager initialized.");
    }

    /// <summary>设置当前批次ID</summary>
    public void SetBatchId(string batchId)
    {
        _currentBatchId = batchId;
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseRecordManager.SetBatchId: {batchId}");
    }

    /// <summary>创建一条新的持久化记录</summary>
    public DatabaseRecordSO CreateRecord(string entityType, string entityId, string stateType, string version, string payload)
    {
        if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(entityId) || string.IsNullOrEmpty(stateType))
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseRecordManager.CreateRecord failed: invalid key parameters.");
            return null;
        }
        string recordKey = DatabaseKeyGenerator.GenerateKey(entityType, entityId, stateType);
        if (string.IsNullOrEmpty(recordKey))
        {
            return null;
        }
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string payloadHash = DatabaseSerializer.ComputeHash(payload ?? string.Empty);
        var record = ScriptableObject.CreateInstance<DatabaseRecordSO>();
        record.recordKey = recordKey;
        record.entityType = entityType;
        record.recordVersion = version ?? DatabaseConstants.DefaultVersion;
        record.batchId = _currentBatchId;
        record.createTimestamp = now;
        record.modifyTimestamp = now;
        record.payload = payload ?? string.Empty;
        record.payloadHash = payloadHash;
        record.status = EDatabaseRecordStatus.Active;
        _recordCache[recordKey] = record;
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseRecordManager.CreateRecord: {recordKey}, batch={_currentBatchId}");
        return record;
    }

    /// <summary>更新已存在的记录负载</summary>
    public bool UpdateRecordPayload(string entityType, string entityId, string stateType, string payload)
    {
        string recordKey = DatabaseKeyGenerator.GenerateKey(entityType, entityId, stateType);
        if (!_recordCache.TryGetValue(recordKey, out DatabaseRecordSO record))
        {
            Debug.LogWarning($"{DatabaseConstants.LogPrefix} DatabaseRecordManager.UpdateRecordPayload: record not found: {recordKey}");
            return false;
        }
        record.payload = payload ?? string.Empty;
        record.payloadHash = DatabaseSerializer.ComputeHash(record.payload);
        record.modifyTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        record.status = EDatabaseRecordStatus.Dirty;
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseRecordManager.UpdateRecordPayload: {recordKey}");
        return true;
    }

    /// <summary>通过键获取记录</summary>
    public DatabaseRecordSO GetRecord(string entityType, string entityId, string stateType)
    {
        string recordKey = DatabaseKeyGenerator.GenerateKey(entityType, entityId, stateType);
        _recordCache.TryGetValue(recordKey, out DatabaseRecordSO record);
        return record;
    }

    /// <summary>获取当前批次所有记录</summary>
    public IReadOnlyList<DatabaseRecordSO> GetBatchRecords(string batchId)
    {
        var result = new List<DatabaseRecordSO>();
        foreach (var kvp in _recordCache)
        {
            if (kvp.Value.batchId == batchId)
            {
                result.Add(kvp.Value);
            }
        }
        return result;
    }

    /// <summary>获取当前批次所有记录键</summary>
    public List<string> GetBatchRecordKeys(string batchId)
    {
        var keys = new List<string>();
        foreach (var kvp in _recordCache)
        {
            if (kvp.Value.batchId == batchId)
            {
                keys.Add(kvp.Key);
            }
        }
        return keys;
    }

    /// <summary>将单条记录写入文件系统</summary>
    public bool WriteRecordToFile(DatabaseRecordSO record)
    {
        if (record == null || string.IsNullOrEmpty(record.recordKey))
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseRecordManager.WriteRecordToFile failed: invalid record.");
            return false;
        }
        string json = JsonUtility.ToJson(record);
        string fileName = record.recordKey + RECORD_FILE_EXTENSION;
        bool success = DatabaseFileStore.WriteText(RECORD_DIRECTORY, fileName, json);
        if (success)
        {
            record.status = EDatabaseRecordStatus.Active;
        }
        return success;
    }

    /// <summary>从文件系统读取单条记录</summary>
    public DatabaseRecordSO ReadRecordFromFile(string entityType, string entityId, string stateType)
    {
        string recordKey = DatabaseKeyGenerator.GenerateKey(entityType, entityId, stateType);
        string fileName = recordKey + RECORD_FILE_EXTENSION;
        string json = DatabaseFileStore.ReadText(RECORD_DIRECTORY, fileName);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }
        try
        {
            var record = ScriptableObject.CreateInstance<DatabaseRecordSO>();
            JsonUtility.FromJsonOverwrite(json, record);
            if (DatabaseSerializer.VerifyHash(record.payload, record.payloadHash))
            {
                _recordCache[recordKey] = record;
                Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseRecordManager.ReadRecordFromFile: {recordKey}");
                return record;
            }
            else
            {
                record.status = EDatabaseRecordStatus.Corrupted;
                Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseRecordManager.ReadRecordFromFile hash mismatch: {recordKey}");
                return record;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseRecordManager.ReadRecordFromFile exception: {ex.Message}");
            return null;
        }
    }

    /// <summary>兼容旧调用名，等价于 ReadRecordFromFile</summary>
    public DatabaseRecordSO LoadRecordFromFile(string entityType, string entityId, string stateType)
    {
        return ReadRecordFromFile(entityType, entityId, stateType);
    }

    /// <summary>将当前批次所有记录写入文件系统</summary>
    public int WriteAllRecordsToFile(string batchId)
    {
        int successCount = 0;
        foreach (var kvp in _recordCache)
        {
            if (kvp.Value.batchId == batchId)
            {
                if (WriteRecordToFile(kvp.Value))
                {
                    successCount++;
                }
            }
        }
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseRecordManager.WriteAllRecordsToFile: batch={batchId}, success={successCount}");
        return successCount;
    }

    /// <summary>将指定记录标记为已删除（逻辑删除，物理保留）</summary>
    public void ArchiveRecord(string entityType, string entityId, string stateType)
    {
        string recordKey = DatabaseKeyGenerator.GenerateKey(entityType, entityId, stateType);
        if (_recordCache.TryGetValue(recordKey, out DatabaseRecordSO record))
        {
            record.status = EDatabaseRecordStatus.Archived;
            Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseRecordManager.ArchiveRecord: {recordKey}");
        }
    }

    /// <summary>清空内存中的记录缓存</summary>
    public void ClearCache()
    {
        foreach (var kvp in _recordCache)
        {
            if (kvp.Value != null)
            {
                UnityEngine.Object.Destroy(kvp.Value);
            }
        }
        _recordCache.Clear();
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseRecordManager.ClearCache.");
    }
}
