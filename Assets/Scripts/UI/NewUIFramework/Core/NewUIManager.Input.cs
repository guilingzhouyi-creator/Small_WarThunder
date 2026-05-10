namespace NNewUIFramework
{
    /// <summary>
    /// NewUIManager partial：输入事件绑定与回调路由
    /// </summary>
    public partial class NewUIManager
    {
        internal void BindInputEvents()
        {
            if (_isInputBound || MIddleInputingController.Instance == null)
                return;

            MIddleInputingController.Instance.OnPauseInputProcessed += HandlePauseInput;
            MIddleInputingController.Instance.OnTabInputProcessed += HandleTabInput;
            MIddleInputingController.Instance.OnMapShowInputProcessed += HandleMapShowInput;
            _isInputBound = true;
        }

        internal void UnbindInputEvents()
        {
            if (!_isInputBound || MIddleInputingController.Instance == null)
            {
                _isInputBound = false;
                return;
            }

            MIddleInputingController.Instance.OnPauseInputProcessed -= HandlePauseInput;
            MIddleInputingController.Instance.OnTabInputProcessed -= HandleTabInput;
            MIddleInputingController.Instance.OnMapShowInputProcessed -= HandleMapShowInput;
            _isInputBound = false;
        }

        private void HandlePauseInput(object sender, System.EventArgs e)
        {
            if (_isSettingUIVisible)
            {
                CloseSettingsUI();
                return;
            }

            if (_isPaused)
                ClosePauseUI();
            else
                ShowPauseUI();
        }

        private void HandleTabInput(object sender, System.EventArgs e)
        {
            if (_isPaused || _isSettingUIVisible)
                return;

            if (_isMapShown)
            {
                SubtitleOverlayController.Instance?.ToggleManualVisibility();
                return;
            }

            if (_isTabed)
                CloseMissionUI();
            else
                ShowMissionUI();
        }

        private void HandleMapShowInput(object sender, System.EventArgs e)
        {
            if (_isPaused || _isSettingUIVisible)
                return;

            ToggleMap();
        }
    }
}
