using UnityEngine;
using UnityEngine.UI;
using NNewUIFramework;

public class PauseUIController : UGUIViewAdapter
{
    public override EUIIdentity identity => EUIIdentity.PausePanel;

    [SerializeField] private Button OptionsButton;
    [SerializeField] private Button QuitButton;
    [SerializeField] private Button ResumeButton;
    [SerializeField] private Button SaveButton;

    protected override void Awake()
    {
        base.Awake();

        if (OptionsButton == null || QuitButton == null || ResumeButton == null || SaveButton == null)
        {
            Debug.LogError("PauseUIController: 按钮组尚有按钮组件没有分配，请检查。", this);
            return;
        }

        OptionsButton.onClick.AddListener(() =>
        {
            if (NewUIManager.instance == null)
            {
                Debug.LogError("PauseUIController: UIManager的实例未找到。", this);
                return;
            }

            NewUIManager.instance.ShowSettingsUI();
        });
    }

    private void Start()
    {
        ResumeButton.onClick.AddListener(() =>
        {
            if (NewUIManager.instance != null)
            {
                NewUIManager.instance.ClosePauseUI();
            }
        });

        QuitButton.onClick.AddListener(() =>
        {
            if (NewUIManager.instance != null)
            {
                NewUIManager.instance.ClosePauseUI();
            }

            MissionNarrativeRuntime.ResetAll(false);
            GlobalSubtitleEngine.Instance?.ClearCurrentOutput();
            SubtitleOverlayController.Instance?.ClearOverlay();

            CursorEngine.Unlock();
            SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
        });

        SaveButton.onClick.AddListener(() =>
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.TestSave();
            }
            else
            {
                Debug.LogError("PauseUIController: SaveManager 实例未找到，请确保场景中存在 SaveManager GameObject。", this);
            }
        });
    }
}
