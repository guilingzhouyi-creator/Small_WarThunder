using System;
using System.Collections.Generic;
using UnityEngine;

//资产配置的栏数据显示（Scriptable Objects//SCRIPTNAME//是可该目录部分）
[CreateAssetMenu(fileName = "TankMainData", menuName = "SmallWarThunder/坦克/主系统/坦克主数据")]
public class TankMainData : ScriptableObject
{
    public float TankMaxHealth;

    //弹药最大数量
    public int TankMaxAmmo;


    public bool IsAutoReload; // 是否为自动装填弹药机

    //待发弹药数
    public int TankMaxReadyAmmo;
    public float ReplenishTime = 5f; // 待发弹药装填时间

    //烟雾弹最大数量
    public int TankMaxSmokeAmmo;

    //引擎可排烟数量
    public int TankMaxEngineSmokeCount;

    //替补成员数
    public int TankMaxCrewCount;

    [Header("弹药载荷")]
    // 默认弹种：当弹种清单为空时，用这个类型作为回退
    public ProjectileType DefaultAmmoType = ProjectileType.AP;

    // 每种弹药的初始携弹量配置
    public AmmoLoadoutEntry[] AmmoLoadout;

    [Serializable]
    public class AmmoLoadoutEntry
    {
        public ProjectileType AmmoType = ProjectileType.AP;
        public int InitialCount = 0;
    }

    public ProjectileType[] GetConfiguredAmmoTypes()
    {
        if (AmmoLoadout == null || AmmoLoadout.Length == 0)
        {
            return Array.Empty<ProjectileType>();
        }

        List<ProjectileType> ammoTypes = new List<ProjectileType>();

        foreach (AmmoLoadoutEntry ammoLoadoutEntry in AmmoLoadout)
        {
            if (ammoLoadoutEntry == null)
            {
                continue;
            }

            if (!ammoTypes.Contains(ammoLoadoutEntry.AmmoType))
            {
                ammoTypes.Add(ammoLoadoutEntry.AmmoType);
            }
        }

        return ammoTypes.ToArray();
    }

    public int GetInitialAmmoCount(ProjectileType ammoType)
    {
        if (AmmoLoadout == null)
        {
            return 0;
        }

        int ammoCount = 0;

        foreach (AmmoLoadoutEntry ammoLoadoutEntry in AmmoLoadout)
        {
            if (ammoLoadoutEntry == null)
            {
                continue;
            }

            if (ammoLoadoutEntry.AmmoType == ammoType)
            {
                ammoCount += Mathf.Max(0, ammoLoadoutEntry.InitialCount);
            }
        }

        return ammoCount;
    }

    public int GetTotalInitialAmmoCount()
    {
        if (AmmoLoadout == null)
        {
            return 0;
        }

        int totalAmmoCount = 0;

        foreach (AmmoLoadoutEntry ammoLoadoutEntry in AmmoLoadout)
        {
            if (ammoLoadoutEntry == null)
            {
                continue;
            }

            totalAmmoCount += Mathf.Max(0, ammoLoadoutEntry.InitialCount);
        }

        return totalAmmoCount;
    }


}