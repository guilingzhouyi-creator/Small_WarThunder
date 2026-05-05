using UnityEngine;
using UnityEngine.UI;

public class GameOverUIController : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;

    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() =>
        {
            MissionNarrativeRuntime.ResetAll(false);
            GlobalSubtitleEngine.Instance?.ClearCurrentOutput();
            SubtitleOverlayController.Instance?.ClearOverlay();
            SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
        });
    }

}
