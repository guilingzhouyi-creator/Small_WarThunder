namespace NSettingSystem
{
    /// <summary>
    /// 统一提示请求数据。
    /// </summary>
    public readonly struct SettingPromptData
    {
        public SettingPromptData(string tabKey, string messageKey, string messageText, SettingPromptType promptType, float durationSeconds, int priority, bool autoClose)
        {
            TabKey = tabKey;
            MessageKey = messageKey;
            MessageText = messageText;
            PromptType = promptType;
            DurationSeconds = durationSeconds;
            Priority = priority;
            AutoClose = autoClose;
        }

        public string TabKey { get; }

        public string MessageKey { get; }

        public string MessageText { get; }

        public SettingPromptType PromptType { get; }

        public float DurationSeconds { get; }

        public int Priority { get; }

        public bool AutoClose { get; }
    }
}