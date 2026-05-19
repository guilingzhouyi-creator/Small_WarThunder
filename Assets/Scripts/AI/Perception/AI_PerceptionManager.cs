using UnityEngine;
using NGameData.NAIData;
using NGameData.NAIConfigs;

/// <summary>
/// AI感知管理器——分片轮询方案
/// 每帧只检测 PerceptionSliceCount 个切片方向，降低Physics.OverlapSphereNonAlloc开销
/// 将检测到的敌人写入黑板 Key: TargetEnemy
/// </summary>
namespace NAI
{
    public class AI_PerceptionManager : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private AI_Controller _aiController;
        [SerializeField] private Transform _eyePoint;

        [Header("扫描调度")]
        [SerializeField, Min(0.05f)] private float _scanInterval = 0.12f;

        private AI_Blackboard _blackboard;
        private EnemyConfig _config;
        private Collider[] _overlapResults;
        private int _currentSliceIndex;
        private LayerMask _layerMask;
        private float _nextScanTime;

        private void Awake()
        {
            _overlapResults = new Collider[AIConstants.MaxPerceptionResults];
            _layerMask = Physics.DefaultRaycastLayers;
            if (_layerMask.value == 0) _layerMask = Physics.DefaultRaycastLayers;
        }

        private void Start()
        {
            if (_aiController == null) _aiController = GetComponent<AI_Controller>();
            if (_eyePoint == null) _eyePoint = transform;

            _blackboard = _aiController?.Blackboard;
            _config = _aiController?.EnemyConfig;
            _nextScanTime = Time.time + ComputeInitialScanOffset();
        }

        private void Update()
        {
            if (Time.time < _nextScanTime)
            {
                return;
            }

            PerformSlicedScan();
            ScheduleNextScan();
        }

        /// <summary>
        /// 分片感知扫描
        /// 将360度视野分成多个切片，每帧检测一个切片
        /// </summary>
        private void PerformSlicedScan()
        {
            if (_config == null || _blackboard == null) return;

            float range = _config.maxDetectionRange * AIConstants.DetectionRangeFactor;
            int sliceCount = AIConstants.PerceptionSliceCount;
            if (sliceCount <= 0) sliceCount = 4;

            // 计算当前切片的扇形角度
            float sliceAngleSize = 360f / sliceCount;
            float startAngle = _currentSliceIndex * sliceAngleSize;
            Vector3 center = _eyePoint.position;

            int hitCount = Physics.OverlapSphereNonAlloc(center, range, _overlapResults, _layerMask);
            Transform bestTarget = null;
            float bestAngle = float.MaxValue;

            Vector3 forward = _eyePoint.forward;
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _overlapResults[i];
                if (col == null) continue;

                Transform candidateTarget = ResolvePlayerTarget(col);
                if (candidateTarget == null) continue;
                if (candidateTarget == transform || candidateTarget == transform.root) continue;

                Vector3 dirToTarget = (candidateTarget.position - center).normalized;
                float angleToTarget = Vector3.Angle(forward, dirToTarget);

                // 判断是否在当前切片角度范围内
                float relativeYaw = Mathf.Atan2(dirToTarget.x, dirToTarget.z) * Mathf.Rad2Deg;
                if (relativeYaw < 0f) relativeYaw += 360f;

                bool inSlice = relativeYaw >= startAngle && relativeYaw < startAngle + sliceAngleSize;
                if (!inSlice) continue;

                if (angleToTarget < bestAngle)
                {
                    bestAngle = angleToTarget;
                    bestTarget = candidateTarget;
                }
            }

            if (bestTarget != null)
            {
                _blackboard.Set(AIConstants.BbKeyTargetEnemy, bestTarget);
                Debug.Log($"{AIConstants.DebugTagPerception} Detected target: {bestTarget.name} at slice {_currentSliceIndex}");
            }

            // 轮询下一个切片
            _currentSliceIndex = (_currentSliceIndex + 1) % sliceCount;
        }

        private static Transform ResolvePlayerTarget(Collider collider)
        {
            if (collider == null)
            {
                return null;
            }

            if (collider.TryGetComponent<PlayerMarker>(out PlayerMarker directMarker))
            {
                return directMarker.transform;
            }

            PlayerMarker markerInParents = collider.GetComponentInParent<PlayerMarker>();
            if (markerInParents != null)
            {
                return markerInParents.transform;
            }

            Transform root = collider.transform.root;
            if (root != null && root.TryGetComponent<PlayerMarker>(out PlayerMarker rootMarker))
            {
                return rootMarker.transform;
            }

            return null;
        }

        private float ComputeInitialScanOffset()
        {
            float interval = Mathf.Max(0.05f, _scanInterval);
            int stableHash = Mathf.Abs(GetInstanceID());
            float normalizedOffset = (stableHash % 997) / 997f;
            return interval * normalizedOffset;
        }

        private void ScheduleNextScan()
        {
            float interval = Mathf.Max(0.05f, _scanInterval);
            if (_nextScanTime <= 0f)
            {
                _nextScanTime = Time.time + interval;
                return;
            }

            _nextScanTime += interval;
            if (_nextScanTime < Time.time)
            {
                _nextScanTime = Time.time + interval;
            }
        }
    }
}
