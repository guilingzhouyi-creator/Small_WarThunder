using UnityEngine;

/// <summary>
/// 关卡任务区域标记组件。
/// 挂载在超大地图的子区域预制体上，标识该区域是一个任务区域。
/// LevelStreamingEngine 加载地图时扫描此组件，确保 GameLevelManager 已注册到 GameManager。
/// </summary>
public class LevelMissionMarker : MonoBehaviour
{
    [SerializeField] private int _levelIndex;
    public int LevelIndex => _levelIndex;
}
