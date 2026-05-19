using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 自动化工具箱 - 综合功能页（Combine）
/// 组合操作：一对一赋值Mesh + 统一批量刷材质，一步完成。
/// </summary>
public partial class CreateParentForSelected
{
    // =========================================================
    // Combine 页面字段
    // =========================================================

    private List<GameObject> _combinedTargets = new List<GameObject>();
    private List<Mesh> _combinedMeshes = new List<Mesh>();
    private List<Material> _combinedMaterials = new List<Material>();
    private Vector2 _combinedTargetScrollPos;
    private Vector2 _combinedMeshScrollPos;
    private Vector2 _combinedMaterialScrollPos;
    private string _combinedStatusMessage = "";

    // =========================================================
    // Combine 页面 GUI
    // =========================================================

    private void DrawCombinedGUI()
    {
        GUILayout.Space(8);

        EditorGUILayout.LabelField("综合功能（Mesh + 材质一键批处理）", EditorStyles.boldLabel);
        GUILayout.Space(8);

        // ---------- 第一步：选择目标物体 ----------
        EditorGUILayout.LabelField("第一步：选择目标物体（Hierarchy）", EditorStyles.boldLabel);
        GUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("从Hierarchy选中", GUILayout.Height(28)))
        {
            PickCombinedTargets();
        }
        if (GUILayout.Button("清空全部", GUILayout.Width(60), GUILayout.Height(28)))
        {
            _combinedTargets.Clear();
            _combinedMeshes.Clear();
            _combinedMaterials.Clear();
            _combinedStatusMessage = "已清空所有选择。";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"已选目标: {_combinedTargets.Count} 个", EditorStyles.miniLabel);

