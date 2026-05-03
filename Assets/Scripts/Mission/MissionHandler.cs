using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Globalization;

public class MissionHandler : MonoBehaviour
{
    private Label _localTimeLabel;
    private Label _worldTimeLabel;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // 查找 UXML 中定义的 Name
        _localTimeLabel = root.Q<Label>("Local-Timer");
        _worldTimeLabel = root.Q<Label>("World-Timer");

        // UXML 引用断裂时自动创建标签，不依赖外部资源
        if (_localTimeLabel == null || _worldTimeLabel == null)
        {
            CreateTimeLabels(root);
        }

        TimeSystem.OnTimeChanged += RefreshUI;
    }

    private void OnDisable()
    {
        TimeSystem.OnTimeChanged -= RefreshUI;
    }

    /// <summary>
    /// UXML 缺失时自动创建时间标签容器，确保时间显示不受资源迁移影响。
    /// </summary>
    private void CreateTimeLabels(VisualElement root)
    {
        // 清除旧查询（可能已失效）
        _localTimeLabel = null;
        _worldTimeLabel = null;

        var container = new VisualElement { name = "time-group-container" };
        container.style.flexGrow = 1;
        container.style.flexDirection = FlexDirection.ColumnReverse;
        container.style.alignItems = Align.Stretch;
        container.style.justifyContent = Justify.FlexEnd;
        container.style.backgroundColor = new Color(0f, 0.078f, 0.196f, 0.5f);
        container.style.paddingTop = 10;
        container.style.paddingBottom = 10;
        container.style.paddingLeft = 10;
        container.style.paddingRight = 10;

        _localTimeLabel = new Label("Label") { name = "Local-Timer" };
        _localTimeLabel.style.color = new Color(1f, 0.824f, 0f);
        _localTimeLabel.style.fontSize = 15;
        _localTimeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _localTimeLabel.style.marginBottom = 15;
        _localTimeLabel.style.unityTextAlign = TextAnchor.UpperRight;

        _worldTimeLabel = new Label("Label") { name = "World-Timer" };
        _worldTimeLabel.style.color = new Color(1f, 0.824f, 0f);
        _worldTimeLabel.style.fontSize = 15;
        _worldTimeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _worldTimeLabel.style.unityTextAlign = TextAnchor.UpperRight;

        container.Add(_localTimeLabel);
        container.Add(_worldTimeLabel);

        root.Add(container);
    }

    private void RefreshUI(DateTime currentTime)
    {
        string formattedTime = currentTime.ToString("yyyy年MM月dd日 tt hh:mm", new CultureInfo("zh-CN"));

        if (_localTimeLabel != null)
            _localTimeLabel.text = $"<color=blue>本地时间:</color> {formattedTime}";

        if (_worldTimeLabel != null)
            _worldTimeLabel.text = $"<color=green>世界时间:</color> {formattedTime}";
    }
}
