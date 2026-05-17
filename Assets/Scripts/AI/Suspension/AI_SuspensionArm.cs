using UnityEngine;

/// <summary>
/// AI 坦克悬挂臂组件
/// 负责：SphereCast 地面探测 + 弹簧-阻尼力计算 + 视觉角度插值
/// 由 AI_SuspensionManager 统一驱动和配置
/// 借鉴自 TankSuspensionArm，但不引用 Tank 文件夹
/// </summary>
public class AI_SuspensionArm : MonoBehaviour
{
    [Header("悬挂臂参数")]
    [SerializeField] private float _restLength = 0.8f;
    [SerializeField] private float _springStrength = 15000f;
    [SerializeField] private float _damperStrength = 2000f;
    [SerializeField] private float _maxCompression = 0.4f;
    [SerializeField] private float _maxExtension = 0.3f;
    [SerializeField] private float _wheelRadius = 0.4f;

    [Header("地面探测")]
    [SerializeField] private LayerMask _groundMask = ~0;
    [SerializeField] private float _castRadius = 0.15f;
    [SerializeField] private float _castDistance = 2f;

    [Header("视觉角度")]
    [SerializeField] private float _angleMultiplier = 40f;
    [SerializeField] private float _extensionAngleMultiplier = 60f;
    [SerializeField] private float _minAngle = -55f;
    [SerializeField] private float _maxAngle = 22f;
    [SerializeField] private float _rotationSmoothTime = 0.06f;
    [SerializeField] private float _rotationMaxSpeed = 400f;

    private Rigidbody _rigidbody;
    private float _currentCompression;
    private float _lastCompression;
    private float _compressionVelocity;
    private float _currentVisualAngle;
    private float _visualAngularVelocity;
    private bool _isGrounded;
    private Vector3 _hitPoint;
    private Vector3 _hitNormal;
    private RaycastHit _lastHit;

    public float Compression => _currentCompression;
    public float CompressionVelocity => _compressionVelocity;
    public bool IsGrounded => _isGrounded;
    public Vector3 HitPoint => _hitPoint;
    public Vector3 HitNormal => _hitNormal;
    public float RestLength => _restLength;

    /// <summary>
    /// 由 AI_SuspensionManager 统一设置悬挂参数
    /// </summary>
    public void ApplySharedSettings(
        float sharedRestLength,
        float sharedSpringStrength,
        float sharedDamperStrength,
        float sharedMaxCompression,
        float sharedMaxExtension,
        float sharedWheelRadius,
        LayerMask sharedGroundMask)
    {
        _restLength = sharedRestLength;
        _springStrength = sharedSpringStrength;
        _damperStrength = sharedDamperStrength;
        _maxCompression = sharedMaxCompression;
        _maxExtension = sharedMaxExtension;
        _wheelRadius = sharedWheelRadius;
        _groundMask = sharedGroundMask;
        _castDistance = _restLength + _maxCompression + _maxExtension + 0.2f;
    }

    private void Awake()
    {
        _rigidbody = GetComponentInParent<Rigidbody>();
        Debug.Log($"[AI_SuspensionArm] {name} 初始化完毕");
    }

    private void Start()
    {
        // 初始压缩量预计算，防止首帧 velocity 尖峰
        Vector3 origin = transform.position;
        Vector3 down = GetSuspensionDown();
        if (Physics.SphereCast(origin, _wheelRadius, down, out RaycastHit hit, _castDistance, _groundMask))
        {
            float compression = GetTargetDistance() - hit.distance;
            _currentCompression = Mathf.Clamp(compression, -_maxExtension, _maxCompression);
        }
        else
        {
            _currentCompression = -_maxExtension;
        }
        _lastCompression = _currentCompression;
    }

    private void FixedUpdate()
    {
        if (_rigidbody == null) return;
        EvaluateSuspension();
    }

    private void LateUpdate()
    {
        UpdateVisualAngle(Time.deltaTime);
    }

    /// <summary>
    /// 核心悬挂评估：SphereCast 探测地面 → 计算弹簧-阻尼力 → 施加到 Rigidbody
    /// </summary>
    private void EvaluateSuspension()
    {
        Vector3 origin = transform.position;
        Vector3 down = GetSuspensionDown();

        if (Physics.SphereCast(origin, _wheelRadius, down, out RaycastHit hit, _castDistance, _groundMask))
        {
            _lastHit = hit;
            _isGrounded = true;
            _hitPoint = hit.point;
            _hitNormal = hit.normal;

            float compression = GetTargetDistance() - hit.distance;
            _currentCompression = Mathf.Clamp(compression, -_maxExtension, _maxCompression);

            // 用刚体实际速度在悬挂方向的分量计算阻尼
            float suspensionVelocity = Vector3.Dot(_rigidbody.GetPointVelocity(transform.position), -down);

            float springForce = Mathf.Clamp(
                _currentCompression * _springStrength,
                -_springStrength * _maxExtension,
                _springStrength * _maxCompression);
            float damperForce = -suspensionVelocity * _damperStrength;
            float totalForce = springForce + damperForce;

            _rigidbody.AddForceAtPosition(-down * totalForce, transform.position, ForceMode.Force);
        }
        else
        {
            _isGrounded = false;
            _currentCompression = -_maxExtension;
            _hitPoint = origin + down * _castDistance;
            _hitNormal = Vector3.up;
        }

        _lastCompression = _currentCompression;
    }

    /// <summary>
    /// 视觉角度插值
    /// </summary>
    private void UpdateVisualAngle(float deltaTime)
    {
        float targetAngle = ResolveTargetAngle();
        _currentVisualAngle = Mathf.SmoothDampAngle(
            _currentVisualAngle,
            targetAngle,
            ref _visualAngularVelocity,
            _rotationSmoothTime,
            _rotationMaxSpeed,
            deltaTime);

        transform.localRotation = Quaternion.AngleAxis(_currentVisualAngle, Vector3.right);
    }

    private float ResolveTargetAngle()
    {
        if (!_isGrounded) return _minAngle;

        float deltaAngle = _currentCompression >= 0f
            ? _currentCompression * _angleMultiplier
            : _currentCompression * _extensionAngleMultiplier;

        return Mathf.Clamp(deltaAngle, _minAngle, _maxAngle);
    }

    private float GetTargetDistance()
    {
        return _restLength + _wheelRadius;
    }

    private Vector3 GetSuspensionDown()
    {
        return _rigidbody != null ? -_rigidbody.transform.up : -Vector3.up;
    }
}
