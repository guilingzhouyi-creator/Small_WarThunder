using UnityEngine;


public partial class GameManager : MonoBehaviour
{
    // 必要组件检查（自身）
    private void ValidateComponentsInThis()
    {
        if (tankPrefab == null)
        {
            Debug.LogError("GameManager: tankPrefab 未设置，请在 Inspector 中分配 Tank 预制体。", this);
        }

        if (audioManager == null)
        {
            Debug.LogWarning("GameManager: AudioManager 尚未准备好，后续会在场景加载后重试。", this);
        }

        if (settingManager == null)
        {
            Debug.LogWarning("GameManager: SettingManager 尚未准备好，后续会在场景加载后重试。", this);
        }
    }

    //必要组件检查（外部）
    private void ValidateComponentsInExternal()
    {
        if (audioManager == null)
        {
            audioManager = AudioManager.Instance;
        }

        if (settingManager == null)
        {
            settingManager = SettingManager.Instance;
        }

    }


    private void SpawnPlayerTankValidate()
    {
        if (tankPrefab == null)
        {
            Debug.LogError("GameManager: 无法生成玩家坦克，因为 tankPrefab 未设置。请在 Inspector 中分配 Tank 预制体。", this);
            return;
        }

    }
}