using UnityEngine;
using System;

/// <summary>
/// 负责炮弹的命中检测逻辑，使用射线步进的方式检测炮弹在飞行过程中是否与任何物体发生碰撞。该组件会在每个 FixedUpdate 中计算炮弹的当前位置和上一帧位置之间的路径，并使用 Physics.Raycast 来检测是否有碰撞发生。如果检测到碰撞，它会将命中信息传递给被命中的对象，以便进行伤害计算、视觉反馈等后续处理。
/// </summary>
public partial class CannonBall : MonoBehaviour
{

    // 用 RaycastAll 做步进检测：如果射线路径中有多个命中体，只取最先命中的非自身 Collider。

    private bool TryRayStepHit(Vector3 startPosition, Vector3 endPosition, out RaycastHit bestHit)
    {
        bestHit = default;

        Vector3 displacement = endPosition - startPosition;
        float distance = displacement.magnitude;
        if (distance <= 0f)
        {
            return false;
        }

        Vector3 direction = displacement / distance;
        RaycastHit[] hits = Physics.RaycastAll(startPosition, direction, distance, _hitMask, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (IsSelfCollider(hit.collider))
            {
                continue;
            }

            bestHit = hit;
            return true;
        }

        return false;
    }

    // 自身 Collider 是炮弹探针或炮弹根节点时，Raycast 命中它们要忽略。
    private bool IsSelfCollider(Collider other)
    {
        return other != null && other.transform.IsChildOf(transform);
    }
}