using System;
using UnityEngine;

/// <summary>
/// 基于尼基金公式的履带车移动控制器。
/// 旧的状态机式转向/加减速逻辑已移除，改为 FixedUpdate 内的功率预算分配。
/// </summary>
public partial class TankMoveController : MonoBehaviour
{
    public static TankMoveController Instance { get; private set; }

    [Header("引用参数")]
    [SerializeField] private GameObject tanker;// 载具主体对象，通常是坦克模型的根节点
    [SerializeField] private TankMoveData tankMoveData;

    [Header("物理采样")]
    [SerializeField] private LayerMask groundLayerMask = ~0;
    [SerializeField] private float groundProbeHeight = 1.5f;
    [SerializeField] private float groundProbeDistance = 4f;
    [SerializeField] private float groundProbeRadius = 0.45f;

    [Header("运行时阻尼")]
    [SerializeField] private float movingLinearDamping = 0.08f;
    [SerializeField] private float idleLinearDamping = 0.75f;

    [Header("兜底参数")]
    [SerializeField] private float fallbackGroundFrictionCoefficient = 1f;
    [SerializeField] private float rollingResistanceScale = 0.03f;
    [SerializeField] private float maxRollingResistanceCoefficient = 0.05f;

    [Header("载具DNA (Vehicle DNA)")]
    [Tooltip("重心高度 (h)，决定翻滚力矩的杠杆臂")]
    [SerializeField] private float cogHeight = 1.0f;
    [Tooltip("履带/轮距中心距 (T)")]
    [SerializeField] private float trackWidth = 3.0f;
    [Tooltip("侧滑摩擦力乘数 (mu_y 增益)")]
    [SerializeField] private float lateralFrictionMultiplier = 2.5f;

    [Header("未来地形扩展")]
    [SerializeField] private bool useTerrainImportWhenAvailable = true;

    [Header("转向调试")]
    [SerializeField] private bool showTurnCenterGizmo = true;
    [SerializeField] private Color turnCenterGizmoColor = Color.magenta;
    [SerializeField] private Color turnCenterLineColor = Color.white;

    [Header("左右履带驱动点")]
    [SerializeField] private TankTrackSideDrivePoint leftTrackDrivePoint;
    [SerializeField] private TankTrackSideDrivePoint rightTrackDrivePoint;

    public event Action<float> OnSpeedChanged;

    public float CurrentSpeed
    {
        get
        {
            if (tankRigidbody == null)
            {
                return _cachedForwardSpeed;
            }

            Vector3 groundNormal = _groundContact.HasHit ? _groundContact.Normal : Vector3.up;
            Vector3 forwardAxis = GetGroundProjectedForward(groundNormal);
            return Vector3.Dot(tankRigidbody.linearVelocity, forwardAxis);
        }
    }

    public float GetDifferentialTrackSpeed(bool isLeftTrack)
    {
        if (tankRigidbody == null)
        {
            return 0f;
        }

        Vector3 groundNormal = _groundContact.HasHit ? _groundContact.Normal : Vector3.up;
        Vector3 forwardAxis = GetGroundProjectedForward(groundNormal);
        float forwardSpeed = Vector3.Dot(tankRigidbody.linearVelocity, forwardAxis);

        Vector3 localAngularVelocity = tanker != null
            ? tanker.transform.InverseTransformDirection(tankRigidbody.angularVelocity)
            : tankRigidbody.angularVelocity;

        float yawRate = localAngularVelocity.y;
        float halfTrackWidth = Mathf.Max(0.01f, GetTrackCenterDistance() * 0.5f);
        float differentialComponent = yawRate * halfTrackWidth;

        return forwardSpeed + (isLeftTrack ? -differentialComponent : differentialComponent);
    }

    private float GetTrackCenterDistance()
    {
        if (tankMoveData != null && tankMoveData.TrackCenterDistance > 0f)
        {
            return tankMoveData.TrackCenterDistance;
        }

        return Mathf.Max(0.01f, trackWidth);
    }

    private float GetTrackContactLength()
    {
        if (tankMoveData != null && tankMoveData.TrackContactLength > 0f)
        {
            return tankMoveData.TrackContactLength;
        }

        return 4.5f;
    }

