namespace NNewUIFramework
{
    /// <summary>
    /// NewUIManager partial：Mission 任务面板管理
    /// </summary>
    public partial class NewUIManager
    {
        /// <summary>显示 Mission 任务面板（Tab 面板）</summary>
        public void ShowMissionUI()
        {
            if (_isPaused || _isSettingUIVisible || _isCgPlaying)
                return;

            _isTabed = true;

            RefreshCursorLockState();
            RefreshUIState();
        }

        /// <summary>关闭 Mission 任务面板</summary>
        public void CloseMissionUI()
        {
            if (!_isTabed) return;

            _isTabed = false;

            RefreshCursorLockState();
            RefreshUIState();
        }
    }
}
