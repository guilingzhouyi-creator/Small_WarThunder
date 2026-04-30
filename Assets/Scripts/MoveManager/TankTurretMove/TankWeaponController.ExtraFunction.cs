using UnityEngine;



public partial class TankWeaponController : MonoBehaviour
{
#if UNITY_EDITOR

    [Header("--- FCS 设置 ---")]
    public float sphereRadius = 100f;  // 球体半径

    // 🔵🟡🔴 三层胶囊体检测范围（每层都可独立调整长度和半径）
    [Header("🔵 外层（蓝色）")]
    [SerializeField] private float barrelLengthOuter = 6f;       // 外层：完整检测范围长度
    [SerializeField] private float barrelRadiusOuter = 0.3f;     // 外层：胶囊体半径

    [Header("🟡 中层（黄色）")]
    [SerializeField] private float barrelLengthMiddle = 3f;      // 中层：预判触发范围长度
    [SerializeField] private float barrelRadiusMiddle = 0.25f;   // 中层：胶囊体半径

    [Header("🔴 内层（红色）")]
    [SerializeField] private float barrelLengthInner = 1.2f;     // 内层：紧急避撞范围长度
    [SerializeField] private float barrelRadiusInner = 0.15f;    // 内层：胶囊体半径
    private void OnDrawGizmos()
    {
        // 绘制球体预览 (以炮口为中心)
        if (barrel != null)
        {
            Vector3 barrelTip = barrel.position + barrel.forward * 0.5f;
            Gizmos.color = new Color(1, 1, 0, 0.1f);
            Gizmos.DrawWireSphere(barrelTip, sphereRadius);
        }
        else if (turret != null)
        {
            Gizmos.color = new Color(1, 1, 0, 0.1f);
            Gizmos.DrawWireSphere(turret.position, sphereRadius);
        }

        // 绘制目标交点
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(_currentAimPoint, 0.8f);

        // ✨ 绘制炮管碰撞检测范围（SphereCast可视化）
        VisualizeBarrelCollisionRange();
    }

    /// <summary>
    /// 可视化三层胶囊体碰撞检测范围
    /// 🔵 蓝胶囊体 = 外层检测范围（独立长度和半径）
    /// 🟡 黄胶囊体 = 中间预判范围（独立长度和半径）
    /// 🔴 红胶囊体 = 内层紧急避撞范围（独立长度和半径）
    /// </summary>
    private void VisualizeBarrelCollisionRange()
    {
        if (barrelRoot == null || barrel == null) return;

        Vector3 rayStart = barrelRoot.position;
        Vector3 rayDirection = barrel.forward.normalized;

        // 计算三个胶囊体的终点
        Vector3 outerEnd = rayStart + rayDirection * barrelLengthOuter;      // 蓝
        Vector3 middleEnd = rayStart + rayDirection * barrelLengthMiddle;    // 黄
        Vector3 innerEnd = rayStart + rayDirection * barrelLengthInner;      // 红

        // 计算垂直于射线方向的四个侧线方向
        Vector3[] sideDirections = new Vector3[]
        {
            Vector3.Cross(rayDirection, Vector3.up).normalized,
            Vector3.Cross(rayDirection, Vector3.right).normalized,
            -Vector3.Cross(rayDirection, Vector3.up).normalized,
            -Vector3.Cross(rayDirection, Vector3.right).normalized
        };

        // ========== 🔵 蓝色胶囊体 = 外层完整检测范围 ==========
        DrawCapsule(rayStart, outerEnd, sideDirections, barrelRadiusOuter,
                    new Color(0.2f, 0.6f, 1f, 0.3f), new Color(0.2f, 0.6f, 1f, 0.4f));

        // ========== 🟡 黄色胶囊体 = 中间预判范围 ==========
        DrawCapsule(rayStart, middleEnd, sideDirections, barrelRadiusMiddle,
                    new Color(1f, 1f, 0.2f, 0.3f), new Color(1f, 1f, 0.2f, 0.4f));

        // ========== 🔴 红色胶囊体 = 内层紧急避撞范围 ==========
        DrawCapsule(rayStart, innerEnd, sideDirections, barrelRadiusInner,
                    new Color(1f, 0.2f, 0.2f, 0.3f), new Color(1f, 0.2f, 0.2f, 0.4f));

        // 标记三个范围的边界
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
        Gizmos.DrawSphere(innerEnd, barrelRadiusInner * 0.2f);
        Gizmos.color = new Color(1f, 1f, 0.2f, 0.6f);
        Gizmos.DrawSphere(middleEnd, barrelRadiusMiddle * 0.2f);
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.6f);
        Gizmos.DrawSphere(outerEnd, barrelRadiusOuter * 0.2f);
    }

    /// <summary>
    /// 绘制一个胶囊体（由起点球体、终点球体和4条侧线组成）
    /// </summary>
    private void DrawCapsule(Vector3 start, Vector3 end, Vector3[] sideDirections, float radius, Color sphereColor, Color lineColor)
    {
        // 起点球体
        Gizmos.color = sphereColor;
        Gizmos.DrawWireSphere(start, radius);

        // 终点球体
        Gizmos.color = sphereColor;
        Gizmos.DrawWireSphere(end, radius);

        // 4条侧线
        Gizmos.color = lineColor;
        foreach (Vector3 side in sideDirections)
        {
            Vector3 sideOffset = side * radius;
            Gizmos.DrawLine(start + sideOffset, end + sideOffset);
        }
    }
#endif
}