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
/// 自动化工具箱 - 功能二：重新指定父级
/// </summary>
public partial class CreateParentForSelected
{
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
}
