using UnityEngine;
/// <summary>
/// 通用命中检测系统的顶层组件，负责接收来自 GeneralHitPosition 的命中信息，处理视觉反馈、受击动画、命中部位记录等额外的逻辑。
/// 该组件还可以继续上传信息给坦克Root层的一个更高层级的组件，例如 TankHitManager，以便进行全局的命中管理和反馈处理。
/// </summary>
public class GeneralHitTopLevel : MonoBehaviour
{
    public void ReceiveHit(string hitPartName, CannonBall cannonBall, Collider hiCtCollider)
    {
        Vector3 hitPoint = hiCtCollider != null && cannonBall != null
            ? hiCtCollider.ClosestPoint(cannonBall.transform.position)
            : transform.position;

        Vector3 hitDirection = cannonBall != null ? cannonBall.transform.forward : Vector3.zero;
        string projectileName = cannonBall != null ? cannonBall.gameObject.name : "UnknownProjectile";

        Debug.Log($"GeneralHitTopLevel 收到来自 GeneralHitPosition 的命中信息！");
        Debug.Log($"命中部位 = {hitPartName}, 命中点 = {hitPoint}, 来袭方向 = {hitDirection}, 命中对象 = {projectileName}");
    }


}