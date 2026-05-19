using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// 数据库序列化层，负责将运行时数据转换为持久化的JSON负载，
/// 以及将持久化负载反序列化回运行时对象。同时提供哈希校验。
/// </summary>
public static class DatabaseSerializer
{
    /// <summary>将对象序列化为JSON字符串</summary>
    public static string Serialize<T>(T data)
    {
        if (data == null)
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} Serialize failed: data is null.");
            return string.Empty;
        }
        try
        {
            string json = JsonUtility.ToJson(data);
            Debug.Log($"{DatabaseConstants.LogPrefix} {typeof(T).Name} serialized, length={json.Length}");
            return json;
        }
        catch (Exception ex)
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} Serialize exception: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>将JSON字符串反序列化为对象</summary>
    public static T Deserialize<T>(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} Deserialize failed: json is null or empty.");
            return default(T);
        }
        try
        {
            T data = JsonUtility.FromJson<T>(json);
            Debug.Log($"{DatabaseConstants.LogPrefix} {typeof(T).Name} deserialized.");
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"{DatabaseConstants.LogPrefix} Deserialize exception: {ex.Message}");
            return default(T);
        }
    }

    /// <summary>计算数据的SHA256哈希值</summary>
    public static string ComputeHash(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return string.Empty;
        }
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            byte[] hash = sha256.ComputeHash(bytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }

    /// <summary>验证数据和哈希值是否匹配</summary>
    public static bool VerifyHash(string data, string expectedHash)
    {
        if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(expectedHash))
        {
            return false;
        }
        string computedHash = ComputeHash(data);
        bool valid = string.Equals(computedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        if (!valid)
        {
            Debug.LogWarning($"{DatabaseConstants.LogPrefix} Hash verification failed. Expected={expectedHash}, Computed={computedHash}");
        }
        return valid;
    }
}
