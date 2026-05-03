using UnityEngine;
using System;

public class TimeSystem : MonoBehaviour
{
    public static TimeSystem Instance;
    [SerializeField] private TimeData _config;

    public DateTime CurrentTime { get; private set; }
    public static event Action<DateTime> OnTimeChanged;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (!DateTime.TryParse(_config.StartTime, out var start))
        {
            Debug.LogError("无法解析 StartTime，请确保它是一个有效的日期时间字符串。");
            return;
        }

        CurrentTime = start;
    }

    private void Update()
    {
        CurrentTime = CurrentTime.AddSeconds(Time.deltaTime * _config.TimeMultiplier);
        OnTimeChanged?.Invoke(CurrentTime);
    }
}