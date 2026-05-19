using System.Collections.Generic;
using UnityEngine;

public partial class AI_TankSuspensionManager : MonoBehaviour
{
    [System.Serializable]
    public struct NodeBinding
    {
        public Transform node;
        public AI_SuspensionArm arm;
        public Vector3 localOffset;
    }

    [Header("履带节点绑定")]
    public List<NodeBinding> nodeBindings = new List<NodeBinding>();
    public bool autoBindNodes = true;
    public float nodeBindingExtraDistance = 0.2f;
    public float nodeFollowVerticalOffset = 0f;

    private void Start()
    {
        if (autoBindNodes)
        {
            BuildNodeBindings();
        }
    }

    private void LateUpdate()
    {
        foreach (NodeBinding binding in nodeBindings)
        {
            if (binding.node == null || binding.arm == null || binding.arm.wheelPivot == null)
            {
                continue;
            }

            Vector3 newPosition = binding.arm.wheelPivot.TransformPoint(binding.localOffset);
            if (!Mathf.Approximately(nodeFollowVerticalOffset, 0f))
            {
                newPosition += binding.arm.SuspensionUp * nodeFollowVerticalOffset;
            }

            if (binding.arm.IsGrounded)
            {
                Vector3 hitPoint = binding.arm.GroundHitPoint;
                Vector3 normal = binding.arm.LastGroundNormal;

                if (Mathf.Abs(normal.y) > 0.01f)
                {
                    float groundY = hitPoint.y
                        - (normal.x * (newPosition.x - hitPoint.x) + normal.z * (newPosition.z - hitPoint.z))
                        / normal.y;
                    if (newPosition.y < groundY)
                    {
                        newPosition.y = groundY;
                    }
                }
            }

            binding.node.position = newPosition;
        }
    }

    private void BuildNodeBindings()
    {
        nodeBindings.Clear();
        float bindingMaxDistance = ResolveNodeBindingMaxDistance();
        FixedWheelMarker[] fixedWheels = GetComponentsInChildren<FixedWheelMarker>(true);

        TrackPathRendererBase[] tracks = GetComponentsInChildren<TrackPathRendererBase>(true);
        foreach (TrackPathRendererBase track in tracks)
        {
            if (track == null || track.pathRoot == null)
            {
                continue;
            }

            foreach (Transform child in track.pathRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child == track.pathRoot || !child.name.Contains("Node"))
                {
                    continue;
                }

                AI_SuspensionArm nearest = FindNearestArm(child.position);
                if (nearest == null || nearest.wheelPivot == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(child.position, nearest.wheelPivot.position);
                if (distance > bindingMaxDistance)
                {
                    continue;
                }

                bool nearFixedWheel = false;
                foreach (FixedWheelMarker fixedWheel in fixedWheels)
                {
                    if (fixedWheel == null)
                    {
                        continue;
                    }

                    AI_SuspensionArm fixedWheelArm = fixedWheel.GetComponent<AI_SuspensionArm>();
                    Vector3 fixedWheelPosition = fixedWheelArm != null && fixedWheelArm.wheelPivot != null
                        ? fixedWheelArm.wheelPivot.position
                        : fixedWheel.transform.position;
                    if (Vector3.Distance(child.position, fixedWheelPosition) < distance)
                    {
                        nearFixedWheel = true;
                        break;
                    }
                }

                if (nearFixedWheel)
                {
                    continue;
                }

                nodeBindings.Add(new NodeBinding
                {
                    node = child,
                    arm = nearest,
                    localOffset = nearest.wheelPivot.InverseTransformPoint(child.position)
                });
            }
        }
    }

    private AI_SuspensionArm FindNearestArm(Vector3 worldPosition)
    {
        AI_SuspensionArm nearest = null;
        float minDistance = float.MaxValue;

        foreach (AI_SuspensionArm arm in _suspensionArms)
        {
            if (arm == null || arm.wheelPivot == null)
            {
                continue;
            }

            float distance = Vector3.Distance(worldPosition, arm.wheelPivot.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = arm;
            }
        }

        return nearest;
    }

    private float ResolveNodeBindingMaxDistance()
    {
        float extraDistance = Mathf.Max(0f, nodeBindingExtraDistance);
        return Mathf.Max(0.01f, _wheelRadius + _maxCompression + extraDistance);
    }
}