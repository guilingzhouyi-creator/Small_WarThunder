using UnityEngine;
using UnityEditor;

/// <summary>
/// 自动化工具箱 - 功能四：批量重命名
/// </summary>
public partial class CreateParentForSelected
{
    // ============================================================
    // 功能四：批量重命名 GUI
    // ============================================================

    private void DrawRenameGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("请输入重命名前缀：", EditorStyles.boldLabel);
        GUILayout.Space(5);
        renamePrefixInput = EditorGUILayout.TextField("前缀", renamePrefixInput);
        GUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "选中 N 个物体，一键重命名为：\n" +
            "前缀、前缀1、前缀2……（第1个不带编号）\n" +
            "按 Hierarchy 中选中顺序依次编号",
            MessageType.Info);

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("确定", GUILayout.Width(80)))
        {
            if (string.IsNullOrWhiteSpace(renamePrefixInput))
            {
                EditorUtility.DisplayDialog("错误", "前缀名不能为空！", "确定");
                return;
            }
            ExecuteRename(renamePrefixInput);
            renamePrefixInput = "";
            GUI.FocusControl(null);
            Repaint();
        }

        if (GUILayout.Button("取消", GUILayout.Width(80)))
        {
            GoToMainMenu();
        }

        EditorGUILayout.EndHorizontal();
    }

    // ============================================================
    // 批量重命名：执行
    // ============================================================

    private static void ExecuteRename(string prefix)
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Hierarchy 中选中至少一个 GameObject。", "确定");
            return;
        }

        Undo.SetCurrentGroupName("批量重命名");
        int undoGroup = Undo.GetCurrentGroup();

        // 按 Hierarchy 顺序排序（通过 sibling index）
        System.Array.Sort(selectedObjects, (a, b) =>
        {
            int indexA = a.transform.GetSiblingIndex();
            int indexB = b.transform.GetSiblingIndex();
            if (a.transform.parent == b.transform.parent)
                return indexA.CompareTo(indexB);
            return 0;
        });

        int index = 0;
        foreach (GameObject go in selectedObjects)
        {
            string newName = index == 0 ? prefix : $"{prefix}{index}";
            Undo.RecordObject(go, "批量重命名");
            go.name = newName;
            index++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorApplication.DirtyHierarchyWindowSorting();

        Debug.Log($"[批量重命名] 已重命名 {index} 个 GameObject（前缀: {prefix}）");
    }
}
