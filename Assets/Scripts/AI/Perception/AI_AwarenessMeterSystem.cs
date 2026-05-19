using UnityEngine;
using NGameData.NAIData;
using NGameData.NAIConfigs;

/// <summary>
/// AI警觉度量表系统
/// 根据距离、视线遮挡、开火事件等因素累积/衰减awareness值
/// 写入黑板 BBKey_AwarenessLevel
/// </summary>
namespace NAI
{
    public class AI_AwarenessMeterSystem : MonoBehaviour
    {
        private readonly RaycastHit[] _losHits = new RaycastHit[16];

        [Header("引用")]
        [SerializeField] private AI_Controller _aiController;
        [SerializeField] private Transform _eyePoint;

        [Header("参数")]
        [SerializeField] private float _accumulationPerSecond = 0.4f;
        [SerializeField] private float _decayPerSecond = 0.15f;
        [SerializeField] private float _maxAwareness = 1f;

        private AI_Blackboard _blackboard;
        private EnemyConfig _config;
        private LayerMask _occlusionMask;
        private float _lastAwarenessDebugTime;

        private void Start()
        {
            if (_aiController == null) _aiController = GetComponent<AI_Controller>();
            if (_eyePoint == null) _eyePoint = transform;

            _blackboard = _aiController?.Blackboard;
            _config = _aiController?.EnemyConfig;
            _occlusionMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("AI_Perception");
        }

        private void Update()
        {
            UpdateAwareness();
        }

        private void UpdateAwareness()
        {
            if (_config == null || _blackboard == null) return;

            float effectiveDetectionRange = _config.maxDetectionRange * AIConstants.DetectionRangeFactor;

            float current = _blackboard.Get<float>(AIConstants.BbKeyCurrentAwareness, 0f);
            Transform target = _blackboard.Get<Transform>(AIConstants.BbKeyTargetEnemy);
            Vector3 targetPoint = AI_TargetingUtility.ResolveTargetPoint(target);

            if (target != null && IsLineOfSightClear(target))
            {
                float dist = Vector3.Distance(_eyePoint.position, targetPoint);
                float normalizedDist = Mathf.Clamp01(1f - dist / Mathf.Max(1f, effectiveDetectionRange));

                // 近距离 + 无遮挡 = 快速累积
                float accumulation = _accumulationPerSecond * (0.5f + 0.5f * normalizedDist) * Time.deltaTime;
                current = Mathf.Min(_maxAwareness, current + accumulation);
            }
            else
            {
                // 无目标时缓慢衰减
                current = Mathf.Max(0f, current - _decayPerSecond * Time.deltaTime);
            }

            _blackboard.Set(AIConstants.BbKeyCurrentAwareness, current);

            if (Application.isPlaying && target != null && Time.time - _lastAwarenessDebugTime >= 1f)
            {
                _lastAwarenessDebugTime = Time.time;
                bool hasLos = IsLineOfSightClear(target);
                Debug.Log($"{AIConstants.DebugTagPerception} Awareness update: target={target.name}, awareness={current:F2}, hasLOS={hasLos}, eye={_eyePoint.position}, targetPoint={targetPoint}");
            }
        }

        /// <summary>
        /// 外部触发：被玩家开火时快速提升awareness
        /// </summary>
        public void OnIncomingFire(Vector3 fireOrigin)
        {
            if (_blackboard == null || _config == null) return;

            float effectiveDetectionRange = _config.maxDetectionRange * AIConstants.DetectionRangeFactor;

            float dist = Vector3.Distance(transform.position, fireOrigin);
            float impact = Mathf.Clamp01(1f - dist / Mathf.Max(1f, effectiveDetectionRange * 2f));
            float current = _blackboard.Get<float>(AIConstants.BbKeyCurrentAwareness, 0f);
            float newVal = Mathf.Min(_maxAwareness, current + 0.5f * impact);

            _blackboard.Set(AIConstants.BbKeyCurrentAwareness, newVal);
            _blackboard.Set(AIConstants.BbKeyLastKnownPosition, fireOrigin);

            Debug.Log($"{AIConstants.DebugTagPerception} Incoming fire! Awareness: {newVal:F2}");
        }

        /// <summary>
        /// 视线遮挡检测
        /// </summary>
        private bool IsLineOfSightClear(Transform target)
        {
            if (target == null) return false;

            Vector3 origin = _eyePoint.position;
            Vector3 targetPoint = AI_TargetingUtility.ResolveTargetPoint(target);
            Vector3 toTarget = targetPoint - origin;
            float dist = toTarget.magnitude;
            if (dist <= 0.001f)
            {
                return true;
            }

            Vector3 dir = toTarget / dist;
            int hitCount = Physics.RaycastNonAlloc(origin, dir, _losHits, dist, _occlusionMask, QueryTriggerInteraction.Ignore);
            if (hitCount == 0)
            {
                return true;
            }

            float nearestDistance = float.MaxValue;
            RaycastHit nearestHit = default;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _losHits[i];
                if (hit.collider == null)
                {
                    continue;
                }

                if (hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    nearestHit = hit;
                }
            }

            if (nearestHit.collider == null)
            {
                return true;
            }

            Transform hitTransform = nearestHit.collider.transform;
            return hitTransform == target || hitTransform.IsChildOf(target) || target.IsChildOf(hitTransform);
        }
    }
}
