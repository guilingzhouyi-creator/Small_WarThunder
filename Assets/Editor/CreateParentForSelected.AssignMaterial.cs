using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 自动化工具箱 - 功能五：赋值材质
/// </summary>
public partial class CreateParentForSelected
{
    // =========================================================
    // 功能五：赋值材质 GUI
    // =========================================================

    private void DrawAssignMaterialGUI()
    {
        GUILayout.Space(8);

        // ---------- 模式选择 ----------
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("赋值模式", GUILayout.Width(60));
        _assignMaterialMode = (AssignMaterialMode)EditorGUILayout.EnumPopup(_assignMaterialMode, GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        // ---------- 第一步：选择目标物体 ----------
        EditorGUILayout.LabelField("第一步：选择目标物体（Hierarchy）", EditorStyles.boldLabel);
        GUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("从Hierarchy选中", GUILayout.Height(28)))
        {
            PickAssignMatTargets();
        }
        if (GUILayout.Button("清空", GUILayout.Width(50), GUILayout.Height(28)))
        {
            _assignMatTargets.Clear();
            _assignMaterials.Clear();
            _assignMatStatusMessage = "已清空所有选择。";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"已选目标: {_assignMatTargets.Count} 个", EditorStyles.miniLabel);

        if (_assignMatTargets.Count > 0)
        {
            _assignMatTargetScrollPos = EditorGUILayout.BeginScrollView(_assignMatTargetScrollPos, GUILayout.Height(75));
            for (int i = 0; i < _assignMatTargets.Count; i++)
            {
                var go = _assignMatTargets[i];
                string label = go != null ? $"  [{i + 1}] {go.name}" : $"  [{i + 1}] (null)";
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(10);

        // ---------- 第二步：选择Material资源 ----------
        EditorGUILayout.LabelField("第二步：选择Material资源（Project）", EditorStyles.boldLabel);
        GUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("从Project选中", GUILayout.Height(28)))
        {
            PickAssignMaterials();
        }
        if (GUILayout.Button("清空", GUILayout.Width(50), GUILayout.Height(28)))
        {
            _assignMaterials.Clear();
            _assignMatStatusMessage = "已清空Material选择。";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"已选Material: {_assignMaterials.Count} 个", EditorStyles.miniLabel);

        if (_assignMaterials.Count > 0)
        {
            _assignMatMaterialScrollPos = EditorGUILayout.BeginScrollView(_assignMatMaterialScrollPos, GUILayout.Height(75));
            for (int i = 0; i < _assignMaterials.Count; i++)
            {
                var mat = _assignMaterials[i];
                string label = mat != null ? $"  [{i + 1}] {mat.name}" : $"  [{i + 1}] (null)";
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(10);

        // ---------- 匹配规则 ----------
        string helpText = _assignMaterialMode switch
        {
            AssignMaterialMode.OneToOne => "一对一匹配：目标物体与Material数量必须相等，按索引一一对应赋值。",
            AssignMaterialMode.ManyToOne => "多对一匹配：仅1个目标物体，所有选中的Material依次插入该物体的材质槽位。",
            AssignMaterialMode.ManyToMany => "多对多匹配：多份目标物体，每份物体同时刷上所有选中的Material。",
            _ => ""
        };
        EditorGUILayout.HelpBox(helpText, MessageType.None);

        GUILayout.Space(5);

        // ---------- 状态提示 ----------
        if (!string.IsNullOrEmpty(_assignMatStatusMessage))
        {
            EditorGUILayout.HelpBox(_assignMatStatusMessage, MessageType.Warning);
        }

        GUILayout.Space(10);

        // ---------- 执行 / 取消 ----------
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        EditorGUI.BeginDisabledGroup(!IsAssignMaterialReady());
        if (GUILayout.Button("执行赋值", GUILayout.Width(90), GUILayout.Height(28)))
        {
            ExecuteAssignMaterial();

            _assignMatTargets.Clear();
            _assignMaterials.Clear();
            _assignMatStatusMessage = "材质赋值完成！";
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
    // 赋值材质：选择逻辑
    // =========================================================

    private void PickAssignMatTargets()
    {
        var current = Selection.gameObjects;

        if (current == null || current.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Hierarchy 中选中目标 GameObject，再点击此按钮。", "确定");
            return;
        }

        _assignMatTargets = current.ToList();
        _assignMatStatusMessage = $"已记录 {_assignMatTargets.Count} 个目标物体。";
        Debug.Log($"[赋值材质] 已记录目标物体: {_assignMatTargets.Count} 个");
        Repaint();
    }

    private void PickAssignMaterials()
    {
        var selected = Selection.objects;

        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 窗口中选中 Material 资源，再点击此按钮。", "确定");
            return;
        }

        _assignMaterials.Clear();
        foreach (var obj in selected)
        {
            Material mat = obj as Material;
            if (mat != null)
            {
                _assignMaterials.Add(mat);
            }
        }

        if (_assignMaterials.Count == 0)
        {
            _assignMatStatusMessage = "未检测到任何 Material 资源，请确认选中的是 .mat 文件。";
            EditorUtility.DisplayDialog("提示", "未检测到任何 Material 资源。请确保在 Project 窗口选中的是 Material 文件（.mat）。", "确定");
            return;
        }

        _assignMatStatusMessage = $"已记录 {_assignMaterials.Count} 个 Material 资源。";
        Debug.Log($"[赋值材质] 已记录Material资源: {_assignMaterials.Count} 个");
        Repaint();
    }

    // =========================================================
    // 赋值材质：校验
    // =========================================================

    private bool IsAssignMaterialReady()
    {
        _assignMatStatusMessage = ValidateAssignMaterialSetup();
        return string.IsNullOrEmpty(_assignMatStatusMessage);
    }

    private string ValidateAssignMaterialSetup()
    {
        if (_assignMatTargets.Count == 0)
            return "请先选择目标物体。";

        if (_assignMaterials.Count == 0)
            return "请先选择 Material 资源。";

        switch (_assignMaterialMode)
        {
            case AssignMaterialMode.OneToOne:
                if (_assignMatTargets.Count != _assignMaterials.Count)
                    return $"一对一模式下目标物体数量 ({_assignMatTargets.Count}) 必须等于 Material 数量 ({_assignMaterials.Count})。";
                break;
            case AssignMaterialMode.ManyToOne:
                if (_assignMatTargets.Count != 1)
                    return $"多对一模式下只能选择1个目标物体，当前选中 {_assignMatTargets.Count} 个。";
                break;
            case AssignMaterialMode.ManyToMany:
                if (_assignMatTargets.Count == 0 || _assignMaterials.Count == 0)
                    return "多对多模式下至少需要1个目标物体和1个Material。";
                break;
        }

        for (int i = 0; i < _assignMatTargets.Count; i++)
        {
            if (_assignMatTargets[i] == null)
                return $"第 {i + 1} 个目标物体为 null，请重新选择。";
        }

        for (int i = 0; i < _assignMaterials.Count; i++)
        {
            if (_assignMaterials[i] == null)
                return $"第 {i + 1} 个 Material 为 null，请重新选择。";
        }

        for (int i = 0; i < _assignMatTargets.Count; i++)
        {
            var go = _assignMatTargets[i];
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return $"目标物体 \"{go.name}\" (第{i + 1}个) 没有任何 Renderer 组件。";
        }

        return "";
    }

    // =========================================================
    // 赋值材质：执行
    // =========================================================

    private void ExecuteAssignMaterial()
    {
        int targetCount = _assignMatTargets.Count;
        int matCount = _assignMaterials.Count;

        Undo.SetCurrentGroupName("赋值材质");
        int undoGroup = Undo.GetCurrentGroup();

        int successCount = 0;
        int skipCount = 0;

        for (int t = 0; t < targetCount; t++)
        {
            GameObject target = _assignMatTargets[t];

            if (target == null)
            {
                Debug.LogWarning($"[赋值材质] 第 {t + 1} 个目标物体为空，已跳过。");
                skipCount++;
                continue;
            }

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogWarning($"[赋值材质] \"{target.name}\" 没有 Renderer 组件，已跳过。");
                skipCount++;
                continue;
            }

            Undo.RecordObject(renderer, "赋值材质");

            if (_assignMaterialMode == AssignMaterialMode.OneToOne)
            {
                // 一对一模式：按索引一一对应
                Material mat = _assignMaterials[t];
                if (mat == null)
                {
                    Debug.LogWarning($"[赋值材质] 第 {t + 1} 个 Material 为空，\"{target.name}\" 已跳过。");
                    skipCount++;
                    continue;
                }
                renderer.sharedMaterial = mat;
                Debug.Log($"[赋值材质] \"{target.name}\" (Renderer)  一对一 \"{mat.name}\"");
            }
            else if (_assignMaterialMode == AssignMaterialMode.ManyToOne)
            {
                // 多对一模式：多份Material插入到这一个GameObject的材质槽
                var mats = _assignMaterials.ToArray();
                renderer.sharedMaterials = mats;
                Debug.Log($"[赋值材质] \"{target.name}\" (Renderer)  多对一 ({matCount}个Material插入): {string.Join(", ", mats.Select(m => m?.name ?? "null"))}");
            }
            else // ManyToMany
            {
                // 多对多模式：每份目标物体刷上所有选中的Material
                var mats = _assignMaterials.ToArray();
                renderer.sharedMaterials = mats;
                Debug.Log($"[赋值材质] \"{target.name}\" (Renderer)  多对多 ({matCount}个Material刷入): {string.Join(", ", mats.Select(m => m?.name ?? "null"))}");
            }
            successCount++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[赋值材质] 完成！成功: {successCount}, 跳过: {skipCount}, 目标总数: {targetCount}, 材质总数: {matCount}");
        _assignMatStatusMessage = $"完成：成功 {successCount} 个，跳过 {skipCount} 个。";
    }
}
