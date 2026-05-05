#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;


public class MainMenuUIController : MonoBehaviour
{
    [SerializeField] private Button StartGameButton;
    [SerializeField] private Button OptionsButton;
    [SerializeField] private Button QuitButton;

    // private LoadingManager loadingManager;

    private void Awake()
    {
        TimeManager.Instance.EnsureNormalTime();
        StartGameButton.onClick.AddListener(() =>
        {
            GameManager.ResetStaticData(); // 在进入游戏场景前重置所有静态数据，确保游戏状态干净且一致。
            SceneLoader.LoadScene(SceneLoader.Scene.LoadingScene);
        });



        OptionsButton.onClick.AddListener(() =>
        {
            Debug.Log("Options 按钮被点击了！正在尝试打开设置面板...");

            if (UIManager.Instance == null)
            {
                Debug.LogError("找不到 UIManager 实例！");
                return;
            }

            UIManager.Instance.ShowSettingsUI();
        });


        QuitButton.onClick.AddListener(() =>
        {
            Debug.Log("退出游戏...");

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
        });
    }



}
