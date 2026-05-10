using UnityEngine;
using UnityEditor;

/// <summary>
/// 自动化工具箱 - 功能一：创建空父级
/// </summary>
public partial class CreateParentForSelected
{
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
}
