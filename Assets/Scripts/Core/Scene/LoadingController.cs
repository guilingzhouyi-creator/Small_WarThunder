// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
// using System.Collections;

// public class LoadingController : MonoBehaviour
// {
//     [Header("UI 设置")]
//     [SerializeField] private CanvasGroup loadingOverlay;
//     [SerializeField] private Image progressBar;
//     [SerializeField] private TextMeshProUGUI progressText;

//     public void Start()
//     {
//         StartCoroutine(AsyncLoadingRoutine(SceneLoader.Scene.GameScene));
//     }

//     private IEnumerator AsyncLoadingRoutine(SceneLoader.Scene targetScene)
//     {
//         loadingOverlay.gameObject.SetActive(true);
//         loadingOverlay.alpha = 0f;
//         progressBar.fillAmount = 0f;

//         while (loadingOverlay.alpha < 1f)
//         {
//             loadingOverlay.alpha += Time.deltaTime * 2f; // 2秒淡入
//             yield return null;
//         }


//         AsyncOperation asyncLoad = SceneLoader.LoadSceneAsync(targetScene);
//         asyncLoad.allowSceneActivation = false;

//         while (!asyncLoad.isDone)
//         {
//             float smoothedProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

//             progressBar.fillAmount = smoothedProgress;

//             progressText.text = $"Loading... {Mathf.RoundToInt(smoothedProgress * 100f)}%";

//             if (asyncLoad.progress >= 0.9f && smoothedProgress >= 1f)
//             {
//                 // 加载完成，等待一小段时间后激活场景
//                 progressText.text = "Press any key to continue...";
//                 if (Input.anyKeyDown)
//                 {
//                     asyncLoad.allowSceneActivation = true;
//                 }

//                 // yield return new WaitForSeconds(0.5f);
//             }

//             yield return null;
//         }
//     }
// }


using UnityEngine;
using System.Collections;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private RectTransform progressFill;     // 必须拖 ProgressFill 的 RectTransform
    [SerializeField] private TextMeshProUGUI stateTextMesh;
    [SerializeField] private float minDisplayTime = 2.0f;    // 增加到 2 秒，避免太快结束
    [SerializeField] private float lerpSpeed = 6f;           // 平滑速度，可调

    private float maxWidth = 1920f;   // 你想要的满进度宽度（直接写死 1920）
    private AsyncOperation currentOperation;

    private void Awake()
    {
        // 防御性确保 Loading 场景时间流速正常（避免从 Pause 状态跳转进入）
        TimeManager.Instance.EnsureNormalTime();

        // 确保初始宽度为 0
        if (progressFill != null)
        {
            progressFill.sizeDelta = new Vector2(0f, progressFill.sizeDelta.y);
        }

        EnsureSceneAudioListener();
    }

    private void Start()
    {
        Debug.Log("✅ LoadingManager.Start() 执行，开始异步加载 GameScene");
        StartCoroutine(LoadGameSceneAsync());
    }

    private IEnumerator LoadGameSceneAsync()
    {

        currentOperation = SceneLoader.LoadSceneAsync(SceneLoader.Scene.GameScene, false);

        if (currentOperation == null)
        {
            Debug.LogError("❌ LoadSceneAsync 返回 null！");
            yield break;
        }

        float timer = 0f;
        float displayedProgress = 0f;

        while (!currentOperation.isDone)
        {
            timer += Time.deltaTime;

            // 真实进度（0~0.9 映射到 0~1）
            float realProgress = Mathf.Clamp01(currentOperation.progress / 0.9f);

            // 平滑插值（关键，让条慢慢伸长）
            displayedProgress = Mathf.Lerp(displayedProgress, realProgress, Time.deltaTime * lerpSpeed);

            // === 用 Width 控制进度条 ===
            if (progressFill != null)
            {
                float currentWidth = displayedProgress * maxWidth;
                progressFill.sizeDelta = new Vector2(currentWidth, progressFill.sizeDelta.y);
            }

            // 更新文字
            if (stateTextMesh != null)
            {
                stateTextMesh.text = $"Loading {Mathf.RoundToInt(displayedProgress * 100)}%";
            }

            // 只有真实加载完成 + 过了最短时间，才允许切换
            if (currentOperation.progress >= 0.9f && timer >= minDisplayTime)
            {
                currentOperation.allowSceneActivation = true;
                Debug.Log($"✅ 允许激活场景！当前显示进度: {displayedProgress:P0}");
            }

            yield return null;
        }

        Debug.Log("🎉 GameScene 加载完成！");
    }

    private void EnsureSceneAudioListener()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        if (cameras == null || cameras.Length == 0)
        {
            return;
        }

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null)
            {
                continue;
            }

            AudioListener existingListener = camera.GetComponent<AudioListener>();
            if (existingListener != null && existingListener.enabled)
            {
                return;
            }
        }

        Camera targetCamera = Camera.main != null ? Camera.main : cameras[0];
        if (targetCamera == null)
        {
            return;
        }

        AudioListener listener = targetCamera.GetComponent<AudioListener>();
        if (listener == null)
        {
            listener = targetCamera.gameObject.AddComponent<AudioListener>();
        }

        listener.enabled = true;
    }
}
