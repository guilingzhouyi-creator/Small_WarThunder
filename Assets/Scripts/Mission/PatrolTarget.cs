using System;
using UnityEngine;
using NAI;

/// <summary>
/// 物理驱动巡逻靶标车：沿固定路径点循环移动，通过 AI_MotionDriver 实现履带物理+悬挂+地形响应。
/// 挂载在敌人坦克根对象上，配合 EnemyMarker + GeneralHitPosition + TargetDamageResolver 使用。
/// 
/// 路径配置方式（二选一）：
/// 1. 拖入 _waypointParent → 自动扫描所有直接子物体，按 sibling index 排序
/// 2. 留空 _waypointParent → 手动填 _waypoints 数组
/// 
/// 兼容性：若 AI_MotionDriver 未挂载，降级为 transform.position 直线移动（原行为）。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PatrolTarget : MonoBehaviour
{
    [Header("路径父级（自动扫描子物体，留空则用手动数组）")]
    [SerializeField] private Transform _waypointParent;

    [Header("手动路径点（_waypointParent 为空时生效）")]
    [SerializeField] private Transform[] _waypoints;

    [Header("移动速度 (m/s) — 驱动层输入量")]
    [SerializeField] private float _speed = 0.5f;

    [Header("到达路径点的判定距离")]
    [SerializeField] private float _arriveDistance = 2f;

    [Header("转向速度系数（越高越快对准路径点）")]
    [SerializeField] private float _turnSharpness = 1f;

    [Header("路径点循环模式")]
    [SerializeField] private WaypointLoopMode _loopMode = WaypointLoopMode.Loop;

    [Header("Gizmos 可视化")]
    [SerializeField] private Color _gizmoColor = new Color(1f, 0.3f, 0.1f, 0.8f);
    [SerializeField] private float _waypointRadius = 1f;
    [SerializeField] private bool _showLabels = true;

    public enum WaypointLoopMode { Loop, PingPong, StopAtEnd }

    private int _currentIndex;
    private int _pingPongDirection = 1;
    private AI_MotionDriver _motionDriver;
    private Rigidbody _rigidbody;
    private bool _usePhysicsDrive;

    private Transform[] GetEffectiveWaypoints()
    {
        if (_waypointParent != null)
        {
            Transform[] children = new Transform[_waypointParent.childCount];
            for (int i = 0; i < _waypointParent.childCount; i++)
                children[i] = _waypointParent.GetChild(i);
            return children;
        }
        return _waypoints ?? System.Array.Empty<Transform>();
    }

    private void Start()
    {
        _motionDriver = GetComponent<AI_MotionDriver>();
        _rigidbody = GetComponent<Rigidbody>();
        _usePhysicsDrive = _motionDriver != null;

        Transform[] waypoints = GetEffectiveWaypoints();
        if (waypoints.Length == 0)
        {
            Debug.LogWarning($"[PatrolTarget] {name} 没有设置路径点，将停留在原地", this);
            enabled = false;
            return;
        }

        if (!_usePhysicsDrive)
            Debug.LogWarning($"[PatrolTarget] {name} 缺少 AI_MotionDriver，降级为 Transform 直线移动", this);
    }

    private void FixedUpdate()
    {
        Transform[] waypoints = GetEffectiveWaypoints();
        if (waypoints.Length == 0) return;

        if (waypoints[_currentIndex] == null)
        {
            AdvanceWaypoint();
            return;
        }

        Transform targetWp = waypoints[_currentIndex];
        Vector3 flatTarget = new Vector3(targetWp.position.x, transform.position.y, targetWp.position.z);
        Vector3 direction = flatTarget - transform.position;
        float dist = direction.magnitude;

        if (_usePhysicsDrive)
        {
            // 物理驱动模式
            float forwardInput = Mathf.Clamp(dist / (_arriveDistance * 2f), 0.1f, 1f) * _speed;

            Vector3 flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            Vector3 flatDir = direction.normalized;
            float rawAngle = Vector3.SignedAngle(flatForward, flatDir, Vector3.up);
            float turnInput = Mathf.Clamp(rawAngle / 45f * _turnSharpness, -1f, 1f);

            _motionDriver.SetMoveInput(forwardInput, turnInput);

            if (dist <= _arriveDistance)
                AdvanceWaypoint();
        }
        else
        {
            // 降级模式：Transform 直线移动
            float step = _speed * Time.fixedDeltaTime * 10f; // 缩放以匹配原行为
            if (dist <= _arriveDistance || step >= dist)
            {
                transform.position = flatTarget;
                AdvanceWaypoint();
            }
            else
            {
                transform.position += direction.normalized * step;
            }
        }
    }

    private void AdvanceWaypoint()
    {
        Transform[] waypoints = GetEffectiveWaypoints();
        switch (_loopMode)
        {
            case WaypointLoopMode.Loop:
                _currentIndex = (_currentIndex + 1) % waypoints.Length;
                break;
            case WaypointLoopMode.PingPong:
                _currentIndex += _pingPongDirection;
                if (_currentIndex >= waypoints.Length)
                {
                    _currentIndex = waypoints.Length - 2;
                    _pingPongDirection = -1;
                }
                else if (_currentIndex < 0)
                {
                    _currentIndex = 1;
                    _pingPongDirection = 1;
                }
                break;
            case WaypointLoopMode.StopAtEnd:
                if (_currentIndex < waypoints.Length - 1)
                    _currentIndex++;
                else
                    enabled = false;
                break;
        }
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
                if (_loopMode == WaypointLoopMode.PingPong && i < waypoints.Length - 2)
                    Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[waypoints.Length - 2].position);
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
