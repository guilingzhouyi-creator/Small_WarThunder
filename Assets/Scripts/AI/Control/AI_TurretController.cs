using UnityEngine;
using NGameData.NAIData;
using NGameData.NAIConfigs;

/// <summary>
/// AI炮塔控制系统
/// 从黑板读取目标Transform，计算瞄准角度并旋转炮塔
/// 支持水平(Yaw) + 垂直(Pitch) 双轴旋转
/// </summary>
namespace NAI
{
    public class AI_TurretController : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private AI_Controller _aiController;
        [SerializeField] private Transform _turretHorizontal; // 水平旋转根
        [SerializeField] private Transform _turretBarrel;     // 炮管/俯仰旋转

        [Header("参数")]
        [SerializeField] private float _rotationSpeed = 30f;
        [SerializeField] private float _pitchSpeed = 20f;
        [SerializeField] private float _minPitch = -10f;
        [SerializeField] private float _maxPitch = 30f;

        private AI_Blackboard _blackboard;
        private BehaviorConfig _behavior;

        private void Start()
        {
            if (_aiController == null) _aiController = GetComponent<AI_Controller>();
            _blackboard = _aiController?.Blackboard;
            _behavior = _aiController?.BehaviorConfig;

            if (_turretHorizontal == null)
                Debug.LogWarning($"{AIConstants.DebugTagTurret} turretHorizontal not assigned.");
        }

        private void Update()
        {
            if (_blackboard == null) return;

            string state = _blackboard.Get<string>(AIConstants.BbKeyCurrentState);
            if (state == AIConstants.StateDead || state == AIConstants.StateWatch || state == AIConstants.StateWatchBuffer) return;

            Transform target = _blackboard.Get<Transform>(AIConstants.BbKeyTargetEnemy);
            if (target == null)
            {
                // 无目标时回正
                ResetToForward();
                return;
            }

            RotateTurret(target);
        }

        /// <summary>
        /// 双轴旋转炮塔瞄准目标
        /// </summary>
        private void RotateTurret(Transform target)
        {
            Vector3 dirToTarget = (target.position - transform.position).normalized;

            // 水平旋转 (Yaw)
            if (_turretHorizontal != null)
            {
                Quaternion targetRot = Quaternion.LookRotation(
                    new Vector3(dirToTarget.x, 0f, dirToTarget.z).normalized
                );
                _turretHorizontal.rotation = Quaternion.RotateTowards(
                    _turretHorizontal.rotation, targetRot, _rotationSpeed * Time.deltaTime
                );
            }

            // 垂直旋转 (Pitch)
            if (_turretBarrel != null && _turretHorizontal != null)
            {
                float pitchAngle = Mathf.Asin(Mathf.Clamp(dirToTarget.y, -1f, 1f)) * Mathf.Rad2Deg;
                pitchAngle = Mathf.Clamp(pitchAngle, _minPitch, _maxPitch);

                Quaternion barrelTarget = _turretHorizontal.rotation * Quaternion.Euler(pitchAngle, 0f, 0f);
                _turretBarrel.rotation = Quaternion.RotateTowards(
                    _turretBarrel.rotation, barrelTarget, _pitchSpeed * Time.deltaTime
                );
            }
        }

        /// <summary>
        /// 无目标时炮塔回正
        /// </summary>
        private void ResetToForward()
        {
            if (_turretHorizontal != null)
            {
                Quaternion forwardRot = Quaternion.LookRotation(transform.forward);
                _turretHorizontal.rotation = Quaternion.RotateTowards(
                    _turretHorizontal.rotation, forwardRot, _rotationSpeed * 0.5f * Time.deltaTime
                );
            }
            if (_turretBarrel != null && _turretHorizontal != null)
            {
                Quaternion barrelNeutral = _turretHorizontal.rotation * Quaternion.identity;
                _turretBarrel.localRotation = Quaternion.RotateTowards(
                    _turretBarrel.localRotation, Quaternion.identity, _pitchSpeed * 0.5f * Time.deltaTime
                );
            }
        }
    }
}
