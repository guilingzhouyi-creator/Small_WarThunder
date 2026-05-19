using UnityEngine;

/// <summary>
/// AI 物理驱动配置 ScriptableObject
/// 定义移动物理参数：地面探测、履带驱动力、各向异性摩擦、速度限制等
/// 由 AI_MotionDriver 在运行时读取
/// </summary>
namespace NGameData.NAIConfigs
{
    [CreateAssetMenu(fileName = "AIMotionConfig", menuName = "SmallWarThunder/AI/运动/AI运动配置", order = 3)]
    public class AIMotionConfig : ScriptableObject
    {
        [Header("车体刚体")]
        [Tooltip("AI 坦克刚体质量，保持与当前项目玩家坦克物理标尺一致")]
        public float vehicleMass = 100f;

        [Tooltip("线性阻尼")]
        public float linearDamping = 0f;

        [Tooltip("角阻尼")]
        public float angularDamping = 0.05f;

        [Header("地面探测")]
        [Tooltip("地面探测层遮罩")]
        public LayerMask groundMask = ~0;

        [Tooltip("地面探测长度（米）")]
        public float groundProbeLength = 2f;

        [Tooltip("地面探测球半径（米）")]
        public float groundProbeRadius = 0.3f;

        [Tooltip("悬空时向下吸附力")]
        public float groundStickForce = 5000f;

        [Header("履带驱动力")]
        [Tooltip("最大马达力")]
        public float maxMotorForce = 8000f;

        [Tooltip("最大制动力")]
        public float maxBrakeForce = 12000f;

        [Tooltip("最大前进速度（米/秒）")]
        public float maxForwardSpeed = 25f;

        [Tooltip("最大后退速度（米/秒）")]
        public float maxReverseSpeed = 8f;

        [Tooltip("最大转向角速度（度/秒）")]
        public float maxTurnSpeed = 60f;

        [Tooltip("履带间距（米）")]
        public float trackWidth = 3f;

        [Tooltip("履带轴距（米）")]
        public float trackWheelBase = 4.5f;

        [Header("运行时阻尼")]
        [Tooltip("有输入时的线性阻尼")]
        public float movingLinearDamping = 0.03f;

        [Tooltip("无输入时的线性阻尼")]
        public float idleLinearDamping = 1f;

        [Header("各向异性摩擦")]
        [Tooltip("前进方向摩擦系数")]
        public float forwardFriction = 1.5f;

        [Tooltip("侧向摩擦系数")]
        public float sideFriction = 0.6f;

        [Tooltip("摩擦混合锐度（前进/侧向摩擦的过渡曲线）")]
        public float frictionBlendSharpness = 3f;

        [Header("转向模型")]
        [Tooltip("枢轴转向混合锐度")]
        public float pivotTurnBlendSharpness = 5f;

        [Header("滚动阻力")]
        [Tooltip("默认地面摩擦系数")]
        public float fallbackGroundFrictionCoefficient = 1f;

        [Tooltip("滚动阻力缩放")]
        public float rollingResistanceScale = 0.03f;

        [Tooltip("滚阻上限")]
        public float maxRollingResistanceCoefficient = 0.05f;

        [Tooltip("滚动阻力系数")]
        public float rollingResistanceCoeff = 0.05f;

        [Tooltip("静摩擦阈值（米/秒），低于此速度不施加滚动阻力")]
        public float staticFrictionThreshold = 0.5f;

        [Header("载具 DNA")]
        [Tooltip("重心高度，参考玩家坦克移动逻辑")]
        public float cogHeight = 0.65f;

        [Tooltip("侧滑摩擦乘数，参考玩家坦克移动逻辑")]
        public float lateralFrictionMultiplier = 3.5f;

        [Header("转向响应")]
        [Tooltip("偏航响应时间常数")]
        public float turnResponseTime = 0.15f;

        [Header("安全护栏")]
        [Tooltip("侧向速度上限相对前进速度上限的比例")]
        public float maxLateralSpeedRatio = 0.3f;

        [Tooltip("总速度上限倍数")]
        public float maxTotalSpeedMultiplier = 1.5f;

        [Tooltip("角速度上限倍数")]
        public float maxAngularSpeedMultiplier = 1.5f;

        [Header("斜坡补偿")]
        [Tooltip("斜坡补偿系数")]
        public float slopeCompensationFactor = 0.3f;

        [Tooltip("陡坡恢复力")]
        public float slopeRecoveryForce = 2000f;
    }
}
