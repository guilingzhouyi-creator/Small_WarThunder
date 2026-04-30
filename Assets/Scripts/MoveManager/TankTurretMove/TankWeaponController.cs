using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
// using UnityEngine.InputSystem; // 冗余：当前文件未直接使用 InputSystem 类型

public partial class TankWeaponController : MonoBehaviour
{

    public static TankWeaponController Instance { get; private set; }

    [Header("资产配置")]
    [SerializeField] private TankTurretData turretData;
    [SerializeField] private NewAimConfigData aimConfigData;
    [SerializeField] private NewAimConfigData tpsConfigData;

    [Header("--- 自由视角设置 ---")]
    [SerializeField] private float snapSmoothTime = 0.15f; // 回归平滑时间，越小越快
    [SerializeField] private float maxRecoverDuration = 0.5f; // 回归超时时间，防止状态卡死

    [Header("--- AIM 模式鼠标角速度 ---")]
    [SerializeField] private float aimYawMouseSensitivity = 0.18f;   // 每像素对应的水平角速度系数
    [SerializeField] private float aimPitchMouseSensitivity = 0.12f; // 每像素对应的俯仰角速度系数

    private CinemachineOrbitalFollow _orbitalFollow;
    private CinemachineOrbitalFollow _freeLookOrbitalFollow;
    private Camera _mainCamera;
    private CinemachineBrain _cinemachineBrain;
    private float _savedHorizontalAxis, _savedVerticalAxis;
    private float _horizontalVelocity, _verticalVelocity;
    private float _recoverElapsed;
    private bool _isFreeLooking = false;
    private bool _isRecovering = false;

    public bool IsFreeLooking => _isFreeLooking;
    public bool IsRecovering => _isRecovering;
    //暴露炮管前方方向，供外部系统（如子弹发射）使用
    public Vector3 GetBarrelForward() => barrel != null ? barrel.forward : transform.forward;
    public Vector3 GetBarrelMuzzlePosition() => barrel != null ? barrel.position : transform.position;


    [Header("--- 硬件引用 ---")]
    [SerializeField] private Transform turret;
    [SerializeField] private Transform barrel;

    [Header("--- 碰撞规避设置 ---")]
    [SerializeField] private Transform barrelRoot;//高低机
    // private Camera _mainCamera;
    private Vector3 _currentAimPoint;
    private Transform _tankRoot;
    private readonly List<Collider> _selfAvoidanceColliders = new List<Collider>();

    void Awake()
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

        _mainCamera = Camera.main;
        if (_mainCamera != null)
        {
            _cinemachineBrain = _mainCamera.GetComponent<CinemachineBrain>();
        }

        _tankRoot = transform.root;
        _orbitalFollow = FindFirstObjectByType<CinemachineOrbitalFollow>();


        CacheSelfAvoidanceColliders();

    }

    void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsGameplayControlLocked)
        {
            _isFreeLooking = false;
            _isRecovering = false;
            return;
        }



        ReportToFcsRegistry(); // 每帧向 FCSRegistry 报告当前状态，供 UI 系统使用[cite: 14]


        OnFreeLook();
        CalculateTargetPoint();

        // 只在非自由观察和非回归时执行硬件旋转
        if (!_isFreeLooking && !_isRecovering)
        {
            RotateHardware();
        }
    }

    private CinemachineOrbitalFollow ResolveActiveOrbitalFollow()
    {
        if (_cinemachineBrain != null)
        {
            ICinemachineCamera activeVirtualCamera = _cinemachineBrain.ActiveVirtualCamera;
            CinemachineCamera activeCinemachineCamera = activeVirtualCamera as CinemachineCamera;
            if (activeCinemachineCamera != null)
            {
                CinemachineOrbitalFollow activeOrbitalFollow = activeCinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
                if (activeOrbitalFollow != null)
                {
                    return activeOrbitalFollow;
                }
            }
        }

        return _orbitalFollow;
    }



    private void ReportToFcsRegistry()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera == null)
        {
            return;
        }

        var snapshot = new FCSSnapshot
        {
            InstanceID = gameObject.GetInstanceID(),
            MuzzlePos = GetBarrelMuzzlePosition(), // 使用 source 16 的接口
            BarrelForward = GetBarrelForward(),    // 使用 source 16 的接口
            ViewMatrix = _mainCamera.worldToCameraMatrix,
            ProjectionMatrix = _mainCamera.projectionMatrix,
            CurrentFov = _mainCamera.fieldOfView,
            ScreenWidth = Screen.width,
            ScreenHeight = Screen.height
        };

        bool isAimMode = UIManager.Instance != null && UIManager.Instance.IsAimMode;
        NewAimConfigData activeConfig = isAimMode
            ? aimConfigData
            : (tpsConfigData != null ? tpsConfigData : aimConfigData);

        FCSRegistry.RegisterPlayerFCS(snapshot, activeConfig);
    }

    public void SetAimPointFromScreen(Vector2 screenPos, Camera cam, float distance, LayerMask layer)
    {
        if (cam == null)
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(screenPos); // 从 UI 准星位置发射射线[cite: 14]
        if (Physics.Raycast(ray, out RaycastHit hit, distance, layer))
            _currentAimPoint = hit.point;
        else
            _currentAimPoint = ray.GetPoint(distance);
    }

    // private static FCSRegistrySystem FindRegistrySystemAsset()
    // {
    //     FCSRegistrySystem asset = Resources.Load<FCSRegistrySystem>("FCSRegistrySystem");
    //     if (asset != null)
    //     {
    //         return asset;
    //     }

    //     FCSRegistrySystem[] assets = Resources.FindObjectsOfTypeAll<FCSRegistrySystem>();
    //     for (int i = 0; i < assets.Length; i++)
    //     {
    //         if (assets[i] != null)
    //         {
    //             return assets[i];
    //         }
    //     }

    //     return null;
    // }




}