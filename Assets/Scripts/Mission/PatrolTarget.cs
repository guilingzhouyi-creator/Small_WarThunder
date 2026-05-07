using UnityEngine;

/// <summary>
/// 教程关卡巡逻靶标车：沿固定路径点循环移动。
/// 挂载在敌人坦克根对象上，配合 EnemyMarker + GeneralHitPosition + TargetDamageResolver 使用。
/// 
/// 路径配置方式（二选一）：
/// 1. 拖入 _waypointParent → 自动扫描所有直接子物体，按 sibling index 排序
/// 2. 留空 _waypointParent → 手动填 _waypoints 数组
/// </summary>
public class PatrolTarget : MonoBehaviour
{
    [Header("路径父级（自动扫描子物体，留空则用手动数组）")]
    [SerializeField] private Transform _waypointParent;

    [Header("手动路径点（_waypointParent 为空时生效）")]
    [SerializeField] private Transform[] _waypoints;

    [Header("移动速度 (m/s)")]
    [SerializeField] private float _speed = 5f;

    [Header("到达路径点的判定距离")]
    [SerializeField] private float _arriveDistance = 1f;

    [Header("Gizmos 可视化")]
    [SerializeField] private Color _gizmoColor = new Color(1f, 0.3f, 0.1f, 0.8f);
    [SerializeField] private float _waypointRadius = 1f;
    [SerializeField] private bool _showLabels = true;

    private int _currentIndex;

    /// <summary>
    /// 从 _waypointParent 或 _waypoints 解析当前有效的路径点列表。
    /// _waypointParent 优先：扫描所有直接子物体的 Transform，按 sibling index 排序。
    /// </summary>
    private Transform[] GetEffectiveWaypoints()
    {
        if (_waypointParent != null)
        {
            Transform[] children = new Transform[_waypointParent.childCount];
            for (int i = 0; i < _waypointParent.childCount; i++)
            {
                children[i] = _waypointParent.GetChild(i);
            }
            return children;
        }

        return _waypoints ?? System.Array.Empty<Transform>();
    }

    private void Start()
    {
        Transform[] waypoints = GetEffectiveWaypoints();
        if (waypoints.Length == 0)
        {
            Debug.LogWarning($"[PatrolTarget] {name} 没有设置路径点，将停留在原地", this);
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        Transform[] waypoints = GetEffectiveWaypoints();
        if (waypoints.Length == 0) return;

        if (waypoints[_currentIndex] == null)
        {
            AdvanceWaypoint();
            return;
        }

        Transform target = waypoints[_currentIndex];
        Vector3 direction = target.position - transform.position;
        float step = _speed * Time.deltaTime;

        if (direction.magnitude <= _arriveDistance || step >= direction.magnitude)
        {
            transform.position = target.position;
            AdvanceWaypoint();
        }
        else
        {
            transform.position += direction.normalized * step;
        }
    }

    private void AdvanceWaypoint()
    {
        Transform[] waypoints = GetEffectiveWaypoints();
        _currentIndex = (_currentIndex + 1) % waypoints.Length;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Transform[] waypoints = GetEffectiveWaypoints();
        if (waypoints.Length == 0) return;

        Gizmos.color = _gizmoColor;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            Vector3 pos = waypoints[i].position;
            Gizmos.DrawWireSphere(pos, _waypointRadius);

            int next = (i + 1) % waypoints.Length;
            if (waypoints[next] != null)
            {
                Gizmos.DrawLine(pos, waypoints[next].position);
            }

            if (_showLabels)
            {
                UnityEditor.Handles.Label(pos + Vector3.up * (_waypointRadius + 0.5f), $"[{i}]", new GUIStyle
                {
                    normal = new GUIStyleState { textColor = _gizmoColor },
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                });
            }
        }
    }
#endif
}