    private void CacheTrackDrivePoints()
    {
        TankTrackSideDrivePoint[] drivePoints = GetComponentsInChildren<TankTrackSideDrivePoint>(true);
        foreach (TankTrackSideDrivePoint drivePoint in drivePoints)
        {
            if (drivePoint == null)
            {
                continue;
            }

            if (drivePoint.Side == TankTrackSideDrivePoint.TrackSide.Left)
            {
                leftTrackDrivePoint = drivePoint;
            }
            else if (drivePoint.Side == TankTrackSideDrivePoint.TrackSide.Right)
            {
                rightTrackDrivePoint = drivePoint;
            }
        }
    }

    private bool TryGetTurnCenterPoint(float turnInput, out Vector3 turnCenterPoint)
    {
        turnCenterPoint = default;

        if (tanker == null || Mathf.Abs(turnInput) < 0.001f)
        {
            return false;
        }

        float halfTrackDistance = Mathf.Max(0.01f, GetTrackCenterDistance() * 0.5f);
        Vector3 lateralOffset = tanker.transform.right * Mathf.Sign(turnInput) * halfTrackDistance;
        turnCenterPoint = tanker.transform.position + lateralOffset - tanker.transform.up * cogHeight;
        return true;
    }

    private void OnDrawGizmos()
    {
        if (!showTurnCenterGizmo || tanker == null)
        {
            return;
        }

        if (!_hasDebugTurnCenterPoint)
        {
            return;
        }

        Vector3 tankCenter = tanker.transform.position - tanker.transform.up * cogHeight;
        Gizmos.color = turnCenterLineColor;
        Gizmos.DrawLine(tankCenter, _debugTurnCenterPoint);

        Gizmos.color = turnCenterGizmoColor;
        Gizmos.DrawWireSphere(_debugTurnCenterPoint, 0.2f);
    }

    private Rigidbody tankRigidbody;
    private float _cachedForwardSpeed;
    private GroundContactInfo _groundContact;
    private bool _hasLoggedSetupError;
    private float _debugForwardInput;
    private float _debugTurnInput;
    private Vector3 _debugTurnCenterPoint;
    private bool _hasDebugTurnCenterPoint;
    private bool _runtimeStateInitialized;

    private struct GroundContactInfo
    {
        public bool HasHit;
        public Vector3 Normal;
        public float SlopeAngle;
        public float FrictionCoefficient;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }

            return;
        }

        Instance = this;
        InitializePowerState();
        InitializeAudioHooks();
        CacheRigidbody();
        CacheTrackDrivePoints();
    }

    private void Start()
    {
        EnsureRuntimeStateInitialized();
    }

    private void OnDestroy()
    {
        CleanupAudioHooks();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void FixedUpdate()
    {
        if (!ValidateSetup())
        {
            return;
        }

        SampleGroundContact();
        SimulatePowerSplit();
        RefreshSpeedEvent();
        UpdateEngineAudioStateMachine();
    }

    public bool EnsureRuntimeStateInitialized()
    {
        if (_runtimeStateInitialized)
        {
            return true;
        }

        if (!ValidateSetup())
        {
            return false;
        }

        CacheTrackDrivePoints();
        ConfigureRuntimeMass();
        RefreshSpeedEvent();
        _runtimeStateInitialized = true;
        return true;
    }


    private void RefreshSpeedEvent()
    {
        _cachedForwardSpeed = CurrentSpeed;
        OnSpeedChanged?.Invoke(_cachedForwardSpeed);
    }

    private float GetForwardSpeedLimit()
    {
        return ResolveTerrainAdjustedSpeedLimit(tankMoveData != null ? tankMoveData.MoveMaxSpeed : 0f, true);
    }

    private float GetBackwardSpeedLimit()
    {
        return ResolveTerrainAdjustedSpeedLimit(tankMoveData != null ? tankMoveData.BackMoveMaxSpeed : 0f, false);
    }

    private float ResolveTerrainAdjustedSpeedLimit(float baseLimit, bool isForwardLimit)
    {
        float clampedBaseLimit = Mathf.Max(0f, baseLimit);

        if (!useTerrainImportWhenAvailable)
        {
            return clampedBaseLimit;
        }

        // 未来地形导入点：
        // 这里可以接入地形材质、坡度、湿滑程度、泥地阻力等信息，动态调低/调高速度上限。
        // 当前没有地形系统时，直接返回 SO 中定义的基础上限。
        _ = isForwardLimit;
        return clampedBaseLimit;
    }



}

