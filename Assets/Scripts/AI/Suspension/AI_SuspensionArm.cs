using UnityEngine;

public class AI_SuspensionArm : MonoBehaviour
{
    [SerializeField] private Quaternion _authoredRestLocalRotation = Quaternion.identity;

    [Header("引用")]
    public Transform wheelPivot;
    public Transform groundCheckPoint;

    [Header("悬挂参数")]
    [Tooltip("静止预压量，必须 < maxCompression，推荐 0.6~0.7 倍 maxCompression")]
    [SerializeField] private float _restLength = 0.25f;
    [SerializeField] private float _springStrength = 8500f;
    [SerializeField] private float _damperStrength = 900f;
    [SerializeField] private float _maxCompression = 0.38f;
    [SerializeField] private float _maxExtension = 0.5f;
    [SerializeField] private float _wheelRadius = 0.85f;
    [SerializeField] private float _probeExtraLength = 0.2f;
    [SerializeField] private LayerMask _groundMask = ~0;

    [Header("主悬挂旋转限制")]
    [SerializeField] private Vector3 _suspensionRotationAxis = new Vector3(1f, 0f, 0f);
    [SerializeField] private float _angleMultiplier = 40f;
    [SerializeField] private float _extensionAngleMultiplier = 110f;
    [SerializeField] private float _minAngle = -55f;
    [SerializeField] private float _maxAngle = 22f;
    [SerializeField] private float _rotationLerpSpeed = 20f;
    [SerializeField] private float _rotationSmoothTime = 0.06f;

    private readonly RaycastHit[] _probeHits = new RaycastHit[8];

    private Quaternion _restRotation;
    private Rigidbody _rigidbody;
    private RaycastHit _lastHit;
    private float _currentCompression;
    private float _lastCompression;
    private float _compressionVelocity;
    private float _currentVisualAngle;
    private float _visualAngularVelocity;
    private float _visualAngleSign = 1f;
    private float _restCenterDistance;

