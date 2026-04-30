using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

public partial class TankSuspensionManager : MonoBehaviour
{
    [Header("=== 统一参数（改这里影响所有悬挂） ===")]
    public float restHeight = 0.45f;
    public float springStrength = 8500f;
    public float damperStrength = 900f;
    public float maxCompression = 0.38f;
    public float maxExtension = 0.5f;
    public float probeExtraLength = 0.2f;
    public LayerMask groundMask = ~0;

    [Header("自动整定")]
    public bool autoTuneSpringDamper = true;
    [Range(0.1f, 0.9f)] public float targetRideCompressionRatio = 0.4f;
    [Range(0.1f, 2f)] public float damperRatio = 0.9f;

    [Header("主悬挂旋转限制")]
    public Vector3 suspensionRotationAxis = new Vector3(1, 0, 0);
    public float angleMultiplier = 40f;
    public float extensionAngleMultiplier = 110f; // 伸展下压时独立倍率
    public float minAngle = -55f;
    public float maxAngle = 22f;
    public float rotationLerpSpeed = 20f;
    public float rotationSmoothTime = 0.06f;

    [Header("轮子旋转")]
    public float wheelRadius = 0.85f;
    public Vector3 wheelRotationAxis = new Vector3(1, 0, 0);
    [FormerlySerializedAs("invertWheel")]
    [InspectorName("是否属于左侧轮子转动")]
    public bool isLeftWheelRotation = true;

    public float WheelVisualDirectionMultiplier => isLeftWheelRotation ? 1f : -1f;

    [Header("防翻滚 (Anti-Roll Bar)")]
    public float antiRollStrength = 5000f;

    [Header("地形法线对齐")]
    public float terrainAlignStrength = 3000f;
    public float terrainAlignDamping = 500f;

    [Header("调试")]
    public bool showDebugGizmos = true;
    public Color rayColor = Color.yellow;
    public Color wheelColor = Color.cyan;
    public Color hingeColor = Color.green;

    private readonly List<TankSuspensionArm> suspensionArms = new List<TankSuspensionArm>();
    public TrackController trackGenerator;
    private Rigidbody _rb;
    private float _lastConfiguredMass = -1f;

    void OnEnable()
    {
        RefreshSuspensionList();
        DistributeControlToTracks();
        _rb = GetComponentInParent<Rigidbody>();
        _lastConfiguredMass = -1f;
        UpdateRuntimeSuspensionTuning();
    }

    void OnValidate()
    {
        RefreshSuspensionList();
        DistributeControlToTracks();
        _lastConfiguredMass = -1f;
    }

    void FixedUpdate()
    {
        if (_rb == null || suspensionArms.Count == 0) return;
        UpdateRuntimeSuspensionTuning();
        ApplyAntiRoll();
        ApplyTerrainAlign();
    }

    private void UpdateRuntimeSuspensionTuning()
    {
        if (_rb == null)
        {
            return;
        }

        if (!autoTuneSpringDamper)
        {
            return;
        }

        if (Mathf.Approximately(_lastConfiguredMass, _rb.mass))
        {
            return;
        }

        float armCount = Mathf.Max(1f, suspensionArms.Count);
        float gravity = Mathf.Abs(Physics.gravity.y);
        float targetCompression = Mathf.Max(0.01f, maxCompression * targetRideCompressionRatio);
        float supportedMassPerArm = _rb.mass / armCount;
        springStrength = (supportedMassPerArm * gravity) / targetCompression;
        damperStrength = 2f * Mathf.Sqrt(springStrength * supportedMassPerArm) * damperRatio;

        foreach (TankSuspensionArm arm in suspensionArms)
        {
            ApplyParametersToArm(arm);
        }

        _lastConfiguredMass = _rb.mass;
    }

    private void ApplyAntiRoll()
    {
        float leftSum = 0f, rightSum = 0f;
        int leftCount = 0, rightCount = 0;

        foreach (TankSuspensionArm arm in suspensionArms)
        {
            if (arm == null || !arm.IsGrounded) continue;
            // 用本地 X 坐标判断左右侧
            float localX = transform.InverseTransformPoint(arm.transform.position).x;
            if (localX < 0f) { leftSum += arm.CurrentCompression; leftCount++; }
            else { rightSum += arm.CurrentCompression; rightCount++; }
        }

        if (leftCount == 0 || rightCount == 0) return;
        float diff = (leftSum / leftCount) - (rightSum / rightCount);
        _rb.AddRelativeTorque(Vector3.forward * (diff * antiRollStrength), ForceMode.Force);
    }

