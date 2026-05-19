using UnityEngine;

//资产配置的栏数据显示（Scriptable Objects//SCRIPTNAME//是可该目录部分）
[CreateAssetMenu(fileName = "TimeData", menuName = "SmallWarThunder/核心/时间/时间数据")]
public class TimeData : ScriptableObject
{
    public string StartTime = "1991-03-12 08:00:00";
    public float TimeMultiplier = 1f;
}