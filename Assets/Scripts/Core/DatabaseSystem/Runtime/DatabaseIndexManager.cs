using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 数据库索引管理器，负责维护记录键、类型键和版本信息的索引映射。
/// 保存时登记索引，重载时通过索引快速定位记录。
/// </summary>
public class DatabaseIndexManager
{
    private const string INDEX_DIRECTORY = "DatabaseIndexes";
    private const string KEY_INDEX_FILE = "keyIndex.json";
    private const string TYPE_INDEX_FILE = "typeIndex.json";
    private const string VERSION_INDEX_FILE = "versionIndex.json";

    // 键索引：recordKey -> { entityType, version, filePath }
    private readonly Dictionary<string, DatabaseIndexEntry> _keyIndex = new Dictionary<string, DatabaseIndexEntry>();
    // 类型索引：entityType -> List<recordKey>
    private readonly Dictionary<string, List<string>> _typeIndex = new Dictionary<string, List<string>>();
    // 版本索引：version -> List<recordKey>
    private readonly Dictionary<string, List<string>> _versionIndex = new Dictionary<string, List<string>>();

    private string _currentBatchId;

    public DatabaseIndexManager()
    {
        DatabaseFileStore.EnsureDirectory(INDEX_DIRECTORY);
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseIndexManager initialized.");
    }

    /// <summary>设置当前批次ID</summary>
    public void SetBatchId(string batchId)
    {
        _currentBatchId = batchId;
    }

