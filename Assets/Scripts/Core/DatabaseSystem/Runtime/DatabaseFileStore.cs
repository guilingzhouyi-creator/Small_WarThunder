using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 数据库文件存储层，负责将序列化后的记录写入本地文件系统，
/// 以及从文件系统读取持久化数据。采用 Application.persistentDataPath 作为根路径。
/// </summary>
public static class DatabaseFileStore
{
    /// <summary>写入文本数据到指定相对路径的文件</summary>
    public static bool WriteText(string relativePath, string fileName, string content)
    {
        if (string.IsNullOrEmpty(relativePath) || string.IsNullOrEmpty(fileName) || content == null)
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseFileStore.WriteText failed: invalid parameters.");
            return false;
        }
        try
        {
            string fullDir = Path.Combine(Application.persistentDataPath, relativePath);
            if (!Directory.Exists(fullDir))
            {
                Directory.CreateDirectory(fullDir);
            }
            string fullPath = Path.Combine(fullDir, fileName);
            File.WriteAllText(fullPath, content);
            Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseFileStore.WriteText success: {fullPath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseFileStore.WriteText exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>以追加模式写入文本数据到指定相对路径的文件</summary>
    public static bool AppendText(string relativePath, string fileName, string content)
    {
        if (string.IsNullOrEmpty(relativePath) || string.IsNullOrEmpty(fileName) || content == null)
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseFileStore.AppendText failed: invalid parameters.");
            return false;
        }

        try
        {
            string fullDir = Path.Combine(Application.persistentDataPath, relativePath);
            if (!Directory.Exists(fullDir))
            {
                Directory.CreateDirectory(fullDir);
            }

            string fullPath = Path.Combine(fullDir, fileName);
            File.AppendAllText(fullPath, content);
            Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseFileStore.AppendText success: {fullPath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseFileStore.AppendText exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>从指定相对路径的文件读取文本数据</summary>
    public static string ReadText(string relativePath, string fileName)
    {
        if (string.IsNullOrEmpty(relativePath) || string.IsNullOrEmpty(fileName))
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseFileStore.ReadText failed: invalid parameters.");
            return null;
        }
        try
        {
            string fullPath = Path.Combine(Application.persistentDataPath, relativePath, fileName);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"{DatabaseConstants.LogPrefix} DatabaseFileStore.ReadText: file not found: {fullPath}");
                return null;
            }
            string content = File.ReadAllText(fullPath);
            Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseFileStore.ReadText success: {fullPath}");
            return content;
        }
        catch (Exception ex)
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseFileStore.ReadText exception: {ex.Message}");
            return null;
        }
    }

    /// <summary>删除指定相对路径的文件</summary>
    public static bool DeleteFile(string relativePath, string fileName)
    {
        if (string.IsNullOrEmpty(relativePath) || string.IsNullOrEmpty(fileName))
        {
            return false;
        }
        try
        {
            string fullPath = Path.Combine(Application.persistentDataPath, relativePath, fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseFileStore.DeleteFile: {fullPath}");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} DatabaseFileStore.DeleteFile exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>检查文件是否存在</summary>
    public static bool FileExists(string relativePath, string fileName)
    {
        if (string.IsNullOrEmpty(relativePath) || string.IsNullOrEmpty(fileName))
        {
            return false;
        }
        string fullPath = Path.Combine(Application.persistentDataPath, relativePath, fileName);
        return File.Exists(fullPath);
    }

    /// <summary>确保目录存在</summary>
    public static void EnsureDirectory(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;
        string fullDir = Path.Combine(Application.persistentDataPath, relativePath);
        if (!Directory.Exists(fullDir))
        {
            Directory.CreateDirectory(fullDir);
            Debug.Log($"{DatabaseConstants.LogPrefix} DatabaseFileStore.EnsureDirectory created: {fullDir}");
        }
    }

    /// <summary>兼容旧调用的无状态清理入口</summary>
    public static void ClearCache()
    {
    }
}
