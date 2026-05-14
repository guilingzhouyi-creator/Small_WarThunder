using System;
using System.Collections.Generic;
using NSettingSystem;
using UnityEngine;
using UnityEngine.UI;

public partial class SettingManager
{
    private void NormalizeTabConfiguration()
    {
        SortNavigationButtonsBySiblingIndex();
        _subSettingEntries = BuildResolvedSubSettingEntries();
    }

    private void SortNavigationButtonsBySiblingIndex()
    {
        if (_settingTabNavigationButtons == null)
        {
            return;
        }

        _settingTabNavigationButtons.Sort((left, right) =>
        {
            if (left == right) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex());
        });
    }

    private List<SubSettingEntry> BuildResolvedSubSettingEntries()
    {
        List<SubSettingEntry> resolvedEntries = new List<SubSettingEntry>();
        Dictionary<string, SubSettingEntry> existingEntriesByKey = new Dictionary<string, SubSettingEntry>(StringComparer.Ordinal);

        if (_subSettingEntries != null)
        {
            foreach (SubSettingEntry entry in _subSettingEntries)
            {
                if (!(entry.controller is ISettingTabController controller) || string.IsNullOrEmpty(controller.tabKey))
                {
                    continue;
                }

                existingEntriesByKey[controller.tabKey] = entry;
            }
        }

        if (_settingTabNavigationButtons == null)
        {
            return _subSettingEntries ?? resolvedEntries;
        }

        for (int index = 0; index < _settingTabNavigationButtons.Count; index++)
        {
            Button navigationButton = _settingTabNavigationButtons[index];
            if (navigationButton == null)
            {
                continue;
            }

            string tabKey = ResolveTabKeyFromButtonName(navigationButton.gameObject.name);
            if (string.IsNullOrEmpty(tabKey))
            {
                continue;
            }

            if (existingEntriesByKey.TryGetValue(tabKey, out SubSettingEntry existingEntry))
            {
                resolvedEntries.Add(existingEntry);
                continue;
            }

            MonoBehaviour controller = ResolveOrCreateController(tabKey);
            if (controller == null)
            {
                continue;
            }

            resolvedEntries.Add(new SubSettingEntry
            {
                controller = controller,
                applyButton = FindFirstNamedButton(controller.transform, "ApplyButton"),
                cancelButton = FindFirstNamedButton(controller.transform, "CancelButton")
            });
        }

        return resolvedEntries;
    }

    private MonoBehaviour ResolveOrCreateController(string tabKey)
    {
        string panelName = ResolvePanelNameForTabKey(tabKey);
        if (string.IsNullOrEmpty(panelName))
        {
            return null;
        }

        Transform panelTransform = transform.Find(panelName);
        if (panelTransform == null)
        {
            panelTransform = CreatePlaceholderPanel(panelName, tabKey).transform;
        }

        MonoBehaviour dedicatedPlaceholderController = TryAttachDedicatedPlaceholderController(panelTransform.gameObject, tabKey);
        if (dedicatedPlaceholderController != null)
        {
            return dedicatedPlaceholderController;
        }

        MonoBehaviour controller = FindTabController(panelTransform.gameObject, tabKey);
        if (controller != null)
        {
            return controller;
        }

        Debug.LogWarning($"[SettingManager] Tab '{tabKey}' 缺少对应控制器，且未配置专用占位控制器。", this);
        return null;
    }

    private static MonoBehaviour TryAttachDedicatedPlaceholderController(GameObject panelObject, string tabKey)
    {
        if (panelObject == null)
        {
            return null;
        }

        if (string.Equals(tabKey, SettingConstants.TabKeyGeneral, StringComparison.Ordinal))
        {
            GeneralSettingController controller = panelObject.GetComponent<GeneralSettingController>();
            if (controller == null)
            {
                controller = panelObject.AddComponent<GeneralSettingController>();
            }

            return controller;
        }

        if (string.Equals(tabKey, SettingConstants.TabKeyVisual, StringComparison.Ordinal))
        {
            VisualSettingController controller = panelObject.GetComponent<VisualSettingController>();
            if (controller == null)
            {
                controller = panelObject.AddComponent<VisualSettingController>();
            }

            return controller;
        }

        return null;
    }

    private static MonoBehaviour FindTabController(GameObject gameObject, string tabKey)
    {
        MonoBehaviour[] behaviours = gameObject.GetComponents<MonoBehaviour>();
        for (int index = 0; index < behaviours.Length; index++)
        {
            MonoBehaviour behaviour = behaviours[index];
            if (!(behaviour is ISettingTabController controller))
            {
                continue;
            }

            if (string.Equals(controller.tabKey, tabKey, StringComparison.Ordinal))
            {
                return behaviour;
            }
        }

        return null;
    }

    private static Button FindFirstNamedButton(Transform root, string buttonName)
    {
        if (root == null || string.IsNullOrEmpty(buttonName))
        {
            return null;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int index = 0; index < buttons.Length; index++)
        {
            Button button = buttons[index];
            if (button != null && string.Equals(button.gameObject.name, buttonName, StringComparison.Ordinal))
            {
                return button;
            }
        }

        return null;
    }

    private GameObject CreatePlaceholderPanel(string panelName, string tabKey)
    {
        GameObject panel = new GameObject(panelName, typeof(RectTransform));
        panel.transform.SetParent(transform, false);

        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, -87.5f);
        rectTransform.sizeDelta = new Vector2(-16f, -195f);

        CreatePlaceholderLabel(rectTransform, GetPlaceholderTitle(tabKey));

        panel.SetActive(false);
        return panel;
    }

    private static void CreatePlaceholderLabel(RectTransform parent, string title)
    {
        GameObject labelObject = new GameObject("PlaceholderLabel", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(parent, false);

        RectTransform labelTransform = labelObject.GetComponent<RectTransform>();
        labelTransform.anchorMin = new Vector2(0.5f, 0.5f);
        labelTransform.anchorMax = new Vector2(0.5f, 0.5f);
        labelTransform.pivot = new Vector2(0.5f, 0.5f);
        labelTransform.sizeDelta = new Vector2(820f, 120f);
        labelTransform.anchoredPosition = Vector2.zero;

        Text text = labelObject.GetComponent<Text>();
        text.text = $"{title} 暂未接入，当前为临时占位页";
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontSize = 30;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static string ResolveTabKeyFromButtonName(string buttonName)
    {
        if (string.IsNullOrEmpty(buttonName))
        {
            return null;
        }

        if (buttonName.StartsWith("General", StringComparison.OrdinalIgnoreCase))
        {
            return SettingConstants.TabKeyGeneral;
        }

        if (buttonName.StartsWith("Visual", StringComparison.OrdinalIgnoreCase))
        {
            return SettingConstants.TabKeyVisual;
        }

        if (buttonName.StartsWith("Sound", StringComparison.OrdinalIgnoreCase))
        {
            return SettingConstants.TabKeyAudio;
        }

        if (buttonName.StartsWith("Key", StringComparison.OrdinalIgnoreCase))
        {
            return SettingConstants.TabKeyKeyBinding;
        }

        return null;
    }

    private static string ResolvePanelNameForTabKey(string tabKey)
    {
        switch (tabKey)
        {
            case SettingConstants.TabKeyGeneral:
                return "GeneralSettingPannel";
            case SettingConstants.TabKeyVisual:
                return "VisualSettingPannel";
            case SettingConstants.TabKeyAudio:
                return "SoundSettingPannel";
            case SettingConstants.TabKeyKeyBinding:
                return "KeyBingSettingPannel";
            default:
                return null;
        }
    }

    private static string GetPlaceholderTitle(string tabKey)
    {
        switch (tabKey)
        {
            case SettingConstants.TabKeyGeneral:
                return "通用设置";
            case SettingConstants.TabKeyVisual:
                return "画面设置";
            case SettingConstants.TabKeyAudio:
                return "音频设置";
            case SettingConstants.TabKeyKeyBinding:
                return "按键设置";
            default:
                return "设置";
        }
    }
}
