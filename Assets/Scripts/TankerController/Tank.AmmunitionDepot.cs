using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 坦克弹药库组件，负责总管坦克的弹药相关数据
public partial class Tank : MonoBehaviour
{

    /// <summary>
    /// 当前已上膛、当前这一发真正会被发射出去的弹种。
    /// 这个值在切换下一发时不会变化，只会在装填完成后由下一发提交过来。
    /// </summary>
    public ProjectileType CurrentAmmoType => _currentAmmoType;


    /// <summary>
    /// 下一次准备装填的弹种。
    /// 玩家在装填过程中切换弹药时，只应该改这个值，不要改当前已上膛弹种。
    /// </summary>
    public ProjectileType NextAmmoType => _nextAmmoType;

    private Dictionary<ProjectileType, int> _ammoInventory;
    private List<ProjectileType> _ammoOrder;
    private ProjectileType _currentAmmoType = ProjectileType.AP;
    private ProjectileType _nextAmmoType = ProjectileType.AP;

    public event Action<ProjectileType, int> OnAmmoCountChanged;
    public event Action<ProjectileType> OnCurrentAmmoTypeChanged;
    public event Action<ProjectileType> OnNextAmmoTypeChanged;
    public event Action<ProjectileType> OnAmmoTypeChanged;

    private int _currentReadyAmmo; // 当前待发弹药数量
    private float _replenishTimer; // 补给计时器，用于自动补给逻辑 
    public int CurrentReadyAmmo => _currentReadyAmmo;
    public event Action<int, int> OnReadyAmmoChanged;

    public bool HasReadyAmmo() => _currentReadyAmmo > 0;
    public bool HasAmmo(ProjectileType ammoType) => GetAmmoCount(ammoType) > 0;


    /// <summary>
    /// 获取坦克当前总弹药数量的公共方法，供外部系统查询使用
    /// </summary>
    public int GetTotalAmmoCount()
    {
        if (_ammoInventory == null)
        {
            return 0;
        }

        int totalAmmoCount = 0;

        foreach (KeyValuePair<ProjectileType, int> ammoEntry in _ammoInventory)
        {
            totalAmmoCount += Mathf.Max(0, ammoEntry.Value);
        }

        return totalAmmoCount;
    }

    /// <summary>
    /// 获取指定弹药类型的当前库存数量。
    /// 这个方法会被发射流程调用，以检查当前是否有足够的库存来发射指定类型的弹药。它会返回当前库存数量，供发射流程进行判断和处理。
    /// </summary>
    public int GetAmmoCount(ProjectileType ammoType)
    {
        if (_ammoInventory == null || !_ammoInventory.TryGetValue(ammoType, out int ammoCount))
        {
            return 0;
        }

        return Mathf.Max(0, ammoCount);
    }

    /// <summary>
    /// 获取指定弹药类型的当前库存数量。
    /// </summary>
    private ProjectileType SelectFirstAvailableAmmoType(ProjectileType[] configuredAmmoTypes, ProjectileType fallbackAmmoType)
    {
        if (_ammoInventory == null)
        {
            return fallbackAmmoType;
        }

        foreach (ProjectileType ammoType in configuredAmmoTypes)
        {
            if (GetAmmoCount(ammoType) > 0)
            {
                return ammoType;
            }
        }

        return GetAmmoCount(fallbackAmmoType) > 0 ? fallbackAmmoType : fallbackAmmoType;
    }





