using UnityEngine;

/// <summary>
/// AI 坦克负重轮视觉驱动，基于速度和悬挂角度驱动轮子自旋与悬挂摆动。
/// 借鉴 TankSuspensionArm 的视觉部分，不引用 Tank 文件夹。
/// 挂载在每个负重轮的 Transform 上（或统一管理）。
/// </summary>
namespace NAI
{
    public class AI_WheelVisualDriver : MonoBehaviour
    {
        [Header("轮子 Transform（若无则用自身）")]
        [SerializeField] private Transform _wheelTransform;

        [Header("旋转轴")]
        [SerializeField] private Vector3 _spinAxis = Vector3.right;

        [Header("半径 (m) — 用于计算转速")]
        [SerializeField] private float _wheelRadius = 0.3f;

        [Header("数据源")]
        [SerializeField] private AI_MotionDriver _motionDriver;
        [SerializeField] private AI_SuspensionArm _suspensionArm;

        private Quaternion _restLocalRotation;
        private float _currentSpinAngle;

        private void Awake()
        {
            if (_wheelTransform == null) _wheelTransform = transform;
            if (_motionDriver == null) _motionDriver = GetComponentInParent<AI_MotionDriver>();
            if (_suspensionArm == null) _suspensionArm = GetComponentInParent<AI_SuspensionArm>();

            _restLocalRotation = _wheelTransform.localRotation;

            Debug.Log($"[AI_WheelVisualDriver] {name} 初始化");
        }

        private void Update()
        {
            if (_wheelTransform == null) return;

            // 1. 滚转（自旋）：基于实际车体速度
            _currentSpinAngle += CalculateSpinAngle();

            Quaternion spinRotation = Quaternion.AngleAxis(_currentSpinAngle, _spinAxis.normalized);
            _wheelTransform.localRotation = _restLocalRotation * spinRotation;
        }

        private float CalculateSpinAngle()
        {
            if (_motionDriver == null) return 0f;

            Rigidbody rb = _motionDriver.GetComponent<Rigidbody>();
            if (rb == null) return _motionDriver.ForwardInput * 5f * Time.deltaTime;

            Vector3 localVel = _motionDriver.transform.InverseTransformDirection(rb.linearVelocity);
            float speed = localVel.z; // m/s

            // 线速度 = 角速度 × 半径 → 角速度 (rad/s) = 线速度 / 半径
            float angularSpeed = speed / Mathf.Max(_wheelRadius, 0.01f);
            return angularSpeed * Time.deltaTime * Mathf.Rad2Deg;
        }
    }
}
