using System;
using UnityEngine;

namespace NSettingSystem
{
    internal sealed class SettingPromptRouter
    {
        private readonly SettingPromptService _promptService;
        private readonly Action _onCloseSettingsRequested;

        public SettingPromptRouter(SettingPromptService promptService, Action onCloseSettingsRequested)
        {
            _promptService = promptService;
            _onCloseSettingsRequested = onCloseSettingsRequested;
        }

        public void Route(SettingActionResult result)
        {
            Debug.Log($"[SettingPromptFlow][Router] Route status={result.Status}, action={result.ActionType}, showPrompt={result.ShowPrompt}, closeSettings={result.CloseSettings}, message={result.MessageText}");

            if (!result.IsValid || result.Status == SettingActionStatus.NoOp)
            {
                Debug.LogWarning("[SettingPromptFlow][Router] Route exited early because result is invalid or NoOp");
                return;
            }

            if (result.ShowPrompt)
            {
                SettingPromptData promptData = BuildPromptData(result);
                Debug.Log($"[SettingPromptFlow][Router] Dispatching prompt text='{promptData.MessageText}', type={promptData.PromptType}, duration={promptData.DurationSeconds}");
                _promptService?.Show(promptData);
            }

            if (result.CloseSettings)
            {
                Debug.Log("[SettingPromptFlow][Router] Closing settings after result routing");
                _onCloseSettingsRequested?.Invoke();
            }
        }

        private static SettingPromptData BuildPromptData(SettingActionResult result)
        {
            if (result.Status == SettingActionStatus.Fail)
            {
                return new SettingPromptData(result.TabKey, result.MessageKey, ResolveMessageText(result), SettingPromptType.Error, 1.8f, 10, true);
            }

            return new SettingPromptData(result.TabKey, result.MessageKey, ResolveMessageText(result), SettingPromptType.Success, 1.4f, 0, true);
        }

        private static string ResolveMessageText(SettingActionResult result)
        {
            if (!string.IsNullOrWhiteSpace(result.MessageText))
            {
                return result.MessageText;
            }

            switch (result.ActionType)
            {
                case SettingActionType.Apply:
                    return "设置已应用";
                case SettingActionType.Reset:
                    return "设置已重置";
                default:
                    return "设置已更新";
            }
        }
    }
}