    public float CurrentCompression => _currentCompression;
    public float Compression => _currentCompression;
    public float CompressionVelocity => _compressionVelocity;
    public bool IsGrounded { get; private set; }
    public float ProbeLength => _restCenterDistance + _restLength + _maxCompression + _maxExtension + _probeExtraLength;
    public float RestLength => TargetWheelCenterDistance;
    public float TargetWheelCenterDistance => _restCenterDistance + _restLength;
    public Vector3 ProbeOrigin => GetProbeOrigin();
    public Vector3 SuspensionUp => GetSuspensionUpDirection();
    public Vector3 LastGroundNormal => IsGrounded ? _lastHit.normal : Vector3.up;
    public Vector3 GroundHitPoint => IsGrounded
        ? _lastHit.point
        : GetWheelCenterWorldPosition() - Vector3.up * _wheelRadius;
    public Vector3 HitPoint => GroundHitPoint;
    public Vector3 HitNormal => LastGroundNormal;

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
        float sharedRestLength,
        float sharedSpringStrength,
        float sharedDamperStrength,
        float sharedMaxCompression,
        float sharedMaxExtension,
        float sharedWheelRadius,
        LayerMask sharedGroundMask)
    {
        ApplySharedSettings(
            sharedRestLength,
            sharedSpringStrength,
            sharedDamperStrength,
            sharedMaxCompression,
            sharedMaxExtension,
            sharedWheelRadius,
            _probeExtraLength,
            sharedGroundMask,
            _suspensionRotationAxis,
            _angleMultiplier,
            _extensionAngleMultiplier,
            _minAngle,
            _maxAngle,
            _rotationLerpSpeed,
            _rotationSmoothTime);
    }

    public void ApplySharedSettings(
        float sharedRestLength,
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
        _restLength = sharedRestLength;
        _springStrength = sharedSpringStrength;
        _damperStrength = sharedDamperStrength;
        _maxCompression = sharedMaxCompression;
        _maxExtension = sharedMaxExtension;
        _wheelRadius = sharedWheelRadius;
        _probeExtraLength = sharedProbeExtraLength;
        _groundMask = sharedGroundMask;
        _suspensionRotationAxis = sharedRotationAxis;
        _angleMultiplier = sharedAngleMultiplier;
        _extensionAngleMultiplier = sharedExtensionAngleMultiplier;
        _minAngle = sharedMinAngle;
        _maxAngle = sharedMaxAngle;
        _rotationLerpSpeed = sharedRotationLerpSpeed;
        _rotationSmoothTime = sharedRotationSmoothTime;

        SanitizeValues();
    }

    private void FixedUpdate()
    {
        EnsureRuntimeReferences();
        EvaluateSuspension();
        UpdateVisualPose(Time.fixedDeltaTime);
        _compressionVelocity = Time.fixedDeltaTime > 0f
            ? (_currentCompression - _lastCompression) / Time.fixedDeltaTime
            : 0f;
        _lastCompression = _currentCompression;
    }

    private void InitializeRuntimeState()
    {
        EnsureRuntimeReferences();
        _restRotation = _authoredRestLocalRotation;
        _visualAngleSign = ResolveVisualAngleSign();
        _restCenterDistance = ResolveRestCenterDistance();
        _currentVisualAngle = 0f;
        _visualAngularVelocity = 0f;

        Vector3 origin = GetProbeOrigin();
        Vector3 direction = -GetSuspensionUpDirection();
        if (TryProbeGround(origin, direction, out RaycastHit hit))
        {
            float compression = TargetWheelCenterDistance - hit.distance;
            _currentCompression = Mathf.Clamp(compression, -_maxExtension, _maxCompression);
        }
        else
        {
            _currentCompression = -_maxExtension;
        }

        _lastCompression = _currentCompression;
        _compressionVelocity = 0f;
    }

    private void EnsureRuntimeReferences()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponentInParent<Rigidbody>();
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
            _currentCompression = Mathf.Clamp(compression, -_maxExtension, _maxCompression);

            float suspensionVelocity = _rigidbody != null
                ? Vector3.Dot(_rigidbody.GetPointVelocity(transform.position), upDirection)
                : 0f;

            float springForce = Mathf.Clamp(
                _currentCompression * _springStrength,
                -_springStrength * _maxExtension,
                _springStrength * _maxCompression);
            float damperForce = -suspensionVelocity * _damperStrength;
            float totalForce = springForce + damperForce;

            if (_rigidbody != null)
            {
                _rigidbody.AddForceAtPosition(upDirection * totalForce, transform.position, ForceMode.Force);
            }
        }
        else
        {
            IsGrounded = false;
            _currentCompression = -_maxExtension;
        }
    }

    private void UpdateVisualPose(float deltaTime)
    {
        float targetAngle = ResolveVisualTargetAngle();
        float smoothedAngle = Mathf.SmoothDampAngle(
            _currentVisualAngle,
            targetAngle,
            ref _visualAngularVelocity,
            _rotationSmoothTime,
            _rotationLerpSpeed <= 0f ? Mathf.Infinity : _rotationLerpSpeed * 10f,
            deltaTime);

        _currentVisualAngle = smoothedAngle;
        Vector3 rotationAxis = _suspensionRotationAxis.sqrMagnitude > 0.0001f
            ? _suspensionRotationAxis.normalized
            : Vector3.right;
        transform.localRotation = _restRotation * Quaternion.AngleAxis(_currentVisualAngle, rotationAxis);
    }

    private float ResolveVisualTargetAngle()
    {
        if (!IsGrounded)
        {
            return _minAngle;
        }

        float deltaAngle = _currentCompression >= 0f
            ? _currentCompression * _angleMultiplier
            : _currentCompression * _extensionAngleMultiplier;

        deltaAngle *= _visualAngleSign;
        return Mathf.Clamp(deltaAngle, _minAngle, _maxAngle);
    }

    private bool TryProbeGround(Vector3 origin, Vector3 direction, out RaycastHit bestHit)
    {
        bestHit = default;
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            _wheelRadius,
            direction,
            _probeHits,
            ProbeLength,
            _groundMask,
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

        Transform root = _rigidbody != null ? _rigidbody.transform : transform.root;
        if (hit.rigidbody != null && hit.rigidbody == _rigidbody)
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

        Vector3 axis = _suspensionRotationAxis.sqrMagnitude > 0.0001f
            ? _suspensionRotationAxis.normalized
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
            return _restLength + _wheelRadius;
        }

        Vector3 downDirection = -GetSuspensionUpDirection();
        Vector3 offset = wheelPivot.position - transform.position;
        float projectedDistance = Vector3.Dot(offset, downDirection.normalized);
        return Mathf.Max(0.05f, projectedDistance);
    }

    private Vector3 GetSuspensionUpDirection()
    {
        if (_rigidbody != null)
        {
            return _rigidbody.transform.up;
        }

        if (transform.parent != null)
        {
            return transform.parent.up;
        }

        return Vector3.up;
    }

    private void CaptureAuthoredRestRotation()
    {
        _authoredRestLocalRotation = transform.localRotation;
    }

    private void SanitizeValues()
    {
        _springStrength = Mathf.Max(0f, _springStrength);
        _damperStrength = Mathf.Max(0f, _damperStrength);
        _maxCompression = Mathf.Max(0.01f, _maxCompression);
        _maxExtension = Mathf.Max(0.01f, _maxExtension);
        _wheelRadius = Mathf.Max(0.01f, _wheelRadius);
        _probeExtraLength = Mathf.Max(0f, _probeExtraLength);
        _rotationLerpSpeed = Mathf.Max(0f, _rotationLerpSpeed);
        _rotationSmoothTime = Mathf.Max(0.001f, _rotationSmoothTime);
    }
}
