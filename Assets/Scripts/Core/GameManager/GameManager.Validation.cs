using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager 的可插拔验证层。
/// 仅做诊断性检查（日志输出），不污染总控层的调度逻辑。
/// 如不需要验证功能，可直接删除此文件。
/// </summary>
public partial class GameManager : MonoBehaviour
{
    [Header("验证选项")]
    [SerializeField] private bool _runValidationOnStart = true;
    [SerializeField] private bool _runValidationOnSceneLoad = true;

    private void StartValidationIfNeeded()
    {
        if (!_runValidationOnStart) return;
        ValidateSystemReferences();
    }

    private void OnSceneLoadedValidationIfNeeded(Scene scene)
    {
        if (!_runValidationOnSceneLoad) return;
        if (!SceneLoader.IsScene(scene, SceneLoader.Scene.GameScene)) return;
        ValidateSystemReferences();
    }

    // ───────────── 检查方法 ─────────────

    /// <summary>
    /// 检查系统引用是否可解析。
    /// </summary>
    private void ValidateSystemReferences()
    {
        if (_settingManager == null && SettingManager.Instance == null)
        {
            Debug.LogWarning("[GameManager.Validation] SettingManager 未绑定且单例不可用。", this);
        }

        if (_audioManager == null && AudioManager.Instance == null)
        {
            Debug.LogWarning("[GameManager.Validation] AudioManager 未绑定且单例不可用。", this);
        }
    }
}
