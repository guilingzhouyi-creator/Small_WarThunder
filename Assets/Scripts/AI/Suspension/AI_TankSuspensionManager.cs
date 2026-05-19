using System.Collections.Generic;
using NGameData.NAIConfigs;
using UnityEngine;

public partial class AI_TankSuspensionManager : MonoBehaviour
{
    [Header("=== AI 统一参数（改这里影响所有悬挂） ===")]
    [Tooltip("静止预压量，必须 < maxCompression，推荐 0.6~0.7 倍 maxCompression")]
    [SerializeField] private float _restLength = 0.25f;
    [SerializeField] private float _springStrength = 8500f;
    [SerializeField] private float _damperStrength = 900f;
    [SerializeField] private float _maxCompression = 0.38f;
    [SerializeField] private float _maxExtension = 0.5f;
    [SerializeField] private float _probeExtraLength = 0.2f;
    [SerializeField] private LayerMask _groundMask = ~0;

    [Header("自动整定")]
    [SerializeField] private bool _autoTuneSpringDamper = true;
    [SerializeField, Range(0.3f, 0.8f)] private float _targetRideCompressionRatio = 0.5f;
    [SerializeField, Range(0.1f, 2f)] private float _damperRatio = 0.9f;

    [Header("主悬挂旋转限制")]
    [SerializeField] private Vector3 _suspensionRotationAxis = new Vector3(1f, 0f, 0f);
    [SerializeField] private float _angleMultiplier = 40f;
    [SerializeField] private float _extensionAngleMultiplier = 110f;
    [SerializeField] private float _minAngle = -55f;
    [SerializeField] private float _maxAngle = 22f;
    [SerializeField] private float _rotationLerpSpeed = 20f;
    [SerializeField] private float _rotationSmoothTime = 0.06f;

    [Header("轮子旋转")]
    [SerializeField] private float _wheelRadius = 0.85f;
    [SerializeField] private Vector3 _wheelRotationAxis = new Vector3(1f, 0f, 0f);
    [SerializeField] private bool _isLeftWheelRotation = true;

    [Header("防翻滚 (Anti-Roll Bar)")]
    [SerializeField] private float _antiRollForce = 5000f;
    [SerializeField] private bool _enableAntiRoll = true;

    [Header("地形法线对齐")]
    [SerializeField] private float _normalAlignStrength = 3000f;
    [SerializeField] private float _normalAlignDamping = 500f;
    [SerializeField] private float _maxAlignAngle = 15f;

    [Header("车身稳定")]
    [SerializeField] private float _pitchStabilization = 2000f;
    [SerializeField] private float _rollStabilization = 3000f;

    [Header("稳定扭矩门槛")]
    [SerializeField] private float _stabilizationWarmupSeconds = 0.35f;
    [SerializeField, Min(2)] private int _minimumGroundedArmsForBodyTorque = 4;

    [Header("调试")]
    [SerializeField] private bool _showDebugGizmos = true;
    [SerializeField] private Color _rayColor = Color.yellow;
    [SerializeField] private Color _wheelColor = Color.cyan;
    [SerializeField] private Color _hingeColor = Color.green;

    private readonly List<AI_SuspensionArm> _suspensionArms = new List<AI_SuspensionArm>();
    private Rigidbody _rigidbody;
    private float _lastConfiguredMass = -1f;
    private float _bodyTorqueEnableTime;

    public List<AI_SuspensionArm> SuspensionArms => _suspensionArms;
    public float WheelVisualDirectionMultiplier => _isLeftWheelRotation ? 1f : -1f;
    public Vector3 WheelRotationAxis => _wheelRotationAxis;
    public bool ShowDebugGizmos => _showDebugGizmos;
    public Color RayColor => _rayColor;
    public Color WheelColor => _wheelColor;
    public Color HingeColor => _hingeColor;

    private void OnEnable()
    {
        RefreshSuspensionList();
        DistributeControlToTracks();
        _rigidbody = GetComponentInParent<Rigidbody>();
        _lastConfiguredMass = -1f;
        _bodyTorqueEnableTime = Time.time + Mathf.Max(0f, _stabilizationWarmupSeconds);
        UpdateRuntimeSuspensionTuning();
    }

    private void OnValidate()
    {
        RefreshSuspensionList();
        DistributeControlToTracks();
        _lastConfiguredMass = -1f;
    }

    private void FixedUpdate()
    {
        if (_rigidbody == null || _suspensionArms.Count == 0)
        {
            return;
        }

        UpdateRuntimeSuspensionTuning();
        ApplyAntiRoll();
        ApplyTerrainAlign();
    }