    private void InitializeAmmoInventory()
    {
        _ammoInventory = new Dictionary<ProjectileType, int>();
        _ammoOrder = new List<ProjectileType>();

        if (mainData == null)
        {
            _currentAmmoType = ProjectileType.AP;
            _nextAmmoType = ProjectileType.AP;
            return;
        }

        ProjectileType[] configuredAmmoTypes = mainData.GetConfiguredAmmoTypes();

        if (configuredAmmoTypes.Length == 0)
        {
            // 如果核心数据里没有配置任何弹药类型，则默认使用 DefaultAmmoType，并且数量为 TankMaxReadyAmmo（如果有）或 TankMaxAmmo（如果没有）。这样即使设计师忘了配置弹药，坦克也不会完全没弹药了。
            int fallbackAmmoCount = Mathf.Max(0, mainData.TankMaxReadyAmmo > 0 ? mainData.TankMaxReadyAmmo : mainData.TankMaxAmmo);
            _ammoInventory[mainData.DefaultAmmoType] = fallbackAmmoCount;
            _ammoOrder.Add(mainData.DefaultAmmoType);
            _currentAmmoType = fallbackAmmoCount > 0 ? mainData.DefaultAmmoType : ProjectileType.AP;
            _nextAmmoType = _currentAmmoType;
            _currentAmmo = fallbackAmmoCount;
            return;
        }

        int ammoBudget = Mathf.Max(0, mainData.TankMaxAmmo);
        int remainingBudget = ammoBudget > 0 ? ammoBudget : int.MaxValue;

        foreach (ProjectileType ammoType in configuredAmmoTypes)
        {
            _ammoOrder.Add(ammoType);
            int initialCount = Mathf.Max(0, mainData.GetInitialAmmoCount(ammoType));

            if (ammoBudget > 0)
            {
                int allocatedCount = Mathf.Min(initialCount, remainingBudget);
                _ammoInventory[ammoType] = allocatedCount;
                remainingBudget -= allocatedCount;
            }
            else
            {
                _ammoInventory[ammoType] = initialCount;
            }
        }

        _currentAmmoType = SelectFirstAvailableAmmoType(configuredAmmoTypes, mainData.DefaultAmmoType);
        _nextAmmoType = _currentAmmoType;
        _currentAmmo = GetTotalAmmoCount();
    }


    /// <summary>
    /// 消耗一发指定弹药，并同步当前总弹药量与库存变化事件。
    /// 这个方法由发射流程调用，用于在真正生成炮弹前扣除库存。
    /// </summary>
    public bool TryConsumeAmmo(ProjectileType ammoType)
    {
        if (_ammoInventory == null || !_ammoInventory.TryGetValue(ammoType, out int ammoCount) || ammoCount <= 0)
        {
            return false;
        }

        _ammoInventory[ammoType] = ammoCount - 1;

        _currentAmmo = GetTotalAmmoCount();

        if (_currentReadyAmmo > 0)
        {
            _currentReadyAmmo--;
            _replenishTimer = 0f;
        }

        OnAmmoCountChanged?.Invoke(ammoType, _ammoInventory[ammoType]);
        OnReadyAmmoChanged?.Invoke(_currentReadyAmmo, mainData.TankMaxReadyAmmo);

        if (_ammoInventory[ammoType] == 0)
        {
            TryAutoAdvanceNextAmmoType(ammoType);
        }

        return true;
    }

    /// <summary>
    /// 当前弹种打空后，自动把“下一发准备装填”切到仍有库存的其他弹种。
    /// 这样装填完成时就不会继续停留在已经打空的弹种上。
    /// </summary>
    private bool TryAutoAdvanceNextAmmoType(ProjectileType exhaustedAmmoType)
    {
        if (!TryGetNextAvailableAmmoType(exhaustedAmmoType, out ProjectileType nextAmmoType))
        {
            return false;
        }

        return TrySetNextAmmoType(nextAmmoType);
    }

    /// <summary>
    /// 尝试设置下一次准备装填的弹种。
    /// 这里不会改变当前已上膛弹种，所以即使正在显示当前弹药，也不会被切弹操作污染。
    /// </summary>
    public bool TrySetNextAmmoType(ProjectileType ammoType)
    {
        if (!HasAmmo(ammoType))
        {
            return false;
        }

        if (_nextAmmoType == ammoType)
        {
            return true;
        }

        _nextAmmoType = ammoType;
        OnNextAmmoTypeChanged?.Invoke(ammoType);
        return true;
    }

