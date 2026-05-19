using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;
using NNewUIFramework;

public class TankAImUIController : UIToolkitViewAdapter<object>
{
    public override EUIIdentity identity => EUIIdentity.TankAimPanel;

    public static TankAImUIController Instance { get; private set; }
    public Vector3 WorldAimPoint { get; private set; }

    [Header("TPS准星设置")]
    [SerializeField] private float sensitivity = 6f;
    [SerializeField] private float smoothTime = 0.04f;
    [SerializeField] private Vector2 maxOffsetFromCenter = new Vector2(50f, 50f);

    [Header("FCSHUD 绑定 — 使用框架层 _uiDocument.rootVisualElement，不再独立扫描 UIDocument")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float aimProjectionDistance = 1000f;

    [Header("旧TPS覆盖物")]
    [SerializeField] private GameObject tpsDirectionIndicator;
    [SerializeField] private GameObject tpsAimIndicator;

    private Vector2 _tpsTargetScreenPos;
    private FcsHudPainter _painter;
    private CinemachineBrain _cinemachineBrain;
    private float _targetFov;
    private int _currentZoomIndex;
    private bool _wasAimModeLastFrame;
    private bool _wasControlLockedLastFrame;
    private bool _shouldLockCursor = true;
    private bool _isCursorLocked;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != null && Instance != this)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }

            return;
        }

        Instance = this;
        ResolveSceneReferences();
        ResetAimTarget();
    }

    protected override void OnOpened(object data)
    {
        ResolveSceneReferences();
        TryBindHudPresenter();
        ResetAimTarget();
        ApplyCursorState();
    }

    protected override void OnClosing()
    {
        UnbindHudPresenter();
    }

    private void Update()
    {
        ResolveSceneReferences();
        TryBindHudPresenter();
        ApplyCursorState();
        SetLegacyTpsOverlayVisible(false);

        if (mainCamera == null)
        {
            WorldAimPoint = Vector3.zero;
            return;
        }

        bool isControlLocked = NewUIManager.instance != null && NewUIManager.instance.IsGameplayControlLocked;
        bool isAimMode = NewUIManager.instance != null && NewUIManager.instance.IsAimMode;

        if (isControlLocked)
        {
            ResetAimTarget();
            _wasControlLockedLastFrame = true;
            return;
        }

        if (_wasControlLockedLastFrame)
        {
            ResetAimTarget();
            _wasControlLockedLastFrame = false;
            _wasAimModeLastFrame = isAimMode;
            return;
        }

        if (!FCSRegistry.TryGetPlayerState(out FCSSnapshot state, out NewAimConfigData config))
        {
            WorldAimPoint = Vector3.zero;
            _wasAimModeLastFrame = isAimMode;
            return;
        }

        HandleAimZoom(isAimMode, config);

        Vector2 targetScreenPos = ResolveTargetScreenPosition(isAimMode, state, config);
        Vector2 uiTargetPos = ConvertScreenToHudPosition(targetScreenPos, state);
        WorldAimPoint = ResolveWorldAimPoint(targetScreenPos, config);

        if (!isAimMode && TankWeaponController.Instance != null)
        {
            TankWeaponController.Instance.SetAimPointFromScreen(targetScreenPos, mainCamera, config.MaxDetectionRange, config.AimLayerMask);
        }

        if (_painter != null)
        {
            _painter.Refresh(uiTargetPos, state, config, isAimMode, smoothTime);
        }

        _wasAimModeLastFrame = isAimMode;
    }

    private Vector2 ConvertScreenToHudPosition(Vector2 screenPos, FCSSnapshot state)
    {
        IPanel panel = _painter != null ? _painter.panel : _rootVisual?.panel;
        if (panel != null)
        {
            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, screenPos);
            float panelHeight = ResolveHudPanelHeight(state);
            return new Vector2(panelPosition.x, panelHeight - panelPosition.y);
        }

        return FCSRenderingEngine.ScreenToUIToolkit(screenPos, state.ScreenHeight);
    }

    private float ResolveHudPanelHeight(FCSSnapshot state)
    {
        if (_painter != null)
        {
            Rect painterContentRect = _painter.contentRect;
            if (painterContentRect.height > 0.01f)
            {
                return painterContentRect.height;
            }

            Rect painterLayout = _painter.layout;
            if (painterLayout.height > 0.01f)
            {
                return painterLayout.height;
            }
        }

        if (_rootVisual != null)
        {
            Rect rootContentRect = _rootVisual.contentRect;
            if (rootContentRect.height > 0.01f)
            {
                return rootContentRect.height;
            }

            Rect rootLayout = _rootVisual.layout;
            if (rootLayout.height > 0.01f)
            {
                return rootLayout.height;
            }
        }

        return Mathf.Max(1f, state.ScreenHeight);
    }

    private void ResolveSceneReferences()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (_cinemachineBrain == null && mainCamera != null)
        {
            _cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
            _targetFov = mainCamera.fieldOfView;
        }
    }

    private Vector2 ResolveTargetScreenPosition(bool isAimMode, FCSSnapshot state, NewAimConfigData config)
    {
        Vector2 center = new Vector2(state.ScreenWidth * 0.5f, state.ScreenHeight * 0.5f);

        if (isAimMode)
        {
            return center;
        }

        Vector2 mouseDelta = MIddleInputingController.Instance != null
            ? MIddleInputingController.Instance.GetMouseDelta()
            : Vector2.zero;
        float zoomModifier = mainCamera != null ? mainCamera.fieldOfView / 60f : 1f;
        float activeSensitivity = config != null && config.BaseSensitivity > 0f ? config.BaseSensitivity : sensitivity;

        _tpsTargetScreenPos = FCSRenderingEngine.CalculateTpsOffset(
            _tpsTargetScreenPos,
            mouseDelta,
            activeSensitivity,
            zoomModifier,
            center,
            maxOffsetFromCenter);

        return _tpsTargetScreenPos;
    }

    private Vector3 ResolveWorldAimPoint(Vector2 screenPos, NewAimConfigData config)
    {
        if (mainCamera == null)
        {
            return Vector3.zero;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        float maxDistance = config != null ? Mathf.Max(0f, config.MaxDetectionRange) : aimProjectionDistance;
        LayerMask layerMask = config != null ? config.AimLayerMask : Physics.DefaultRaycastLayers;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask))
        {
            return hit.point;
        }

        return ray.GetPoint(maxDistance > 0f ? maxDistance : aimProjectionDistance);
    }

    private void ResetAimTarget()
    {
        _tpsTargetScreenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private void HandleAimZoom(bool isAimMode, NewAimConfigData config)
    {
        if (config == null || config.ZoomFovLevels == null || config.ZoomFovLevels.Length == 0)
        {
            return;
        }

        CameraTransitionConfig transitionConfig = FCSRegistry.CameraTransitionConfig;
        bool shouldResetZoom = transitionConfig != null ? transitionConfig.ResetZoomOnAim : true;

        if (!_wasAimModeLastFrame && isAimMode && shouldResetZoom)
        {
            _currentZoomIndex = 0;
            _targetFov = config.ZoomFovLevels[_currentZoomIndex];
        }
        else if (_wasAimModeLastFrame && !isAimMode && config.AutoResetZoom)
        {
            _currentZoomIndex = 0;
            _targetFov = config.ZoomFovLevels[_currentZoomIndex];
        }

        if (isAimMode && MIddleInputingController.Instance != null)
        {
            float scroll = MIddleInputingController.Instance.GetMouseScrollDelta().y;
            if (scroll > 0.01f)
            {
                _currentZoomIndex = Mathf.Clamp(_currentZoomIndex + 1, 0, config.ZoomFovLevels.Length - 1);
                _targetFov = config.ZoomFovLevels[_currentZoomIndex];
            }
            else if (scroll < -0.01f)
            {
                _currentZoomIndex = Mathf.Clamp(_currentZoomIndex - 1, 0, config.ZoomFovLevels.Length - 1);
                _targetFov = config.ZoomFovLevels[_currentZoomIndex];
            }
        }

        if (isAimMode)
        {
            ApplyAimZoom(config.ZoomSmoothSpeed);
        }
    }

    private void ApplyAimZoom(float zoomSmoothSpeed)
    {
        if (mainCamera == null)
        {
            return;
        }

        CinemachineCamera activeCinemachineCamera = _cinemachineBrain != null
            ? _cinemachineBrain.ActiveVirtualCamera as CinemachineCamera
            : null;

        if (activeCinemachineCamera != null)
        {
            LensSettings lens = activeCinemachineCamera.Lens;
            if (Mathf.Abs(lens.FieldOfView - _targetFov) > 0.01f)
            {
                lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, _targetFov, Time.deltaTime * zoomSmoothSpeed);
                activeCinemachineCamera.Lens = lens;
            }

            return;
        }

        if (Mathf.Abs(mainCamera.fieldOfView - _targetFov) > 0.01f)
        {
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, _targetFov, Time.deltaTime * zoomSmoothSpeed);
        }
    }

    private void ApplyCursorState()
    {
        bool shouldLock = _shouldLockCursor && (NewUIManager.instance == null || !NewUIManager.instance.IsGameplayControlLocked);
        if (_isCursorLocked == shouldLock && UnityEngine.Cursor.visible == !shouldLock)
        {
            return;
        }

        _isCursorLocked = shouldLock;
        UnityEngine.Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        UnityEngine.Cursor.visible = !shouldLock;
    }

    private void TryBindHudPresenter()
    {
        // 使用框架层的 _rootVisual（_uiDocument.rootVisualElement）作为 HUD 挂载点
        if (_rootVisual == null)
        {
            ResolveUIDocumentIfNeeded();
        }

        if (_rootVisual == null)
        {
            return;
        }

        if (_painter != null && _painter.hierarchy.parent == _rootVisual)
        {
            return; // 已经绑定
        }

        if (_painter == null)
        {
            _painter = new FcsHudPainter();
        }
        else if (_painter.hierarchy.parent != null)
        {
            _painter.RemoveFromHierarchy();
        }

        _rootVisual.Add(_painter);
    }

    private void UnbindHudPresenter()
    {
        if (_painter != null)
        {
            _painter.RemoveFromHierarchy();
        }
    }

    private void SetLegacyTpsOverlayVisible(bool visible)
    {
        if (tpsDirectionIndicator != null && tpsDirectionIndicator.activeSelf != visible)
        {
            tpsDirectionIndicator.SetActive(visible);
        }

        if (tpsAimIndicator != null && tpsAimIndicator.activeSelf != visible)
        {
            tpsAimIndicator.SetActive(visible);
        }
    }
}
