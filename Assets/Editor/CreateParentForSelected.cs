using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 匹配模式枚举
/// </summary>
public enum MatchingMode
{
    /// <summary>一对一配对（子物体数量 == 父物体数量）</summary>
    OneToOne = 0,
    /// <summary>多对一（所有子物体放入同一个父物体）</summary>
    ManyToOne = 1,
    /// <summary>轮询配对（循环分配子物体到父物体）</summary>
    RoundRobin = 2
}

/// <summary>
/// 编辑器工具窗口：自动化工具箱
/// 提供两个功能 -
/// 1. 创建空父级：为选中物体各自创建空父级，子级本地位置归零
/// 2. 重新指定父级：将选中的子物体批量分配到选中的父物体下
/// </summary>
public class CreateParentForSelected : EditorWindow
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
        Reparent
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
        }
    }

    // ============================================================
    // 功能一：创建空父级 GUI
    // ============================================================

    private void DrawCreateParentGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("请输入父级前缀名：", EditorStyles.boldLabel);
        GUILayout.Space(5);
        prefixInput = EditorGUILayout.TextField("前缀", prefixInput);
        GUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "选中 N 个物体，将为每个物体各创建一个空父级。\n" +
            "父级命名：前缀、前缀1、前缀2……（第1个不带编号）\n" +
            "父级 Position = 子级世界坐标，Rotation/Scale 默认值",
            MessageType.Info);

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("确定", GUILayout.Width(80)))
        {
            if (string.IsNullOrWhiteSpace(prefixInput))
            {
                EditorUtility.DisplayDialog("错误", "前缀名不能为空！", "确定");
                return;
            }
            CreateParents(prefixInput);
            prefixInput = "";
            GUI.FocusControl(null);
            Repaint();
        }

        if (GUILayout.Button("取消", GUILayout.Width(80)))
        {
            GoToMainMenu();
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 为选中的每个 GameObject 创建父级
    /// </summary>
    private static void CreateParents(string prefix)
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Hierarchy 中选中至少一个 GameObject。", "确定");
            return;
        }

        Undo.SetCurrentGroupName("创建空父级");
        int undoGroup = Undo.GetCurrentGroup();

        int index = 0;
        foreach (GameObject child in selectedObjects)
        {
            string parentName = index == 0 ? prefix : $"{prefix}{index}";
            Vector3 worldPosition = child.transform.position;

            GameObject parent = new GameObject(parentName);
            parent.transform.position = worldPosition;
            parent.transform.rotation = Quaternion.identity;
            parent.transform.localScale = Vector3.one;

            if (child.transform.parent != null)
            {
                parent.transform.SetParent(child.transform.parent, true);
            }

            child.transform.SetParent(parent.transform, true);
            child.transform.localPosition = Vector3.zero;

            Undo.RegisterCreatedObjectUndo(parent, "创建父级");
            Undo.RecordObject(child.transform, "归零子级位置");

            index++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorApplication.DirtyHierarchyWindowSorting();

        Debug.Log($"[创建空父级] 已为 {index} 个 GameObject 创建父级（前缀: {prefix}）");
    }

    // ============================================================
    // 功能二：重新指定父级 GUI
    // ============================================================

    private void DrawReparentGUI()
    {
        GUILayout.Space(8);

        // ---------- 第一步：选择子物体 ----------
        EditorGUILayout.LabelField("● 第一步：选择子物体", EditorStyles.boldLabel);
        GUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("点击选择子物体", GUILayout.Height(28)))
        {
            PickChildrenFromSelection();
        }
        if (GUILayout.Button("清空", GUILayout.Width(50), GUILayout.Height(28)))
        {
            selectedChildren.Clear();
            UpdateStatusMessage();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"已选子物体: {selectedChildren.Count} 个", EditorStyles.miniLabel);

        if (selectedChildren.Count > 0)
        {
            childScrollPos = EditorGUILayout.BeginScrollView(childScrollPos, GUILayout.Height(75));
            foreach (var obj in selectedChildren)
            {
                EditorGUILayout.LabelField($"  · {obj.name}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(10);

        // ---------- 第二步：选择父物体 ----------
        EditorGUILayout.LabelField("● 第二步：选择父物体", EditorStyles.boldLabel);
        GUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("点击选择父物体", GUILayout.Height(28)))
        {
            PickParentsFromSelection();
        }
        if (GUILayout.Button("清空", GUILayout.Width(50), GUILayout.Height(28)))
        {
            selectedParents.Clear();
            UpdateStatusMessage();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"已选父物体: {selectedParents.Count} 个", EditorStyles.miniLabel);

        if (selectedParents.Count > 0)
        {
            parentScrollPos = EditorGUILayout.BeginScrollView(parentScrollPos, GUILayout.Height(75));
            foreach (var obj in selectedParents)
            {
                EditorGUILayout.LabelField($"  · {obj.name}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(12);

        // ---------- 匹配模式 ----------
        EditorGUILayout.LabelField("匹配模式：", EditorStyles.boldLabel);
        matchingMode = (MatchingMode)EditorGUILayout.EnumPopup("", matchingMode);

        GUILayout.Space(5);

        // 模式说明
        string modeDesc = matchingMode switch
        {
            MatchingMode.OneToOne => "一对一：第1个子 → 第1个父，第2个子 → 第2个父…（要求数量相等）",
            MatchingMode.ManyToOne => "多对一：所有子物体放入第1个父物体中（需要至少1个父物体）",
            MatchingMode.RoundRobin => "轮询：子1→父1，子2→父2，子3→父1…（循环分配）",
            _ => ""
        };
        EditorGUILayout.HelpBox(modeDesc, MessageType.None);

        GUILayout.Space(8);

        // ---------- 状态提示 ----------
        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, MessageType.Warning);
        }

        GUILayout.Space(10);

        // ---------- 执行 / 取消 ----------
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        EditorGUI.BeginDisabledGroup(!IsReparentReady());
        if (GUILayout.Button("执行", GUILayout.Width(80), GUILayout.Height(28)))
        {
            ExecuteReparent();

            // 执行后清空选择，准备下一轮操作
            selectedChildren.Clear();
            selectedParents.Clear();
            UpdateStatusMessage();
            Repaint();
        }
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("取消", GUILayout.Width(80), GUILayout.Height(28)))
        {
            GoToMainMenu();
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);
    }

    // ============================================================
    // 重新指定父级：选择逻辑
    // ============================================================

    private void PickChildrenFromSelection()
    {
        var current = Selection.gameObjects;

        if (current == null || current.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Hierarchy 中选中想要作为【子物体】的 GameObject，再点击此按钮。", "确定");
            return;
        }

        int beforeCount = selectedChildren.Count;
        selectedChildren = current.ToList();

        Debug.Log($"[重新指定父级] 已记录子物体: {selectedChildren.Count} 个 (原先 {beforeCount} 个)");
        UpdateStatusMessage();
        Repaint();
    }

    private void PickParentsFromSelection()
    {
        var current = Selection.gameObjects;

        if (current == null || current.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Hierarchy 中选中想要作为【父物体】的 GameObject，再点击此按钮。", "确定");
            return;
        }

        int beforeCount = selectedParents.Count;
        selectedParents = current.ToList();

        Debug.Log($"[重新指定父级] 已记录父物体: {selectedParents.Count} 个 (原先 {beforeCount} 个)");
        UpdateStatusMessage();
        Repaint();
    }

    // ============================================================
    // 重新指定父级：校验
    // ============================================================

    private bool IsReparentReady()
    {
        UpdateStatusMessage();
        return string.IsNullOrEmpty(statusMessage);
    }

    private void UpdateStatusMessage()
    {
        statusMessage = ValidateReparentSetup();
    }

    private string ValidateReparentSetup()
    {
        int childCount = selectedChildren.Count;
        int parentCount = selectedParents.Count;

        if (childCount == 0)
            return "请先选择子物体。";

        if (parentCount == 0)
            return "请先选择父物体。";

        switch (matchingMode)
        {
            case MatchingMode.OneToOne:
                if (childCount != parentCount)
                    return $"一对一模式要求子物体数量 ({childCount}) 等于父物体数量 ({parentCount})。";
                break;

            case MatchingMode.ManyToOne:
                // 至少 1 子 + 1 父即可（只用第 1 个父物体）
                break;

            case MatchingMode.RoundRobin:
                // 至少 1 子 + 1 父即可
                break;
        }

        return "";
    }

    // ============================================================
    // 重新指定父级：执行
    // ============================================================

    private void ExecuteReparent()
    {
        int childCount = selectedChildren.Count;
        int parentCount = selectedParents.Count;

        Undo.SetCurrentGroupName("重新指定父级");
        int undoGroup = Undo.GetCurrentGroup();

        for (int i = 0; i < childCount; i++)
        {
            GameObject child = selectedChildren[i];

            if (child == null)
            {
                Debug.LogWarning($"[重新指定父级] 第 {i + 1} 个子物体为 null，已跳过。");
                continue;
            }

            // 确定该子物体对应的父物体
            GameObject parent = GetMatchingParent(i, parentCount);

            if (parent == null)
            {
                Debug.LogWarning($"[重新指定父级] 第 {i + 1} 个子物体 \"{child.name}\" 找不到对应的父物体，已跳过。");
                continue;
            }

            // 校验：禁止循环引用
            if (!ValidateNoCircularReference(child, parent))
            {
                continue;
            }

            // 记录 Undo
            Undo.RecordObject(child.transform, "重新指定父级");

            // 设置父级，保持世界位置（与手动拖拽效果一致）
            child.transform.SetParent(parent.transform, true);

            Debug.Log($"[重新指定父级] \"{child.name}\" → \"{parent.name}\"");
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorApplication.DirtyHierarchyWindowSorting();

        Debug.Log($"[重新指定父级] 完成！共处理 {childCount} 个子物体。");
    }

    /// <summary>
    /// 根据匹配模式获取第 childIndex 个子物体对应的父物体
    /// </summary>
    private GameObject GetMatchingParent(int childIndex, int parentCount)
    {
        return matchingMode switch
        {
            MatchingMode.OneToOne => selectedParents[childIndex],
            MatchingMode.ManyToOne => selectedParents[0],
            MatchingMode.RoundRobin => selectedParents[childIndex % parentCount],
            _ => null
        };
    }

    /// <summary>
    /// 校验是否存在循环引用
    /// </summary>
    /// <returns>true = 安全，false = 存在循环或自身引用</returns>
    private bool ValidateNoCircularReference(GameObject child, GameObject parent)
    {
        // 禁止父物体是自己
        if (child == parent)
        {
            Debug.LogError($"[重新指定父级] 错误：不能将物体 \"{child.name}\" 的父级设为其自身！已跳过。");
            return false;
        }

        // 禁止父物体是子物体的后代（层级循环）
        if (IsDescendantOf(parent.transform, child.transform))
        {
            Debug.LogError($"[重新指定父级] 错误：\"{parent.name}\" 是 \"{child.name}\" 的子物体，不能反过来设为父物体！已跳过。");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 检查 candidate 是否为 root 的子物体（即 root 是 candidate 的祖先）
    /// </summary>
    private bool IsDescendantOf(Transform candidate, Transform root)
    {
        Transform current = candidate;
        while (current != null)
        {
            if (current == root)
                return true;
            current = current.parent;
        }
        return false;
    }

    // =========================================================
    // 导航：回到主菜单
    // =========================================================

    private void GoToMainMenu()
    {
        currentPage = Page.MainMenu;
        UpdateWindowSizeForPage(Page.MainMenu);
        // 保持窗口底部位置不变，向上扩展到 300px 高度，避免视觉瞬移
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
