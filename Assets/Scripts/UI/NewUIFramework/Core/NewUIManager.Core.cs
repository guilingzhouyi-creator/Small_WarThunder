using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NNewUIFramework
{
    /// <summary>
    /// 新 UI 框架核心调度器（partial：核心骨架 + 初始化 + SceneLoad 生命周期）
    /// </summary>
    public partial class NewUIManager : MonoBehaviour
    {
        public static NewUIManager instance { get; private set; }

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLog = true;

        [Header("UI 引用 — 各 Panel Controller")]
        [SerializeField] private PauseUIController pauseUIController;
        [SerializeField] private MissionPannelUIController missionPannelUIController;
        [SerializeField] private SettingManager settingManager;
        [SerializeField] private TankStatsUIController tankStatsUIController;
        [SerializeField] private TankAImUIController sightUIPanel;
        [SerializeField] private MapUIController mapUIController;

        [Header("小地图状态")]
        [SerializeField] private bool _showMiniMapInTPS = true;
        [SerializeField] private bool _showMiniMapInAim;

        [Header("跨层级排序")]
        [SerializeField] private UICrossLayeringController _crossLayering;

        /// <summary>全局事件：游戏暂停</summary>
        public event EventHandler OnGamePaused;

        /// <summary>全局事件：游戏继续</summary>
        public event EventHandler OnGameUnPaused;

        private IUIRegistry _registry;
        private UIStackManager _stackManager;

        private bool _isInputBound;
        private bool _isSettingBound;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[NewUIManager] 已存在实例，正在销毁新的实例。", this);
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            _registry = new UIRegistry();
            _stackManager = new UIStackManager(_registry);

            _stackManager.onOpen = OnPushOpen;
            _stackManager.onClose = OnPopClose;
            _stackManager.onSuspend = OnSuspend;
            _stackManager.onResume = OnResume;
            _stackManager.onCover = OnCover;
            _stackManager.onReveal = OnReveal;

            AutoResolveReferences();
            BindSettingEvents();

            // 关键：没有注册就永远 Open 不了。
            AutoRegisterUIFromScene();

            Log("NewUIManager 初始化完成。");
        }

        private void Start()
        {
            BindInputEvents();
            BindSettingEvents();
            RefreshUIState();
        }

        private void OnEnable()
        {
            BindInputEvents();
            BindSettingEvents();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            UnbindInputEvents();
            UnbindSettingEvents();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        /// <summary>自动解析空引用（后备方案）</summary>
        private void AutoResolveReferences()
        {
            if (pauseUIController == null)
                pauseUIController = GetComponentInChildren<PauseUIController>(true);
            if (settingManager == null)
                settingManager = GetComponentInChildren<SettingManager>(true);
            if (tankStatsUIController == null)
                tankStatsUIController = GetComponentInChildren<TankStatsUIController>(true);
            if (missionPannelUIController == null)
                missionPannelUIController = GetComponentInChildren<MissionPannelUIController>(true);
            if (mapUIController == null)
                mapUIController = GetComponentInChildren<MapUIController>(true);
            if (_crossLayering == null)
                _crossLayering = GetComponentInChildren<UICrossLayeringController>(true);
        }

        private void AutoRegisterUIFromScene()
        {
            // 场景内扫描：把实现了 IUIController<object> + IUIViewAdapter 的组件注册进 registry，
            // 使 RefreshUIState 的框架 Open/Close 调用能够工作。
#if UNITY_2022_3_OR_NEWER
            var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var all = FindObjectsOfType<MonoBehaviour>(true);
#endif
            Log($"[AutoRegisterUIFromScene] scanning MonoBehaviours: {all.Length}");

            int foundCandidate = 0;
            int registered = 0;

            foreach (var mb in all)
            {
                if (mb is IUIController<object> controller && mb is IUIViewAdapter viewAdapter)
                {
                    foundCandidate++;

                    var identity = controller.identity;
                    Log($"[AutoRegisterUIFromScene] candidate: {identity} on {mb.name} ({mb.GetType().Name})");

                    if (identity == EUIIdentity.None)
                        continue;

                    var contextType = GetDefaultContextType(identity);
                    if (contextType == null)
                    {
                        Log($"[AutoRegisterUIFromScene] skip {identity}: no context mapping");
                        continue;
                    }

                    // RegisterUI 内部会覆盖/更新，避免重复注册导致崩溃
                    RegisterUI(identity, contextType.Value, controller, viewAdapter);
                    registered++;
                }
            }

            Log($"[AutoRegisterUIFromScene] foundCandidate={foundCandidate}, registered={registered}");

            // 重点：把你当前桥接器会用到的 identity 打印出来，快速定位是否注册成功
            foreach (var id in new[]
                     {
                         EUIIdentity.PausePanel,
                         EUIIdentity.TankAimPanel,
                         EUIIdentity.TankStatePanel,
                         EUIIdentity.MapPanel
                     })
            {
                Log($"[AutoRegisterUIFromScene] registry has {id} ? {(_registry != null && _registry.IsRegistered(id))}");
            }
        }

        /// <summary>
        /// 最小映射：给自动注册用。
        /// 若未来你要改 contextType，只需调整这里。
        /// </summary>
        private EUIContextType? GetDefaultContextType(EUIIdentity identity)
        {
            return identity switch
            {
                EUIIdentity.SubtitlePanel => EUIContextType.Permanent,
                EUIIdentity.FpsPanel => EUIContextType.Permanent,

                EUIIdentity.TankAimPanel => EUIContextType.Gameplay,
                EUIIdentity.TankStatePanel => EUIContextType.Gameplay,
                EUIIdentity.FcsHudPanel => EUIContextType.Gameplay,

                EUIIdentity.MapPanel => EUIContextType.Overlay,
                EUIIdentity.PausePanel => EUIContextType.Overlay,
                EUIIdentity.MissionPanel => EUIContextType.Overlay,
                EUIIdentity.SubtitleOverlay => EUIContextType.Overlay,

                EUIIdentity.SettingsPanel => EUIContextType.System,

                _ => null
            };
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (!SceneLoader.IsScene(scene, SceneLoader.Scene.GameScene))
            {
                SetCursorLocked(false);
            }

            // 状态重置（含 _isCgPlaying：场景切换必须清除 CG 锁，防止 CgPlaybackSystem 跨场景残留状态锁住 HUD）
            _isPaused = false;
            _isTabed = false;
            _isMapShown = false;
            _isSettingUIVisible = false;
            _isAimMode = false;
            _isCgPlaying = false;

            _crossLayering?.RestoreAll();

            // 每次进入 GameScene 时重新扫描注册 UI（Awake 在主菜单场景执行时 GameScene 内组件尚未加载）
            if (SceneLoader.IsScene(scene, SceneLoader.Scene.GameScene))
            {
                AutoRegisterUIFromScene();
            }

            BindInputEvents();
            RefreshCursorLockState();
            RefreshUIState();

            if (!SceneLoader.IsScene(scene, SceneLoader.Scene.GameScene))
                SetCursorLocked(false);
        }

        private void Update()
        {
            if (IsGameplayControlLocked)
                return;

            if (MIddleInputingController.Instance != null && MIddleInputingController.Instance.IsAimingPressed())
            {
                ToggleAimMode();
            }
        }

        #region Public API — Register / Push / Pop / Query

        /// <summary>注册一个 UI 面板（Controller + ViewAdapter）</summary>
        public void RegisterUI<TData>(EUIIdentity identity, EUIContextType contextType, IUIController<TData> controller, IUIViewAdapter viewAdapter)
        {
            _registry.Register(identity, contextType, controller, viewAdapter);
        }

        /// <summary>注销一个 UI 面板</summary>
        public void UnregisterUI(EUIIdentity identity)
        {
            _stackManager.Pop(identity);
        }

        /// <summary>打开 UI（推入对应层级栈）</summary>
        public void OpenUI<TData>(EUIIdentity identity, TData data, EUIPushBehavior behavior = EUIPushBehavior.Additive)
        {
            if (!_registry.IsRegistered(identity))
            {
                Debug.LogError($"[NewUIManager.OpenUI] 面板 {identity} 未注册，无法打开。");
                return;
            }

            var controller = _registry.GetController<TData>(identity);
            if (controller == null)
            {
                Debug.LogError($"[NewUIManager.OpenUI] 面板 {identity} 控制器类型不匹配。");
                return;
            }

            controller.Open(data);
            _stackManager.Push(identity, behavior, autoOpen: true);
            Log($"OpenUI: {identity} (behavior: {behavior})");
        }

        /// <summary>关闭 UI（从对应层级栈弹出）</summary>
        public void CloseUI(EUIIdentity identity)
        {
            _stackManager.Pop(identity);
        }

        /// <summary>根据标识获取已注册的控制器</summary>
        public IUIController<TData> GetController<TData>(EUIIdentity identity)
        {
            return _registry.GetController<TData>(identity);
        }

        /// <summary>面板是否已注册</summary>
        public bool IsUIRegistered(EUIIdentity identity)
        {
            return _registry.IsRegistered(identity);
        }

        /// <summary>面板是否已打开（存在于栈中）</summary>
        public bool IsOpen(EUIIdentity identity)
        {
            return _stackManager.IsPanelInStack(identity);
        }

        /// <summary>便捷方法：打开 UI（无数据）</summary>
        internal void Open(EUIIdentity identity, EUIPushBehavior behavior = EUIPushBehavior.Additive)
        {
            OpenUI<object>(identity, null, behavior);
        }

        /// <summary>便捷方法：关闭 UI</summary>
        internal void Close(EUIIdentity identity)
        {
            CloseUI(identity);
        }

        #endregion

        #region Stack → Controller/ViewAdapter Bridge

        private void OnPushOpen(EUIIdentity identity)
        {
            var adapter = _registry.GetViewAdapter(identity);
            if (adapter != null)
            {
                adapter.SetVisible(true);
                adapter.SetSortingOrder((int)_registry.GetContextType(identity) * 100);
            }
        }

        private void OnPopClose(EUIIdentity identity)
        {
            var controller = _registry.GetController<object>(identity);
            controller?.Close();

            var adapter = _registry.GetViewAdapter(identity);
            adapter?.SetVisible(false);
            Log($"CloseUI: {identity}");
        }

        private void OnSuspend(EUIIdentity identity)
        {
            var controller = _registry.GetController<object>(identity);
            controller?.Suspend();

            var adapter = _registry.GetViewAdapter(identity);
            adapter?.SetVisible(false);
            Log($"Suspend: {identity}");
        }

        private void OnResume(EUIIdentity identity)
        {
            var controller = _registry.GetController<object>(identity);
            controller?.Resume();

            var adapter = _registry.GetViewAdapter(identity);
            if (adapter != null)
            {
                adapter.SetVisible(true);
                adapter.SetSortingOrder((int)_registry.GetContextType(identity) * 100);
            }
            Log($"Resume: {identity}");
        }

        private void OnCover(EUIIdentity identity)
        {
            var controller = _registry.GetController<object>(identity);
            controller?.OnCovered();

            var adapter = _registry.GetViewAdapter(identity);
            adapter?.SetVisible(false);
            Log($"Cover: {identity}");
        }

        private void OnReveal(EUIIdentity identity)
        {
            var controller = _registry.GetController<object>(identity);
            controller?.OnRevealed();

            var adapter = _registry.GetViewAdapter(identity);
            if (adapter != null)
            {
                adapter.SetVisible(true);
                adapter.SetSortingOrder((int)_registry.GetContextType(identity) * 100);
            }
            Log($"Reveal: {identity}");
        }

        #endregion

        internal void BindSettingEvents()
        {
            if (_isSettingBound || settingManager == null)
                return;

            settingManager.OnApplyAllSettings += HandleSettingsApplied;
            settingManager.OnCancelAllSettings += HandleSettingsCancelled;
            _isSettingBound = true;
        }

        internal void UnbindSettingEvents()
        {
            if (!_isSettingBound || settingManager == null)
            {
                _isSettingBound = false;
                return;
            }

            settingManager.OnApplyAllSettings -= HandleSettingsApplied;
            settingManager.OnCancelAllSettings -= HandleSettingsCancelled;
            _isSettingBound = false;
        }

        private void HandleSettingsApplied(SettingManager manager)
        {
            ShowPauseUI();
        }

        private void HandleSettingsCancelled(SettingManager manager)
        {
            ShowPauseUI();
        }

        /// <summary>判断当前是否在 GameScene</summary>
        private bool IsGameplayScene()
        {
            return SceneLoader.IsScene(SceneManager.GetActiveScene(), SceneLoader.Scene.GameScene);
        }

        private void SetCursorLocked(bool locked)
        {
            CursorEngine.SetLocked(locked);
        }

        internal void Log(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log($"[NewUIManager] {message}");
            }
        }
    }
}
