using UnityEngine;

//资产配置的栏数据显示（Scriptable Objects//SCRIPTNAME//是可该目录部分）
[CreateAssetMenu(fileName = "TankTurretData", menuName = "Scriptable Objects/TankTurretData")]
public class TankTurretData : ScriptableObject
{
    //炮塔最大旋转速度（未受损时）——电传
    public float TankRotationSpeed;

    //炮塔最大旋转速度（受损时，但未完全失效）——手摇
    public float TankDamagedRotationSpeed;

    //炮管高低方向上的旋转速度
    public float TankerGunRotationSpeed;

    //炮管最大仰角
    public float TankMaxElevationAngle;

    //炮管最大俯角
    public float TankMaxDepressionAngle;

    public AnimationCurve pitchSensitivityCurve;

    [Header("炮管碰撞避撞配置")]
    public AnimationCurve BarrelAvoidancePitchCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(1f, 1f)
    );

    public AnimationCurve BarrelAvoidanceSpeedCurve = new AnimationCurve(
        new Keyframe(0f, 1.3f),
        new Keyframe(1f, 2.8f)
    );

    public float BarrelEnvelopeLength = 6.5f;
    public float BarrelEnvelopeRadius = 0.18f;
    public float BarrelEnvelopeStartOffset = 0.25f;

    [Range(3, 12)]
    public int BarrelEnvelopeSampleCount = 6;

    [Min(0f)]
    public float BarrelAvoidanceRequiredClearance = 0.02f;

    [Range(0.05f, 2f)]
    public float BarrelAvoidanceSearchStepDegrees = 0.25f;

    [Range(0, 8)]
    public int BarrelAvoidanceBinaryRefinementSteps = 4;

    public float BarrelAvoidanceStartClearance = 0.6f;
    public float BarrelAvoidanceEndClearance = -0.05f;

    public NewAimConfigData AimConfig;

}