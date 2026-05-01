using UnityEngine;

/// <summary>
/// 极简标记组件：挂载后自动 DontDestroyOnLoad，使节点在场景切换时保持。
/// 适用于 ObjectPool 下的空容器节点（如 TerrainObjectPoolGroup）。
/// </summary>
public class DontDestroyOnLoadMarker : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
