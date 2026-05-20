using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager 的可插拔验证层。
/// 验证时机由 GameManager 控制，避免 SceneInitializer 反向持有启动调用权。
/// </summary>
public partial class GameManager : MonoBehaviour
{
    [Header("验证选项")]
    [SerializeField] private bool _runValidationOnStart = true;
    [SerializeField] private bool _runValidationOnSceneLoad = true;

    private void StartValidationIfNeeded()
    {
        if (!_runValidationOnStart)
        {
            return;
        }

        ValidateSystemReferences();
    }

    private void OnSceneLoadedValidationIfNeeded(Scene scene)
    {
        if (!_runValidationOnSceneLoad)
        {
            return;
        }

        if (!SceneLoader.IsScene(scene, SceneLoader.Scene.GameScene))
        {
            return;
        }

        ValidateSystemReferences();
    }

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
