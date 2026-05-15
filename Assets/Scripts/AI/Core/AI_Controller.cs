using UnityEngine;
using NGameData.NAIData;
using NGameData.NAIConfigs;

/// <summary>
/// AI两层七态FSM控制器
/// 使用新的七态命名：Watch/WatchBuffer/Suspicious/LockBuffer/RandomAttack/Special/Dead
/// 依赖AI_Blackboard进行统一数据交互
/// </summary>
namespace NAI
{
    public class AI_Controller : MonoBehaviour
    {
        [Header("配置引用")]
        [SerializeField] private AICentralConfig _centralConfig;
        [SerializeField] private EnemyConfig _enemyConfig;
        [SerializeField] private BehaviorConfig _behaviorConfig;

        [Header("运行时")]
        [SerializeField] private AI_Blackboard _blackboard;

        private string _currentState;
        private string _previousState;
        private float _stateEnterTime;

        public AI_Blackboard Blackboard => _blackboard;
        public string CurrentState => _currentState;
        public EnemyConfig EnemyConfig => _enemyConfig;
        public BehaviorConfig BehaviorConfig => _behaviorConfig;

        private void Awake()
        {
            _blackboard = new AI_Blackboard();
            ApplyConfig(_enemyConfig);
            TransitionToState(AIConstants.StateWatch);
        }

        /// <summary>
        /// 通过ScriptableObject配置初始化黑板
        /// </summary>
        public void ApplyConfig(EnemyConfig config)
        {
            if (config == null)
            {
                Debug.LogWarning($"{AIConstants.DebugTagFSM} ApplyConfig: enemyConfig is null");
                return;
            }

            _enemyConfig = config;
            _blackboard.Set(AIConstants.BbKeyHealth, config.maxHealth);
            _blackboard.Set(AIConstants.BbKeyCurrentAwareness, 0f);

            float effectiveDetectionRange = config.maxDetectionRange * AIConstants.DetectionRangeFactor;
            Debug.Log($"{AIConstants.DebugTagFSM} ApplyConfig: maxHealth={config.maxHealth}, effectiveDetectionRange={effectiveDetectionRange}");
        }

        /// <summary>
        /// 七态状态机转换入口
        /// </summary>
        public void TransitionToState(string newState)
        {
            if (_currentState == newState) return;

            _previousState = _currentState;
            _currentState = newState;
            _stateEnterTime = Time.time;
            _blackboard.Set(AIConstants.BbKeyCurrentState, newState);

            OnStateEnter(newState);
            Debug.Log($"{AIConstants.DebugTagFSM} Transition: {_previousState} → {_currentState}");
        }

        private void OnStateEnter(string state)
        {
            switch (state)
            {
                case AIConstants.StateWatch:
                    break;
                case AIConstants.StateWatchBuffer:
                    break;
                case AIConstants.StateSuspicious:
                    break;
                case AIConstants.StateLockBuffer:
                    break;
                case AIConstants.StateRandomAttack:
                    break;
                case AIConstants.StateSpecial:
                    break;
                case AIConstants.StateDead:
                    break;
            }
        }

        /// <summary>
        /// 外部驱动状态评估，由行为树或Manager每帧调用
        /// </summary>
        public void EvaluateState()
        {
            float health = _blackboard.Get<float>(AIConstants.BbKeyHealth);
            Transform target = _blackboard.Get<Transform>(AIConstants.BbKeyTargetEnemy);
            float awareness = _blackboard.Get<float>(AIConstants.BbKeyCurrentAwareness);

            if (_currentState == AIConstants.StateDead) return;

            if (health <= 0f)
            {
                TransitionToState(AIConstants.StateDead);
                return;
            }

            if (target != null && awareness > 0.7f)
            {
                float dist = Vector3.Distance(transform.position, target.position);
                if (dist < (_enemyConfig?.attackRange ?? 50f))
                    TransitionToState(AIConstants.StateRandomAttack);
                else if (awareness > 0.9f)
                    TransitionToState(AIConstants.StateLockBuffer);
                else
                    TransitionToState(AIConstants.StateSuspicious);
            }
            else if (awareness > 0.3f)
            {
                TransitionToState(AIConstants.StateSuspicious);
            }
            else if (_currentState == AIConstants.StateWatch || _currentState == AIConstants.StateWatchBuffer)
            {
                // 保持在巡逻或空闲
            }
            else
            {
                TransitionToState(AIConstants.StateWatchBuffer);
            }
        }

        private void Update()
        {
            EvaluateState();
        }
    }
}
