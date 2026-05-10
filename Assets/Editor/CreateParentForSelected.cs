using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 编辑器工具窗口：自动化工具箱
/// 提供三个功能 -
/// 1. 创建空父级：为选中物体各自创建空父级，子级本地位置归零
/// 2. 重新指定父级：将选中的子物体批量分配到选中的父物体下
/// 3. 赋值Mesh：将Project中的Mesh资源一对一赋值到Hierarchy物体的Mesh组件
/// </summary>
public partial class CreateParentForSelected : EditorWindow
{
    // ============================================================
    // 菜单入口（单一入口）
    // ============================================================

    [MenuItem("Tools/自动化工具箱")]
    private static void ShowWindow()
    {
        var window = GetWindow<CreateParentForSelected>(true, "自动化工具箱");
        window.currentPage = Page.MainMenu;
        window.minSize = new Vector2(340, 300);
        window.maxSize = new Vector2(560, 600);
        window.Show();
    }

    // ============================================================
    // 页面枚举
    // ============================================================

    private enum Page
    {
        MainMenu,
        CreateParent,
        Reparent,
        AssignMesh,
        Rename
    }

    // ============================================================
    // 共享字段
    // ============================================================

    private Page currentPage = Page.MainMenu;

    // ---- 创建空父级用 ----
    private string prefixInput = "";

    // ---- 重新指定父级用 ----
    private List<GameObject> selectedChildren = new List<GameObject>();
    private List<GameObject> selectedParents = new List<GameObject>();
    private MatchingMode matchingMode = MatchingMode.OneToOne;
    private Vector2 childScrollPos;
    private Vector2 parentScrollPos;
    private string statusMessage = "";

    // ---- 赋值Mesh用 ----
    private List<GameObject> _assignTargets = new List<GameObject>();
    private List<Mesh> _assignMeshes = new List<Mesh>();
    private Vector2 _assignTargetScrollPos;
    private Vector2 _assignMeshScrollPos;
    private string _assignStatusMessage = "";

    // ---- 批量重命名用 ----
    private string renamePrefixInput = "";

    // ============================================================
    // OnGUI 主分发
    // ============================================================

    private void OnGUI()
    {
        switch (currentPage)
        {
            case Page.MainMenu:
                DrawMainMenuGUI();
                break;
            case Page.CreateParent:
                DrawCreateParentGUI();
                break;
            case Page.Reparent:
                DrawReparentGUI();
                break;
            case Page.AssignMesh:
                DrawAssignMeshGUI();
                break;
            case Page.Rename:
                DrawRenameGUI();
                break;
        }
    }

    // ============================================================
    // 主菜单页
    // ============================================================

    private void DrawMainMenuGUI()
    {
        GUILayout.Space(30);
        EditorGUILayout.LabelField("自动化工具箱", EditorStyles.boldLabel);
        GUILayout.Space(5);
        EditorGUILayout.LabelField("选择一个功能：", EditorStyles.wordWrappedLabel);
        GUILayout.Space(30);

        if (GUILayout.Button("创建空父级", GUILayout.Height(45)))
        {
            currentPage = Page.CreateParent;
            prefixInput = "";
            UpdateWindowSizeForPage(Page.CreateParent);
            Repaint();
        }

        GUILayout.Space(15);

        if (GUILayout.Button("重新指定父级", GUILayout.Height(45)))
        {
            currentPage = Page.Reparent;
            UpdateWindowSizeForPage(Page.Reparent);
            Repaint();
        }

        GUILayout.Space(15);

        if (GUILayout.Button("赋值Mesh", GUILayout.Height(45)))
        {
            currentPage = Page.AssignMesh;
            UpdateWindowSizeForPage(Page.AssignMesh);
            Repaint();
        }

        GUILayout.Space(15);

        if (GUILayout.Button("批量重命名", GUILayout.Height(45)))
        {
            currentPage = Page.Rename;
            UpdateWindowSizeForPage(Page.Rename);
            Repaint();
        }

        GUILayout.Space(40);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("关闭", GUILayout.Width(100), GUILayout.Height(30)))
        {
            Close();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 根据页面调整窗口尺寸限制
    /// </summary>
    private void UpdateWindowSizeForPage(Page page)
    {
        switch (page)
        {
            case Page.MainMenu:
                minSize = new Vector2(340, 300);
                maxSize = new Vector2(560, 600);
                break;
            case Page.CreateParent:
                minSize = new Vector2(340, 130);
                maxSize = new Vector2(460, 160);
                break;
            case Page.Reparent:
                minSize = new Vector2(420, 480);
                maxSize = new Vector2(560, 600);
                break;
            case Page.AssignMesh:
                minSize = new Vector2(420, 480);
                maxSize = new Vector2(560, 600);
                break;
            case Page.Rename:
                minSize = new Vector2(340, 200);
                maxSize = new Vector2(460, 240);
                break;
        }
    }

    // =========================================================
    // 导航：回到主菜单
    // =========================================================

    private void GoToMainMenu()
    {
        currentPage = Page.MainMenu;
        UpdateWindowSizeForPage(Page.MainMenu);
        float bottom = position.y + position.height;
        position = new Rect(position.x, bottom - 300, position.width, 300);
        Repaint();
    }

    // =========================================================
    // 生命周期
    // =========================================================

    private void OnDestroy()
    {
        // 窗口关闭时保留数据，方便重复打开
    }
}
