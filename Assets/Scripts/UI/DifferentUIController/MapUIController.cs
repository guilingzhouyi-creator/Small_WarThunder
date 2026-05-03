using UnityEngine;
using UnityEngine.UIElements;

public class MapUIController : MonoBehaviour
{
    public static MapUIController Instance { get; private set; }

    [Header("UIDocument")]
    [SerializeField] private UIDocument _uiDocument;

    [Header("地图配置")]
    [SerializeField] private MapConfigSO _config;

    [Header("俯拍相机引用")]
    [SerializeField] private MapCameraPosition _mapCamera;

    [Header("任务字幕样式")]
    [SerializeField] private Font _missionLabelFont;
    [SerializeField] private float _missionLabelFontSize = 24f;
    [SerializeField] private Color _missionLabelColor = Color.white;

    private MapRenderingEngine _engine;
    private Transform _playerTransform;
    private bool _isInitialized;

    private Label _missionOverlayLabel;
    private VisualElement _missionOverlayContainer;

    private const string MISSION_OVERLAY_CONTAINER_NAME = "mission-subtitle-overlay";
    private const string MISSION_OVERLAY_LABEL_NAME = "mission-subtitle-text";

    public bool IsFullMapShown => _engine != null && _engine.IsFullMapOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveSceneReferences();
    }

    private void OnEnable()
    {
        ResolveSceneReferences();
        InitializeEngine();
    }

    private void Start()
    {
        ResolveSceneReferences();
        InitializeEngine();
    }

    private void Update()
    {
        ResolveSceneReferences();

        if (!_isInitialized || _engine == null)
        {
            return;
        }

        _engine.Tick();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (_engine != null && _uiDocument != null)
        {
            _uiDocument.rootVisualElement?.Remove(_engine);
        }
    }

    private void ResolveSceneReferences()
    {
        if (_uiDocument == null)
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                _uiDocument = GetComponentInChildren<UIDocument>(true);
            }
        }

        if (_mapCamera == null)
        {
            _mapCamera = FindFirstObjectByType<MapCameraPosition>(FindObjectsInactive.Include);
        }

        GameObject playerTank = GameManager.Instance != null ? GameManager.Instance.PlayerTank : null;
        Transform nextPlayerTransform = playerTank != null ? playerTank.transform : null;
        if (_playerTransform != nextPlayerTransform)
        {
            _playerTransform = nextPlayerTransform;
            _engine?.SetPlayer(_playerTransform);
            if (_mapCamera != null && _playerTransform != null)
            {
                _mapCamera.BindTarget(_playerTransform);
            }
        }

        if (_engine != null)
        {
            _engine.SetCamera(_mapCamera);
        }
    }

    private void InitializeEngine()
    {
        if (_isInitialized)
        {
            if (_engine != null)
            {
                _engine.SetCamera(_mapCamera);
                _engine.SetPlayer(_playerTransform);
            }

            return;
        }

        if (_uiDocument == null)
        {
            Debug.LogError("[MapUIController] UIDocument 未赋值，请检查 UI 预制体。", this);
            return;
        }

        if (_config == null)
        {
            Debug.LogError("[MapUIController] MapConfigSO 未赋值，请在 Inspector 中配置。", this);
            return;
        }

        _engine = new MapRenderingEngine(_config);
        _engine.SetCamera(_mapCamera);
        _engine.SetPlayer(_playerTransform);
        _engine.SetVisible(false);

        _uiDocument.rootVisualElement.Add(_engine);

        CreateMissionOverlay();

        _isInitialized = true;
    }

    private void CreateMissionOverlay()
    {
        if (_uiDocument == null)
        {
            return;
        }

        _missionOverlayContainer = new VisualElement
        {
            name = MISSION_OVERLAY_CONTAINER_NAME,
            style =
            {
                position = Position.Absolute,
                left = 0f,
                right = 0f,
                bottom = 80f,
                height = 60f,
                flexDirection = FlexDirection.Row,
                justifyContent = Justify.Center,
                alignItems = Align.Center,
                backgroundColor = new Color(0f, 0f, 0f, 0.6f)
            }
        };

        _missionOverlayLabel = new Label
        {
            name = MISSION_OVERLAY_LABEL_NAME,
            text = string.Empty,
            style =
            {
                color = _missionLabelColor,
                fontSize = _missionLabelFontSize,
                unityTextAlign = TextAnchor.MiddleCenter,
                whiteSpace = WhiteSpace.Normal
            }
        };

        _missionOverlayContainer.Add(_missionOverlayLabel);
        _missionOverlayContainer.style.display = DisplayStyle.None;

        _uiDocument.rootVisualElement.Add(_missionOverlayContainer);
    }

    /// <summary>
    /// Tab 叠加层显示/隐藏字幕底部栏
    /// </summary>
    public void SetMissionOverlayVisible(bool visible)
    {
        if (_missionOverlayContainer == null)
        {
            return;
        }

        _missionOverlayContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>
    /// 更新任务字幕文本（由 MissionPannelUIController 驱动）
    /// </summary>
    public void SetMissionText(string text)
    {
        if (_missionOverlayLabel != null)
        {
            _missionOverlayLabel.text = text ?? string.Empty;
        }
    }

    public void SetPlayerTransform(Transform playerTransform)
    {
        _playerTransform = playerTransform;
        _engine?.SetPlayer(playerTransform);
        if (_mapCamera != null && playerTransform != null)
        {
            _mapCamera.BindTarget(playerTransform);
        }
    }

    public void OpenFullMap()
    {
        if (_engine == null)
        {
            return;
        }

        _engine.SetVisible(true);
        _engine.OpenFullMap();
    }

    public void CloseFullMap()
    {
        if (_engine == null)
        {
            return;
        }

        _engine.CloseFullMap();
    }

    public void SetMiniMapVisible(bool visible)
    {
        if (_engine == null)
        {
            return;
        }

        if (_engine.IsFullMapOpen)
        {
            _engine.SetVisible(true);
            return;
        }

        _engine.SetVisible(visible);
    }
}