    public void ApplyConfig(AISuspensionConfig config)
    {
        if (config == null)
        {
            return;
        }

        _restLength = config.restLength;
        _springStrength = config.springStrength;
        _damperStrength = config.damperStrength;
        _maxCompression = config.maxCompression;
        _maxExtension = config.maxExtension;
        _probeExtraLength = config.probeExtraLength;
        _groundMask = config.groundMask;
        _autoTuneSpringDamper = config.autoTuneSpringDamper;
        _targetRideCompressionRatio = config.targetRideCompressionRatio;
        _damperRatio = config.damperRatio;
        _suspensionRotationAxis = config.suspensionRotationAxis;
        _angleMultiplier = config.angleMultiplier;
        _extensionAngleMultiplier = config.extensionAngleMultiplier;
        _minAngle = config.minAngle;
        _maxAngle = config.maxAngle;
        _rotationLerpSpeed = config.rotationLerpSpeed;
        _rotationSmoothTime = config.rotationSmoothTime;
        _wheelRadius = config.wheelRadius;
        _antiRollForce = config.antiRollForce;
        _enableAntiRoll = config.enableAntiRoll;
        _normalAlignStrength = config.normalAlignStrength;
        _normalAlignDamping = config.normalAlignDamping;
        _maxAlignAngle = config.maxAlignAngle;
        _pitchStabilization = config.pitchStabilization;
        _rollStabilization = config.rollStabilization;

        ApplySharedSettingsToAll();
        _lastConfiguredMass = -1f;
    }

    public void ScanSuspensionArms()
    {
        RefreshSuspensionList();
    }

    public void ApplySharedSettingsToAll()
    {
        foreach (AI_SuspensionArm arm in _suspensionArms)
        {
            ApplyParametersToArm(arm);
        }
    }

    public void UpdateGlobalSettings(
        float restLength,
        float springStrength,
        float damperStrength,
        float maxCompression,
        float maxExtension,
        float wheelRadius)
    {
        _restLength = restLength;
        _springStrength = springStrength;
        _damperStrength = damperStrength;
        _maxCompression = maxCompression;
        _maxExtension = maxExtension;
        _wheelRadius = wheelRadius;
        ApplySharedSettingsToAll();
        _lastConfiguredMass = -1f;
        UpdateRuntimeSuspensionTuning();
    }

    private void UpdateRuntimeSuspensionTuning()
    {
        if (_rigidbody == null || !_autoTuneSpringDamper)
        {
            return;
        }

        if (Mathf.Approximately(_lastConfiguredMass, _rigidbody.mass))
        {
            return;
        }

        float armCount = Mathf.Max(1f, _suspensionArms.Count);
        float gravity = Mathf.Abs(Physics.gravity.y);
        float targetCompression = Mathf.Max(0.01f, _maxCompression * _targetRideCompressionRatio);
        float supportedMassPerArm = _rigidbody.mass / armCount;
        _springStrength = (supportedMassPerArm * gravity) / targetCompression;
        _damperStrength = 2f * Mathf.Sqrt(_springStrength * supportedMassPerArm) * _damperRatio;

        ApplySharedSettingsToAll();
        _lastConfiguredMass = _rigidbody.mass;
    }

    private void ApplyAntiRoll()
    {
        if (!CanApplyBodyTorque())
        {
            return;
        }

        if (!_enableAntiRoll)
        {
            return;
        }

        float leftSum = 0f;
        float rightSum = 0f;
        int leftCount = 0;
        int rightCount = 0;

        foreach (AI_SuspensionArm arm in _suspensionArms)
        {
            if (arm == null || !arm.IsGrounded)
            {
                continue;
            }

            float localX = transform.InverseTransformPoint(arm.transform.position).x;
            if (localX < 0f)
            {
                leftSum += arm.CurrentCompression;
                leftCount++;
            }
            else
            {
                rightSum += arm.CurrentCompression;
                rightCount++;
            }
        }

        if (leftCount == 0 || rightCount == 0)
        {
            return;
        }

        float diff = (leftSum / leftCount) - (rightSum / rightCount);
        _rigidbody.AddRelativeTorque(Vector3.forward * (diff * _antiRollForce), ForceMode.Force);
    }

    private void ApplyTerrainAlign()
    {
        if (!CanApplyBodyTorque())
        {
            return;
        }

        Vector3 normalSum = Vector3.zero;
        int count = 0;

        foreach (AI_SuspensionArm arm in _suspensionArms)
        {
            if (arm == null || !arm.IsGrounded)
            {
                continue;
            }

            normalSum += arm.LastGroundNormal;
            count++;
        }

        if (count == 0)
        {
            return;
        }

        Vector3 averageNormal = (normalSum / count).normalized;
        Vector3 currentUp = transform.up;
        float angle = Vector3.Angle(currentUp, averageNormal);
        if (_maxAlignAngle > 0f && angle > _maxAlignAngle)
        {
            averageNormal = Vector3.Slerp(currentUp, averageNormal, _maxAlignAngle / angle).normalized;
            angle = _maxAlignAngle;
        }

        Vector3 torqueAxis = Vector3.Cross(currentUp, averageNormal);

        _rigidbody.AddTorque(torqueAxis * (angle * _normalAlignStrength), ForceMode.Force);
        _rigidbody.AddTorque(-_rigidbody.angularVelocity * _normalAlignDamping, ForceMode.Force);
    }