    /// <summary>登记一条记录到索引</summary>
    public void RegisterRecord(string recordKey, string entityType, string version)
    {
        if (string.IsNullOrEmpty(recordKey) || string.IsNullOrEmpty(entityType))
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseIndexManager.RegisterRecord failed: invalid parameters.");
            return;
        }
        var entry = new DatabaseIndexEntry
        {
            recordKey = recordKey,
            entityType = entityType,
            version = version ?? DatabaseConstants.DefaultVersion
        };
        _keyIndex[recordKey] = entry;
        if (!_typeIndex.ContainsKey(entityType))
        {
            _typeIndex[entityType] = new List<string>();
        }
        if (!_typeIndex[entityType].Contains(recordKey))
        {
            _typeIndex[entityType].Add(recordKey);
        }
        string ver = version ?? DatabaseConstants.DefaultVersion;
        if (!_versionIndex.ContainsKey(ver))
        {
            _versionIndex[ver] = new List<string>();
        }
        if (!_versionIndex[ver].Contains(recordKey))
        {
            _versionIndex[ver].Add(recordKey);
        }
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseIndexManager.RegisterRecord: {recordKey}");
    }

    /// <summary>通过键获取索引条目</summary>
    public DatabaseIndexEntry GetKeyIndex(string recordKey)
    {
        _keyIndex.TryGetValue(recordKey, out DatabaseIndexEntry entry);
        return entry;
    }

    /// <summary>通过类型获取所有记录键</summary>
    public IReadOnlyList<string> GetKeysByType(string entityType)
    {
        if (_typeIndex.TryGetValue(entityType, out List<string> keys))
        {
            return keys;
        }
        return new List<string>();
    }

    /// <summary>通过版本获取所有记录键</summary>
    public IReadOnlyList<string> GetKeysByVersion(string version)
    {
        if (_versionIndex.TryGetValue(version, out List<string> keys))
        {
            return keys;
        }
        return new List<string>();
    }

    /// <summary>获取所有已索引的记录键</summary>
    public IReadOnlyList<string> GetAllKeys()
    {
        return new List<string>(_keyIndex.Keys);
    }

    /// <summary>检查记录键是否已索引</summary>
    public bool ContainsKey(string recordKey)
    {
        return _keyIndex.ContainsKey(recordKey);
    }

    /// <summary>从索引中移除一条记录</summary>
    public void RemoveRecord(string recordKey)
    {
        if (_keyIndex.TryGetValue(recordKey, out DatabaseIndexEntry entry))
        {
            _keyIndex.Remove(recordKey);
            if (_typeIndex.TryGetValue(entry.entityType, out List<string> typeKeys))
            {
                typeKeys.Remove(recordKey);
            }
            if (_versionIndex.TryGetValue(entry.version, out List<string> versionKeys))
            {
                versionKeys.Remove(recordKey);
            }
            Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseIndexManager.RemoveRecord: {recordKey}");
        }
    }

    /// <summary>将索引持久化到文件</summary>
    public void SaveIndexes()
    {
        try
        {
            var keyIndexData = new SerializableKeyIndex { entries = new List<DatabaseIndexEntry>(_keyIndex.Values) };
            string keyJson = JsonUtility.ToJson(keyIndexData);
            DatabaseFileStore.WriteText(INDEX_DIRECTORY, KEY_INDEX_FILE, keyJson);
            var typeIndexData = new SerializableTypeIndex { entries = new List<TypeIndexEntry>() };
            foreach (var kvp in _typeIndex)
            {
                typeIndexData.entries.Add(new TypeIndexEntry { entityType = kvp.Key, recordKeys = kvp.Value.ToArray() });
            }
            string typeJson = JsonUtility.ToJson(typeIndexData);
            DatabaseFileStore.WriteText(INDEX_DIRECTORY, TYPE_INDEX_FILE, typeJson);
            var versionIndexData = new SerializableVersionIndex { entries = new List<VersionIndexEntry>() };
            foreach (var kvp in _versionIndex)
            {
                versionIndexData.entries.Add(new VersionIndexEntry { version = kvp.Key, recordKeys = kvp.Value.ToArray() });
            }
            string versionJson = JsonUtility.ToJson(versionIndexData);
            DatabaseFileStore.WriteText(INDEX_DIRECTORY, VERSION_INDEX_FILE, versionJson);
            Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseIndexManager.SaveIndexes completed.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseIndexManager.SaveIndexes exception: {ex.Message}");
        }
    }

    /// <summary>从文件加载索引</summary>
    public void LoadIndexes()
    {
        try
        {
            string keyJson = DatabaseFileStore.ReadText(INDEX_DIRECTORY, KEY_INDEX_FILE);
            if (!string.IsNullOrEmpty(keyJson))
            {
                var keyIndexData = JsonUtility.FromJson<SerializableKeyIndex>(keyJson);
                if (keyIndexData?.entries != null)
                {
                    foreach (var entry in keyIndexData.entries)
                    {
                        _keyIndex[entry.recordKey] = entry;
                    }
                }
            }
            string typeJson = DatabaseFileStore.ReadText(INDEX_DIRECTORY, TYPE_INDEX_FILE);
            if (!string.IsNullOrEmpty(typeJson))
            {
                var typeIndexData = JsonUtility.FromJson<SerializableTypeIndex>(typeJson);
                if (typeIndexData?.entries != null)
                {
                    foreach (var entry in typeIndexData.entries)
                    {
                        _typeIndex[entry.entityType] = new List<string>(entry.recordKeys ?? new string[0]);
                    }
                }
            }
            string versionJson = DatabaseFileStore.ReadText(INDEX_DIRECTORY, VERSION_INDEX_FILE);
            if (!string.IsNullOrEmpty(versionJson))
            {
                var versionIndexData = JsonUtility.FromJson<SerializableVersionIndex>(versionJson);
                if (versionIndexData?.entries != null)
                {
                    foreach (var entry in versionIndexData.entries)
                    {
                        _versionIndex[entry.version] = new List<string>(entry.recordKeys ?? new string[0]);
                    }
                }
            }
            Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseIndexManager.LoadIndexes completed.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseIndexManager.LoadIndexes exception: {ex.Message}");
        }
    }

    /// <summary>清空内存中的索引</summary>
    public void ClearIndexes()
    {
        _keyIndex.Clear();
        _typeIndex.Clear();
        _versionIndex.Clear();
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseIndexManager.ClearIndexes.");
    }
}

/// <summary>
/// 键索引条目，记录单条持久化数据的索引信息。
/// </summary>
[System.Serializable]
public class DatabaseIndexEntry
{
    public string recordKey;
    public string entityType;
    public string version;
}

[System.Serializable]
internal class SerializableKeyIndex { public List<DatabaseIndexEntry> entries; }

[System.Serializable]
internal class TypeIndexEntry { public string entityType; public string[] recordKeys; }

[System.Serializable]
internal class SerializableTypeIndex { public List<TypeIndexEntry> entries; }

[System.Serializable]
internal class VersionIndexEntry { public string version; public string[] recordKeys; }

[System.Serializable]
internal class SerializableVersionIndex { public List<VersionIndexEntry> entries; }
