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
        ValidateRuntimeGameLevel(scene);
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

    /// <summary>
    /// 检查运行时关卡是否已注册。
    /// </summary>
    private void ValidateRuntimeGameLevel(Scene scene)
    {
        if (_runtimeGameLevel == null)
        {
            Debug.LogWarning($"[GameManager.Validation] 场景 '{scene.name}' 中没有解析到 GameLevelManager（_runtimeGameLevel 为 null）。", this);
        }
        else if (_runtimeGameLevel.gameObject.scene != scene)
        {
            Debug.LogWarning(
                $"[GameManager.Validation] _runtimeGameLevel 所在的场景 '{_runtimeGameLevel.gameObject.scene.name}' 与当前场景 '{scene.name}' 不一致。",
                this);
        }
    }
}
