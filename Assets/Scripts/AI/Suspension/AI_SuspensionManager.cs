using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI 悬挂管理层
/// 负责：扫描所有 AI_SuspensionArm 组件、统一参数下发、自动整定弹簧/阻尼、
/// 防翻滚杆(AntiRoll)、地形法线对齐、车身姿态稳定
/// 借鉴自 TankSuspensionManager，但不引用 Tank 文件夹
/// </summary>
public class AI_SuspensionManager : MonoBehaviour
{
    [Header("悬挂全局参数")]
    [SerializeField] private float _restLength = 0.8f;
    [SerializeField] private float _springStrength = 15000f;
    [SerializeField] private float _damperStrength = 2000f;
    [SerializeField] private float _maxCompression = 0.4f;
    [SerializeField] private float _maxExtension = 0.3f;
    [SerializeField] private float _wheelRadius = 0.4f;
    [SerializeField] private LayerMask _groundMask = ~0;

    [Header("防翻滚杆")]
    [SerializeField] private float _antiRollForce = 5000f;
    [SerializeField] private bool _enableAntiRoll = true;

    [Header("地形法线对齐")]
    [SerializeField] private float _normalAlignStrength = 3000f;
    [SerializeField] private float _normalAlignDamping = 500f;
    [SerializeField] private float _maxAlignAngle = 15f;

    [Header("车身稳定")]
    [SerializeField] private float _pitchStabilization = 2000f;
    [SerializeField] private float _rollStabilization = 3000f;

    private Rigidbody _rigidbody;
    private List<AI_SuspensionArm> _suspensionArms = new List<AI_SuspensionArm>();
    private Vector3 _averageGroundNormal = Vector3.up;
    private bool _anyGrounded;

