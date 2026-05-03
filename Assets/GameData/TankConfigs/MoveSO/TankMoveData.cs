using UnityEngine;
using UnityEngine.Serialization;

//资产配置的栏数据显示（Scriptable Objects//SCRIPTNAME//是可该目录部分）
[CreateAssetMenu(fileName = "TankMoveData", menuName = "Scriptable Objects/TankMoveData")]
public class TankMoveData : ScriptableObject
{

    public TankType TankType;
    //坦克质量（以千克为单位）
    public float Mass;

    //前进速度（最大）
    public float MoveMaxSpeed;

    //后退速度（最大）
    public float BackMoveMaxSpeed;

    //旋转速度——单侧制动转向时的最大角速度（度/秒）
    [FormerlySerializedAs("MoveTurnMaxSpeed")]
    public float LocalOneTurnMaxSpeed = 12f;

    //旋转速度——双流传动原地转向时的最大角速度（度/秒）
    [FormerlySerializedAs("LocalTurnMaxSpeed")]
    public float LocalTwoTurnMaxSpeed = 18f;

    // 行进间偏航速度——前进/后退中进行偏转时的目标角速度（度/秒）
    public float MovingTurnYawSpeed = 8f;

    [Header("转向分段")]
    // 单侧制动转向的最大前进速度。低于或等于这个速度时，移动转向优先走单侧制动。
    public float BrakeTurnMaxSpeed = 3f;

    // 中圆转向切换到大圆弧转向的最小前进速度。高于或等于这个速度时，移动转向更偏向大半径弧线。
    public float HighSpeedArcTurnMinSpeed = 10f;

    [Header("原地转向")]
    // 是否采用双流传动的原地转向。勾选后仅影响原地转向，移动中的单侧制动转向不受影响。
    public bool UseDualStreamTransmission = false;

    //前进加速度
    public float MoveAcceleration;

    //后退加速度
    public float BackMoveAcceleration;

    //转向响应时间——越小越跟手。当前作为转向响应调参项保留。
    public float TurnAccelerationTime = 0.25f;

    //最大爬坡角度——坦克能够爬升的最大坡度（以度为单位）
    public float MaxClimbAngle;

    //坦克预制体——用于实例化坦克对象的预制体，包含模型（3D模型）、碰撞体（Collider），以便在游戏中创建坦克实例
    //以实现不同类型的坦克具有不同的外观模型和物理属性（如碰撞体大小和形状），其他数据统一由各自的SO提供，以便于管理和调整坦克的性能参数
    public GameObject Prefab;
    [Header("动力系统")]
    // 引擎额定功率（千瓦）
    public float EnginePowerKw = 800f;

    // 传动效率
    [Range(0f, 1f)] public float TransmissionEfficiency = 0.82f;

    // 动力解算的最小速度阈值，避免 v≈0 时 P=Fv 数值爆炸
    public float MinimumPowerSpeed = 0.5f;

    // 空档/原地转向时的功率折减系数
    [Range(0f, 1f)] public float PivotTurnPowerFactor = 0.6f;

    // 原地转向响应系数
    [Range(0f, 1f)] public float PivotTurnEfficiency = 0.65f;

    // 引擎功率曲线：横轴为归一化速度，纵轴为当前可用功率倍率
    public AnimationCurve EnginePowerCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.85f);

    // 转向阻力修正曲线：横轴为 R/B，纵轴为尼基金公式中的修正系数 Phi(rho)
    public AnimationCurve SteeringResistanceCurve = new AnimationCurve(
        new Keyframe(0f, 2.2f),
        new Keyframe(1f, 1.5f),
        new Keyframe(4f, 1.1f),
        new Keyframe(10f, 1f));

    [Header("保速层")]
    // 转向保速曲线：横轴为归一化速度，纵轴为转向时保留的推进比例。
    // 数值越高，坦克在高速转向时越不容易掉速。
    public AnimationCurve TurnSpeedRetentionCurve = new AnimationCurve(
        new Keyframe(0f, 0.65f),
        new Keyframe(0.5f, 0.82f),
        new Keyframe(1f, 0.95f));

    [Header("翻车层")]
    // 高速转向滚转曲线：横轴为归一化速度，纵轴为额外翻滚力矩倍率。
    // 数值越高，高速猛转时越容易抬起另一侧履带甚至侧翻。
    public AnimationCurve HighSpeedTurnRollCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.35f, 0.1f),
        new Keyframe(0.7f, 0.55f),
        new Keyframe(1f, 1f));

    [Range(0f, 3f)] public float HighSpeedTurnRollMultiplier = 1.0f;

    [Header("地面与履带")]
    // 默认地面阻力输入值，不要直接理解为真实摩擦系数；常见有效量级通常很小
    // 例如 0.01~0.05，更大时会明显压制车辆的前进能力
    public float DefaultGroundFrictionCoefficient = 1f;

    // 履带接地长度 L
    public float TrackContactLength = 4.5f;

    // 履带中心距 B
    public float TrackCenterDistance = 2.8f;


}