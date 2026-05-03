using UnityEngine;
using System.Collections.Generic;

public partial class TankSuspensionManager : MonoBehaviour
{
    [System.Serializable]
    public struct NodeBinding
    {
        public Transform node;          // 要跟随的 path Node（Node_X）
        public TankSuspensionArm arm;   // 对应的悬挂臂
        public Vector3 localOffset;     // 相对 wheelPivot 的偏移（通常 Y 轴微调）
    }

    [Header("履带节点绑定")]
    public List<NodeBinding> nodeBindings = new List<NodeBinding>();
    public bool autoBindNodes = true;   // 运行时自动匹配最近悬挂臂
    public float nodeBindingExtraDistance = 0.2f;
    public float nodeFollowVerticalOffset = 0f;

    void Start()
    {
        if (autoBindNodes)
            BuildNodeBindings();
    }

    // 每帧在 LateUpdate 同步，确保在 TrackController.LateUpdate 之前完成
    void LateUpdate()
    {
        foreach (NodeBinding binding in nodeBindings)
        {
            if (binding.node == null || binding.arm == null || binding.arm.wheelPivot == null)
                continue;

            Vector3 newPos = binding.arm.wheelPivot.TransformPoint(binding.localOffset);
            if (!Mathf.Approximately(nodeFollowVerticalOffset, 0f))
            {
                newPos += binding.arm.SuspensionUp * nodeFollowVerticalOffset;
            }

            if (binding.arm.IsGrounded)
            {
                Vector3 hitPoint = binding.arm.GroundHitPoint;
                Vector3 normal = binding.arm.LastGroundNormal;

                // 平面方程：dot(normal, P - hitPoint) = 0，解出节点 X/Z 对应的地面 Y
                if (Mathf.Abs(normal.y) > 0.01f)
                {
                    float groundY = hitPoint.y
                        - (normal.x * (newPos.x - hitPoint.x) + normal.z * (newPos.z - hitPoint.z))
                        / normal.y;
                    if (newPos.y < groundY)
                        newPos.y = groundY;
                }
            }

            binding.node.position = newPos;
        }
    }

    // 自动将 pathRoot 下的 Node 与最近的悬挂臂 wheelPivot 配对
    private void BuildNodeBindings()
    {
        nodeBindings.Clear();
        float bindingMaxDistance = ResolveNodeBindingMaxDistance();

        // 收集所有固定轮（诱导轮/传动轮）的位置，这些轮子附近的节点不应被悬挂臂拉动
        FixedWheelMarker[] fixedWheels = GetComponentsInChildren<FixedWheelMarker>(true);

        TrackController[] tracks = GetComponentsInChildren<TrackController>(true);
        foreach (TrackController track in tracks)
        {
            if (track == null || track.pathRoot == null) continue;

            foreach (Transform child in track.pathRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child == track.pathRoot) continue;
                if (!child.name.Contains("Node")) continue;

                TankSuspensionArm nearest = FindNearestArm(child.position);
                if (nearest == null || nearest.wheelPivot == null) continue;

                float dist = Vector3.Distance(child.position, nearest.wheelPivot.position);
                if (dist > bindingMaxDistance) continue;

                // 如果节点比悬挂臂更靠近某个固定轮的 wheelPivot，跳过绑定
                bool nearFixedWheel = false;
                foreach (FixedWheelMarker fw in fixedWheels)
                {
                    if (fw == null) continue;
                    TankSuspensionArm fwArm = fw.GetComponent<TankSuspensionArm>();
                    Vector3 fwPos = (fwArm != null && fwArm.wheelPivot != null)
                        ? fwArm.wheelPivot.position
                        : fw.transform.position;
                    if (Vector3.Distance(child.position, fwPos) < dist)
                    {
                        nearFixedWheel = true;
                        break;
                    }
                }
                if (nearFixedWheel) continue;

                nodeBindings.Add(new NodeBinding
                {
                    node = child,
                    arm = nearest,
                    localOffset = nearest.wheelPivot.InverseTransformPoint(child.position)
                });
            }
        }
    }

    private TankSuspensionArm FindNearestArm(Vector3 worldPos)
    {
        TankSuspensionArm nearest = null;
        float minDist = float.MaxValue;
        foreach (TankSuspensionArm arm in suspensionArms)
        {
            if (arm == null || arm.wheelPivot == null) continue;
            float d = Vector3.Distance(worldPos, arm.wheelPivot.position);
            if (d < minDist) { minDist = d; nearest = arm; }
        }
        return nearest;
    }

    private float ResolveNodeBindingMaxDistance()
    {
        float extraDistance = Mathf.Max(0f, nodeBindingExtraDistance);
        return Mathf.Max(0.01f, wheelRadius + maxCompression + extraDistance);
    }
}
