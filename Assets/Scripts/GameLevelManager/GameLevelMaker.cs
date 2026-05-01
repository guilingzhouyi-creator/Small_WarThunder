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
    private static bool _hasLoggedNonConvexFallbackInfo;

    [Header("区域标识")]
    [SerializeField] private string _regionId;

    private GameLevelManager _gameLevelManager;
    private Collider _regionCollider;
    private bool _usesTriggerCallbacks = true;
    private bool _isPlayerInsideByPolling;
    private float _outsideDetectedTime = float.NegativeInfinity;
    [SerializeField] private float _pollingExitGraceSeconds = 0.2f;
    private string RegionId => string.IsNullOrWhiteSpace(_regionId) ? gameObject.name : _regionId;

    private void Awake()
    {
        _gameLevelManager = GetComponent<GameLevelManager>();

        _regionCollider = GetComponent<Collider>();
        if (_regionCollider != null)
        {
            if (_regionCollider is MeshCollider meshCollider && !meshCollider.convex)
            {
                _usesTriggerCallbacks = false;

                if (!_hasLoggedNonConvexFallbackInfo)
                {
                    _hasLoggedNonConvexFallbackInfo = true;
                    Debug.Log("[GameLevelMaker] 检测到任务区域使用非凸 MeshCollider，已自动切换为轮询检测模式；后续同类区域不再重复提示。", this);
                }

                return;
            }

            _regionCollider.isTrigger = true;
        }
    }

    private void Update()
    {
        if (_usesTriggerCallbacks || _regionCollider == null)
        {
            return;
        }

        GameObject playerTank = GameManager.Instance != null ? GameManager.Instance.PlayerTank : null;
        if (playerTank == null)
        {
            if (_isPlayerInsideByPolling)
            {
                _gameLevelManager?.NotifyPlayerExitedRegion(null, RegionId);
                _isPlayerInsideByPolling = false;
            }

            return;
        }

        bool isInside = IsPlayerInsideRegion(playerTank);
        if (isInside)
        {
            _outsideDetectedTime = float.NegativeInfinity;

            if (!_isPlayerInsideByPolling)
            {
                LevelStreamingEngine.Instance?.ShowNearbyRegions(playerTank.transform.position);
                _gameLevelManager?.NotifyPlayerEnteredRegion(playerTank, RegionId);
                _isPlayerInsideByPolling = true;
            }

            _gameLevelManager?.NotifyPlayerStayedRegion(playerTank, RegionId);
            return;
        }

        if (_isPlayerInsideByPolling)
        {
            if (_outsideDetectedTime < 0f)
            {
                _outsideDetectedTime = Time.unscaledTime;
                return;
            }

            if (Time.unscaledTime - _outsideDetectedTime < Mathf.Max(0.01f, _pollingExitGraceSeconds))
            {
                return;
            }

            _outsideDetectedTime = float.NegativeInfinity;
            LevelStreamingEngine.Instance?.ShowNearbyRegions(playerTank.transform.position);
            _gameLevelManager?.NotifyPlayerExitedRegion(playerTank, RegionId);
            _isPlayerInsideByPolling = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsTank(other))
        {
            LevelStreamingEngine.Instance?.ShowNearbyRegions(other.transform.position);
        }

        if (TryGetPlayerTank(other, out GameObject playerTank))
        {
            _gameLevelManager?.NotifyPlayerEnteredRegion(playerTank, RegionId);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (TryGetPlayerTank(other, out GameObject playerTank))
        {
            _gameLevelManager?.NotifyPlayerStayedRegion(playerTank, RegionId);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsTank(other))
        {
            LevelStreamingEngine.Instance?.ShowNearbyRegions(other.transform.position);
        }

        if (TryGetPlayerTank(other, out GameObject playerTank))
        {
            _gameLevelManager?.NotifyPlayerExitedRegion(playerTank, RegionId);
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

    private static bool TryGetPlayerTank(Collider other, out GameObject playerTank)
    {
        playerTank = null;
        if (other == null)
        {
            return false;
        }

        if (other.TryGetComponent<PlayerMaker>(out _))
        {
            playerTank = other.gameObject;
            return true;
        }

        Transform root = other.transform.root;
        if (root != null && root.TryGetComponent<PlayerMaker>(out _))
        {
            playerTank = root.gameObject;
            return true;
        }

        return false;
    }

    private bool IsPlayerInsideRegion(GameObject playerTank)
    {
        if (_regionCollider == null || playerTank == null)
        {
            return false;
        }

        bool supportsClosestPoint = SupportsClosestPoint(_regionCollider);
        Collider[] playerColliders = playerTank.GetComponentsInChildren<Collider>();
        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider playerCollider = playerColliders[i];
            if (playerCollider == null || !playerCollider.enabled || playerCollider.isTrigger)
            {
                continue;
            }

            if (!_regionCollider.bounds.Intersects(playerCollider.bounds))
            {
                continue;
            }

            if (Physics.ComputePenetration(
                _regionCollider,
                _regionCollider.transform.position,
                _regionCollider.transform.rotation,
                playerCollider,
                playerCollider.transform.position,
                playerCollider.transform.rotation,
                out _,
                out _))
            {
                return true;
            }

            if (supportsClosestPoint && IsPointInsideRegion(playerCollider.bounds.center))
            {
                return true;
            }
        }

        return supportsClosestPoint && IsPointInsideRegion(playerTank.transform.position);
    }

    private static bool SupportsClosestPoint(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider)
        {
            return true;
        }

        if (collider is MeshCollider meshCollider)
        {
            return meshCollider.convex;
        }

        return false;
    }

    private bool IsPointInsideRegion(Vector3 point)
    {
        if (_regionCollider == null || !_regionCollider.bounds.Contains(point))
        {
            return false;
        }

        Vector3 closestPoint = _regionCollider.ClosestPoint(point);
        return (closestPoint - point).sqrMagnitude <= 0.0001f;
    }
}
