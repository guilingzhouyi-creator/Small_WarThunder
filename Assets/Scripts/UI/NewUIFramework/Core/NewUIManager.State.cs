namespace NNewUIFramework
{
    /// <summary>
    /// NewUIManager partial：全局状态属性查�?
    /// IsAimMode, IsCgPlaying, IsGameplayControlLocked, IsSettingUIVisible
    /// 以及 NewUIManager.instance 静态兼容层
    /// </summary>
    public partial class NewUIManager
    {
        private bool _isPaused;
        private bool _isTabed;
        private bool _isMapShown;
        private bool _isSettingUIVisible;
        private bool _isSightUIVisible = true;
        private bool _isAimMode;
        private bool _isCgPlaying;

        /// <summary>是否处于暂停状�?/summary>
        public bool IsPaused => _isPaused;

        /// <summary>Gameplay 控制是否被锁定（任何 Overlay 打开 �?CG 播放中）</summary>
        public bool IsGameplayControlLocked => _isTabed || _isMapShown || _isPaused || _isCgPlaying;

        /// <summary>是否处于瞄准模式</summary>
        public bool IsAimMode => _isAimMode;

        /// <summary>大地图是否显�?/summary>
        public bool IsMapShown => _isMapShown;

        /// <summary>Tab 面板是否显示</summary>
        public bool IsTabed => _isTabed;

        /// <summary>设置 UI 是否可见</summary>
        public bool IsSettingUIVisible => _isSettingUIVisible;

        /// <summary>CG 是否正在播放</summary>
        public bool IsCgPlaying => _isCgPlaying;

        /// <summary>瞄准 UI 是否激活（等价 IsAimMode�?/summary>
        public bool IsTankAimActive => _isAimMode;

        /// <summary>TankStats UI 是否激�?/summary>
        public bool IsTankStatsActive => !IsGameplayControlLocked && !_isAimMode;

        /// <summary>设置 Aim 模式</summary>
        public void SetAimMode(bool isAimMode)
        {
            if (_isAimMode == isAimMode)
                return;

            _isAimMode = isAimMode;
            RefreshUIState();
        }

        /// <summary>切换 Aim 模式</summary>
        public void ToggleAimMode()
        {
            SetAimMode(!_isAimMode);
        }

        /// <summary>设置 CG 播放状�?/summary>
        public void SetCgPlaying(bool playing)
        {
            if (_isCgPlaying == playing)
                return;

            _isCgPlaying = playing;

            if (playing)
            {
                _isAimMode = false;
                _isSettingUIVisible = false;
                _isPaused = false;
            }

            RefreshCursorLockState();
            RefreshUIState();
        }

        /// <summary>刷新鼠标锁定状�?/summary>
        private void RefreshCursorLockState()
        {
            bool shouldUnlockCursor = IsGameplayControlLocked;
            SetCursorLocked(!shouldUnlockCursor);
        }

        /// <summary>使用框架 Open/Close 统一管理所有 UI 面板的可见性，取代旧有的直接 SetActive 模式。</summary>
        private void RefreshUIState()
        {
            bool isGame = IsGameplayScene();

            // ── 地图面板（UI Toolkit，引擎方法管理 mini/full 切换）──
            if (isGame)
            {
                if (!IsOpen(EUIIdentity.MapPanel))
                    Open(EUIIdentity.MapPanel);

                if (_isMapShown)
                    mapUIController?.OpenFullMap();
                else
                {
                    mapUIController?.CloseFullMap();
                    bool miniVisible = !_isPaused && !_isSettingUIVisible && !_isTabed && !_isCgPlaying;
                    bool show = miniVisible && (_isAimMode ? _showMiniMapInAim : _showMiniMapInTPS);
                    mapUIController?.SetMiniMapVisible(show);
                }
            }
            else
            {
                if (IsOpen(EUIIdentity.MapPanel))
                    Close(EUIIdentity.MapPanel);
            }

            // ── 暂停面板 ──
            bool showPause = isGame && _isPaused && !_isSettingUIVisible;
            SyncPanel(EUIIdentity.PausePanel, showPause, EUIPushBehavior.Exclusive);

            // ── 设置面板 ──
            bool showSettings = isGame && _isPaused && _isSettingUIVisible;
            SyncPanel(EUIIdentity.SettingsPanel, showSettings);

            // ── 任务面板 ──
            bool showMission = isGame && _isTabed;
            SyncPanel(EUIIdentity.MissionPanel, showMission);

            // ── 坦克状态面板 ──
            bool showTankStats = isGame && !IsGameplayControlLocked && !_isAimMode;
            SyncPanel(EUIIdentity.TankStatePanel, showTankStats);

            // ── 瞄准 UI ──
            bool showAim = isGame && !IsGameplayControlLocked && _isSightUIVisible;
            SyncPanel(EUIIdentity.TankAimPanel, showAim);

            // ── 字幕 Overlay ──
            SubtitleOverlayController.Instance?.ApplyVisibility();
        }

        /// <summary>根据目标状态打开或关闭面板。若面板未在框架栈中但 GameObject 仍处于 active 状态（场景默认），
        /// 也会直接通过控制器关闭，确保不会残留。</summary>
        private void SyncPanel(EUIIdentity identity, bool shouldBeOpen, EUIPushBehavior behavior = EUIPushBehavior.Additive)
        {
            if (!_registry.IsRegistered(identity))
                return;

            bool isOpen = IsOpen(identity);
            if (shouldBeOpen && !isOpen)
            {
                Open(identity, behavior);
            }
            else if (!shouldBeOpen)
            {
                // 框架栈中则通过正常路径关闭
                if (isOpen)
                {
                    Close(identity);
                }
                else
                {
                    // 框架未追踪但面板可能仍处于场景默认 active 状态——直接关闭控制器
                    var controller = _registry.GetController<object>(identity);
                    controller?.Close();
                }
            }
        }
    }

}
