using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 自动化工具箱 - 功能三：赋值Mesh
/// </summary>
public partial class CreateParentForSelected
{
    // =========================================================
    // 功能三：赋值Mesh GUI
    // =========================================================

    private void DrawAssignMeshGUI()
    {
        GUILayout.Space(8);

        // ---------- 第一步：选择目标物体 ----------
        EditorGUILayout.LabelField("● 第一步：选择目标物体（Hierarchy）", EditorStyles.boldLabel);
        GUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("从Hierarchy选中", GUILayout.Height(28)))
        {
            PickAssignTargets();
        }
        if (GUILayout.Button("清空", GUILayout.Width(50), GUILayout.Height(28)))
        {
            _assignTargets.Clear();
            _assignMeshes.Clear();
            _assignStatusMessage = "已清空所有选择。";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"已选目标: {_assignTargets.Count} 个", EditorStyles.miniLabel);

        if (_assignTargets.Count > 0)
        {
            _assignTargetScrollPos = EditorGUILayout.BeginScrollView(_assignTargetScrollPos, GUILayout.Height(75));
            for (int i = 0; i < _assignTargets.Count; i++)
            {
                var go = _assignTargets[i];
                string label = go != null ? $"  [{i + 1}] {go.name}" : $"  [{i + 1}] (null)";
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(10);

        // ---------- 第二步：选择Mesh资源 ----------
        EditorGUILayout.LabelField("● 第二步：选择Mesh资源（Project）", EditorStyles.boldLabel);
        GUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("从Project选中", GUILayout.Height(28)))
        {
            PickAssignMeshes();
        }
        if (GUILayout.Button("清空", GUILayout.Width(50), GUILayout.Height(28)))
        {
            _assignMeshes.Clear();
            _assignStatusMessage = "已清空Mesh选择。";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"已选Mesh: {_assignMeshes.Count} 个", EditorStyles.miniLabel);

        if (_assignMeshes.Count > 0)
        {
            _assignMeshScrollPos = EditorGUILayout.BeginScrollView(_assignMeshScrollPos, GUILayout.Height(75));
            for (int i = 0; i < _assignMeshes.Count; i++)
            {
                var mesh = _assignMeshes[i];
                string label = mesh != null ? $"  [{i + 1}] {mesh.name}" : $"  [{i + 1}] (null)";
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(10);

        // ---------- 匹配规则 ----------
        EditorGUILayout.HelpBox("一对一匹配：第1个目标 ← 第1个Mesh，第2个目标 ← 第2个Mesh…（要求数量相等）", MessageType.None);

        GUILayout.Space(5);

        // ---------- 状态提示 ----------
        if (!string.IsNullOrEmpty(_assignStatusMessage))
        {
            EditorGUILayout.HelpBox(_assignStatusMessage, MessageType.Warning);
        }

        GUILayout.Space(10);

        // ---------- 执行 / 取消 ----------
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        EditorGUI.BeginDisabledGroup(!IsAssignMeshReady());
        if (GUILayout.Button("执行赋值", GUILayout.Width(90), GUILayout.Height(28)))
        {
            ExecuteAssignMesh();

            _assignTargets.Clear();
            _assignMeshes.Clear();
            _assignStatusMessage = "赋值完成！";
            Repaint();
        }
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("取消", GUILayout.Width(70), GUILayout.Height(28)))
        {
            GoToMainMenu();
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);
    }

    // =========================================================
    // 赋值Mesh：选择逻辑
    // =========================================================

    private void PickAssignTargets()
    {
        var current = Selection.gameObjects;

        if (current == null || current.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Hierarchy 中选中目标 GameObject，再点击此按钮。", "确定");
            return;
        }

        _assignTargets = current.ToList();
        _assignStatusMessage = $"已记录 {_assignTargets.Count} 个目标物体。";
        Debug.Log($"[赋值Mesh] 已记录目标物体: {_assignTargets.Count} 个");
        Repaint();
    }

    private void PickAssignMeshes()
    {
        var selected = Selection.objects;

        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 窗口中选中 Mesh 资源，再点击此按钮。", "确定");
            return;
        }

        _assignMeshes.Clear();
        foreach (var obj in selected)
        {
            Mesh mesh = obj as Mesh;
            if (mesh != null)
            {
                _assignMeshes.Add(mesh);
            }
        }

        if (_assignMeshes.Count == 0)
        {
            _assignStatusMessage = "未检测到任何 Mesh 资源，请确认选中的是 .mesh 文件。";
            EditorUtility.DisplayDialog("提示", "未检测到任何 Mesh 资源。请确保在 Project 窗口选中的是 Mesh 文件（.mesh）。", "确定");
            return;
        }

        _assignStatusMessage = $"已记录 {_assignMeshes.Count} 个 Mesh 资源。";
        Debug.Log($"[赋值Mesh] 已记录Mesh资源: {_assignMeshes.Count} 个");
        Repaint();
    }

