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

        TimeSystem.OnTimeChanged += RefreshUI;

    }

    private void OnDisable()
    {
        TimeSystem.OnTimeChanged -= RefreshUI;
    }

    private void RefreshUI(DateTime currentTime)
    {

        string formattedTime = currentTime.ToString("yyyy年MM月dd日 tt hh:mm", new CultureInfo("zh-CN"));
        _localTimeLabel.text = $"<color=blue>本地时间:</color> {formattedTime}";
        _worldTimeLabel.text = $"<color=green>世界时间:</color> {formattedTime}";
    }



}