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
            Debug.LogError("PauseUIController: buttons are not assigned in the inspector.", this);
            return;
        }

        OptionsButton.onClick.AddListener(() =>
        {
            if (UIManager.Instance == null)
            {
                Debug.LogError("PauseUIController: UIManager instance not found.", this);
                return;
            }

            UIManager.Instance.OpenOverlay(UIOverlayId.Setting);
        });
    }

    private void Start()
    {
        ResumeButton.onClick.AddListener(() =>
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CloseOverlay(UIOverlayId.Pause);
            }
        });

        QuitButton.onClick.AddListener(() =>
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CloseOverlay(UIOverlayId.Pause);
            }

            SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
        });
    }
}
