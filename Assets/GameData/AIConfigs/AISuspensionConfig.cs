using UnityEngine;

/// <summary>
/// AI悬挂系统全局参数配置SO
/// 包含悬挂臂几何/物理参数、防翻滚杆、地形对齐、车身稳定参数
/// 由AI_SuspensionManager运行时读取，实现数据驱动调参
/// </summary>
namespace NGameData.NAIConfigs
{
    [CreateAssetMenu(fileName = "AISuspensionConfig", menuName = "AI/AISuspensionConfig", order = 1)]
    public class AISuspensionConfig : ScriptableObject
    {
        [Header("悬挂臂几何参数")]
        [Tooltip("悬挂臂静止长度(m)")]
        public float restLength = 0.8f;
        [Tooltip("弹簧刚度(N/m)")]
        public float springStrength = 15000f;
        [Tooltip("阻尼系数(N·s/m)")]
        public float damperStrength = 2000f;
        [Tooltip("最大压缩量(m)")]
        public float maxCompression = 0.4f;
        [Tooltip("最大伸展量(m)")]
        public float maxExtension = 0.3f;
        [Tooltip("负重轮半径(m)，用于SphereCast和视觉")]
        public float wheelRadius = 0.4f;
        [Tooltip("地面探测图层遮罩")]
        public LayerMask groundMask = ~0;

        [Header("悬挂臂视觉参数")]
        [Tooltip("压缩角度乘数(deg/m)")]
        public float angleMultiplier = 40f;
        [Tooltip("伸展角度乘数(deg/m)")]
        public float extensionAngleMultiplier = 60f;
        [Tooltip("最小视觉角度(deg)")]
        public float minAngle = -55f;
        [Tooltip("最大视觉角度(deg)")]
        public float maxAngle = 22f;
        [Tooltip("视觉角度平滑时间(s)")]
        public float rotationSmoothTime = 0.06f;
        [Tooltip("视觉角度最大速度(deg/s)")]
        public float rotationMaxSpeed = 400f;

        [Header("防翻滚杆")]
        [Tooltip("防翻滚杆力矩系数")]
        public float antiRollForce = 5000f;
        [Tooltip("启用防翻滚杆")]
        public bool enableAntiRoll = true;

        [Header("地形法线对齐")]
        [Tooltip("法线对齐力矩强度")]
        public float normalAlignStrength = 3000f;
        [Tooltip("法线对齐阻尼系数")]
        public float normalAlignDamping = 500f;
        [Tooltip("最大对齐角度(deg)")]
        public float maxAlignAngle = 15f;

        [Header("车身稳定")]
        [Tooltip("俯仰稳定力矩系数")]
        public float pitchStabilization = 2000f;
        [Tooltip("翻滚稳定力矩系数")]
        public float rollStabilization = 3000f;
    }
}
