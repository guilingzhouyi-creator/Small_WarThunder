using UnityEngine;

/// <summary>
/// 极简标记组件：挂载后自动 DontDestroyOnLoad，使节点在场景切换时保持。
/// 适用于 ObjectPool 下的空容器节点（如 TerrainObjectPoolGroup）。
/// </summary>
public class DontDestroyOnLoadMarker : MonoBehaviour
{
    private static bool _hasLoggedNonRootSkipInfo;

    private void Awake()
    {
        if (transform.parent != null)
        {
            if (!_hasLoggedNonRootSkipInfo)
            {
                _hasLoggedNonRootSkipInfo = true;
                Debug.Log("[DontDestroyOnLoadMarker] 检测到非根节点标记，已跳过 DontDestroyOnLoad。需要常驻时请把标记挂到根对象上。", this);
            }

            return;
        }

        DontDestroyOnLoad(gameObject);
    }
}
