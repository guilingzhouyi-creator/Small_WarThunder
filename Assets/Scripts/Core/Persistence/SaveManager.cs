using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 轻量级存档管理器，提供保存/加载/删除入口。
/// 当前为单元测试阶段，使用硬编码测试数据。
/// </summary>
public class SaveManager : MonoBehaviour
{
    /// <summary>全局单例</summary>
    public static SaveManager Instance { get; private set; }

    /// <summary>测试存档数据结构（硬编码演示用）</summary>
    [Serializable]
    private struct SaveTestData
    {
        public string saveTime;
        public string playerName;
        public int level;
        public float posX;
        public float posY;
        public float posZ;
        public int health;
        public int ammo;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SaveManager] 已存在实例，正在销毁新的实例。", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[SaveManager] 初始化完成。");
    }

    /// <summary>
    /// 测试保存：使用硬编码数据写入 JSON 文件。
    /// 点击后关闭 Pause 界面并切回 TPS 视角。
    /// </summary>
    public void TestSave()
    {
        // ─── 硬编码测试数据 ───
        var testData = new SaveTestData
        {
            saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            playerName = "TestPlayer_001",
            level = 5,
            posX = 105.3f,
            posY = 0.15f,
            posZ = 210.7f,
            health = 85,
            ammo = 28
        };

        string json = JsonUtility.ToJson(testData, true);
        string dir = Path.Combine(Application.persistentDataPath, "Saves");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "test_save.json");

        File.WriteAllText(path, json);

        Debug.Log($"[SaveManager] 保存成功！");
        Debug.Log($"[SaveManager] 路径: {path}");
        Debug.Log($"[SaveManager] 内容: {json}");

        // ─── 关闭 Pause → 回到 TPS ───
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseOverlay(UIOverlayId.Pause);
        }
    }
}
