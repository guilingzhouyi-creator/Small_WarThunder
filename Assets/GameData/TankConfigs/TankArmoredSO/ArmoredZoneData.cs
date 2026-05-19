using UnityEngine;

//资产配置的栏数据显示（Scriptable Objects//SCRIPTNAME//是可该目录部分）
[CreateAssetMenu(fileName = "ArmoredZoneData", menuName = "SmallWarThunder/坦克/装甲/装甲区数据")]
public class ArmoredZoneData : ScriptableObject
{
    public string zoneName; //装甲区名称，例如：车体、炮塔、炮管等
    public float damageMultiplier; //伤害倍率，例如：0.5f 表示该区域受到的伤害为原来的 50%，2.0f 表示该区域受到的伤害为原来的 200%。
    public float penetrationResistance; //穿透抵抗值，例如：50f 表示该区域需要至少 50 的穿透力才能造成伤害。
    public float maxHealth; //装甲区的最大生命值





}