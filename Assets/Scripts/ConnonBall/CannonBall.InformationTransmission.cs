using UnityEngine;

/// <summary>
/// 负责炮弹的信息传递逻辑，例如将命中信息传递给被命中的对象，或者将飞行状态信息传递给其他系统（例如飞行轨迹显示系统）。该组件可以作为 CannonBall 的一个子组件，专门处理与信息传递相关的逻辑，以保持代码的清晰和模块化。
/// </summary>

public partial class CannonBall : MonoBehaviour
{
    // 命中后先转给受击区，再转给目标根对象；最后回收炮弹对象。
    private void ResolveHit(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return;
        }

        Debug.Log($"CannonBall 射线步进命中：碰撞对象 = {hitCollider.name}, 炮弹 = {name}");

        HitPosition hitPosition = hitCollider.GetComponentInParent<HitPosition>();
        GeneralHitPosition generalHitPosition = hitCollider.GetComponentInParent<GeneralHitPosition>();

        if (hitPosition != null)
        {
            Debug.Log($"CannonBall 找到 HitPosition：{hitPosition.name}，开始转发命中");
            hitPosition.ReceiveHit(this, hitCollider);
        }
        else if (generalHitPosition != null)
        {
            Debug.Log($"CannonBall 找到 GeneralHitPosition：{generalHitPosition.name}，开始转发命中");
            generalHitPosition.UploadHitData(this, hitCollider);
        }
        else
        {
            Debug.LogWarning($"CannonBall 没有在 {hitCollider.name} 的父级链上找到 HitPosition 或 GeneralHitPosition，尝试直接找 TargetDurone。", this);

            TargetDurone targetDurone = hitCollider.GetComponentInParent<TargetDurone>();
            if (targetDurone != null)
            {
                Debug.Log($"CannonBall 直接找到 TargetDurone：{targetDurone.name}，使用默认受击区转发");
                targetDurone.ReceiveHit("未定义受击区", this, hitCollider);
            }
            else
            {
                Debug.LogWarning($"CannonBall 既没有找到 HitPosition、GeneralHitPosition，也没有找到 TargetDurone，但是命中物体：{hitCollider.name}", this);
            }
        }

        CononBallReturnToPool();
    }

    private void CononBallReturnToPool()
    {
        if (_pooledObject != null)
        {
            _pooledObject.ReturnToPool();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }


}