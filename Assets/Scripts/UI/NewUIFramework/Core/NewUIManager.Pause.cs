namespace NNewUIFramework
{
    /// <summary>
    /// NewUIManager partial：Pause / Setting 切换和时间缩放管理
    /// </summary>
    public partial class NewUIManager
    {
        /// <summary>暂停游戏</summary>
        public void SetPaused(bool paused)
        {
            if (paused)
                ShowPauseUI();
            else
                ClosePauseUI();
        }

        /// <summary>显示暂停 UI</summary>
        public void ShowPauseUI()
        {
            if (_isPaused) return;

            _isPaused = true;
            TimeManager.Instance.Pause();
            OnGamePaused?.Invoke(this, System.EventArgs.Empty);

            // 关闭所有瞬态游戏 UI
            CloseMissionUI();
            CloseMapUI();
            _isTabed = false;
            _isMapShown = false;

            _crossLayering?.CacheIfNeeded();
            _crossLayering?.ApplyForPause();

            RefreshCursorLockState();
            RefreshUIState();
        }

        /// <summary>关闭暂停 UI</summary>
        public void ClosePauseUI()
        {
            if (!_isPaused) return;

            _isSettingUIVisible = false;
            _isPaused = false;
            TimeManager.Instance.Resume();
            OnGameUnPaused?.Invoke(this, System.EventArgs.Empty);

            _crossLayering?.RestoreAll();

            RefreshCursorLockState();
            RefreshUIState();
        }

        /// <summary>场景加载时强制关闭 Pause</summary>
        public void HidePauseUIOnSceneLoad()
        {
            _isSettingUIVisible = false;
            _isPaused = false;
            _crossLayering?.RestoreAll();
        }

        /// <summary>显示设置 UI</summary>
        public void ShowSettingsUI()
        {
            if (!_isPaused)
                ShowPauseUI();

            _isSettingUIVisible = true;

            _crossLayering?.CacheIfNeeded();
            if (_isPaused)
                _crossLayering?.ApplyForSetting();

            RefreshCursorLockState();
            RefreshUIState();
        }

        /// <summary>关闭设置 UI</summary>
        public void CloseSettingsUI()
        {
            if (!_isSettingUIVisible) return;

            _isSettingUIVisible = false;

            if (_isPaused)
                _crossLayering?.RestoreSettingCanvas();

            RefreshCursorLockState();
            RefreshUIState();
        }
    }
}
