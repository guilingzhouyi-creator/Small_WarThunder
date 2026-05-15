using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI黑板数据容器，存储AI运行时键值对数据，支持任意类型读取
/// </summary>
namespace NGameData.NAIData
{
    [System.Serializable]
    public class AI_Blackboard
    {
        private Dictionary<string, object> _data = new Dictionary<string, object>();

        /// <summary>设置键值</summary>
        public void Set<T>(string key, T value)
        {
            _data[key] = value;
        }

        /// <summary>获取键值，不存在则返回默认值</summary>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (_data.TryGetValue(key, out object val) && val is T typedVal)
            {
                return typedVal;
            }
            return defaultValue;
        }

        /// <summary>检查键是否存在</summary>
        public bool HasKey(string key) => _data.ContainsKey(key);

        /// <summary>移除键</summary>
        public void Remove(string key) => _data.Remove(key);

        /// <summary>清空所有数据</summary>
        public void Clear() => _data.Clear();
    }
}
