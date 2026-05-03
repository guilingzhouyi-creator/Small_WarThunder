using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 火控系统的测距仪功能相关的代码部分，负责处理与测距仪相关的逻辑
/// 1、将得到处理结果中转到TankStateUI上显示出来 
/// 2、将得到结果拷贝一份给武器WeponController，调整弹道，以确保射击的准确性
/// </summary>
public partial class TankFireController : MonoBehaviour
{

    private float HandleRangeFinderInput()
    {
        // 1. 确定射线的起点，通常是炮口位置
        // 注意：如果火控系统的Transform不是直接挂在炮管上的，可能需要根据炮管的位置来调整起点
        Vector3 origin = _firePoint != null ? _firePoint.position : transform.position;

        // 2. 获取炮管的真实前向矢量
        Vector3 direction = ResolveFireDirection(_firePoint);

        // 3. 这里的 LayerMask 非常关键！必须忽略坦克自身层级，防止“自撞”
        // 假设坦克层是 Layer 6，我们可以使用 ~(1 << 6)
        int layerMask = ~LayerMask.GetMask("Player", "Ignore Raycast");

        if (Physics.Raycast(origin, direction, out var hit, _MainGunWeaponData.MaxFCSdistance, layerMask))
        {
            float distance = Vector3.Distance(origin, hit.point);

            Debug.DrawRay(origin, direction * hit.distance, Color.red, 1f);

            RangerFinderResultUpdated?.Invoke(distance);//将测距结果通过事件传递给UI和WeaponController，确保它们都能获得最新的距离信息以更新显示和调整弹道
            Debug.Log($"激光测距仪命中: {hit.collider.gameObject.name} 距离: {distance:F2} 米");
            return distance;
        }

        Debug.DrawRay(origin, direction * _MainGunWeaponData.MaxFCSdistance, Color.green, 1f);

        Debug.Log("激光测距仪未命中任何目标，距离超过最大测距范围");
        RangerFinderResultUpdated?.Invoke(-1f);
        return -1f;
    }
}