using System.Collections.Generic;
using UnityEngine;

public partial class TankFireController : MonoBehaviour // 采用 partial 类分文件管理，方便多人协作和功能模块划分
{
    [Header("--- 弹药池设置 ---")]
    [SerializeField] private TankAmmoPoolGroup _ammoPoolGroup;

    private Dictionary<ProjectileType, Objectpooler> _cannonBallPoolsDictionary;
    private Dictionary<ProjectileType, ProjectileData> _projectileDataLookup;
    private float _lastSwitchTime = 0f;
    private const float _ammoSwitchCooldown = 0.3f;
    private TankMainData _tankMainData;

    private void InitializeAmmoPools()
    {
        ResolveAmmoPoolGroup();

        _cannonBallPoolsDictionary = new Dictionary<ProjectileType, Objectpooler>();

        if (_ammoPoolGroup != null)
        {

            if (_ammoPoolGroup.TryGetPool(ProjectileType.AP, out Objectpooler apPool))
            {
                _cannonBallPoolsDictionary[ProjectileType.AP] = apPool;
            }

            if (_ammoPoolGroup.TryGetPool(ProjectileType.HE, out Objectpooler hePool))
            {
                _cannonBallPoolsDictionary[ProjectileType.HE] = hePool;
            }

            if (_ammoPoolGroup.TryGetPool(ProjectileType.HEAT, out Objectpooler heatPool))
            {
                _cannonBallPoolsDictionary[ProjectileType.HEAT] = heatPool;
            }
        }


        InitializeProjectileDataLookup();
    }

    private void ResolveAmmoPoolGroup()
    {
        if (_ammoPoolGroup != null)
        {
            return;
        }

        _ammoPoolGroup = TankAmmoPoolGroup.Instance;
    }

    /// <summary>
    /// 根据武器 SO 中配置的 ProjectileData 列表建立查找表。
    /// 这样发射时就可以按弹种类型取对应数据，而不是去修改共享 ScriptableObject 的 cannonType 字段。
    /// </summary>
    private void InitializeProjectileDataLookup()
    {
        _projectileDataLookup = new Dictionary<ProjectileType, ProjectileData>();

        if (_MainGunWeaponData == null || _MainGunWeaponData.ProjectileSOList == null)
        {
            return;
        }

        foreach (ProjectileData projectileData in _MainGunWeaponData.ProjectileSOList)
        {
            if (projectileData == null)
            {
                continue;
            }

            _projectileDataLookup[projectileData.cannonType] = projectileData;
        }
    }

    public void UpdateStatusFromMainData(TankMainData mainData)
    {
        _tankMainData = mainData;
    }

    private void HandleAmmoSwitchInput()
    {
        if (MIddleInputingController.Instance == null)
        {
            return;
        }

        if (UIManager.Instance != null && UIManager.Instance.IsGameplayControlLocked)
        {
            return;
        }

        if (!MIddleInputingController.Instance.IsSwitchAmmoPressed())
        {
            return;
        }

        if (Time.time - _lastSwitchTime < _ammoSwitchCooldown)
        {
            return;
        }

        SwitchAmmo();
    }

    private void SwitchAmmo()
    {
        _lastSwitchTime = Time.time;

        // 这里切换的是“下一次准备装填”的弹种，不影响当前已上膛的弹种。
        ProjectileType currentNextAmmoType = _tank.NextAmmoType;

        // if (!_tank.TryGetNextAvailableAmmoType(currentNextAmmoType, out ProjectileType nextAmmoType))
        // {
        //     Debug.LogWarning("TankFireController: 没有可切换的弹药类型！");
        //     return;
        // }

        // if (nextAmmoType == currentNextAmmoType)
        // {
        //     Debug.Log($"TankFireController: 当前下一发弹种 {currentNextAmmoType} 已是可用弹药，无需切换。");
        //     return;
        // }

        CheckAmmoSwitching(currentNextAmmoType, out ProjectileType nextAmmoType);


        if (_tank.TrySetNextAmmoType(nextAmmoType))
        {
            // 只有在装填过程中切换，才增加装填时间。
            if (IsReloading)
            {
                AddReloadTimePenalty(1f);
            }

            Debug.Log($"切换下一发弹药类型为: {nextAmmoType}");
        }
    }

    /// <summary>
    /// 根据弹种查找对应的 ProjectileData。
    /// 优先使用武器 SO 里配置的列表，其次使用 Inspector 上单独拖入的回退数据。
    /// </summary>
    private ProjectileData ResolveProjectileData(ProjectileType ammoType)
    {
        if (_projectileDataLookup != null && _projectileDataLookup.TryGetValue(ammoType, out ProjectileData projectileData) && projectileData != null)
        {
            return projectileData;
        }

        if (_projectileData != null && _projectileData.cannonType == ammoType)
        {
            return _projectileData;
        }

        // 兜底逻辑：如果 WeaponData 没有按弹种配置 ProjectileData，就先回退到 Inspector 里拖入的通用 ProjectileData。
        // 这样不会因为配置缺失直接阻断开火，但仍然会提示你补齐 ProjectileSOList。
        // if (_projectileData != null)//如果回退的数据存在
        // {
        //     Debug.LogWarning($"TankFireController: 没有找到弹种 {ammoType} 对应的 ProjectileData，临时回退使用 Inspector 上的通用 ProjectileData（{_projectileData.name}）。请尽快在 WeaponData 的 ProjectileSOList 中补齐该弹种。", this);
        //     return _projectileData;
        // }
        CheckProjectileDataAndReturnFallback();

        // if (_weaponData != null && _weaponData.ProjectileSOList != null)//如果 WeaponData 的 ProjectileSOList 存在但没有配置对应弹种
        // {
        //     string configuredTypes = string.Join(", ", _weaponData.ProjectileSOList.ConvertAll(data => data == null ? "null" : data.cannonType.ToString()));
        //     Debug.LogError($"TankFireController: 没有找到弹种 {ammoType} 对应的 ProjectileData。当前 WeaponData 已配置弹种：[{configuredTypes}]，且没有可回退的通用 ProjectileData。请确认 HE/HEAT/AP 是否都各自挂了对应的 ProjectileData。", this);
        // }
        // else
        // {
        //     Debug.LogError($"TankFireController: 没有找到弹种 {ammoType} 对应的 ProjectileData，而且 WeaponData 的 ProjectileSOList 为空。", this);
        // }

        // return null;

        return CheckWeaponDataLookupProjectileData(ammoType);
    }

    private bool TryGetAmmoPool(ProjectileType ammoType, out Objectpooler pool)
    {
        pool = null;

        if (_ammoPoolGroup != null && _ammoPoolGroup.TryGetPool(ammoType, out pool))
        {
            return true;
        }

        if (_cannonBallPoolsDictionary == null)
        {
            return false;
        }

        if (!_cannonBallPoolsDictionary.TryGetValue(ammoType, out pool))
        {
            return false;
        }

        return pool != null;
    }
}