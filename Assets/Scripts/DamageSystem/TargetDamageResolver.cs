using UnityEngine;
/// <summary>
/// 伤害逻辑链的第二环，负责实际的伤害计算和应用，处理后的结果转发给 TargetDurone 也可以广播给其他系统，例如 UI 系统显示伤害数字、音效系统播放受击声音等。
/// 
/// </summary>
public class TargetDamageResolver : MonoBehaviour
{
    private float _maxHealth = 100f;
    [SerializeField] private ArmoredZoneData armoredZoneData;
    private float _currentHealth;
    private bool _isDestroyed = false;
    private bool _enableDamageLogging = true;

    // private float _lastDamageAmount;

    // private string _lastHitPartName;

    // private string _lastProjectileName;
    // private Vector3 _lastHitPoint;// 这里可以存储最后一次命中的部位和位置，供后续使用，例如显示伤害数字、播放特效等。

    // private GameObject _OwnerRoot;


    private void Start()
    {
        _currentHealth = armoredZoneData != null ? armoredZoneData.maxHealth : _maxHealth;
    }


    public void ApplyDamage(float damageAmount, string hitPartName, string projectileName, Vector3 hitPoint)
    {
        if (_isDestroyed)
        {
            return;
        }

        // _lastDamageAmount = damageAmount;
        // _lastHitPartName = hitPartName;
        // _lastProjectileName = projectileName;

        _currentHealth -= damageAmount;

        if (_enableDamageLogging)
        {
            Debug.Log($"{name} 受到 {damageAmount} 点伤害，当前生命值：{_currentHealth}! 命中部位：{hitPartName}, 攻击来源：{projectileName}, 命中位置：{hitPoint}");
        }

        if (_currentHealth <= 0)
        {
            _isDestroyed = true;
            Debug.Log($"{name} 已被摧毁！");
            // 在这里可以添加目标被摧毁后的逻辑，例如播放爆炸动画、掉落物品等
        }
    }






}