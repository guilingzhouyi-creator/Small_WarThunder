using UnityEngine;
using NGameData.NAIData;
using NGameData.NAIConfigs;

/// <summary>
/// AI移动行为状态机（纯状态决策，物理交由 AI_MotionDriver）
/// 状态：Chase / Strafe / Retreat / Patrol / Idle
/// 借鉴自 TankMoveController 的行为逻辑，但禁止引用 Tank 文件夹
/// </summary>
namespace NAI
{
    [RequireComponent(typeof(AI_MotionDriver))]
    public class AI_MoveController : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private AI_Controller _aiController;
        [SerializeField] private AI_MotionDriver _motionDriver;

        [Header("行为参数")]
        [SerializeField] private float _chaseApproachDistance = 5f;
        [SerializeField] private float _strafeIdealDistanceFactor = 0.7f;
        [SerializeField] private float _retreatDistance = 10f;
        [SerializeField] private float _patrolRandomInterval = 3f;
        [SerializeField] private float _patrolDirectionBias = 0.3f;

        private AI_Blackboard _blackboard;
        private EnemyConfig _config;
        private float _patrolTimer;
        private Vector3 _patrolMoveDir;
        private Vector3 _previousTargetPos;
        private float _targetPosChangeThreshold = 2f;

        private void Start()
        {
            if (_aiController == null) _aiController = GetComponent<AI_Controller>();
            if (_motionDriver == null) _motionDriver = GetComponent<AI_MotionDriver>();
            _blackboard = _aiController?.Blackboard;
            _config = _aiController?.EnemyConfig;
            _patrolMoveDir = Random.onUnitSphere;
            _patrolMoveDir.y = 0f;
            _patrolMoveDir.Normalize();
        }

        private void FixedUpdate()
        {
            if (_blackboard == null || _motionDriver == null) return;

            string state = _blackboard.Get<string>(AIConstants.BbKeyCurrentState);
            if (state == AIConstants.StateDead)
            {
                _motionDriver.SetMoveInput(0f, 0f);
                return;
            }

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
                    IdleStop();
                    break;
            }
        }

        /// <summary>
        /// Chase: 朝目标前进，到达攻击距离附近时减速停车
        /// </summary>
        private void ChaseTarget(Transform target)
        {
            if (target == null) { IdleStop(); return; }

            Vector3 dir = (target.position - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, target.position);
            float attackRange = _config?.attackRange ?? 50f;

            if (dist > attackRange + _chaseApproachDistance)
            {
                float forward = 1f;
                float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
                float turn = Mathf.Clamp(angle / 45f, -1f, 1f);
                _motionDriver.SetMoveInput(forward, turn);
            }
            else if (dist < attackRange * 0.5f)
            {
                // 太近时后退
                float forward = -0.3f;
                float angle = Vector3.SignedAngle(transform.forward, -dir, Vector3.up);
                float turn = Mathf.Clamp(angle / 60f, -1f, 1f);
                _motionDriver.SetMoveInput(forward, turn);
            }
            else
            {
                IdleStop();
            }
        }

        /// <summary>
        /// Strafe: 围绕目标横向移动，保持理想交战距离
        /// </summary>
        private void StrafeTarget(Transform target)
        {
            if (target == null) { IdleStop(); return; }

            Vector3 dir = (target.position - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, target.position);
            float attackRange = _config?.attackRange ?? 50f;
            float idealDist = attackRange * _strafeIdealDistanceFactor;

            float forward = 0f;
            if (dist > idealDist + 3f)
                forward = 0.5f;
            else if (dist < idealDist - 3f)
                forward = -0.3f;

            // 横向环绕
            Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized;
            float sign = Mathf.Sin(Time.time * 0.6f) > 0 ? 1f : -1f;
            float turn = Mathf.Clamp(sign * 0.6f, -1f, 1f);

            _motionDriver.SetMoveInput(forward, turn);
        }

        /// <summary>
        /// Retreat: 背离目标撤退
        /// </summary>
        private void RetreatFromTarget(Transform target)
        {
            if (target == null) { IdleStop(); return; }

            Vector3 away = (transform.position - target.position).normalized;
            float angle = Vector3.SignedAngle(transform.forward, away, Vector3.up);
            float forward = Mathf.Max(0.3f, 1f - Mathf.Abs(angle) / 90f);
            float turn = Mathf.Clamp(angle / 45f, -1f, 1f);

            _motionDriver.SetMoveInput(forward, turn);
        }

        /// <summary>
        /// Patrol: 随机方向漫步，周期性改变方向
        /// </summary>
        private void Patrol()
        {
            _patrolTimer -= Time.fixedDeltaTime;
            if (_patrolTimer <= 0f)
            {
                _patrolMoveDir = Random.insideUnitSphere;
                _patrolMoveDir.y = 0f;
                _patrolMoveDir.Normalize();
                _patrolTimer = _patrolRandomInterval * Random.Range(0.8f, 1.2f);
            }

            float angle = Vector3.SignedAngle(transform.forward, _patrolMoveDir, Vector3.up);
            float forward = Mathf.Max(0f, 1f - Mathf.Abs(angle) / 60f) * _patrolDirectionBias;
            float turn = Mathf.Clamp(angle / 45f, -1f, 1f);

            _motionDriver.SetMoveInput(forward, turn);
        }

        /// <summary>
        /// Idle: 完全停止（MotionDriver 内部自行刹车+摩擦）
        /// </summary>
        private void IdleStop()
        {
            _motionDriver.SetMoveInput(0f, 0f);
        }
    }
}
