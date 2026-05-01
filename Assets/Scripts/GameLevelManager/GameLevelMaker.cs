using UnityEngine;

/// <summary>
/// 区域触发器：挂载在区域 Prefab 中的触发碰撞体上。
/// 检测进入区域的玩家/敌人坦克（通过 PlayerMaker/EnemyMaker 组件），
/// 通知 LevelStreamingEngine 刷新附近区域可见性。
/// 不依赖 Tag 字符串，使用强类型组件检测更安全。
/// </summary>
[RequireComponent(typeof(Collider))]
public class GameLevelMaker : MonoBehaviour
{
    [Header("区域标识")]
    [SerializeField] private string _regionId;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (col != null)
        {
            if (col is MeshCollider meshCollider && !meshCollider.convex)
            {
                Debug.LogWarning($"[GameLevelMaker] {gameObject.name} 使用了非凸 MeshCollider，无法作为 Trigger。将跳过触发器设置，改由玩家位置自动刷新区域可见性。", this);
                return;
            }

            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsTank(other))
        {
            LevelStreamingEngine.Instance?.ShowNearbyRegions(other.transform.position);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsTank(other))
        {
            LevelStreamingEngine.Instance?.ShowNearbyRegions(other.transform.position);
        }
    }

    /// <summary>
    /// 通过组件检测判断是否为坦克（玩家或敌人）。
    /// 先检查进入对象自身，再向上查找根对象。
    /// </summary>
    private static bool IsTank(Collider other)
    {
        // 先检查自身
        if (other.TryGetComponent<PlayerMaker>(out _))
            return true;
        if (other.TryGetComponent<EnemyMaker>(out _))
            return true;

        // 再检查根对象（坦克可能有多个子碰撞体）
        var root = other.transform.root;
        if (root.TryGetComponent<PlayerMaker>(out _))
            return true;
        if (root.TryGetComponent<EnemyMaker>(out _))
            return true;

        return false;
    }
}
