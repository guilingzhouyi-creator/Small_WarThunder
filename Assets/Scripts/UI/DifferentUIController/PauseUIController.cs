using UnityEngine;
using UnityEngine.UI;


public class PauseUIController : MonoBehaviour
{
    [SerializeField] private Button OptionsButton;
    [SerializeField] private Button QuitButton;
    [SerializeField] private Button ResumeButton;


    private void Awake()
    {

        if (OptionsButton == null || QuitButton == null || ResumeButton == null)
        {
            Debug.LogError("PauseUIController: 请确保在 Inspector 中分配了所有按钮组件（OptionsButton、QuitButton、ResumeButton）。", this);
            return;
        }

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

    }

    private void Start()
    {
        ResumeButton.onClick.AddListener(() =>
        {
            Debug.Log("Resume 按钮被点击了！正在尝试恢复游戏...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetPaused(false);   // 让 UIManager 统一处理
            }
        });

        QuitButton.onClick.AddListener(() =>
        {
            Debug.Log("Quit 按钮被点击了！正在尝试返回主菜单...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetPaused(false);
            }
            SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
        });
    }


}