    /// <summary>
    /// 当装填完成时，把“下一发准备装填”的弹种正式提交为“当前已上膛弹种”。
    /// 这个方法是装填完成的收口点，能保证 UI 和发射逻辑都切到同一个状态。
    /// </summary>
    public bool CommitNextAmmoTypeToCurrent()
    {
        if (_currentAmmoType == _nextAmmoType)
        {
            return true;
        }

        SetCurrentAmmoTypeInternal(_nextAmmoType, false);
        return true;
    }

    /// <summary>
    /// 内部方法，处理当前弹种和下一发弹种的切换逻辑。
    /// 这个方法会根据传入的参数决定是否同步切换下一发弹种，以确保在切换当前弹种时下一发弹种也能保持一致，避免出现当前弹种和下一发弹种不匹配的情况。
    /// </summary>

    private bool SetCurrentAmmoTypeInternal(ProjectileType ammoType, bool syncNextAmmoType)
    {
        if (_currentAmmoType == ammoType && (!syncNextAmmoType || _nextAmmoType == ammoType))
        {
            return true;
        }

        _currentAmmoType = ammoType;

        if (syncNextAmmoType)
        {
            _nextAmmoType = ammoType;
            OnNextAmmoTypeChanged?.Invoke(ammoType);
        }

        OnCurrentAmmoTypeChanged?.Invoke(ammoType);
        OnAmmoTypeChanged?.Invoke(ammoType);
        return true;
    }

    /// <summary>
    /// 执行一次射击，消耗当前已上膛的弹药。
    /// 这里会自动处理当前弹药数量的扣除，以及如果当前弹药打空了，自动切换到下一发准备装填的弹药类型（如果有的话）。
    /// </summary>
    public bool TryGetNextAvailableAmmoType(ProjectileType currentAmmoType, out ProjectileType nextAmmoType)
    {
        nextAmmoType = currentAmmoType;

        if (_ammoInventory == null || _ammoInventory.Count == 0)
        {
            return false;
        }

        List<ProjectileType> ammoTypes = _ammoOrder != null && _ammoOrder.Count > 0 ? _ammoOrder : new List<ProjectileType>(_ammoInventory.Keys);
        int startIndex = ammoTypes.IndexOf(currentAmmoType);

        for (int offset = 1; offset <= ammoTypes.Count; offset++)
        {
            int candidateIndex = (startIndex + offset) % ammoTypes.Count;
            ProjectileType candidateType = ammoTypes[candidateIndex];

            if (HasAmmo(candidateType))
            {
                nextAmmoType = candidateType;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 监听待发弹补给的核心逻辑方法。这个方法会在每帧更新中被调用，用于检查当前是否满足自动补给待发弹的条件，并在满足条件时执行补给操作。
    /// </summary>
    private void HandleReadyAmmoReplenishment()
    {
        // 1. 状态判定
        // 是否正在装填主炮
        bool isReloading = TankFireController.Instance != null && TankFireController.Instance.IsReloading;

        // 核心逻辑判断：
        // - 待发弹架未满 (_currentReadyAmmo < 最大待发量)
        // - 车体内还有多余的弹药可以搬运 (总弹药 > 当前待发架里的弹药)
        int maxReady = mainData.TankMaxReadyAmmo;
        bool hasSpaceInRack = _currentReadyAmmo < maxReady;
        bool hasAmmoInHull = GetTotalAmmoCount() > _currentReadyAmmo;

        if (hasSpaceInRack && hasAmmoInHull && !isReloading)
        {
            _replenishTimer += Time.deltaTime;

            if (_replenishTimer >= mainData.ReplenishTime)
            {
                _currentReadyAmmo = Mathf.Min(_currentReadyAmmo + 1, maxReady);
                _replenishTimer = 0f;

                OnReadyAmmoChanged?.Invoke(_currentReadyAmmo, maxReady);

                // 扩展：播放一个轻微的搬运炮弹音效
                // PlayReplenishAudio(); 
            }
        }
        else
        {
            // 只要条件不满足（比如开火了、满了、或者开始装填主炮了），计时器立即归零，保证下一次条件满足时都能有完整的补给时间。
            if (_replenishTimer > 0)
            {
                _replenishTimer = 0f;
            }
        }
    }

}