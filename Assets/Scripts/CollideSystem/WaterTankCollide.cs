using UnityEngine;

public class WaterTankCollide : MonoBehaviour
{

    //水箱碰撞检测脚本，挂在水箱上碰撞Object上
    //延伸到其他需要碰撞检测的部件上，例如炮管等，都可以使用类似的脚本来进行碰撞检测和处理
    //具体来说就是在需要进行碰撞检测的部件上挂载碰撞检测脚本，并在脚本中实现相应的碰撞处理逻辑，例如检测与其他物体的碰撞、计算碰撞点和距离等

    //注意事项：
    //1. 确保碰撞检测脚本正确挂载在需要进行碰撞检测的部件上，并且Collider组件正确设置。
    //也就是一个发射端和一个接收端，发射端负责发出碰撞检测请求，接收端负责处理碰撞检测结果。——
    // 在这个例子中，炮管作为发射端，水箱作为接收端。炮管发出碰撞检测请求，水箱接收并处理这些请求，计算碰撞点和距离等信息，并将结果返回给炮管，以便炮管根据这些信息调整射击方向和行为。
    //发射端指的是动的部件，例如炮管，接收端指的是静止的部件，例如水箱


    //2. 在碰撞检测脚本中实现相应的碰撞处理逻辑，例如检测与其他物体的碰撞、计算碰撞点和距离等。
    //3. 根据需要，可以在碰撞检测脚本中添加一些调试功能，例如在Scene视图中显示碰撞点和距离等信息，以便于调试和优化碰撞检测逻辑。

    [SerializeField] private Collider[] _colliders;// 碰撞检测使用的Collider组件数组，可以在Inspector中手动指定，或者通过代码自动获取子物体上的Collider组件
    //支持多个Collider组件的碰撞检测，确保覆盖整个水箱的碰撞范围

    private void Awake()
    {
        CacheColliders();
    }

    private void OnValidate()
    {
        CacheColliders();
    }

    private void CacheColliders()
    {
        if (_colliders == null || _colliders.Length == 0)
        {
            _colliders = GetComponentsInChildren<Collider>(true);
        }
    }

    // 这个方法用于检测一个世界坐标点与水箱的碰撞情况，返回最近的碰撞点和距离
    public bool TryGetClosestPoint(Vector3 worldPoint, out Vector3 closestPoint, out float distance)
    {
        closestPoint = worldPoint;
        distance = float.MaxValue;

        if (_colliders == null || _colliders.Length == 0)
        {
            return false;
        }

        bool found = false;

        foreach (Collider targetCollider in _colliders)
        {
            if (targetCollider == null || !targetCollider.enabled)
            {
                continue;
            }

            Vector3 point = targetCollider.ClosestPoint(worldPoint);
            float currentDistance = Vector3.Distance(worldPoint, point);

            if (!found || currentDistance < distance)
            {
                found = true;
                distance = currentDistance;
                closestPoint = point;
            }
        }

        return found;
        //返回是否找到有效的碰撞点，如果没有找到任何有效的碰撞点，则返回false，closestPoint和distance将保持默认值（worldPoint和float.MaxValue）。
        // 如果找到了有效的碰撞点，则返回true，并且closestPoint和distance将被更新为最近的碰撞点和距离。
    }

    // 这个方法用于检测一个包络线上的多个采样点与水箱的碰撞情况，返回最近的碰撞点和最小的碰撞距离给发射端，以便发射端根据这些信息调整射击方向和行为
    public bool TryGetEnvelopeCollision(Vector3[] envelopeSamplePoints, float envelopeRadius, out Vector3 closestPoint, out float minimumClearance)
    {
        closestPoint = Vector3.zero;
        minimumClearance = float.MaxValue;

        if (envelopeSamplePoints == null || envelopeSamplePoints.Length == 0)
        {
            return false;
        }

        if (_colliders == null || _colliders.Length == 0)
        {
            return false;
        }

        bool hasCollision = false;

        foreach (Vector3 samplePoint in envelopeSamplePoints)
        {
            foreach (Collider targetCollider in _colliders)
            {
                if (targetCollider == null || !targetCollider.enabled)
                {
                    continue;
                }

                Vector3 colliderClosestPoint = targetCollider.ClosestPoint(samplePoint);
                float distanceToCollider = Vector3.Distance(samplePoint, colliderClosestPoint);
                float clearance = distanceToCollider - envelopeRadius;

                if (clearance < minimumClearance)
                {
                    minimumClearance = clearance;
                    closestPoint = colliderClosestPoint;
                }

                if (clearance <= 0f)
                {
                    hasCollision = true;
                }
            }
        }

        return hasCollision;
        //返回是否检测到碰撞，如果检测到碰撞，则返回true，并且closestPoint和minimumClearance将被更新为最近的碰撞点和最小的碰撞距离。
        // 如果没有检测到碰撞，则返回false，closestPoint和minimumClearance将保持默认值（Vector3.zero和float.MaxValue）。
    }


}