using System.Collections.Generic;
using UnityEngine;

//资产配置的栏数据显示（Scriptable Objects//SCRIPTNAME//是可该目录部分）
[CreateAssetMenu(fileName = "TankWeaponData", menuName = "SmallWarThunder/坦克/武器/坦克武器数据")]
public class TankWeaponData : ScriptableObject
{

    /* 基本属性 */
    //弹药装填时间（机枪还是主炮）——决定了每次发射后需要等待的时间
    public float ReloadTime;

    //最大装填时间上限——用于限制切换弹药时追加的装填惩罚，不允许把总装填时间无限拉长。
    //如果不设置或小于等于 0，则默认使用 ReloadTime 作为上限。
    public float MaxReloadTime;

    //后坐力——决定了每次发射后坦克受到的反作用力——可以处理为为坦克施加一个瞬时的反向力，影响坦克的移动和稳定性，增加射击的挑战性和真实感
    public float RecoilForce;

    //炮弹种类（是机枪还是主炮）——与下面的ShellPrefab配合使用，决定了发射的弹药类型和效果（同时也是二重包装的标志，机枪和主炮可以共存以及安全性保护）
    public bool IsMachineGun;
    public float BaseSpread;
    public float MaxFCSdistance;

    /*弹药配置*/
    public List<ProjectileData> ProjectileSOList;

    /*制退后移*/
    public float RecoilBackwardDistance;

}