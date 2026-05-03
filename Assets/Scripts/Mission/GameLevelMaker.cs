using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 区域触发器：挂载在区域 Prefab 中的触发碰撞体上。
/// 检测进入区域的玩家/敌人坦克（通过 PlayerMaker/EnemyMaker 组件），
/// 通知 LevelStreamingEngine 刷新附近区域可见性。
/// </summary>
[RequireComponent(typeof(Collider))]
public class GameLevelMaker : MonoBehaviour
{
    private static bool _hasLoggedNonConvexFallbackInfo;

    [Header("区域标识")]
    [SerializeField] private string _regionId;

    [Header("边界缓冲")]
    [SerializeField] private float _enterBufferMeters = 8f;
    [SerializeField] private float _exitBufferMeters = 12f;
    [SerializeField] private float _enterConfirmSeconds = 0.12f;
    [SerializeField] private float _exitConfirmSeconds = 0.25f;

    private GameLevelManager _gameLevelManager;
    private Collider _regionCollider;
    private bool _usesTriggerCallbacks = true;
    private bool _isPlayerInside;
    private float _insideDetectedTime = float.NegativeInfinity;
    private float _outsideDetectedTime = float.NegativeInfinity;
    private readonly HashSet<int> _playerTriggerColliderIds = new HashSet<int>();

    private string RegionId => string.IsNullOrWhiteSpace(_regionId) ? gameObject.name : _regionId;

    private void Awake()
    {
        _gameLevelManager = GetComponent<GameLevelManager>();
        _regionCollider = GetComponent<Collider>();

        if (_regionCollider == null)
        {
            return;
        }

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

    private void Update()
    {
        GameObject playerTank = GameManager.Instance != null ? GameManager.Instance.PlayerTank : null;
        if (playerTank == null)
        {
            ForceExitIfNeeded();
            return;
        }

        bool shouldEvaluateGeometry = !_usesTriggerCallbacks || _isPlayerInside || _playerTriggerColliderIds.Count > 0;
        bool desiredInside = shouldEvaluateGeometry && IsPlayerInsideRegion(playerTank, _isPlayerInside);

        if (desiredInside)
        {
            _outsideDetectedTime = float.NegativeInfinity;

            if (!_isPlayerInside)
            {
                if (_insideDetectedTime < 0f)
                {
                    _insideDetectedTime = Time.unscaledTime;
                    return;
                }

                if (Time.unscaledTime - _insideDetectedTime < Mathf.Max(0.01f, _enterConfirmSeconds))
                {
                    return;
                }

                _isPlayerInside = true;
                _insideDetectedTime = float.NegativeInfinity;
                LevelStreamingEngine.Instance?.ShowNearbyRegions(playerTank.transform.position);
                _gameLevelManager?.NotifyPlayerEnteredRegion(playerTank, RegionId);
            }

            _gameLevelManager?.NotifyPlayerStayedRegion(playerTank, RegionId);
            return;
        }

        _insideDetectedTime = float.NegativeInfinity;

        if (!_isPlayerInside)
        {
            return;
        }

        if (_outsideDetectedTime < 0f)
        {
            _outsideDetectedTime = Time.unscaledTime;
            return;
        }

        if (Time.unscaledTime - _outsideDetectedTime < Mathf.Max(0.01f, _exitConfirmSeconds))
        {
            return;
        }

        _outsideDetectedTime = float.NegativeInfinity;
        _isPlayerInside = false;
        LevelStreamingEngine.Instance?.ShowNearbyRegions(playerTank.transform.position);
        _gameLevelManager?.NotifyPlayerExitedRegion(playerTank, RegionId);
    }

    private void OnDisable()
    {
        _playerTriggerColliderIds.Clear();
        _insideDetectedTime = float.NegativeInfinity;
        _outsideDetectedTime = float.NegativeInfinity;
        _isPlayerInside = false;
    }

    private void OnDrawGizmosSelected()
    {
        Collider regionCollider = _regionCollider != null ? _regionCollider : GetComponent<Collider>();
        if (regionCollider == null)
        {
            return;
        }

        DrawBufferedBounds(regionCollider.bounds, -_enterBufferMeters, new Color(0.2f, 0.9f, 0.3f, 0.9f));
        DrawBufferedBounds(regionCollider.bounds, _exitBufferMeters, new Color(1f, 0.6f, 0.1f, 0.9f));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsTank(other))
        {
            LevelStreamingEngine.Instance?.ShowNearbyRegions(other.transform.position);
        }

        if (TryGetPlayerTank(other, out _))
        {
            _playerTriggerColliderIds.Add(other.GetInstanceID());
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (TryGetPlayerTank(other, out _))
        {
            _playerTriggerColliderIds.Add(other.GetInstanceID());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsTank(other))
        {
            LevelStreamingEngine.Instance?.ShowNearbyRegions(other.transform.position);
        }

        if (TryGetPlayerTank(other, out _))
        {
            _playerTriggerColliderIds.Remove(other.GetInstanceID());
        }
    }

    private void ForceExitIfNeeded()
    {
        if (!_isPlayerInside)
        {
            _playerTriggerColliderIds.Clear();
            return;
        }

        _gameLevelManager?.NotifyPlayerExitedRegion(null, RegionId);
        _playerTriggerColliderIds.Clear();
        _insideDetectedTime = float.NegativeInfinity;
        _outsideDetectedTime = float.NegativeInfinity;
        _isPlayerInside = false;
    }

    private static bool IsTank(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.TryGetComponent<PlayerMaker>(out _))
            return true;
        if (other.TryGetComponent<EnemyMaker>(out _))
            return true;

        Transform root = other.transform.root;
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

    private bool IsPlayerInsideRegion(GameObject playerTank, bool currentlyInside)
    {
        if (_regionCollider == null || playerTank == null)
        {
            return false;
        }

        Bounds regionBounds = GetBufferedBounds(currentlyInside ? _exitBufferMeters : -_enterBufferMeters);
        if (regionBounds.size.x <= 0f || regionBounds.size.y <= 0f || regionBounds.size.z <= 0f)
        {
            regionBounds = _regionCollider.bounds;
        }

        Collider[] playerColliders = playerTank.GetComponentsInChildren<Collider>();
        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider playerCollider = playerColliders[i];
            if (playerCollider == null || !playerCollider.enabled || playerCollider.isTrigger)
            {
                continue;
            }

            if (regionBounds.Intersects(playerCollider.bounds))
            {
                return true;
            }

            if (regionBounds.Contains(playerCollider.bounds.center))
            {
                return true;
            }
        }

        return regionBounds.Contains(playerTank.transform.position);
    }

