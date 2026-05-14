using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class TrackPathVisualizer : MonoBehaviour
{
    public Color lineColor = Color.green;
    public float nodeSize = 0.05f;
    [Header("仅连接名字包含以下字符的物体")]
    public string filterKeyword = "路径"; // 或者改为 "Node"

    void OnDrawGizmos()
    {
        // 1. 获取旗下所有子物体（无论多深）
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        List<Transform> nodeLinks = new List<Transform>();

        // 2. 精确筛选：只有名字里带“路径”或“Node”的才放进列表
        foreach (var t in allChildren)
        {
            if (t != this.transform && (t.name.Contains(filterKeyword) || t.name.Contains("Node")))
            {
                nodeLinks.Add(t);
            }
        }

        if (nodeLinks.Count < 2) return;

        Gizmos.color = lineColor;

        // 3. 按照在 Hierarchy 里的顺序连线
        for (int i = 0; i < nodeLinks.Count; i++)
        {
            Gizmos.DrawWireSphere(nodeLinks[i].position, nodeSize);

            Vector3 start = nodeLinks[i].position;
            Vector3 end = (i == nodeLinks.Count - 1) ? nodeLinks[0].position : nodeLinks[i + 1].position;

            Gizmos.DrawLine(start, end);

#if UNITY_EDITOR
            // 显示序号，如果序号跳了，说明层级面板里的顺序不对
            UnityEditor.Handles.Label(nodeLinks[i].position + Vector3.up * 0.1f, i.ToString());
#endif
        }
    }
}