        if (_combinedTargets.Count > 0)
        {
            _combinedTargetScrollPos = EditorGUILayout.BeginScrollView(_combinedTargetScrollPos, GUILayout.Height(70));
            for (int i = 0; i < _combinedTargets.Count; i++)
            {
                var go = _combinedTargets[i];
                string label = go != null ? $"  [{i + 1}] {go.name}" : $"  [{i + 1}] (null)";
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(8);

        // ---------- 第二步：选择Mesh资源（一对一） ----------
        EditorGUILayout.LabelField("第二步：选择Mesh资源 - 一对一（Project）", EditorStyles.boldLabel);
        GUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("从Project选中Mesh", GUILayout.Height(28)))
        {
            PickCombinedMeshes();
        }
        if (GUILayout.Button("清空Mesh", GUILayout.Width(70), GUILayout.Height(28)))
        {
            _combinedMeshes.Clear();
            _combinedStatusMessage = "已清空Mesh选择。";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"已选Mesh: {_combinedMeshes.Count} 个", EditorStyles.miniLabel);

        if (_combinedMeshes.Count > 0)
        {
            _combinedMeshScrollPos = EditorGUILayout.BeginScrollView(_combinedMeshScrollPos, GUILayout.Height(70));
            for (int i = 0; i < _combinedMeshes.Count; i++)
            {
                var mesh = _combinedMeshes[i];
                string label = mesh != null ? $"  [{i + 1}] {mesh.name}" : $"  [{i + 1}] (null)";
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(8);

        // ---------- 第三步：选择Material资源（统一批量刷） ----------
        EditorGUILayout.LabelField("第三步：选择Material资源 - 统一批量刷（Project）", EditorStyles.boldLabel);
        GUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("从Project选中Material", GUILayout.Height(28)))
        {
            PickCombinedMaterials();
        }
        if (GUILayout.Button("清空Material", GUILayout.Width(80), GUILayout.Height(28)))
        {
            _combinedMaterials.Clear();
            _combinedStatusMessage = "已清空Material选择。";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"已选Material: {_combinedMaterials.Count} 个", EditorStyles.miniLabel);

        if (_combinedMaterials.Count > 0)
        {
            _combinedMaterialScrollPos = EditorGUILayout.BeginScrollView(_combinedMaterialScrollPos, GUILayout.Height(70));
            for (int i = 0; i < _combinedMaterials.Count; i++)
            {
                var mat = _combinedMaterials[i];
                string label = mat != null ? $"  [{i + 1}] {mat.name}" : $"  [{i + 1}] (null)";
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(8);

        // ---------- 匹配规则说明 ----------
        EditorGUILayout.HelpBox(
            "组合批处理规则：\n" +
            "• Mesh：与目标物体一对一匹配（第1个目标 ← 第1个Mesh，依次类推）\n" +
            "• Material：选中的全部Material统一刷到每个目标物体的Renderer材质槽中\n" +
            "• 要求：目标数量 = Mesh数量，且目标数量 ≥ 1，Material数量 ≥ 1",
            MessageType.None);

        GUILayout.Space(5);

        // ---------- 状态提示 ----------
        if (!string.IsNullOrEmpty(_combinedStatusMessage))
        {
            EditorGUILayout.HelpBox(_combinedStatusMessage, MessageType.Warning);
        }

        GUILayout.Space(8);

        // ---------- 执行 / 取消 ----------
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        EditorGUI.BeginDisabledGroup(!IsCombinedReady());
        if (GUILayout.Button("一键执行（Mesh+材质）", GUILayout.Width(150), GUILayout.Height(30)))
        {
            ExecuteCombined();
            _combinedTargets.Clear();
            _combinedMeshes.Clear();
            _combinedMaterials.Clear();
            _combinedStatusMessage = "综合批处理完成！";
            Repaint();
        }
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("取消", GUILayout.Width(70), GUILayout.Height(30)))
        {
            GoToMainMenu();
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);
    }

    // =========================================================
    // Combine：选择逻辑
    // =========================================================

    /// <summary>
    /// 从Hierarchy选中记录目标物体
    /// </summary>
    private void PickCombinedTargets()
    {
        var current = Selection.gameObjects;

        if (current == null || current.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Hierarchy 中选中目标 GameObject，再点击此按钮。", "确定");
            return;
        }

        _combinedTargets = current.ToList();
        _combinedStatusMessage = $"已记录 {_combinedTargets.Count} 个目标物体。";
        Debug.Log($"[综合功能] PickCombinedTargets: 已记录目标物体 {_combinedTargets.Count} 个");
        Repaint();
    }

    /// <summary>
    /// 从Project选中记录Mesh资源
    /// </summary>
    private void PickCombinedMeshes()
    {
        var selected = Selection.objects;

        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 窗口中选中 Mesh 资源，再点击此按钮。", "确定");
            return;
        }

        _combinedMeshes.Clear();
        foreach (var obj in selected)
        {
            Mesh mesh = obj as Mesh;
            if (mesh != null)
            {
                _combinedMeshes.Add(mesh);
            }
        }

        if (_combinedMeshes.Count == 0)
        {
            _combinedStatusMessage = "未检测到任何 Mesh 资源，请确认选中的是 .mesh 文件。";
            EditorUtility.DisplayDialog("提示", "未检测到任何 Mesh 资源。请确保在 Project 窗口选中的是 Mesh 文件（.mesh）。", "确定");
            return;
        }

        _combinedStatusMessage = $"已记录 {_combinedMeshes.Count} 个 Mesh 资源。";
        Debug.Log($"[综合功能] PickCombinedMeshes: 已记录Mesh资源 {_combinedMeshes.Count} 个");
        Repaint();
    }

    /// <summary>
    /// 从Project选中记录Material资源
    /// </summary>
    private void PickCombinedMaterials()
    {
        var selected = Selection.objects;

        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 窗口中选中 Material 资源，再点击此按钮。", "确定");
            return;
        }

        _combinedMaterials.Clear();
        foreach (var obj in selected)
        {
            Material mat = obj as Material;
            if (mat != null)
            {
                _combinedMaterials.Add(mat);
            }
        }

        if (_combinedMaterials.Count == 0)
        {
            _combinedStatusMessage = "未检测到任何 Material 资源，请确认选中的是 .mat 文件。";
            EditorUtility.DisplayDialog("提示", "未检测到任何 Material 资源。请确保在 Project 窗口选中的是 Material 文件（.mat）。", "确定");
            return;
        }

        _combinedStatusMessage = $"已记录 {_combinedMaterials.Count} 个 Material 资源。";
        Debug.Log($"[综合功能] PickCombinedMaterials: 已记录Material资源 {_combinedMaterials.Count} 个");
        Repaint();
    }

    // =========================================================
    // Combine：校验
    // =========================================================

    /// <summary>
    /// 校验综合功能是否满足执行条件
    /// </summary>
    private bool IsCombinedReady()
    {
        _combinedStatusMessage = ValidateCombinedSetup();
        return string.IsNullOrEmpty(_combinedStatusMessage);
    }