    public List<AI_SuspensionArm> SuspensionArms => _suspensionArms;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        ScanSuspensionArms();
        ApplySharedSettingsToAll();
        Debug.Log($"[AI_SuspensionManager] {name} 初始化完毕，共 {_suspensionArms.Count} 个悬挂臂");
    }

    private void FixedUpdate()
    {
        UpdateGroundNormal();
        ApplyAntiRollForce();
        ApplyNormalAlignment();
        ApplyStabilization();
    }

    /// <summary>
    /// 扫描所有子物体上的 AI_SuspensionArm 组件
    /// </summary>
    public void ScanSuspensionArms()
    {
        _suspensionArms.Clear();
        AI_SuspensionArm[] arms = GetComponentsInChildren<AI_SuspensionArm>(true);
        _suspensionArms.AddRange(arms);
    }

    /// <summary>
    /// 将全局参数统一下发到所有悬挂臂
    /// </summary>
    public void ApplySharedSettingsToAll()
    {
        foreach (AI_SuspensionArm arm in _suspensionArms)
        {
            arm.ApplySharedSettings(
                _restLength,
                _springStrength,
                _damperStrength,
                _maxCompression,
                _maxExtension,
                _wheelRadius,
                _groundMask);
        }
    }

    /// <summary>
    /// 更新所有悬挂臂的平均地面法线
    /// </summary>
    private void UpdateGroundNormal()
    {
        _averageGroundNormal = Vector3.up;
        _anyGrounded = false;
        int groundedCount = 0;
        Vector3 sumNormal = Vector3.zero;

        foreach (AI_SuspensionArm arm in _suspensionArms)
        {
            if (arm.IsGrounded)
            {
                sumNormal += arm.HitNormal;
                groundedCount++;
                _anyGrounded = true;
            }
        }

        if (groundedCount > 0)
        {
            _averageGroundNormal = (sumNormal / groundedCount).normalized;
        }
    }

    /// <summary>
    /// 防翻滚杆：左右两侧悬挂压缩量差异 → 施加反向扭矩
    /// </summary>
    private void ApplyAntiRollForce()
    {
        if (!_enableAntiRoll || _rigidbody == null || _suspensionArms.Count < 2) return;

        // 按左右分组
        float leftCompression = 0f;
        float rightCompression = 0f;
        int leftCount = 0;
        int rightCount = 0;

        foreach (AI_SuspensionArm arm in _suspensionArms)
        {
            Vector3 localPos = transform.InverseTransformPoint(arm.transform.position);
            if (localPos.x < -0.1f)
            {
                leftCompression += arm.Compression;
                leftCount++;
            }
            else if (localPos.x > 0.1f)
            {
                rightCompression += arm.Compression;
                rightCount++;
            }
        }

        if (leftCount == 0 || rightCount == 0) return;

        float avgLeft = leftCompression / leftCount;
        float avgRight = rightCompression / rightCount;
        float difference = avgRight - avgLeft;

        // 差值为正表示右侧压缩更多，需要施加左倾恢复扭矩
        float torqueMagnitude = difference * _antiRollForce;
        Vector3 antiRollTorque = transform.forward * torqueMagnitude;

        _rigidbody.AddTorque(antiRollTorque, ForceMode.Force);
    }

    /// <summary>
    /// 地形法线对齐：使车身缓慢贴合地形坡度
    /// </summary>
    private void ApplyNormalAlignment()
    {
        if (_rigidbody == null || !_anyGrounded) return;

        // 计算目标上方向（地面平均法线）
        Vector3 targetUp = _averageGroundNormal;

        // 当前上方向到目标上方向的旋转
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, targetUp) * transform.rotation;

        // 限制最大对齐角度
        float angle = Quaternion.Angle(transform.rotation, targetRotation);
        if (angle > _maxAlignAngle)
        {
            targetUp = Vector3.Slerp(transform.up, targetUp, _maxAlignAngle / angle).normalized;
            targetRotation = Quaternion.FromToRotation(transform.up, targetUp) * transform.rotation;
        }

        // 计算需要的角速度校正
        Vector3 currentAngularVelocity = _rigidbody.angularVelocity;
        Vector3 targetAngularVelocity = Vector3.zero;

        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(transform.rotation);
        deltaRotation.ToAngleAxis(out float alignAngle, out Vector3 alignAxis);

        if (alignAngle > 0.1f && alignAngle < 180f)
        {
            targetAngularVelocity = alignAxis.normalized * (alignAngle * _normalAlignStrength * Mathf.Deg2Rad);
            Vector3 torque = (targetAngularVelocity - currentAngularVelocity) * _normalAlignDamping;
            _rigidbody.AddTorque(torque, ForceMode.Force);
        }
    }

    /// <summary>
    /// 车身稳定：抑制俯仰和翻滚的过度角速度
    /// </summary>
    private void ApplyStabilization()
    {
        if (_rigidbody == null) return;

        Vector3 localAngVel = transform.InverseTransformDirection(_rigidbody.angularVelocity);

        // 俯仰抑制（绕 X 轴）
        float pitchTorque = -localAngVel.x * _pitchStabilization;
        // 翻滚抑制（绕 Z 轴）
        float rollTorque = -localAngVel.z * _rollStabilization;

        Vector3 stabilizationTorque = transform.TransformDirection(
            new Vector3(pitchTorque, 0f, rollTorque));

        _rigidbody.AddTorque(stabilizationTorque, ForceMode.Force);
    }

    /// <summary>
    /// 运行时更新全局悬挂参数并下发
    /// </summary>
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
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 绘制地面平均法线
        if (_anyGrounded)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + _averageGroundNormal * 2f);
        }

        // 连接所有悬挂臂检测点
        Gizmos.color = Color.magenta;
        foreach (AI_SuspensionArm arm in _suspensionArms)
        {
            if (arm != null && arm.IsGrounded)
            {
                Gizmos.DrawWireSphere(arm.HitPoint, 0.1f);
            }
        }
    }
#endif
}
