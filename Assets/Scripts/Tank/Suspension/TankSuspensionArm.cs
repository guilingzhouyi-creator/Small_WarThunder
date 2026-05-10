using UnityEngine;

public class TankSuspensionArm : MonoBehaviour
{
    [SerializeField] private Quaternion authoredRestLocalRotation = Quaternion.identity;
    [Header("引用")]
    public Transform wheelPivot;
    public Transform groundCheckPoint;

    [Header("悬挂参数")]
    [Tooltip("静止预压量，必须 < maxCompression，推荐 0.6~0.7 倍 maxCompression")]
    public float restHeight = 0.25f;
    public float springStrength = 8500f;
    public float damperStrength = 900f;
    public float maxCompression = 0.38f;
    public float maxExtension = 0.5f;
    public float wheelRadius = 0.85f;
    public float probeExtraLength = 0.2f;
    public LayerMask groundMask = ~0;

    [Header("主悬挂旋转限制")]
    public Vector3 suspensionRotationAxis = new Vector3(1f, 0f, 0f);
    public float angleMultiplier = 40f;
    public float extensionAngleMultiplier = 110f;
    public float minAngle = -55f;
    public float maxAngle = 22f;
    public float rotationLerpSpeed = 20f;
    public float rotationSmoothTime = 0.06f;

    private readonly RaycastHit[] _probeHits = new RaycastHit[8];

    private Quaternion _restRotation;
    private Rigidbody _rb;
    private RaycastHit _lastHit;
    private float _currentCompression;
    private float _lastCompression;
    private float _currentVisualAngle;
    private float _visualAngularVelocity;
    private float _visualAngleSign = 1f;
    private float _restCenterDistance;

    public float CurrentCompression => _currentCompression;
    public bool IsGrounded { get; private set; }
    // Authored rest pose defines the visual baseline. restHeight adds suspension preload on top of it.
    public float ProbeLength => _restCenterDistance + restHeight + maxCompression + maxExtension + probeExtraLength;
    public float TargetWheelCenterDistance => _restCenterDistance + restHeight;
    public Vector3 ProbeOrigin => GetProbeOrigin();
    public Vector3 SuspensionUp => GetSuspensionUpDirection();
    public Vector3 LastGroundNormal => IsGrounded ? _lastHit.normal : Vector3.up;
    public Vector3 GroundHitPoint => IsGrounded
        ? _lastHit.point
        : GetWheelCenterWorldPosition() - Vector3.up * wheelRadius;

    private void Reset()
    {
        CaptureAuthoredRestRotation();
        AutoResolveReferences();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            CaptureAuthoredRestRotation();
        }

