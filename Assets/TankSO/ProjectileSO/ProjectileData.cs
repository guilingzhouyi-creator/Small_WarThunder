using UnityEngine;

//资产配置的栏数据显示（Scriptable Objects//SCRIPTNAME//是可该目录部分）
[CreateAssetMenu(fileName = "ProjectileData", menuName = "Scriptable Objects/ProjectileData")]
public class ProjectileData : ScriptableObject
{
    public ProjectileType cannonType; // 弹药类型，使用枚举来区分不同类型的弹药，例如穿甲弹、爆炸弹等
    //炮弹初始速度——炮弹发射时的初始速度，决定了炮弹的飞行距离和时间
    public float InitialSpeed;
    public float MaxLifetime;

    public float Mass;

    public float GravityScale;

    public float AirResistance;


    //两者综合使用，决定了炮弹的穿透能力和伤害效果，穿透值越高则炮弹能够穿透更多的装甲或障碍物，伤害值越高则炮弹对目标造成的伤害越大
    public float PenetrationValue;

    public float DamageValue;

    public float ExplosionRadius;



}