    private void ApplyTerrainAlign()
    {
        Vector3 normalSum = Vector3.zero;
        int count = 0;
        foreach (TankSuspensionArm arm in suspensionArms)
        {
            if (arm == null || !arm.IsGrounded) continue;
            normalSum += arm.LastGroundNormal;
            count++;
        }
        if (count == 0) return;

        Vector3 avgNormal = (normalSum / count).normalized;
        Vector3 currentUp = transform.up;
        Vector3 torqueAxis = Vector3.Cross(currentUp, avgNormal);
        float angle = Vector3.Angle(currentUp, avgNormal);

        _rb.AddTorque(torqueAxis * (angle * terrainAlignStrength), ForceMode.Force);
        // 阻尼：抑制对齐过冲
        _rb.AddTorque(-_rb.angularVelocity * terrainAlignDamping, ForceMode.Force);
    }

    private void RefreshSuspensionList()
    {
        suspensionArms.Clear();
        HashSet<TankSuspensionArm> uniqueArms = new HashSet<TankSuspensionArm>();

        MainSuspensionMarker[] mainMarkers = GetComponentsInChildren<MainSuspensionMarker>(true);
        foreach (MainSuspensionMarker mainMarker in mainMarkers)
        {
            if (mainMarker == null) continue;

            // 诱导轮/传动轮挂了 FixedWheelMarker，不参与弹簧物理
            if (mainMarker.GetComponent<FixedWheelMarker>() != null) continue;

            TankSuspensionArm arm = mainMarker.GetComponent<TankSuspensionArm>();
            if (arm == null)
            {
                arm = mainMarker.gameObject.AddComponent<TankSuspensionArm>();
            }

            arm.AutoResolveReferences();

            if (arm.wheelPivot == null)
            {
                Debug.LogWarning($"主悬挂点 {mainMarker.name} 没有找到 WheelPivotMarker，请给轮子转动点手动挂上识别脚本");
                continue;
            }

            ApplyParametersToArm(arm);

            if (uniqueArms.Add(arm))
            {
                suspensionArms.Add(arm);
            }
        }

        // 旧版兜底：已经手动挂了 TankSuspensionArm 的对象仍然可以继续工作
        TankSuspensionArm[] legacyArms = GetComponentsInChildren<TankSuspensionArm>(true);
        foreach (TankSuspensionArm arm in legacyArms)
        {
            if (arm == null || arm.wheelPivot == null) continue;

            // 固定轮排除
            if (arm.GetComponent<FixedWheelMarker>() != null) continue;

            ApplyParametersToArm(arm);

            if (uniqueArms.Add(arm))
            {
                suspensionArms.Add(arm);
            }
        }

        if (!Application.isPlaying)
        {
            // Debug.Log($"[Edit Mode] 当前收集到 {suspensionArms.Count} 个有效悬挂");
        }
    }

    private void ApplyParametersToArm(TankSuspensionArm arm)
    {
        if (arm == null)
        {
            return;
        }

        arm.ApplySharedSettings(
            restHeight,
            springStrength,
            damperStrength,
            maxCompression,
            maxExtension,
            wheelRadius,
            probeExtraLength,
            groundMask,
            suspensionRotationAxis,
            angleMultiplier,
            extensionAngleMultiplier,
            minAngle,
            maxAngle,
            rotationLerpSpeed,
            rotationSmoothTime);
    }

    private void DistributeControlToTracks()
    {
        TrackController[] trackControllers = GetComponentsInChildren<TrackController>(true);
        foreach (TrackController trackController in trackControllers)
        {
            if (trackController == null)
            {
                continue;
            }

            trackController.suspensionManager = this;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos)
        {
            return;
        }

        foreach (TankSuspensionArm arm in suspensionArms)
        {
            if (arm == null || arm.wheelPivot == null)
            {
                continue;
            }

            Vector3 origin = arm.ProbeOrigin;
            Vector3 castEnd = origin - arm.SuspensionUp * arm.ProbeLength;

            Gizmos.color = rayColor;
            Gizmos.DrawLine(origin, castEnd);

            Gizmos.color = wheelColor;
            Gizmos.DrawWireSphere(origin, arm.wheelRadius);
            Gizmos.DrawWireSphere(castEnd, arm.wheelRadius);

            Gizmos.color = hingeColor;
            Gizmos.DrawWireSphere(arm.transform.position, 0.15f);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(arm.transform.position, origin);
        }
    }
}