        AutoResolveReferences();
        SanitizeValues();
    }

    private void Awake()
    {
        AutoResolveReferences();
        SanitizeValues();
        InitializeRuntimeState();
    }

    private void Start()
    {
        InitializeRuntimeState();
    }

    public void AutoResolveReferences()
    {
        if (wheelPivot == null)
        {
            WheelPivotMarker wheelPivotMarker = GetComponentInChildren<WheelPivotMarker>(true);
            if (wheelPivotMarker != null)
            {
                wheelPivot = wheelPivotMarker.transform;
            }
        }

        if (groundCheckPoint == null)
        {
            groundCheckPoint = transform;
        }
    }

    public void ApplySharedSettings(
        float sharedRestHeight,
        float sharedSpringStrength,
        float sharedDamperStrength,
        float sharedMaxCompression,
        float sharedMaxExtension,
        float sharedWheelRadius,
        float sharedProbeExtraLength,
        LayerMask sharedGroundMask,
        Vector3 sharedRotationAxis,
        float sharedAngleMultiplier,
        float sharedExtensionAngleMultiplier,
        float sharedMinAngle,
        float sharedMaxAngle,
        float sharedRotationLerpSpeed,
        float sharedRotationSmoothTime)
    {
        restHeight = sharedRestHeight;
        springStrength = sharedSpringStrength;
        damperStrength = sharedDamperStrength;
        maxCompression = sharedMaxCompression;
        maxExtension = sharedMaxExtension;
        wheelRadius = sharedWheelRadius;
        probeExtraLength = sharedProbeExtraLength;
        groundMask = sharedGroundMask;
        suspensionRotationAxis = sharedRotationAxis;
        angleMultiplier = sharedAngleMultiplier;
        extensionAngleMultiplier = sharedExtensionAngleMultiplier;
        minAngle = sharedMinAngle;
        maxAngle = sharedMaxAngle;
        rotationLerpSpeed = sharedRotationLerpSpeed;
        rotationSmoothTime = sharedRotationSmoothTime;

        SanitizeValues();
    }

    private void FixedUpdate()
    {
        EnsureRuntimeReferences();
        EvaluateSuspension();
        UpdateVisualPose(Time.fixedDeltaTime);
        _lastCompression = _currentCompression;
    }

    private void InitializeRuntimeState()
    {
        EnsureRuntimeReferences();
        _restRotation = authoredRestLocalRotation;
        _visualAngleSign = ResolveVisualAngleSign();
        _restCenterDistance = ResolveRestCenterDistance();
        _currentVisualAngle = 0f;
        _visualAngularVelocity = 0f;

        // 提前探测初始压缩量，让 _lastCompression 有正确初值
        // 否则第一帧 compressionVelocity = (real - 0) / dt 产生尖峰，导致起跳
        Vector3 origin = GetProbeOrigin();
        Vector3 direction = -GetSuspensionUpDirection();
        if (TryProbeGround(origin, direction, out RaycastHit hit))
        {
            float compression = TargetWheelCenterDistance - hit.distance;
            _currentCompression = Mathf.Clamp(compression, -maxExtension, maxCompression);
        }
        else
        {
            _currentCompression = -maxExtension;
        }
        _lastCompression = _currentCompression;
    }

    private void EnsureRuntimeReferences()
    {
        if (_rb == null)
        {
            _rb = GetComponentInParent<Rigidbody>();
        }
    }

    private void EvaluateSuspension()
    {
        Vector3 origin = GetProbeOrigin();
        Vector3 upDirection = GetSuspensionUpDirection();
        Vector3 direction = -upDirection;

        if (TryProbeGround(origin, direction, out RaycastHit hit))
        {
            _lastHit = hit;
            IsGrounded = true;

            float compression = TargetWheelCenterDistance - hit.distance;
            _currentCompression = Mathf.Clamp(compression, -maxExtension, maxCompression);

            // 用刚体实际速度在悬挂方向的分量计算阻尼，完全绕开压缩量有限差分法的钳制失真问题
            // dot > 0 = 向上运动（拉伸）；dot < 0 = 向下运动（压缩）
            float suspensionVelocity = (_rb != null)
                ? Vector3.Dot(_rb.GetPointVelocity(transform.position), upDirection)
                : 0f;

            // 弹簧力单独钳制；阻尼力独立叠加，不受 springStrength 区间限制
            float springForce = Mathf.Clamp(
                _currentCompression * springStrength,
                -springStrength * maxExtension,
                springStrength * maxCompression);
            float damperForce = -suspensionVelocity * damperStrength;
            float totalForce = springForce + damperForce;

            if (_rb != null)
            {
                _rb.AddForceAtPosition(upDirection * totalForce, transform.position, ForceMode.Force);
            }
        }
        else
        {
            IsGrounded = false;
            _currentCompression = -maxExtension;
        }
    }

    private void UpdateVisualPose(float deltaTime)
    {
        float targetAngle = ResolveVisualTargetAngle();
        float smoothedAngle = Mathf.SmoothDampAngle(
            _currentVisualAngle,
            targetAngle,
            ref _visualAngularVelocity,
            rotationSmoothTime,
            rotationLerpSpeed <= 0f ? Mathf.Infinity : rotationLerpSpeed * 10f,
            deltaTime);

        _currentVisualAngle = smoothedAngle;
        transform.localRotation = _restRotation * Quaternion.AngleAxis(_currentVisualAngle, suspensionRotationAxis.normalized);
    }

    private float ResolveVisualTargetAngle()
    {
        if (!IsGrounded)
        {
            return minAngle;
        }

        float deltaAngle = _currentCompression >= 0f
            ? _currentCompression * angleMultiplier
            : _currentCompression * extensionAngleMultiplier;

        deltaAngle *= _visualAngleSign;
        return Mathf.Clamp(deltaAngle, minAngle, maxAngle);
    }

    private bool TryProbeGround(Vector3 origin, Vector3 direction, out RaycastHit bestHit)
    {
        bestHit = default;
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            wheelRadius,
            direction,
            _probeHits,
            ProbeLength,
            groundMask,
            QueryTriggerInteraction.Ignore);

        float bestDistance = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _probeHits[i];
            if (!IsValidGroundHit(hit))
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestHit = hit;
            }
        }

        return bestDistance < float.MaxValue;
    }

    private bool IsValidGroundHit(RaycastHit hit)
    {
        if (hit.collider == null)
        {
            return false;
        }

        Transform root = _rb != null ? _rb.transform : transform.root;
        if (hit.rigidbody != null && hit.rigidbody == _rb)
        {
            return false;
        }

        return !hit.collider.transform.IsChildOf(root);
    }

    private Vector3 GetProbeOrigin()
    {
        if (groundCheckPoint != null && groundCheckPoint != wheelPivot)
        {
            return groundCheckPoint.position;
        }

        return transform.position;
    }

    private Vector3 GetWheelCenterWorldPosition()
    {
        return wheelPivot != null ? wheelPivot.position : GetProbeOrigin();
    }

    private float ResolveVisualAngleSign()
    {
        if (wheelPivot == null)
        {
            return 1f;
        }

        Vector3 localOffset = transform.InverseTransformPoint(wheelPivot.position);
        if (localOffset.sqrMagnitude < 0.0001f)
        {
            return 1f;
        }

        Vector3 axis = suspensionRotationAxis.sqrMagnitude > 0.0001f
            ? suspensionRotationAxis.normalized
            : Vector3.right;

        Vector3 localVehicleUp = transform.parent != null
            ? transform.parent.InverseTransformDirection(GetSuspensionUpDirection()).normalized
            : Vector3.up;

        Vector3 rotatedOffset = Quaternion.AngleAxis(5f, axis) * localOffset;
        float baseHeight = Vector3.Dot(localOffset, localVehicleUp);
        float rotatedHeight = Vector3.Dot(rotatedOffset, localVehicleUp);

        return rotatedHeight >= baseHeight ? 1f : -1f;
    }

    private float ResolveRestCenterDistance()
    {
        if (wheelPivot == null)
        {
            return restHeight + wheelRadius;
        }

        Vector3 downDirection = -GetSuspensionUpDirection();
        Vector3 offset = wheelPivot.position - transform.position;
        float projectedDistance = Vector3.Dot(offset, downDirection.normalized);
        return Mathf.Max(0.05f, projectedDistance);
    }

    private Vector3 GetSuspensionUpDirection()
    {
        if (_rb != null)
        {
            return _rb.transform.up;
        }

        if (transform.parent != null)
        {
            return transform.parent.up;
        }

        return Vector3.up;
    }

    private void CaptureAuthoredRestRotation()
    {
        authoredRestLocalRotation = transform.localRotation;
    }

    private void SanitizeValues()
    {
        springStrength = Mathf.Max(0f, springStrength);
        damperStrength = Mathf.Max(0f, damperStrength);
        maxCompression = Mathf.Max(0.01f, maxCompression);
        maxExtension = Mathf.Max(0.01f, maxExtension);
        wheelRadius = Mathf.Max(0.01f, wheelRadius);
        probeExtraLength = Mathf.Max(0f, probeExtraLength);
        rotationLerpSpeed = Mathf.Max(0f, rotationLerpSpeed);
        rotationSmoothTime = Mathf.Max(0.001f, rotationSmoothTime);
    }
}
