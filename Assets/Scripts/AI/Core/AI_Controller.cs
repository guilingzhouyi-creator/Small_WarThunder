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

        [Header("运行时")]
        [SerializeField] private AI_Blackboard _blackboard;

        private string _currentState;
        private string _previousState;
        private float _stateEnterTime;
        private float _lastAwarenessLogTime;

        private EnemyConfig _resolvedEnemyConfig;
        private BehaviorConfig _resolvedBehaviorConfig;
        private AIMotionConfig _resolvedMotionConfig;
        private AISuspensionConfig _resolvedSuspensionConfig;

        public AI_Blackboard Blackboard => _blackboard;
        public string CurrentState => _currentState;
        public EnemyConfig EnemyConfig => _resolvedEnemyConfig;
        public BehaviorConfig BehaviorConfig => _resolvedBehaviorConfig;
        public AIMotionConfig ResolvedMotionConfig => _resolvedMotionConfig;
        public AISuspensionConfig ResolvedSuspensionConfig => _resolvedSuspensionConfig;

        private void Awake()
        {
            _blackboard = new AI_Blackboard();
            ResolveConfigs();
            ApplyConfig(_resolvedEnemyConfig);
            ApplyResolvedRuntimeConfigs();
            TransitionToState(AIConstants.StateWatch);
        }

        private void ResolveConfigs()
        {
            if (_centralConfig == null)
            {
                Debug.LogWarning($"{AIConstants.DebugTagAI} AI_Controller.ResolveConfigs: centralConfig is null on {gameObject.name}");
                return;
            }

            AIConfigMapping mapping = ResolveConfigMapping();

            _resolvedEnemyConfig = mapping != null && mapping.enemyConfig != null
                ? mapping.enemyConfig
                : _centralConfig.defaultEnemyConfig;

            _resolvedBehaviorConfig = mapping != null && mapping.behaviorConfig != null
                ? mapping.behaviorConfig
                : _centralConfig.defaultBehaviorConfig;

            _resolvedMotionConfig = mapping != null && mapping.motionConfig != null
                ? mapping.motionConfig
                : _centralConfig.defaultMotionConfig;

            _resolvedSuspensionConfig = mapping != null && mapping.suspensionConfig != null
                ? mapping.suspensionConfig
                : _centralConfig.defaultSuspensionConfig;

            if (_resolvedEnemyConfig == null)
            {
                Debug.LogWarning($"{AIConstants.DebugTagAI} AI_Controller.ResolveConfigs: no enemyConfig found for {gameObject.name}");
            }

            if (_resolvedBehaviorConfig == null)
            {
                Debug.LogWarning($"{AIConstants.DebugTagAI} AI_Controller.ResolveConfigs: no behaviorConfig found for {gameObject.name}");
            }

            if (_resolvedMotionConfig == null)
            {
                Debug.LogWarning($"{AIConstants.DebugTagAI} AI_Controller.ResolveConfigs: no motionConfig found for {gameObject.name}");
            }

            if (_resolvedSuspensionConfig == null)
            {
                Debug.LogWarning($"{AIConstants.DebugTagAI} AI_Controller.ResolveConfigs: no suspensionConfig found for {gameObject.name}");
            }
        }

        private AIConfigMapping ResolveConfigMapping()
        {
            if (_centralConfig == null || _centralConfig.configMappingList == null)
            {
                return null;
            }

            string normalizedName = NormalizePrefabName(gameObject.name);
            foreach (AIConfigMapping mapping in _centralConfig.configMappingList)
            {
                if (mapping == null || mapping.prefab == null)
                {
                    continue;
                }

                if (mapping.prefab == gameObject)
                {
                    return mapping;
                }

                if (NormalizePrefabName(mapping.prefab.name) == normalizedName)
                {
                    return mapping;
                }
            }

            return null;
        }

        private void ApplyResolvedRuntimeConfigs()
        {
            AI_MotionDriver motionDriver = GetComponent<AI_MotionDriver>();
            if (motionDriver != null)
            {
                motionDriver.ApplyConfig(_resolvedMotionConfig);
            }

            AI_TankSuspensionManager[] suspensionManagers = GetComponentsInChildren<AI_TankSuspensionManager>(true);
            foreach (AI_TankSuspensionManager suspensionManager in suspensionManagers)
            {
                suspensionManager.ApplyConfig(_resolvedSuspensionConfig);
            }
        }

        private static string NormalizePrefabName(string sourceName)
        {
            if (string.IsNullOrEmpty(sourceName))
            {
                return string.Empty;
            }

            const string cloneSuffix = "(Clone)";
            return sourceName.EndsWith(cloneSuffix)
                ? sourceName.Substring(0, sourceName.Length - cloneSuffix.Length).TrimEnd()
                : sourceName;
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

            _resolvedEnemyConfig = config;
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
            float suspiciousThreshold = _resolvedEnemyConfig != null
                ? Mathf.Clamp01(_resolvedEnemyConfig.awarenessThreshold)
                : 0.35f;
            float attackThreshold = Mathf.Clamp01(Mathf.Max(suspiciousThreshold + 0.25f, 0.7f));
            float lockThreshold = Mathf.Clamp01(Mathf.Max(attackThreshold + 0.15f, 0.9f));

            if (_currentState == AIConstants.StateDead) return;

            if (Application.isPlaying && target != null && Time.time - _lastAwarenessLogTime >= 1f)
            {
                _lastAwarenessLogTime = Time.time;
                Debug.Log($"{AIConstants.DebugTagFSM} Evaluate: state={_currentState}, awareness={awareness:F2}, suspiciousThreshold={suspiciousThreshold:F2}, attackThreshold={attackThreshold:F2}, lockThreshold={lockThreshold:F2}, target={target.name}");
            }

            if (health <= 0f)
            {
                TransitionToState(AIConstants.StateDead);
                return;
            }

            if (target != null && awareness > attackThreshold)
            {
                float dist = Vector3.Distance(transform.position, target.position);
                if (dist < (_resolvedEnemyConfig?.attackRange ?? 50f))
                    TransitionToState(AIConstants.StateRandomAttack);
                else if (awareness > lockThreshold)
                    TransitionToState(AIConstants.StateLockBuffer);
                else
                    TransitionToState(AIConstants.StateSuspicious);
            }
            else if (awareness > suspiciousThreshold)
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
