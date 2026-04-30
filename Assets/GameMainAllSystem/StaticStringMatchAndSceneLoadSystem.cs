using UnityEngine;
using UnityEngine.SceneManagement;

public enum TankType
{
    M1A2_SEPv2,
    T_90,
    Leopard_2,
    Type_99,
    Unknown
}


public static class SceneLoader
{
    private const string SceneCatalogResourcePath = "SceneCatalog";
    private static SceneCatalog _cachedCatalog;

    public enum Scene
    {
        MainMenuScene,
        LoadingScene,
        GameScene,
        GameOverScene,
    }

    private static Object LoadSceneContext => _cachedCatalog;

    public static bool IsScene(UnityEngine.SceneManagement.Scene scene, Scene targetScene)
    {
        if (TryGetSceneBuildIndex(targetScene, out int targetBuildIndex) && targetBuildIndex >= 0 && scene.buildIndex >= 0)
        {
            return scene.buildIndex == targetBuildIndex;
        }

        if (!TryGetSceneName(targetScene, out string targetSceneName) || string.IsNullOrWhiteSpace(targetSceneName))
        {
            targetSceneName = GetFallbackSceneName(targetScene);
        }

        return !string.IsNullOrWhiteSpace(scene.name) &&
               !string.IsNullOrWhiteSpace(targetSceneName) &&
               string.Equals(scene.name, targetSceneName, System.StringComparison.OrdinalIgnoreCase);
    }

    public static void LoadScene(Scene scene)
    {
        if (TryGetSceneBuildIndex(scene, out int buildIndex) && buildIndex >= 0)
        {
            SceneManager.LoadScene(buildIndex);
            return;
        }

        if (TryGetSceneName(scene, out string sceneName) && !string.IsNullOrWhiteSpace(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        Debug.LogError($"SceneLoader: 未在 SceneCatalog 中找到场景 {scene} 的配置。请检查 Resources/SceneCatalog.asset。", LoadSceneContext);
    }

    private static bool TryGetSceneBuildIndex(Scene scene, out int buildIndex)
    {
        buildIndex = -1;

        SceneCatalog catalog = GetCatalog();
        if (catalog == null)
        {
            return false;
        }

        return catalog.TryGetSceneBuildIndex(scene, out buildIndex);
    }

    private static bool TryGetSceneName(Scene scene, out string sceneName)
    {
        sceneName = null;

        SceneCatalog catalog = GetCatalog();
        if (catalog == null)
        {
            return false;
        }

        return catalog.TryGetSceneName(scene, out sceneName) && !string.IsNullOrWhiteSpace(sceneName);
    }

    private static SceneCatalog GetCatalog()
    {
        if (_cachedCatalog == null)
        {
            _cachedCatalog = Resources.Load<SceneCatalog>(SceneCatalogResourcePath);
        }

        return _cachedCatalog;
    }

    private static string GetFallbackSceneName(Scene scene)
    {
        switch (scene)
        {
            case Scene.MainMenuScene:
                return "MainMenuScene";
            case Scene.LoadingScene:
                return "LoadingScene";
            case Scene.GameScene:
                return "Game";
            case Scene.GameOverScene:
                return "GameOverScene";
            default:
                return scene.ToString();
        }
    }


    public static AsyncOperation LoadSceneAsync(Scene scene, bool allowSceneActivation = false)
    {
        if (TryGetSceneBuildIndex(scene, out int buildIndex) && buildIndex >= 0)
        {
            AsyncOperation buildIndexOperation = SceneManager.LoadSceneAsync(buildIndex);
            if (buildIndexOperation != null)
            {
                buildIndexOperation.allowSceneActivation = allowSceneActivation;
            }

            return buildIndexOperation;
        }

        if (TryGetSceneName(scene, out string sceneName) && !string.IsNullOrWhiteSpace(sceneName))
        {
            AsyncOperation nameOperation = SceneManager.LoadSceneAsync(sceneName);
            if (nameOperation != null)
            {
                nameOperation.allowSceneActivation = allowSceneActivation;
            }

            return nameOperation;
        }

        Debug.LogError($"SceneLoader: 未在 SceneCatalog 中找到场景 {scene} 的配置。请检查 Resources/SceneCatalog.asset。");
        return null;
    }
}