    private Bounds GetBufferedBounds(float bufferMeters)
    {
        Bounds bounds = _regionCollider.bounds;
        Vector3 expandAmount = Vector3.one * (bufferMeters * 2f);

        if (bufferMeters >= 0f)
        {
            bounds.Expand(expandAmount);
            return bounds;
        }

        Vector3 nextSize = bounds.size + expandAmount;
        nextSize.x = Mathf.Max(0.01f, nextSize.x);
        nextSize.y = Mathf.Max(0.01f, nextSize.y);
        nextSize.z = Mathf.Max(0.01f, nextSize.z);
        bounds.size = nextSize;
        return bounds;
    }

    private static void DrawBufferedBounds(Bounds sourceBounds, float bufferMeters, Color color)
    {
        Bounds drawBounds = sourceBounds;
        Vector3 expandAmount = Vector3.one * (bufferMeters * 2f);

        if (bufferMeters >= 0f)
        {
            drawBounds.Expand(expandAmount);
        }
        else
        {
            Vector3 nextSize = drawBounds.size + expandAmount;
            nextSize.x = Mathf.Max(0.01f, nextSize.x);
            nextSize.y = Mathf.Max(0.01f, nextSize.y);
            nextSize.z = Mathf.Max(0.01f, nextSize.z);
            drawBounds.size = nextSize;
        }

        Gizmos.color = color;
        Gizmos.DrawWireCube(drawBounds.center, drawBounds.size);
    }
}
