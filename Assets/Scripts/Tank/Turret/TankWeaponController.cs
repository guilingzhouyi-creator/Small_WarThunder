using NNewUIFramework;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
// using UnityEngine.InputSystem; // ���ࣺ��ǰ�ļ�δֱ��ʹ�� InputSystem ����

public partial class TankWeaponController : MonoBehaviour
{

    public static TankWeaponController Instance { get; private set; }

    [Header("�ʲ�����")]
    [SerializeField] private TankTurretData turretData;
    [SerializeField] private NewAimConfigData aimConfigData;
    [SerializeField] private NewAimConfigData tpsConfigData;
    [SerializeField] private CameraTransitionConfig cameraTransitionConfig;

    [Header("--- �����ӽ����� ---")]
    [SerializeField] private float snapSmoothTime = 0.15f; // �ع�ƽ��ʱ�䣬ԽСԽ��
    [SerializeField] private float maxRecoverDuration = 0.5f; // �ع鳬ʱʱ�䣬��ֹ״̬����

    [Header("--- AIM ģʽ�����ٶ� ---")]
    [SerializeField] private float aimYawMouseSensitivity = 0.18f;   // ÿ���ض�Ӧ��ˮƽ���ٶ�ϵ��
    [SerializeField] private float aimPitchMouseSensitivity = 0.12f; // ÿ���ض�Ӧ�ĸ������ٶ�ϵ��

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
    //��¶�ڹ�ǰ�����򣬹��ⲿϵͳ�����ӵ����䣩ʹ��
    public Vector3 GetBarrelForward() => barrel != null ? barrel.forward : transform.forward;
    public Vector3 GetBarrelMuzzlePosition() => barrel != null ? barrel.position : transform.position;


    [Header("--- Ӳ������ ---")]
    [SerializeField] private Transform turret;
    [SerializeField] private Transform barrel;

    [Header("--- ��ײ������� ---")]
    [SerializeField] private Transform barrelRoot;//�ߵͻ�
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

        // ����ע�� CameraTransitionConfig��ȷ�� CameraSystem ��ʱ�ܶ��� SO
        // �����ǵȵ� Update() �е� ReportToFcsRegistry ��ע��
        if (cameraTransitionConfig != null)
        {
            FCSRegistry.RegisterCameraTransitionConfig(cameraTransitionConfig);
        }
    }

    void Update()
    {
        if (NewUIManager.instance.IsGameplayControlLocked)
        {
            _isFreeLooking = false;
            _isRecovering = false;
            return;
        }



        ReportToFcsRegistry(); // ÿ֡�� FCSRegistry ���浱ǰ״̬���� UI ϵͳʹ��[cite: 14]


        OnFreeLook();
        CalculateTargetPoint();

        // ֻ�ڷ����ɹ۲�ͷǻع�ʱִ��Ӳ����ת
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
            MuzzlePos = GetBarrelMuzzlePosition(), // ʹ�� source 16 �Ľӿ�
            BarrelForward = GetBarrelForward(),    // ʹ�� source 16 �Ľӿ�
            ViewMatrix = _mainCamera.worldToCameraMatrix,
            ProjectionMatrix = _mainCamera.projectionMatrix,
            CurrentFov = _mainCamera.fieldOfView,
            ScreenWidth = Screen.width,
            ScreenHeight = Screen.height
        };

        bool isAimMode = NewUIManager.instance.IsAimMode;
        NewAimConfigData activeConfig = isAimMode
            ? aimConfigData
            : (tpsConfigData != null ? tpsConfigData : aimConfigData);

        FCSRegistry.RegisterPlayerFCS(snapshot, activeConfig);

        if (cameraTransitionConfig != null)
        {
            FCSRegistry.RegisterCameraTransitionConfig(cameraTransitionConfig);
        }
    }

    public void SetAimPointFromScreen(Vector2 screenPos, Camera cam, float distance, LayerMask layer)
    {
        if (cam == null)
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(screenPos); // �� UI ׼��λ�÷�������[cite: 14]
        if (Physics.Raycast(ray, out RaycastHit hit, distance, layer))
            _currentAimPoint = hit.point;
        else
            _currentAimPoint = ray.GetPoint(distance);
    }



}
