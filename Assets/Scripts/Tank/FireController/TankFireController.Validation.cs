using UnityEngine;

public partial class TankFireController : MonoBehaviour
{
    private void ResolveTankReference()
    {
        if (_tank != null)
        {
            return;
        }

        _tank = Tank.Instance != null ? Tank.Instance : GetComponentInParent<Tank>();
    }

    private void CheckAllComponents()
    {
        if (_MainGunWeaponData == null)
        {
            Debug.LogError("TankFireController: 没有设置武器数据引用！");
        }
        if (_ammoPoolGroup == null)
        {
            Debug.LogError("TankFireController: 没有设置弹药池对象组！");
        }

        if (TankWeaponController.Instance == null)
        {
            Debug.LogError("TankFireController: 没有找到 TankWeaponController 实例，无法获取炮管方向！");
        }
        if (_firePoint == null)
        {
            Debug.LogError("TankFireController: 没有设置炮口位置引用！");
        }

        if (_tank == null)
        {
            Debug.LogWarning("TankFireController: 没有找到 Tank 实例，弹药切换功能将受限！");
        }

        if (_MainGunWeaponData != null && (_MainGunWeaponData.ProjectileSOList == null || _MainGunWeaponData.ProjectileSOList.Count == 0) && _projectileData == null)
        {
            Debug.LogWarning("TankFireController: MainGunWeaponData 里没有 ProjectileSOList，且没有设置回退 ProjectileData，发射时将无法按弹种解析炮弹数据！");
        }

        if (_MainGunWeaponData == null)
        {
            Debug.LogWarning("TankFireController: 没有设置 MainGunWeaponData，装填功能将受限！");
        }
    }

    private ProjectileData CheckProjectileDataAndReturnFallback()
    {
        if (_projectileData != null)
        {
            Debug.LogWarning("TankFireController: 没有在 MainGunWeaponData 的 ProjectileSOList 中找到对应弹种的 ProjectileData，正在使用 Inspector 上的通用 ProjectileData 作为回退。请尽快在 MainGunWeaponData 的 ProjectileSOList 中补齐该弹种。", this);
            return _projectileData;
        }

        Debug.LogWarning("TankFireController: 没有设置回退 ProjectileData，发射时将无法按弹种解析炮弹数据！");
        return null;
    }

    private ProjectileData CheckWeaponDataLookupProjectileData(ProjectileType ammoType)
    {
        if (_MainGunWeaponData != null && _MainGunWeaponData.ProjectileSOList != null)
        {
            string configuredTypes = string.Join(", ", _MainGunWeaponData.ProjectileSOList.ConvertAll(data => data == null ? "null" : data.cannonType.ToString()));
            Debug.LogError($"TankFireController: 没有找到弹种 {ammoType} 对应的 ProjectileData。当前 MainGunWeaponData 已配置弹种：[{configuredTypes}]，请确认 HE/HEAT/AP 是否都各自挂了对应的 ProjectileData。", this);
        }
        else
        {
            Debug.LogError("TankFireController: 没有设置 MainGunWeaponData，无法按弹种解析炮弹数据！");
        }

        return null;
    }

    /// <summary>
    /// 检查是否需要切换下一发弹药类型，并执行切换逻辑。
    /// 参数中的Out参数 nextAmmoType 是为了在外部调用时能够获取到实际切换后的弹药类型，方便后续逻辑使用。
    /// 内都调用了 Tank 组件的 TryGetNextAvailableAmmoType后，会覆盖 nextAmmoType 的值，无论是否成功切换，外部都能拿到一个明确的结果来判断下一步逻辑应该如何处理（例如更新UI显示当前弹药类型）。如果切换失败，nextAmmoType 可能会被设置为一个默认值或者保持不变，这取决于 Tank 组件的实现细节。
    /// 设计这个方法的目的是为了将弹药切换的检查和执行逻辑封装在一起，确保在切换弹药类型时能够正确处理各种边界情况（例如没有可用的弹药类型、当前弹药类型已经是可用的等），同时通过 Out 参数提供清晰的接口供外部调用者使用。    
    /// </summary>
    private void CheckAmmoSwitching(ProjectileType currentNextAmmoType, out ProjectileType nextAmmoType)
    {
        if (!_tank.TryGetNextAvailableAmmoType(currentNextAmmoType, out nextAmmoType))
        {
            Debug.LogWarning("TankFireController: 无法获取下一发弹药类型，可能是因为 Tank 组件缺失或未正确实现 TryGetNextAvailableAmmoType 方法！");
            return;
        }

        if (nextAmmoType == currentNextAmmoType)
        {
            Debug.Log($"TankFireController: 当前下一发弹种 {currentNextAmmoType} 已是可用弹药，无需切换。");
            return;
        }
    }

}