    /// <summary>
    /// 校验逻辑：目标数量 = Mesh数量，Material数量 ≥ 1，所有组件存在
    /// </summary>
    private string ValidateCombinedSetup()
    {
        if (_combinedTargets.Count == 0)
            return "请先选择目标物体。";

        if (_combinedMeshes.Count == 0)
            return "请先选择 Mesh 资源。";

        if (_combinedMaterials.Count == 0)
            return "请先选择 Material 资源。";

        if (_combinedTargets.Count != _combinedMeshes.Count)
            return $"目标物体数量 ({_combinedTargets.Count}) 与 Mesh 数量 ({_combinedMeshes.Count}) 不相等，要求一对一匹配。";

        // 检查null
        for (int i = 0; i < _combinedTargets.Count; i++)
        {
            if (_combinedTargets[i] == null)
                return $"第 {i + 1} 个目标物体为 null，请重新选择。";
        }

        for (int i = 0; i < _combinedMeshes.Count; i++)
        {
            if (_combinedMeshes[i] == null)
                return $"第 {i + 1} 个 Mesh 为 null，请重新选择。";
        }

        for (int i = 0; i < _combinedMaterials.Count; i++)
        {
            if (_combinedMaterials[i] == null)
                return $"第 {i + 1} 个 Material 为 null，请重新选择。";
        }

        // 检查每个目标是否有MeshFilter/SkinnedMeshRenderer + Renderer
        for (int i = 0; i < _combinedTargets.Count; i++)
        {
            var go = _combinedTargets[i];
            var mf = go.GetComponent<MeshFilter>();
            var smr = go.GetComponent<SkinnedMeshRenderer>();
            if (mf == null && smr == null)
                return $"目标物体 \"{go.name}\" (第{i + 1}个) 没有 MeshFilter 或 SkinnedMeshRenderer 组件。";

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return $"目标物体 \"{go.name}\" (第{i + 1}个) 没有任何 Renderer 组件。";
        }

        return "";
    }

    // =========================================================
    // Combine：执行（Mesh一对一 + Material统一刷）
    // =========================================================

    /// <summary>
    /// 执行组合批处理：一对一赋值Mesh，同时将所有Material统一刷到每个目标的Renderer
    /// </summary>
    private void ExecuteCombined()
    {
        int targetCount = _combinedTargets.Count;
        int meshCount = _combinedMeshes.Count;
        int matCount = _combinedMaterials.Count;

        Undo.SetCurrentGroupName("综合功能-Mesh+材质批处理");
        int undoGroup = Undo.GetCurrentGroup();

        int successCount = 0;
        int skipCount = 0;
        string matNameList = string.Join(", ", _combinedMaterials.Select(m => m?.name ?? "null"));

        for (int i = 0; i < targetCount; i++)
        {
            GameObject target = _combinedTargets[i];
            Mesh mesh = _combinedMeshes[i];

            if (target == null || mesh == null)
            {
                Debug.LogWarning($"[综合功能] ExecuteCombined: 第 {i + 1} 对为空（target={target?.name ?? "null"}, mesh={mesh?.name ?? "null"}），已跳过。");
                skipCount++;
                continue;
            }

            // 检查并赋值Mesh
            MeshFilter mf = target.GetComponent<MeshFilter>();
            SkinnedMeshRenderer smr = target.GetComponent<SkinnedMeshRenderer>();
            bool meshAssigned = false;

            if (mf != null)
            {
                Undo.RecordObject(mf, "综合功能-赋值Mesh");
                mf.sharedMesh = mesh;
                meshAssigned = true;
                Debug.Log($"[综合功能] ExecuteCombined: \"{target.name}\" (MeshFilter) ← Mesh \"{mesh.name}\"");
            }
            else if (smr != null)
            {
                Undo.RecordObject(smr, "综合功能-赋值Mesh");
                smr.sharedMesh = mesh;
                meshAssigned = true;
                Debug.Log($"[综合功能] ExecuteCombined: \"{target.name}\" (SkinnedMeshRenderer) ← Mesh \"{mesh.name}\"");
            }
            else
            {
                Debug.LogError($"[综合功能] ExecuteCombined: \"{target.name}\" 没有 MeshFilter 或 SkinnedMeshRenderer 组件，已跳过。");
                skipCount++;
                continue;
            }

            // 赋值材质（统一批量刷）
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                Undo.RecordObject(renderer, "综合功能-统一刷材质");
                var mats = _combinedMaterials.ToArray();
                renderer.sharedMaterials = mats;
                Debug.Log($"[综合功能] ExecuteCombined: \"{target.name}\" (Renderer) ← 统一刷材质 ({matCount}个): {matNameList}");
                successCount++;
            }
            else
            {
                Debug.LogWarning($"[综合功能] ExecuteCombined: \"{target.name}\" Renderer为null，Mesh已赋值但材质跳过。");
                skipCount++;
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[综合功能] ExecuteCombined 完成！成功: {successCount}, 跳过: {skipCount}, 目标总数: {targetCount}, Mesh总数: {meshCount}, 材质总数: {matCount}");
        _combinedStatusMessage = $"完成：成功 {successCount} 个，跳过 {skipCount} 个。";
    }
}
