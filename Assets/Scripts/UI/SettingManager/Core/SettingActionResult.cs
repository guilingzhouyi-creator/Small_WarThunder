namespace NSettingSystem
{
    public enum SettingActionType
    {
        Apply = 0,
        Reset,
        Cancel
    }

    public enum SettingActionStatus
    {
        Success = 0,
        Fail,
        CancelExit,
        CancelRollback,
        NoOp
    }

    public enum SettingPromptType
    {
        Success = 0,
        Error,
        Info
    }

    /// <summary>
    /// 设置动作执行结果：统一承载 Tab 业务执行后的状态、提示与关闭意图。
    /// </summary>
    public readonly struct SettingActionResult
    {
        private SettingActionResult(
            string tabKey,
            SettingActionType actionType,
            SettingActionStatus status,
            string messageText,
            string messageKey,
            bool showPrompt,
            bool closeSettings,
            bool isValid)
        {
            TabKey = tabKey;
            ActionType = actionType;
            Status = status;
            MessageText = messageText;
            MessageKey = messageKey;
            ShowPrompt = showPrompt;
            CloseSettings = closeSettings;
            IsValid = isValid;
        }

        public string TabKey { get; }

        public SettingActionType ActionType { get; }

        public SettingActionStatus Status { get; }

        public string MessageText { get; }

        public string MessageKey { get; }

        public bool ShowPrompt { get; }

        public bool CloseSettings { get; }

        public bool IsValid { get; }

        public static SettingActionResult Success(string tabKey, SettingActionType actionType, string messageText, string messageKey = null)
        {
            return new SettingActionResult(tabKey, actionType, SettingActionStatus.Success, messageText, messageKey, true, false, true);
        }

        public static SettingActionResult Fail(string tabKey, SettingActionType actionType, string messageText, string messageKey = null)
        {
            return new SettingActionResult(tabKey, actionType, SettingActionStatus.Fail, messageText, messageKey, true, false, true);
        }

        public static SettingActionResult CancelExit(string tabKey)
        {
            return new SettingActionResult(tabKey, SettingActionType.Cancel, SettingActionStatus.CancelExit, string.Empty, null, false, true, true);
        }

        public static SettingActionResult CancelRollback(string tabKey)
        {
            return new SettingActionResult(tabKey, SettingActionType.Cancel, SettingActionStatus.CancelRollback, string.Empty, null, false, true, true);
        }

        public static SettingActionResult NoOp(string tabKey, SettingActionType actionType)
        {
            return new SettingActionResult(tabKey, actionType, SettingActionStatus.NoOp, string.Empty, null, false, false, true);
        }
    }
}