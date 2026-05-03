using UnityEngine;

public partial class TankMoveController : MonoBehaviour
{


    private float ResolveGroundFrictionCoefficient(RaycastHit hit)
    {
        float coefficient = GetDefaultGroundFrictionCoefficient();

        if (hit.collider != null && hit.collider.sharedMaterial != null)
        {
            PhysicsMaterial material = hit.collider.sharedMaterial;
            coefficient = Mathf.Max(coefficient, (material.staticFriction + material.dynamicFriction) * 0.5f);
        }

        // 返回的是“地表阻力输入”，后面还会进一步压缩到滚阻量级。
        return Mathf.Max(0.01f, coefficient);
    }

    private float GetDefaultGroundFrictionCoefficient()
    {
        // 当前没有地形导入时，使用 SO 默认值。
        // 后续接入地形数据后，这里可以替换成地表/材质库返回值。
        if (tankMoveData != null && tankMoveData.DefaultGroundFrictionCoefficient > 0f)
        {
            return tankMoveData.DefaultGroundFrictionCoefficient;
        }

        return Mathf.Max(0.01f, fallbackGroundFrictionCoefficient);
    }


}