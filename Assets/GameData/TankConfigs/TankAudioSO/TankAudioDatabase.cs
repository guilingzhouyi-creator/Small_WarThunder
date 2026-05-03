using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TankAudioDatabase", menuName = "TankAudioSystem/AudioDatabase")]
public class TankAudioDatabase : ScriptableObject, ISerializationCallbackReceiver
{
    [SerializeField] private List<TankAudioData> allConfigs;

    // 运行时真正的索引字典
    private Dictionary<TankType, TankAudioData> _database = new Dictionary<TankType, TankAudioData>();

    public void OnAfterDeserialize()
    {
        RebuildDatabase();
    }

    private void OnEnable()
    {
        RebuildDatabase();
    }

    public void OnBeforeSerialize() { }

    // O(1) 检索
    public TankAudioData GetConfig(TankType type)
    {
        EnsureDatabase();
        return _database.GetValueOrDefault(type);
    }

    private void EnsureDatabase()
    {
        if (_database == null)
        {
            _database = new Dictionary<TankType, TankAudioData>();
        }

        if (_database.Count == 0 && allConfigs != null && allConfigs.Count > 0)
        {
            RebuildDatabase();
        }
    }

    private void RebuildDatabase()
    {
        if (_database == null)
        {
            _database = new Dictionary<TankType, TankAudioData>();
        }

        _database.Clear();

        if (allConfigs == null)
        {
            return;
        }

        foreach (TankAudioData cfg in allConfigs)
        {
            if (cfg != null && !_database.ContainsKey(cfg.TankType))
            {
                _database.Add(cfg.TankType, cfg);
            }
        }
    }
}