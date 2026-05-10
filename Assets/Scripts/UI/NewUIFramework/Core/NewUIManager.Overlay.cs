namespace NNewUIFramework
{
    /// <summary>
    /// NewUIManager partial：字幕 Overlay 可见性管理
    /// </summary>
    public partial class NewUIManager
    {
        /// <summary>字幕是否可见</summary>
        public bool IsSubtitleVisible
        {
            get
            {
                var instance = SubtitleOverlayController.Instance;
                return instance != null && instance.IsSubtitleVisible;
            }
        }

        /// <summary>显示字幕</summary>
        public void ShowSubtitle()
        {
            SubtitleOverlayController.Instance?.ShowSubtitle();
        }

        /// <summary>隐藏字幕</summary>
        public void HideSubtitle()
        {
            SubtitleOverlayController.Instance?.HideSubtitle();
        }

        /// <summary>切换字幕手动可见性</summary>
        public void ToggleSubtitleManualVisibility()
        {
            SubtitleOverlayController.Instance?.ToggleManualVisibility();
        }

        /// <summary>刷新字幕可见性（根据当前游戏状态）</summary>
        public void RefreshSubtitleVisibility()
        {
            SubtitleOverlayController.Instance?.ApplyVisibility();
        }
    }
}