    private void ApplyStabilization()
    {
        if (_rigidbody == null || !CanApplyBodyTorque())
        {
            return;
        }

        Vector3 localAngularVelocity = transform.InverseTransformDirection(_rigidbody.angularVelocity);
        float pitchTorque = -localAngularVelocity.x * _pitchStabilization;
        float rollTorque = -localAngularVelocity.z * _rollStabilization;
        Vector3 stabilizationTorque = transform.TransformDirection(new Vector3(pitchTorque, 0f, rollTorque));
        _rigidbody.AddTorque(stabilizationTorque, ForceMode.Force);
    }

    private bool CanApplyBodyTorque()
    {
        if (_rigidbody == null)
        {
            return false;
        }

        if (Application.isPlaying && Time.time < _bodyTorqueEnableTime)
        {
            return false;
        }

        return CountGroundedArms() >= Mathf.Max(2, _minimumGroundedArmsForBodyTorque);
    }

    private int CountGroundedArms()
    {
        int groundedCount = 0;
        foreach (AI_SuspensionArm arm in _suspensionArms)
        {
            if (arm != null && arm.IsGrounded)
            {
                groundedCount++;
            }
        }

        return groundedCount;
    }

    private void RefreshSuspensionList()
    {
        _suspensionArms.Clear();
        HashSet<AI_SuspensionArm> uniqueArms = new HashSet<AI_SuspensionArm>();

        MainSuspensionMarker[] mainMarkers = GetComponentsInChildren<MainSuspensionMarker>(true);
        foreach (MainSuspensionMarker mainMarker in mainMarkers)
        {
            if (mainMarker == null || mainMarker.GetComponent<FixedWheelMarker>() != null)
            {
                continue;
            }

            AI_SuspensionArm arm = mainMarker.GetComponent<AI_SuspensionArm>();
            if (arm == null)
            {
                arm = mainMarker.gameObject.AddComponent<AI_SuspensionArm>();
            }

            arm.AutoResolveReferences();
            if (arm.wheelPivot == null)
            {
                Debug.LogWarning($"AI 主悬挂点 {mainMarker.name} 没有找到 WheelPivotMarker，请给轮子转动点手动挂上识别脚本");
                continue;
            }

            ApplyParametersToArm(arm);
            if (uniqueArms.Add(arm))
            {
                _suspensionArms.Add(arm);
            }
        }

        AI_SuspensionArm[] legacyArms = GetComponentsInChildren<AI_SuspensionArm>(true);
        foreach (AI_SuspensionArm arm in legacyArms)
        {
            if (arm == null || arm.wheelPivot == null || arm.GetComponent<FixedWheelMarker>() != null)
            {
                continue;
            }

            ApplyParametersToArm(arm);
            if (uniqueArms.Add(arm))
            {
                _suspensionArms.Add(arm);
            }
        }
    }

    private void ApplyParametersToArm(AI_SuspensionArm arm)
    {
        if (arm == null)
        {
            return;
        }

        arm.ApplySharedSettings(
            _restLength,
            _springStrength,
            _damperStrength,
            _maxCompression,
            _maxExtension,
            _wheelRadius,
            _probeExtraLength,
            _groundMask,
            _suspensionRotationAxis,
            _angleMultiplier,
            _extensionAngleMultiplier,
            _minAngle,
            _maxAngle,
            _rotationLerpSpeed,
            _rotationSmoothTime);
    }

    private void DistributeControlToTracks()
    {
        TrackPathRendererBase[] trackControllers = GetComponentsInChildren<TrackPathRendererBase>(true);
        foreach (TrackPathRendererBase trackController in trackControllers)
        {
            if (trackController == null)
            {
                continue;
            }

            trackController.SetAiSuspensionManager(this);
        }
    }

    private void OnDrawGizmos()
    {
        if (!_showDebugGizmos)
        {
            return;
        }

        foreach (AI_SuspensionArm arm in _suspensionArms)
        {
            if (arm == null)
            {
                continue;
            }

            Gizmos.color = _rayColor;
            Gizmos.DrawLine(arm.ProbeOrigin, arm.ProbeOrigin - arm.SuspensionUp * arm.ProbeLength);

            if (arm.wheelPivot != null)
            {
                Gizmos.color = _wheelColor;
                Gizmos.DrawWireSphere(arm.wheelPivot.position, _wheelRadius);

                Gizmos.color = _hingeColor;
                Gizmos.DrawWireSphere(arm.transform.position, 0.05f);
            }
        }
    }
}