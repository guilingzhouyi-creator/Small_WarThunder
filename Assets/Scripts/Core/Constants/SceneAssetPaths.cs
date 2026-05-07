/// <summary>
/// 集中管理所有场景和资源路径常量。
/// Resources.Load / SceneManager.LoadScene 等调用均通过此类引用，避免硬编码路径散落各处。
/// </summary>
public static class SceneAssetPaths
{
    /// <summary>场景目录资源路径（Resources 文件夹下的路径，不含扩展名）</summary>
    public const string SceneCatalog = "SceneCatalog";

    // ─── UI 资源路径 ───
    public static class UI
    {
        /// <summary>字幕 HUD 的 USS 样式表路径（Resources 加载）</summary>
        public const string SubtitleHUD = "UI/SubtitleHUD";
    }
}
