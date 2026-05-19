using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 数据库事务管理器，负责批次写入的事务控制——提交、回滚、失败处理。
/// 确保批次要么完整写入，要么整体回滚，避免静态数据库处于脏状态。
/// </summary>
public class DatabaseTransactionManager
{
    private const string TRANSACTION_LOG_DIR = "DatabaseTransactionLogs";
    private const string TRANSACTION_LOG_FILE = "transactionLog.json";

    private readonly List<TransactionEntry> _pendingWrites = new List<TransactionEntry>();
    private readonly List<string> _failedKeys = new List<string>();
    private EDatabaseBatchStatus _batchStatus = EDatabaseBatchStatus.Open;
    private string _currentBatchId;

    private DatabaseRecordManager _recordManager;
    private DatabaseIndexManager _indexManager;
    private DatabaseManifestManager _manifestManager;

    public DatabaseTransactionManager(
        DatabaseRecordManager recordManager,
        DatabaseIndexManager indexManager,
        DatabaseManifestManager manifestManager)
    {
        _recordManager = recordManager ?? throw new ArgumentNullException(nameof(recordManager));
        _indexManager = indexManager ?? throw new ArgumentNullException(nameof(indexManager));
        _manifestManager = manifestManager ?? throw new ArgumentNullException(nameof(manifestManager));
        DatabaseFileStore.EnsureDirectory(TRANSACTION_LOG_DIR);
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseTransactionManager initialized.");
    }

    /// <summary>开始一个新批次事务</summary>
    public void BeginBatch(string batchId, string databaseVersion)
    {
        _currentBatchId = batchId;
        _batchStatus = EDatabaseBatchStatus.Open;
        _pendingWrites.Clear();
        _failedKeys.Clear();
        _manifestManager.BeginBatch(batchId, databaseVersion);
        _recordManager.SetBatchId(batchId);
        _indexManager.SetBatchId(batchId);
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseTransactionManager.BeginBatch: {batchId}");
    }

    /// <summary>注册一条待写入的记录</summary>
    public void EnqueueRecord(string recordKey, string entityType, string version)
    {
        if (_batchStatus != EDatabaseBatchStatus.Open)
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseTransactionManager.EnqueueRecord: no active batch.");
            return;
        }
        _pendingWrites.Add(new TransactionEntry
        {
            recordKey = recordKey,
            entityType = entityType,
            version = version ?? DatabaseConstants.DefaultVersion
        });
        _manifestManager.AddRecordToManifest(recordKey, entityType);
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseTransactionManager.EnqueueRecord: {recordKey}");
    }

    /// <summary>提交当前批次，将待写入记录逐个落库</summary>
    public bool CommitBatch()
    {
        if (_batchStatus != EDatabaseBatchStatus.Open)
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseTransactionManager.CommitBatch: no active batch.");
            return false;
        }
        int successCount = 0;
        foreach (var entry in _pendingWrites)
        {
            var record = _recordManager.GetRecord(entry.entityType,
                ExtractEntityId(entry.recordKey), ExtractStateType(entry.recordKey));
            if (record == null)
            {
                _failedKeys.Add(entry.recordKey);
                Debug.LogWarning($"{DatabaseConstants.LogPrefix} DatabaseTransactionManager.CommitBatch: record not found for {entry.recordKey}");
                continue;
            }
            bool written = _recordManager.WriteRecordToFile(record);
            if (written)
            {
                _indexManager.RegisterRecord(entry.recordKey, entry.entityType, entry.version);
                successCount++;
            }
            else
            {
                _failedKeys.Add(entry.recordKey);
            }
        }
        _manifestManager.CompleteBatch();
        bool manifestSaved = _manifestManager.SaveManifest(_manifestManager.GetCurrentManifest());
        if (!manifestSaved)
        {
            _failedKeys.Add("manifest");
        }
        _indexManager.SaveIndexes();
        if (_failedKeys.Count > 0)
        {
            _batchStatus = EDatabaseBatchStatus.PartialFailure;
            SaveTransactionLog(false);
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseTransactionManager.CommitBatch partial fail: {successCount} succeeded, {_failedKeys.Count} failed.");
            return false;
        }
        _batchStatus = EDatabaseBatchStatus.Committed;
        SaveTransactionLog(true);
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseTransactionManager.CommitBatch success: {successCount} records.");
        return true;
    }

    /// <summary>回滚当前批次，清除待写入列表和清单中的记录</summary>
    public void RollbackBatch()
    {
        if (_batchStatus != EDatabaseBatchStatus.Open && _batchStatus != EDatabaseBatchStatus.PartialFailure)
        {
            Debug.LogWarning($"{DatabaseConstants.LogPrefix} DatabaseTransactionManager.RollbackBatch: nothing to roll back.");
            return;
        }
        foreach (var entry in _pendingWrites)
        {
            _indexManager.RemoveRecord(entry.recordKey);
        }
        _pendingWrites.Clear();
        _failedKeys.Clear();
        _batchStatus = EDatabaseBatchStatus.RolledBack;
        SaveTransactionLog(false);
        Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseTransactionManager.RollbackBatch: {_currentBatchId}");
    }

    /// <summary>获取失败记录键列表</summary>
    public IReadOnlyList<string> GetFailedKeys()
    {
        return _failedKeys;
    }

    /// <summary>获取当前批次状态</summary>
    public EDatabaseBatchStatus GetBatchStatus()
    {
        return _batchStatus;
    }

    /// <summary>保存事务日志</summary>
    private void SaveTransactionLog(bool success)
    {
        try
        {
            var log = new TransactionLogEntry
            {
                batchId = _currentBatchId,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                success = success,
                totalRecords = _pendingWrites.Count,
                failedKeys = _failedKeys.ToArray(),
                status = _batchStatus.ToString()
            };
            string json = JsonUtility.ToJson(log);
            DatabaseFileStore.AppendText(TRANSACTION_LOG_DIR, TRANSACTION_LOG_FILE, json + "\n");
        }
        catch (Exception ex)
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseTransactionManager.SaveTransactionLog exception: {ex.Message}");
        }
    }

    private string ExtractEntityId(string recordKey)
    {
        if (string.IsNullOrEmpty(recordKey)) return string.Empty;
        string[] parts = recordKey.Split('-');
        return parts.Length >= 2 ? parts[1] : string.Empty;
    }

    private string ExtractStateType(string recordKey)
    {
        if (string.IsNullOrEmpty(recordKey)) return string.Empty;
        string[] parts = recordKey.Split('-');
        return parts.Length >= 3 ? parts[2] : string.Empty;
    }
}

[System.Serializable]
internal class TransactionEntry
{
    public string recordKey;
    public string entityType;
    public string version;
}

[System.Serializable]
internal class TransactionLogEntry
{
    public string batchId;
    public long timestamp;
    public bool success;
    public int totalRecords;
    public string[] failedKeys;
    public string status;
}
