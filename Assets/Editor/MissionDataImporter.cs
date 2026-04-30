using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using SmallWar.Data;

public class MissionDataImporter : EditorWindow
{
    // 修改为文件夹路径，支持批量处理
    private string csvFolder = "Assets/GameData/MissionsCSV";
    private string exportFolder = "Assets/SOManager/TextShowSO";

    [MenuItem("SmallWar/Tools/Advanced Multi-Importer")]
    public static void ShowWindow()
    {
        GetWindow<MissionDataImporter>("高级批量导入器");
    }

    private void OnGUI()
    {
        GUILayout.Label("Mission 批量自动化导入工具 (多对多)", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        csvFolder = EditorGUILayout.TextField("CSV 源文件夹", csvFolder);
        exportFolder = EditorGUILayout.TextField("Asset 导出路径", exportFolder);
        EditorGUILayout.Space();

        if (GUILayout.Button("批量同步所有 CSV 文件"))
        {
            ProcessAllCSV();
        }

        if (GUILayout.Button("选择单个 CSV 加载"))
        {
            string path = EditorUtility.OpenFilePanel("选择要导入的 CSV", csvFolder, "csv");
            if (!string.IsNullOrEmpty(path))
            {
                // 将绝对路径转为相对路径
                path = "Assets" + path.Replace(Application.dataPath, "");
                ImportSingleCSV(path);
            }
        }
    }

    private void ProcessAllCSV()
    {
        // 商业级改进：找不到文件夹时自动创建，而不是报错
        if (!Directory.Exists(csvFolder))
        {
            Directory.CreateDirectory(csvFolder);
            AssetDatabase.Refresh();
            Debug.Log($"系统已自动创建源文件夹: {csvFolder}，请将 CSV 文件放入其中后再同步。");
            return;
        }

        string[] files = Directory.GetFiles(csvFolder, "*.csv");

        if (files.Length == 0)
        {
            Debug.LogWarning($"文件夹 {csvFolder} 内没有找到任何 .csv 文件。");
            return;
        }

        foreach (string file in files)
        {
            ImportSingleCSV(file);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=cyan>所有 CSV 同步完成！共处理 {files.Length} 个文件。</color>");
    }

    private void ImportSingleCSV(string path)
    {
        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        // 跳过表头
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] data = lines[i].Split(',');
            if (data.Length < 4) continue;

            // 解析逻辑
            if (System.Enum.TryParse(data[0].Trim(), out MissionCategory cat))
            {
                int id = int.Parse(data[1].Trim());
                string title = data[2].Trim();
                string content = data[3].Trim().Replace("\\n", "\n");

                CreateOrUpdateAsset(cat, id, title, content);
            }
        }
        Debug.Log($"已从 {Path.GetFileName(path)} 导出资产。");
    }

    private void CreateOrUpdateAsset(MissionCategory cat, int id, string title, string content)
    {
        if (!Directory.Exists(exportFolder)) Directory.CreateDirectory(exportFolder);

        string assetName = $"Mission_{cat}_{id}.asset";
        string fullPath = Path.Combine(exportFolder, assetName);

        MissionEntryAsset asset = AssetDatabase.LoadAssetAtPath<MissionEntryAsset>(fullPath);

        if (asset == null)
        {
            asset = CreateInstance<MissionEntryAsset>();
            AssetDatabase.CreateAsset(asset, fullPath);
        }

        asset.category = cat;
        asset.subID = id;
        asset.title = title;
        asset.content = content;

        EditorUtility.SetDirty(asset);
    }
}