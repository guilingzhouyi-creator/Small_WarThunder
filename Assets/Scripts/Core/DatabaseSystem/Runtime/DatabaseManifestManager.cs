using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 数据库清单管理器，负责维护每次保存批次的清单（记录键列表、版本、类型统计）。
/// 保存系统写入后登记清单，重载系统加载前通过清单确定恢复范围。
/// </summary>
public class DatabaseManifestManager
{
    private const string MANIFEST_DIRECTORY = "DatabaseManifests";
    private const string MANIFEST_FILE_PREFIX = "manifest_";

    private readonly Dictionary<string, DatabaseManifestSO> _manifestCache = new Dictionary<string, DatabaseManifestSO>();
    private DatabaseManifestSO _currentManifest;
    private string _currentBatchId;

    public DatabaseManifestManager()
    {
        DatabaseFileStore.EnsureDirectory(MANIFEST_DIRECTORY);
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseManifestManager initialized.");
    }

    /// <summary>设置当前批次ID并创建对应清单</summary>
    public DatabaseManifestSO BeginBatch(string batchId, string databaseVersion)
    {
        _currentBatchId = batchId;
        _currentManifest = ScriptableObject.CreateInstance<DatabaseManifestSO>();
        _currentManifest.manifestId = batchId;
        _currentManifest.manifestName = $"Batch_{batchId}";
        _currentManifest.databaseVersion = databaseVersion ?? DatabaseConstants.DefaultVersion;
        _currentManifest.createTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _currentManifest.recordKeys = new string[0];
        _currentManifest.typeCounts = new TypeCountEntry[0];
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseManifestManager.BeginBatch: {batchId}");
        return _currentManifest;
    }

    /// <summary>将记录登记到当前清单</summary>
    public void AddRecordToManifest(string recordKey, string entityType)
    {
        if (_currentManifest == null || string.IsNullOrEmpty(recordKey))
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseManifestManager.AddRecordToManifest failed: no active manifest or invalid key.");
            return;
        }
        var keyList = new List<string>(_currentManifest.recordKeys ?? new string[0]);
        if (!keyList.Contains(recordKey))
        {
            keyList.Add(recordKey);
            _currentManifest.recordKeys = keyList.ToArray();
        }
        var typeList = new List<TypeCountEntry>(_currentManifest.typeCounts ?? new TypeCountEntry[0]);
        var existing = typeList.Find(t => t.entityType == entityType);
        if (existing != null)
        {
            existing.recordCount++;
        }
        else
        {
            typeList.Add(new TypeCountEntry { entityType = entityType, recordCount = 1 });
        }
        _currentManifest.typeCounts = typeList.ToArray();
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseManifestManager.AddRecordToManifest: {recordKey}");
    }

    /// <summary>完成当前批次清单并计算哈希</summary>
    public void CompleteBatch()
    {
        if (_currentManifest == null) return;
        string hashInput = _currentManifest.manifestId + string.Join(",", _currentManifest.recordKeys ?? new string[0]);
        _currentManifest.manifestHash = DatabaseSerializer.ComputeHash(hashInput);
        _manifestCache[_currentManifest.manifestId] = _currentManifest;
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseManifestManager.CompleteBatch: {_currentManifest.manifestId}, records={_currentManifest.recordKeys?.Length ?? 0}");
    }

    /// <summary>将清单写入文件系统</summary>
    public bool SaveManifest(DatabaseManifestSO manifest)
    {
        if (manifest == null || string.IsNullOrEmpty(manifest.manifestId))
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseManifestManager.SaveManifest failed: invalid manifest.");
            return false;
        }
        string json = JsonUtility.ToJson(manifest);
        string fileName = MANIFEST_FILE_PREFIX + manifest.manifestId + ".json";
        bool success = DatabaseFileStore.WriteText(MANIFEST_DIRECTORY, fileName, json);
        if (success)
        {
            manifest.manifestHash = DatabaseSerializer.ComputeHash(json);
        }
        return success;
    }

    /// <summary>从文件系统加载清单</summary>
    public DatabaseManifestSO LoadManifest(string batchId)
    {
        if (_manifestCache.TryGetValue(batchId, out DatabaseManifestSO cached))
        {
            return cached;
        }
        string fileName = MANIFEST_FILE_PREFIX + batchId + ".json";
        string json = DatabaseFileStore.ReadText(MANIFEST_DIRECTORY, fileName);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }
        try
        {
            var manifest = ScriptableObject.CreateInstance<DatabaseManifestSO>();
            JsonUtility.FromJsonOverwrite(json, manifest);
            _manifestCache[batchId] = manifest;
            Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseManifestManager.LoadManifest: {batchId}");
            return manifest;
        }
        catch (Exception ex)
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseManifestManager.LoadManifest exception: {ex.Message}");
            return null;
        }
    }

    /// <summary>获取所有已缓存清单的批次ID列表</summary>
    public List<string> GetAllBatchIds()
    {
        return new List<string>(_manifestCache.Keys);
    }

    /// <summary>获取当前活动清单</summary>
    public DatabaseManifestSO GetCurrentManifest()
    {
        return _currentManifest;
    }

    /// <summary>清空内存中的清单缓存</summary>
    public void ClearCache()
    {
        foreach (var kvp in _manifestCache)
        {
            if (kvp.Value != null)
            {
                UnityEngine.Object.Destroy(kvp.Value);
            }
        }
        _manifestCache.Clear();
        if (_currentManifest != null)
        {
            UnityEngine.Object.Destroy(_currentManifest);
            _currentManifest = null;
        }
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseManifestManager.ClearCache.");
    }
}
