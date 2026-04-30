using UnityEngine;
using System;

public partial class TankFireController : MonoBehaviour
{


    public static TankFireController Instance { get; private set; }

    [Header("--- 主炮设置 ---")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private ProjectileData _projectileData;
    [SerializeField] private TankWeaponData _MainGunWeaponData;
    // [SerializeField] private TankMainData _tankMainData;
    [SerializeField] private Tank _tank;


    [Header("--- 机枪设置 ---")]
    [SerializeField] private Transform _machineGunFirePoint;
    // [SerializeField] private ProjectileData _machineGunProjectileData;// 机枪的弹药数据，包含伤害、射速等信息
    // [SerializeField] private TankWeaponData _machineGunWeaponData;// 机枪的武器数据，包含射击频率，开火模式等信息

    [SerializeField] private int _machineGunSubType; // 机枪的子类型，用于区分不同类型的机枪，例如同样是机枪，但可能有不同的射速、伤害等属性，通过这个字段可以在弹药池中获取对应类型的机枪弹药对象池
    public event Action<float> OnReloadStatusChanged;
    public event Action<float> RangerFinderResultUpdated;

    public bool IsReloading => _isReloading;
    public float CurrentReloadTime => _currentReloadTime;
    public ProjectileType CurrentAmmoType => _tank != null
        ? _tank.CurrentAmmoType
        : (_projectileData != null ? _projectileData.cannonType : ProjectileType.AP);
    public ProjectileType NextAmmoType => _tank != null
        ? _tank.NextAmmoType
        : (_projectileData != null ? _projectileData.cannonType : ProjectileType.AP);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }

            return;
        }

        Instance = this;

        InitializeAmmoPools();
    }

    private void Start()
    {
        ResolveTankReference();
        CheckAllComponents();

        if (_tank != null)
        {
            UpdateStatusFromMainData(_tank.MainData);
        }

        InitializeDirectionState();
        InitializeReloadState();
    }

    private void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsGameplayControlLocked)
        {
            return;
        }

        if (MIddleInputingController.Instance.IsRangeFinderPressed())
        {
            HandleRangeFinderInput();
        }

        UpdateReferenceBarrelDirection();
        HandleAmmoSwitchInput();
        HandleFireInput();
    }
}