using UnityEngine;
using System;
using System.Collections.Generic;

public partial class Tank : MonoBehaviour
{
    //坦克核心类，作为坦克的主要组件，负责整合和管理坦克的各个子系统，例如移动、射击、状态显示等
    //可以通过组合模式来实现，将不同功能模块作为组件挂载在坦克对象上，确保代码的清晰和可维护性

    public static Tank Instance { get; private set; } // 单例实例，确保全局唯一访问

    [SerializeField] private TankMainData mainData; // 坦克核心数据
    [SerializeField] private Collider[] colliders; // 碰撞检测组件数组，支持多个Collider组件以覆盖整个坦克的碰撞范围——用于自己被击中时的碰撞检测

    public TankMainData MainData => mainData;
    private float _currentHealth; // 当前生命值
    private int _currentAmmo; // 当前弹药数量
    private int _currentCrewCount; // 当前成员数量
    private int _currentSmokeAmmo; // 当前烟雾弹数量
    private int _currentEngineSmokeCount; // 当前发动机排烟数量
    private bool _runtimeStateInitialized;



    private void Awake()
    {



        //如果场景中已经存在一个坦克实例（例如在坦克重生时）并且当前正在运行游戏，则销毁新创建的坦克对象，确保场景中始终只有一个坦克实例存在，避免潜在的冲突和错误。
        if (Instance != null && Instance != this)
        {

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }

            return;
        }
        else
        {
            if (colliders == null || colliders.Length == 0)
            {
                colliders = GetComponentsInChildren<Collider>(true);
            }

            Instance = this;
            // DontDestroyOnLoad(gameObject); // 可选：如果需要在场景切换时保持坦克对象，可以取消注释这行代码
        }

        if (Instance != null && Instance != this)
        {

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }

            return;
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 可选：如果需要在场景切换时保持坦克对象，可以取消注释这行代码
        }

        InitializeRuntimeState();
    }

    private void Start()
    {
        InitializeRuntimeState();
    }

    private void Update()
    {
        HandleReadyAmmoReplenishment();
    }

    /// <summary>
    /// 将SO中的核心数据赋值到坦克的当前状态变量中，这些变量将被坦克的各个子系统使用和修改。这个方法在坦克初始化时调用，确保坦克的状态与核心数据保持一致。
    /// </summary>
    public void EnsureRuntimeStateInitialized()
    {
        if (_runtimeStateInitialized)
        {
            return;
        }

        GetValuesFromMainData();
        _runtimeStateInitialized = true;
    }

    private void InitializeRuntimeState()
    {
        EnsureRuntimeStateInitialized();
    }

    private void GetValuesFromMainData()
    {
        _currentHealth = mainData.TankMaxHealth;
        _currentAmmo = mainData.TankMaxAmmo;
        _currentCrewCount = mainData.TankMaxCrewCount;
        _currentSmokeAmmo = mainData.TankMaxSmokeAmmo;
        _currentEngineSmokeCount = mainData.TankMaxEngineSmokeCount;

        _currentReadyAmmo = mainData.TankMaxReadyAmmo;
        OnReadyAmmoChanged?.Invoke(_currentReadyAmmo, mainData.TankMaxReadyAmmo);

        InitializeAmmoInventory();
    }


}
