using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 构建资源资产包工具 — 将大文件资源打包为 ZIP，用于 Release 分发。
/// 执行菜单: Tools / 构建资源资产包
/// </summary>
public static class BuildAssetZip
{
    private const string MenuPath = "Tools/构建资源资产包";

    [MenuItem(MenuPath)]
    private static void BuildZip()
    {
        string version = GetProjectVersion();
        string outputName = $"Small_WarThunder_Assets_v{version}.zip";
        string outputPath = Path.Combine(Application.dataPath, "..", outputName);
        outputPath = Path.GetFullPath(outputPath);

        // 要打包的资源目录（相对于项目根目录）
        string[] sourceDirs = new[]
        {
            "Assets/Art",
            "Assets/Audio",
            "Assets/Desktop",
            "Assets/prefabs",
            "Assets/PhysicMaterial",
            "Assets/Unity Asset Management System"
        };

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        // 删除已存在的 zip
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        using (FileStream zipStream = new FileStream(outputPath, FileMode.CreateNew))
        using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            long totalSize = 0;
            int fileCount = 0;

            foreach (string dir in sourceDirs)
            {
                string fullDir = Path.Combine(projectRoot, dir);
                if (!Directory.Exists(fullDir))
                {
                    Debug.LogWarning($"[BuildAssetZip] 目录不存在，已跳过: {dir}");
                    continue;
                }

                string[] files = Directory.GetFiles(fullDir, "*", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    // 跳过 .meta 文件
                    if (file.EndsWith(".meta"))
                    {
                        continue;
                    }

                    // 计算相对路径
                    string relativePath = Path.GetRelativePath(projectRoot, file);
                    var entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);

                    using (Stream entryStream = entry.Open())
                    using (FileStream fileStream = File.OpenRead(file))
                    {
                        fileStream.CopyTo(entryStream);
                    }

                    FileInfo fi = new FileInfo(file);
                    totalSize += fi.Length;
                    fileCount++;
                }
            }

            Debug.Log($"[BuildAssetZip] 资源包构建完成:\n" +
                      $"  路径: {outputPath}\n" +
                      $"  文件数: {fileCount}\n" +
                      $"  总大小: {FormatBytes(totalSize)}");
        }

        // 在资源管理器中高亮
        EditorUtility.RevealInFinder(outputPath);
    }

    private static string GetProjectVersion()
    {
        // 从 README.md 中读取版本号，如果失败则回退到日期
        string readmePath = Path.Combine(Application.dataPath, "..", "README.md");
        if (File.Exists(readmePath))
        {
            string[] lines = File.ReadAllLines(readmePath);
            foreach (string line in lines)
            {
                if (line.Contains("版本号"))
                {
                    // 格式: **版本号**: `0.1.000（测试/Beta）`
                    int start = line.IndexOf('`');
                    int end = line.LastIndexOf('`');
                    if (start >= 0 && end > start)
                    {
                        string version = line.Substring(start + 1, end - start - 1);
                        // 去掉括号内的状态说明
                        int paren = version.IndexOf('（');
                        if (paren > 0)
                        {
                            version = version.Substring(0, paren);
                        }
                        return version;
                    }
                }
            }
        }

        return System.DateTime.Now.ToString("yyyyMMdd");
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < suffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {suffixes[order]}";
    }
}
