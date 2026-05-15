using UnityEngine;
using NGameData.NAIData;

/// <summary>
/// AI悬挂系统控制器
/// 模拟坦克悬挂弹簧/阻尼响应
/// 读写黑板中的悬挂偏移和速度，供其他模块（移动/物理）使用
/// </summary>
namespace NAI
{
    public class AI_SuspensionController : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private AI_Controller _aiController;
        [SerializeField] private Rigidbody _rigidbody;

        [Header("参数")]
        [SerializeField] private float _springStrength = 5000f;
        [SerializeField] private float _damperStrength = 500f;
        [SerializeField] private float _restLength = 0.5f;
        [SerializeField] private float _maxOffset = 0.3f;

        private AI_Blackboard _blackboard;
        private float _currentOffset;
        private float _currentVelocity;

        private void Start()
        {
            if (_aiController == null) _aiController = GetComponent<AI_Controller>();
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
            _blackboard = _aiController?.Blackboard;
        }

        private void FixedUpdate()
        {
            if (_blackboard == null || _rigidbody == null) return;
            SimulateSuspension();
        }

        /// <summary>
        /// 简化的弹簧-阻尼模型
        /// 基于Rigidbody垂直速度计算悬挂压缩/反弹
        /// </summary>
        private void SimulateSuspension()
        {
            // 悬挂偏移：基于垂直速度的简单位移模拟
            float targetOffset = Mathf.Clamp(-_rigidbody.linearVelocity.y * 0.05f, -_maxOffset, _maxOffset);
            _currentOffset = Mathf.SmoothDamp(_currentOffset, targetOffset, ref _currentVelocity, _damperStrength / _springStrength);

            _blackboard.Set(AIConstants.BbKeySuspensionOffset, _currentOffset);
            _blackboard.Set(AIConstants.BbKeySuspensionVelocity, _currentVelocity);
        }
    }
}
