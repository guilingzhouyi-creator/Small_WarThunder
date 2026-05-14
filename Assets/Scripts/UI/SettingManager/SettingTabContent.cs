using UnityEngine;

/// <summary>
/// 设置面板 Tab 内容标记组件，绑定到每个 Tab 子面板根节点，
/// 用于 SettingManager 切换 Tab 时激活/隐藏对应内容。
/// </summary>
public class SettingTabContent : MonoBehaviour
{
    [SerializeField] private string _tabKey;

    /// <summary>Tab 唯一标识，对应常量库中的 Key。</summary>
    public string tabKey => _tabKey;

    /// <summary>设置内容可见性。</summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
