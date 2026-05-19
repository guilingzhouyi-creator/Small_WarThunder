/// <summary>
/// 数据库系统字符串常量库，集中管理数据库操作相关的所有字符串常量。
/// 任何涉及数据库操作的脚本必须通过此类引用字符串，避免硬编码。
/// </summary>
public static class DatabaseConstants
{
    // ─── 记录键格式 ───
    public const string RecordKeyFormat = "{0}-{1}-{2}";
    public const string RecordKeySeparator = "-";

    // ─── 批次标识 ───
    public const string BatchIdPrefix = "BATCH_";
    public const string DefaultBatchName = "DefaultBatch";

    // ─── 版本 ───
    public const string DefaultVersion = "1.0.0";
    public const string VersionSeparator = ".";

    // ─── 文件扩展名 ───
    public const string RecordFileExtension = ".dbrec";
    public const string IndexFileExtension = ".dbidx";
    public const string ManifestFileExtension = ".dbman";

    // ─── 存储目录 ───
    public const string DatabaseRootPath = "Database";
    public const string RecordSubPath = "Database/Records";
    public const string IndexSubPath = "Database/Index";
    public const string ManifestSubPath = "Database/Manifest";

    // ─── 日志前缀 ───
    public const string LogPrefix = "[DatabaseSystem]";
    public const string LogWriteSuccess = "WriteSuccess";
    public const string LogWriteFailure = "WriteFailure";
    public const string LogQueryStart = "QueryStart";
    public const string LogQueryResult = "QueryResult";
    public const string LogBatchComplete = "BatchComplete";
    public const string LogBatchFailed = "BatchFailed";
    public const string LogVersionConflict = "VersionConflict";
    public const string LogRecordCorrupted = "RecordCorrupted";
    public const string LogIndexRebuild = "IndexRebuild";
    public const string LogManifestCreated = "ManifestCreated";
    public const string LogTransactionStart = "TransactionStart";
    public const string LogTransactionCommit = "TransactionCommit";
    public const string LogTransactionRollback = "TransactionRollback";

    // ─── 哈希 ───
    public const string HashAlgorithm = "SHA256";

    // ─── 时间戳 ───
    public const long DefaultTimestamp = 0;
}
