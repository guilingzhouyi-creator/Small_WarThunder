using UnityEngine;
using NGameData.NAIData;
using NGameData.NAIConfigs;

/// <summary>
/// AI移动控制——简化的NavMesh-free移动
/// 读取黑板目标位置，计算驱动力并施加到Rigidbody
/// 保持与TankMoveController解耦
/// </summary>
namespace NAI
{
    public class AI_MoveController : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private AI_Controller _aiController;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private float _moveForce = 5000f;
        [SerializeField] private float _maxSpeed = 15f;

        private AI_Blackboard _blackboard;
        private EnemyConfig _config;

        private void Start()
        {
            if (_aiController == null) _aiController = GetComponent<AI_Controller>();
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();

            _blackboard = _aiController?.Blackboard;
            _config = _aiController?.EnemyConfig;
        }

        private void FixedUpdate()
        {
            if (_blackboard == null || _rigidbody == null) return;

            string state = _blackboard.Get<string>(AIConstants.BbKeyCurrentState);
            if (state == AIConstants.StateDead) return;

            Transform target = _blackboard.Get<Transform>(AIConstants.BbKeyTargetEnemy);

            switch (state)
            {
                case AIConstants.StateLockBuffer:
                    ChaseTarget(target);
                    break;
                case AIConstants.StateRandomAttack:
                    StrafeTarget(target);
                    break;
                case AIConstants.StateSpecial:
                    RetreatFromTarget(target);
                    break;
                case AIConstants.StateSuspicious:
                    Patrol();
                    break;
                case AIConstants.StateWatch:
                case AIConstants.StateWatchBuffer:
                default:
                    IdleBrake();
                    break;
            }
        }

        private void ChaseTarget(Transform target)
        {
            if (target == null) { IdleBrake(); return; }
            Vector3 dir = (target.position - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, target.position);

            if (dist > (_config?.attackRange ?? 50f))
            {
                _rigidbody.AddForce(dir * _moveForce, ForceMode.Force);
                LimitSpeed();
            }
            else
            {
                IdleBrake();
            }
        }

        private void StrafeTarget(Transform target)
        {
            if (target == null) { IdleBrake(); return; }
            Vector3 dir = (target.position - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, target.position);
            float idealDist = (_config?.attackRange ?? 50f) * 0.7f;

            if (dist > idealDist + 5f)
                _rigidbody.AddForce(dir * _moveForce * 0.6f, ForceMode.Force);
            else if (dist < idealDist - 5f)
                _rigidbody.AddForce(-dir * _moveForce * 0.6f, ForceMode.Force);
            else
                ApplyRandomStrafe(dir);

            LimitSpeed();
        }

        private void RetreatFromTarget(Transform target)
        {
            if (target == null) { IdleBrake(); return; }
            Vector3 away = (transform.position - target.position).normalized;
            _rigidbody.AddForce(away * _moveForce * 0.8f, ForceMode.Force);
            LimitSpeed();
        }

        private void Patrol()
        {
            // 简化巡逻：随机小幅移动
            Vector3 randomDir = Random.insideUnitSphere;
            randomDir.y = 0f;
            _rigidbody.AddForce(randomDir.normalized * _moveForce * 0.3f, ForceMode.Force);
            LimitSpeed();
        }

        private void IdleBrake()
        {
            if (_rigidbody.linearVelocity.magnitude < 0.3f)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                return;
            }
            _rigidbody.AddForce(-_rigidbody.linearVelocity.normalized * _moveForce * 0.5f, ForceMode.Force);
        }

        private void ApplyRandomStrafe(Vector3 dir)
        {
            Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized;
            float sign = Mathf.Sin(Time.time * 0.5f) > 0 ? 1f : -1f;
            _rigidbody.AddForce(perp * sign * _moveForce * 0.4f, ForceMode.Force);
        }

        private void LimitSpeed()
        {
            if (_rigidbody.linearVelocity.magnitude > _maxSpeed)
                _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * _maxSpeed;
        }
    }
}
