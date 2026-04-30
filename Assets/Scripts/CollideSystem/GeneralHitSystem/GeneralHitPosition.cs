using UnityEngine;

/// <summary>
/// 通用命中检测区域组件，负责处理被炮弹击中时底层的事件响应逻辑。
/// 该组件可以附加在坦克的任何部分（车体、炮塔、炮管等），以实现对不同部位的命中检测和响应。
/// </summary>
public class GeneralHitPosition : MonoBehaviour
{
    /// <summary>
    /// 命中部位的名称，用于区分不同的命中区域（如车体、炮塔、炮管等）。可以在编辑器中设置，默认为 "受击区"。
    /// 外露属性，允许其他系统（如伤害计算系统）根据命中部位名称进行不同的处理逻辑。
    /// </summary>
    [SerializeField] private string hitPartName = "受击区";//这里实际上填的是上层组件挂载顶层脚本的名字
    private TargetDamageResolver _targetDamageResolver;
    private GeneralHitTopLevel _generalHitTopLevel;

    private void Awake()
    {
        //一个组件只能 GetComponentInParent 一次，先获取 GeneralHitTopLevel，再从 GeneralHitTopLevel 上的GameObject 获取 TargetDamageResolver。
        _generalHitTopLevel = GetComponentInParent<GeneralHitTopLevel>();
        _targetDamageResolver = _generalHitTopLevel.GetComponentInParent<TargetDamageResolver>();



        if (_targetDamageResolver == null || _generalHitTopLevel == null)
        {
            Debug.LogError($"{name} 的父级没有 TargetDamageResolver 或 GeneralHitTopLevel 组件，无法处理命中逻辑。", this);
        }
    }

    public void UploadHitData(CannonBall cannonBall, Collider hitCollider)
    {
        Rigidbody cannonBallRigidbody = cannonBall.GetComponent<Rigidbody>();

        // 计算命中点，如果 hitCollider 不为 null，则使用 ClosestPoint 方法获取炮弹位置最近的点作为命中点；否则使用当前组件的位置作为命中点
        Vector3 hitPoint = hitCollider != null ? hitCollider.ClosestPoint(cannonBall.transform.position) : transform.position;

        Vector3 cannonBallVelocity = cannonBallRigidbody != null ? cannonBallRigidbody.linearVelocity : Vector3.zero;
        // 获取炮弹的 Rigidbody 组件，以便获取炮弹的速度信息

        Debug.Log($"GeneralHitPosition 收到命中：部位 = {hitPartName}, 命中点 = {hitPoint}, 炮弹 = {cannonBall.name}, 炮弹速度 = {cannonBallVelocity}", this);

        _generalHitTopLevel?.ReceiveHit(hitPartName, cannonBall, hitCollider);


        if (_targetDamageResolver != null && cannonBall != null)
        {
            float damageAmount = cannonBall.GetDamageAmount();
            _targetDamageResolver.ApplyDamage(damageAmount, hitPartName, cannonBall.gameObject.name, hitPoint);
        }
        else
        {
            Debug.LogWarning($"GeneralHitPosition 在 {_targetDamageResolver.name} 上没有找到 TargetDamageResolver 组件，无法应用伤害。", this);
        }

    }

}