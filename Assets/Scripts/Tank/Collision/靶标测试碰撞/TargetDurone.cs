using UnityEngine;
/// <summary>
/// 伤害逻辑链顶层（第三环）的组件，额外的处理，例如视觉反馈、受击动画、命中部位记录等，可以在这里进行
/// </summary>
public class TargetDurone : MonoBehaviour
{

    // private float _currentHealth = 100f;
    // private CannonBall _cannonBall;
    public void ReceiveHit(string hitPartName, CannonBall cannonBall, Collider hiCtCollider)
    {
        // _cannonBall = cannonBall;
        Vector3 hitPoint = hiCtCollider != null && cannonBall != null
        ? hiCtCollider.ClosestPoint(cannonBall.transform.position)
        : transform.position;

        Vector3 hitDirection = cannonBall != null ? cannonBall.transform.forward : Vector3.zero;
        string projectileName = cannonBall != null ? cannonBall.gameObject.name : "UnknownProjectile";

        Debug.Log($"TargetDurone 收到命中信息：命中 = {hitPartName}, 命中点 = {hitPoint}, 命中方向 = {hitDirection}, 投射物 = {projectileName}");
    }

}