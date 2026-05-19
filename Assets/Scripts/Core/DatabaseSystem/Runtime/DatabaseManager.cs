using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 数据库系统主管理器，作为保存系统和重载系统的统一入口。
/// 聚合键生成、序列化、文件存储、记录管理、索引管理、清单管理和事务控制。
/// 对外暴露写链路（SaveRecord）和读链路（LoadRecord），
/// 内部协调各子管理器完成落库与读取。
/// </summary>
public class DatabaseManager
{
    private static DatabaseManager _instance;
    public static DatabaseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new DatabaseManager();
            }
            return _instance;
        }
    }

    private readonly DatabaseRecordManager _recordManager;
    private readonly DatabaseIndexManager _indexManager;
    private readonly DatabaseManifestManager _manifestManager;
    private readonly DatabaseTransactionManager _transactionManager;

    private string _currentDatabaseVersion;
    private bool _initialized;

    private DatabaseManager()
    {
        _recordManager = new DatabaseRecordManager();
        _indexManager = new DatabaseIndexManager();
        _manifestManager = new DatabaseManifestManager();
        _transactionManager = new DatabaseTransactionManager(_recordManager, _indexManager, _manifestManager);
    }

    /// <summary>初始化数据库系统，加载索引和版本信息</summary>
    public void Initialize(string databaseVersion = null)
    {
        if (_initialized)
        {
            Debug.LogWarning($"{DatabaseConstants.LogPrefix} DatabaseManager already initialized.");
            return;
        }
        _currentDatabaseVersion = databaseVersion ?? DatabaseConstants.DefaultVersion;
        _indexManager.LoadIndexes();
        _initialized = true;
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseManager initialized with version {_currentDatabaseVersion}.");
    }

    /// <summary>关闭数据库系统，释放资源</summary>
    public void Shutdown()
    {
        _indexManager.ClearIndexes();
        _manifestManager.ClearCache();
        _initialized = false;
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseManager shutdown.");
    }

    // ========== 写入链路 ==========

    /// <summary>开始一个保存批次</summary>
    public void BeginSaveBatch()
    {
        EnsureInitialized();
        string batchId = DatabaseKeyGenerator.GenerateBatchId();
        _transactionManager.BeginBatch(batchId, _currentDatabaseVersion);
    }

    /// <summary>构建并注册一条持久化记录</summary>
    /// <param name="entityType">实体类型标识</param>
    /// <param name="entityId">实体唯一ID</param>
    /// <param name="stateType">状态类型标识</param>
    /// <param name="stateData">可序列化的状态数据</param>
    /// <returns>生成的记录键，失败返回null</returns>
    public string SaveRecord(string entityType, string entityId, string stateType, object stateData)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(entityId))
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseManager.SaveRecord: entityType and entityId are required.");
            return null;
        }
        string payload = DatabaseSerializer.Serialize(stateData);
        var record = _recordManager.CreateRecord(entityType, entityId, stateType, _currentDatabaseVersion, payload);
        if (record == null)
        {
            return null;
        }
        _transactionManager.EnqueueRecord(record.recordKey, entityType, _currentDatabaseVersion);
        return record.recordKey;
    }

    /// <summary>提交当前保存批次</summary>
    /// <returns>是否成功提交</returns>
    public bool CommitSaveBatch()
    {
        EnsureInitialized();
        bool success = _transactionManager.CommitBatch();
        if (!success)
        {
            var failedKeys = _transactionManager.GetFailedKeys();
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseManager.CommitSaveBatch failed. Failed keys: {string.Join(", ", failedKeys)}");
        }
        return success;
    }

    /// <summary>回滚当前保存批次</summary>
    public void RollbackSaveBatch()
    {
        EnsureInitialized();
        _transactionManager.RollbackBatch();
    }

    // ========== 读取链路 ==========

    /// <summary>通过记录键加载记录</summary>
    public DatabaseRecordSO LoadRecordByKey(string recordKey)
    {
        EnsureInitialized();
        var entry = _indexManager.GetKeyIndex(recordKey);
        if (entry == null)
        {
            Debug.LogWarning($"{DatabaseConstants.LogPrefix} DatabaseManager.LoadRecordByKey: key not found in index: {recordKey}");
            return null;
        }
        string[] keyParts = recordKey.Split('-');
        if (keyParts.Length < 3)
        {
            return null;
        }
        return _recordManager.ReadRecordFromFile(entry.entityType, keyParts[1], keyParts[2]);
    }

    /// <summary>获取指定类型的所有记录键</summary>
    public IReadOnlyList<string> GetRecordKeysByType(string entityType)
    {
        EnsureInitialized();
        return _indexManager.GetKeysByType(entityType);
    }

    /// <summary>获取所有已索引的记录键</summary>
    public IReadOnlyList<string> GetAllRecordKeys()
    {
        EnsureInitialized();
        return _indexManager.GetAllKeys();
    }

    // ========== 清单查询 ==========

    /// <summary>加载指定批次的清单</summary>
    public DatabaseManifestSO LoadManifest(string batchId)
    {
        return _manifestManager.LoadManifest(batchId);
    }

    /// <summary>获取所有批次ID列表</summary>
    public List<string> GetAllBatchIds()
    {
        return _manifestManager.GetAllBatchIds();
    }

    // ========== 状态查询 ==========

    /// <summary>获取当前批次状态</summary>
    public EDatabaseBatchStatus GetBatchStatus()
    {
        return _transactionManager.GetBatchStatus();
    }

    /// <summary>获取数据库版本</summary>
    public string GetDatabaseVersion()
    {
        return _currentDatabaseVersion;
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            Debug.LogWarning($"{DatabaseConstants.LogPrefix} DatabaseManager not initialized, auto-initializing with default version.");
            Initialize();
        }
    }
}
