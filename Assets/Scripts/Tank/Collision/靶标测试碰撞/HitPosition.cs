using UnityEngine;
/// <summary>
/// 伤害逻辑链的第一环，负责接收被击信息并将其转发给 TargetDamageResolver 进行伤害计算和应用，也可以转发给 TargetDurone 进行一些额外的处理，例如记录命中部位、播放受击动画等。
/// 
/// </summary>
public class HitPosition : MonoBehaviour
{
    [SerializeField] private TargetDurone targetDurone;
    [SerializeField] private string hitPartName = "受击区";
    private TargetDamageResolver _targetDamageResolver;

    private void Awake()
    {
        if (targetDurone == null)
        {
            targetDurone = GetComponentInParent<TargetDurone>();
        }

        if (targetDurone == null)
        {
            Debug.LogError($"{name} 没有找到 TargetDurone 组件，无法转发受击信息。", this);
        }

        if (targetDurone != null)
        {
            _targetDamageResolver = targetDurone.GetComponent<TargetDamageResolver>();
            if (_targetDamageResolver == null)
            {
                Debug.LogError($"{name} 的 TargetDurone 上没有找到 TargetDamageResolver 组件，无法处理伤害逻辑。", this);
            }
        }
    }


    public void ReceiveHit(CannonBall cannonBall, Collider hitCollider)
    {
        // if (targetDurone == null)
        // {
        //     Debug.LogWarning($"{name} 收到命中，但没有 TargetDurone 可转发。", this);
        //     return;
        // }

        Rigidbody cannonBallRigidbody = cannonBall.GetComponent<Rigidbody>();

        Vector3 hitPoint = hitCollider != null ? hitCollider.ClosestPoint(cannonBall.transform.position) : transform.position;

        Vector3 cannonBallVelocity = cannonBallRigidbody != null ? cannonBallRigidbody.linearVelocity : Vector3.zero;

        Debug.Log($"HitPosition 收到命中：部位 = {hitPartName}, 命中点 = {hitPoint}, 炮弹 = {cannonBall.name}, 炮弹速度 = {cannonBallVelocity}", this);

        // 将命中信息转发给 TargetDurone 进行处理
        targetDurone?.ReceiveHit(hitPartName, cannonBall, hitCollider);

        if (_targetDamageResolver != null && cannonBall != null)
        {
            float damageAmount = cannonBall.GetDamageAmount(); // 假设 CannonBall 有一个方法可以获取伤害值
            _targetDamageResolver.ApplyDamage(damageAmount, hitPartName, cannonBall.gameObject.name, hitPoint);
        }
        else
        {
            Debug.LogWarning($"HitPosition 在 {targetDurone.name} 上没有找到 TargetDamageResolver 组件，无法应用伤害。", this);
        }


    }

}