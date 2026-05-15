using UnityEngine;
using NGameData.NAIData;
using NGameData.NAIConfigs;

/// <summary>
/// AI武器控制系统
/// 从黑板读取目标Transformer，计算弹道瞄准并触发射击
/// 调用AI_FireController实例进行开火（解耦自TankFireController）
/// 通过AIConstants获取行为参数
/// </summary>
namespace NAI
{
    public class AI_WeaponController : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private AI_Controller _aiController;
        [SerializeField] private Transform _turretBarrel;

        [Header("参数")]
        [SerializeField] private float _aimToleranceDeg = 2f;

        private AI_Blackboard _blackboard;
        private AI_FireController _fireController;
        private BehaviorConfig _behavior;

        private void Start()
        {
            if (_aiController == null) _aiController = GetComponent<AI_Controller>();
            _blackboard = _aiController?.Blackboard;
            _fireController = GetComponent<AI_FireController>();
            _behavior = _aiController?.BehaviorConfig;
        }

        private void Update()
        {
            if (_blackboard == null || _fireController == null) return;

            string state = _blackboard.Get<string>(AIConstants.BbKeyCurrentState);
            if (state != AIConstants.StateRandomAttack
                && state != AIConstants.StateLockBuffer
                && state != AIConstants.StateSuspicious) return;

            Transform target = _blackboard.Get<Transform>(AIConstants.BbKeyTargetEnemy);
            if (target == null) return;

            AimAndFire(target);
        }

        /// <summary>
        /// 瞄准并开火
        /// 当炮管与目标方向夹角小于容差时触发开火
        /// </summary>
        private void AimAndFire(Transform target)
        {
            if (_turretBarrel == null || target == null) return;

            Vector3 barrelForward = _turretBarrel.forward;
            Vector3 dirToTarget = (target.position - _turretBarrel.position).normalized;
            float angleError = Vector3.Angle(barrelForward, dirToTarget);

            if (angleError <= _aimToleranceDeg)
            {
                _fireController?.FireMainGun();
                Debug.Log($"{AIConstants.DebugTagWeapon} Firing! Angle error: {angleError:F2}°");
            }
        }
    }
}
