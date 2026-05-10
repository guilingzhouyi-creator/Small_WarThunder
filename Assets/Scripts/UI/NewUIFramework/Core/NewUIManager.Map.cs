namespace NNewUIFramework
{
    /// <summary>
    /// NewUIManager partial：小地图 & 大地图切换
    /// </summary>
    public partial class NewUIManager
    {
        /// <summary>切换地图显示</summary>
        public void ToggleMap()
        {
            if (_isPaused || _isSettingUIVisible)
                return;

            if (_isMapShown)
                CloseMapUI();
            else
                ShowMapUI();
        }

        /// <summary>显示大地图</summary>
        public void ShowMapUI()
        {
            if (_isPaused || _isSettingUIVisible || _isCgPlaying)
                return;

            _isMapShown = true;

            RefreshCursorLockState();
            RefreshUIState();
        }

        /// <summary>关闭大地图</summary>
        public void CloseMapUI()
        {
            if (!_isMapShown) return;

            _isMapShown = false;

            RefreshCursorLockState();
            RefreshUIState();
        }
    }
}
