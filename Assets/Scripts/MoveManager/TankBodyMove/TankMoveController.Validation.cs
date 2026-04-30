using UnityEngine;

public partial class TankMoveController : MonoBehaviour
{
    private bool ValidateSetup()
    {
        if (tanker == null)
        {
            LogSetupErrorOnce("TankMoveController 依赖 tanker GameObject，但未分配！");
            return false;
        }

        if (tankMoveData == null)
        {
            LogSetupErrorOnce("TankMoveController 依赖 TankMoveData，但未分配！");
            return false;
        }

        if (tankRigidbody == null)
        {
            CacheRigidbody();
            if (tankRigidbody == null)
            {
                LogSetupErrorOnce("TankMoveController 依赖 tanker 上的 Rigidbody，但未找到！");
                return false;
            }
        }

        if (MIddleInputingController.Instance == null)
        {
            LogSetupErrorOnce("TankMoveController 依赖 MIddleInputingController，但未找到实例！");
            return false;
        }

        _hasLoggedSetupError = false;
        return true;
    }

    private void LogSetupErrorOnce(string message)
    {
        if (_hasLoggedSetupError)
        {
            return;
        }

        Debug.LogError(message);
        _hasLoggedSetupError = true;
    }

    private void CacheRigidbody()
    {
        if (tanker != null)
        {
            tankRigidbody = tanker.GetComponent<Rigidbody>();
        }
    }

    // private void ConfigureRuntimeMass()
    // {
    //     if (tankRigidbody == null || tankMoveData == null)
    //     {
    //         return;
    //     }

    //     tankRigidbody.mass = Mathf.Max(0.01f, tankMoveData.Mass);
    // }


    private void ConfigureRuntimeMass()
    {
        if (tankRigidbody == null || tankMoveData == null) return;

        tankRigidbody.mass = Mathf.Max(0.01f, tankMoveData.Mass);

        // 1. 设定几何中心矩 (CoG Height)
        // 将质心下压或抬高，创造 M_roll = F_c * h 的物理条件
        tankRigidbody.centerOfMass = new Vector3(0, cogHeight, 0);

        // 2. 惯性张量矩阵 (Inertia Tensor)
        // 增加 Y 轴惯性（防止瞬间转身），调整 X/Z 轴赋予翻滚沉重感
        Vector3 inertia = tankRigidbody.inertiaTensor;
        inertia.y *= 1.5f; // 偏航转动惯量
        inertia.z *= 1.2f; // 侧倾转动惯量
        tankRigidbody.inertiaTensor = inertia;
    }
}