    // =========================================================
    // 赋值Mesh：校验
    // =========================================================

    private bool IsAssignMeshReady()
    {
        _assignStatusMessage = ValidateAssignMeshSetup();
        return string.IsNullOrEmpty(_assignStatusMessage);
    }

    private string ValidateAssignMeshSetup()
    {
        if (_assignTargets.Count == 0)
            return "请先选择目标物体。";

        if (_assignMeshes.Count == 0)
            return "请先选择 Mesh 资源。";

        if (_assignTargets.Count != _assignMeshes.Count)
            return $"匹配失败：目标物体数量 ({_assignTargets.Count}) 不等于 Mesh 数量 ({_assignMeshes.Count})。必须一对一匹配。";

        // 检查是否有 null 元素
        for (int i = 0; i < _assignTargets.Count; i++)
        {
            if (_assignTargets[i] == null)
                return $"第 {i + 1} 个目标物体为 null，请重新选择。";
        }

        for (int i = 0; i < _assignMeshes.Count; i++)
        {
            if (_assignMeshes[i] == null)
                return $"第 {i + 1} 个 Mesh 为 null，请重新选择。";
        }

        // 检查每个目标物体是否有 MeshFilter 或 SkinnedMeshRenderer
        for (int i = 0; i < _assignTargets.Count; i++)
        {
            var go = _assignTargets[i];
            var mf = go.GetComponent<MeshFilter>();
            var smr = go.GetComponent<SkinnedMeshRenderer>();
            if (mf == null && smr == null)
                return $"目标物体 \"{go.name}\" (第{i + 1}个) 没有 MeshFilter 或 SkinnedMeshRenderer 组件。";
        }

        return "";
    }

    // =========================================================
    // 赋值Mesh：执行
    // =========================================================

    private void ExecuteAssignMesh()
    {
        int count = _assignTargets.Count;

        Undo.SetCurrentGroupName("赋值Mesh");
        int undoGroup = Undo.GetCurrentGroup();

        int successCount = 0;
        int skipCount = 0;

        for (int i = 0; i < count; i++)
        {
            GameObject target = _assignTargets[i];
            Mesh mesh = _assignMeshes[i];

            if (target == null || mesh == null)
            {
                Debug.LogWarning($"[赋值Mesh] 第 {i + 1} 对为空，已跳过。");
                skipCount++;
                continue;
            }

            // 优先 MeshFilter
            MeshFilter mf = target.GetComponent<MeshFilter>();
            if (mf != null)
            {
                Undo.RecordObject(mf, "赋值Mesh");
                mf.sharedMesh = mesh;
                Debug.Log($"[赋值Mesh] \"{target.name}\" (MeshFilter) ← \"{mesh.name}\"");
                successCount++;
                continue;
            }

            // 后备 SkinnedMeshRenderer
            SkinnedMeshRenderer smr = target.GetComponent<SkinnedMeshRenderer>();
            if (smr != null)
            {
                Undo.RecordObject(smr, "赋值Mesh");
                smr.sharedMesh = mesh;
                Debug.Log($"[赋值Mesh] \"{target.name}\" (SkinnedMeshRenderer) ← \"{mesh.name}\"");
                successCount++;
                continue;
            }

            // 两个都没有
            Debug.LogError($"[赋值Mesh] 错误：\"{target.name}\" 没有 MeshFilter 或 SkinnedMeshRenderer 组件！已跳过。");
            skipCount++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[赋值Mesh] 完成！成功: {successCount}, 跳过: {skipCount}, 总数: {count}");
        _assignStatusMessage = $"完成：成功 {successCount} 个，跳过 {skipCount} 个。";
    }
}
