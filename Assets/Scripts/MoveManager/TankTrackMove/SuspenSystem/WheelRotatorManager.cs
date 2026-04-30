using UnityEngine;
using System.Collections.Generic;

public class WheelRotatorManager : MonoBehaviour
{
    [Header("引用")]
    public TrackController _trackController;

    [Header("参数")]
    public float wheelRadius = 0.85f;
    public Vector3 rotationAxis = new Vector3(1, 0, 0);   // X轴
    public float speedLerp = 10f;

    private List<Transform> wheels = new List<Transform>();
    private readonly Dictionary<Transform, float> wheelSpeeds = new Dictionary<Transform, float>();

    void Start()
    {
        wheels.Clear();

        // 自动查找所有可能的轮子（更宽松的条件）
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            string name = child.name.ToLower();

            // 匹配常见轮子命名：主悬挂、前轮、后轮、诱导轮、负重轮、动力轮、托轮、RoadWheel、RotatePoint 等
            if (!IsWheelAssemblyName(name))
            {
                continue;
            }

            // 找到主悬挂(1)、主悬挂(2)... 后，再找它的子物体中带模型的
            foreach (Transform sub in child.GetComponentsInChildren<Transform>(true))
            {
                string subName = sub.name.ToLower();
                // 优先找有MeshRenderer的物体（真正的轮子模型）
                if ((sub.GetComponent<MeshRenderer>() != null ||
                    sub.GetComponent<SkinnedMeshRenderer>() != null) &&
                    !subName.Contains("悬挂") && !subName.Contains("组") && !subName.Contains("履带"))
                {
                    wheels.Add(sub);
                    wheelSpeeds[sub] = 0f;
                    ValidateWheelRotationScript(sub);
                    // Debug.Log($"找到轮子: {sub.name} (父级: {child.name})");
                    break;   // 一个主悬挂只加一个轮子
                }
            }
        }

        Debug.Log($"总共找到 {wheels.Count} 个轮子");
    }

    void LateUpdate()
    {
        if (_trackController == null || wheels.Count == 0)
        {
            // 每秒提醒一次，避免刷屏
            if (Time.frameCount % 60 == 0)
                Debug.LogWarning("轮子未找到，轮子无法旋转");
            return;
        }

        foreach (var wheel in wheels)
        {
            float linearSpeed = _trackController.GetWheelVisualSpeed();
            float currentWheelSpeed = wheelSpeeds.TryGetValue(wheel, out float cachedSpeed) ? cachedSpeed : 0f;
            currentWheelSpeed = Mathf.Lerp(currentWheelSpeed, linearSpeed, Mathf.Clamp01(speedLerp * Time.deltaTime));
            wheelSpeeds[wheel] = currentWheelSpeed;
            float angularSpeedRad = currentWheelSpeed / wheelRadius;
            float angleThisFrame = angularSpeedRad * Time.deltaTime * Mathf.Rad2Deg;

            wheel.Rotate(rotationAxis * angleThisFrame, Space.Self);
        }
    }

    private bool IsWheelAssemblyName(string loweredName)
    {
        if (string.IsNullOrEmpty(loweredName))
        {
            return false;
        }

        if (loweredName.Contains("组") || loweredName.Contains("悬挂组") || loweredName.Contains("tracklink"))
        {
            return false;
        }

        return loweredName.Contains("主悬挂")
            || loweredName.Contains("前轮")
            || loweredName.Contains("后轮")
            || loweredName.Contains("诱导轮")
            || loweredName.Contains("负重轮")
            || loweredName.Contains("动力轮")
            || loweredName.Contains("托轮")
            || loweredName.Contains("roadwheel")
            || loweredName.Contains("idler")
            || loweredName.Contains("sprocket")
            || loweredName.Contains("wheel");
    }

    private void ValidateWheelRotationScript(Transform wheel)
    {
        if (wheel == null)
        {
            return;
        }

        if (wheel.GetComponentInParent<WheelVisualSpinSign>() == null)
        {
            // Debug.LogWarning($"轮子 {wheel.name} 未挂载 WheelVisualSpinSign，仅做标记检查，不参与方向计算");
        }
    }

}