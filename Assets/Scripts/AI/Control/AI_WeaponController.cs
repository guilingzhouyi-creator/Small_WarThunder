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
        private readonly RaycastHit[] _fireValidationHits = new RaycastHit[16];

        [Header("引用")]
        [SerializeField] private AI_Controller _aiController;
        [SerializeField] private Transform _turretBarrel;

        [Header("参数")]
        [SerializeField] private float _aimToleranceDeg = 2f;
        [SerializeField] private float _fireAssistToleranceDeg = 8f;
        [SerializeField] private float _decisionLogInterval = 0.75f;
        [SerializeField] private LayerMask _fireValidationMask = ~0;
        [SerializeField] private float _fireValidationDistancePadding = 8f;
        [SerializeField] private bool _drawFireValidationRay = true;
        [SerializeField] private bool _enableGeometryDebug = true;

        private AI_Blackboard _blackboard;
        private AI_FireController _fireController;
        private BehaviorConfig _behavior;
        private float _lastDecisionLogTime;

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

            Transform fireOrigin = _fireController != null && _fireController.FirePoint != null
                ? _fireController.FirePoint
                : _turretBarrel;

            Vector3 targetPoint = AI_TargetingUtility.ResolveTargetPoint(target);
            Vector3 dirToTarget = (targetPoint - fireOrigin.position).normalized;
            Vector3 barrelForward = fireOrigin.forward;
            float angleError = Vector3.Angle(barrelForward, dirToTarget);
            bool hasDirectShot = HasDirectShot(fireOrigin, dirToTarget, target, targetPoint, out string shotReason, out RaycastHit nearestHit);
            bool canFireByAim = angleError <= Mathf.Max(_aimToleranceDeg, _fireAssistToleranceDeg);

            if (_enableGeometryDebug && Time.time - _lastDecisionLogTime >= Mathf.Max(0.1f, _decisionLogInterval))
            {
                LogGeometryDebug(fireOrigin, targetPoint, dirToTarget, barrelForward, angleError, nearestHit, shotReason);
            }

            if (hasDirectShot && canFireByAim)
            {
                bool fireResult = _fireController.FireMainGun(dirToTarget);
                Debug.Log($"{AIConstants.DebugTagWeapon} Fire request: target={target.name}, angleError={angleError:F2}, tolerance={_aimToleranceDeg:F2}, assistTolerance={_fireAssistToleranceDeg:F2}, result={fireResult}, aimSource={fireOrigin.name}, rayCheck={shotReason}");
                return;
            }

            if (Time.time - _lastDecisionLogTime >= Mathf.Max(0.1f, _decisionLogInterval))
            {
                _lastDecisionLogTime = Time.time;
                Debug.Log($"{AIConstants.DebugTagWeapon} Aim hold: target={target.name}, angleError={angleError:F2}, tolerance={_aimToleranceDeg:F2}, assistTolerance={_fireAssistToleranceDeg:F2}, aimSource={fireOrigin.name}, rayCheck={shotReason}, barrelForward={barrelForward}, targetPoint={targetPoint}");
            }
        }

        private bool HasDirectShot(Transform fireOrigin, Vector3 direction, Transform target, Vector3 targetPoint, out string reason, out RaycastHit nearestHit)
        {
            reason = "no-valid-hit";
            nearestHit = default;
            if (fireOrigin == null || target == null)
            {
                reason = "missing-aim-or-target";
                return false;
            }

            Vector3 origin = fireOrigin.position;
            float targetDistance = Vector3.Distance(origin, targetPoint);
            float maxDistance = Mathf.Max(1f, targetDistance + Mathf.Max(0f, _fireValidationDistancePadding));

            int hitCount = Physics.RaycastNonAlloc(
                origin,
                direction,
                _fireValidationHits,
                maxDistance,
                _fireValidationMask,
                QueryTriggerInteraction.Ignore);

            Color rayColor = Color.yellow;
            Transform selfRoot = transform.root;
            Transform targetRoot = target.root != null ? target.root : target;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _fireValidationHits[i];
                if (hit.collider == null)
                {
                    continue;
                }

                Transform hitTransform = hit.collider.transform;
                if (selfRoot != null && (hitTransform == selfRoot || hitTransform.IsChildOf(selfRoot)))
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
                reason = "no-hit-after-self-filter";
                rayColor = Color.gray;
            }
            else
            {
                Transform hitTransform = nearestHit.collider.transform;
                bool hitTarget = hitTransform == targetRoot || hitTransform.IsChildOf(targetRoot) || targetRoot.IsChildOf(hitTransform);
                if (hitTarget)
                {
                    reason = $"target-hit:{hitTransform.name}@{nearestHit.distance:F2}";
                    rayColor = Color.green;
                }
                else
                {
                    reason = $"blocked-by:{hitTransform.name}@{nearestHit.distance:F2}";
                    rayColor = Color.red;
                }

                if (_drawFireValidationRay)
                {
                    Debug.DrawRay(origin, direction * nearestHit.distance, rayColor, Mathf.Max(0.05f, _decisionLogInterval));
                }

                return hitTarget;
            }

            if (_drawFireValidationRay)
            {
                Debug.DrawRay(origin, direction * maxDistance, rayColor, Mathf.Max(0.05f, _decisionLogInterval));
            }

            return false;
        }

        private void LogGeometryDebug(
            Transform fireOrigin,
            Vector3 targetPoint,
            Vector3 dirToTarget,
            Vector3 barrelForward,
            float angleError,
            RaycastHit nearestHit,
            string shotReason)
        {
            _lastDecisionLogTime = Time.time;

            Vector3 origin = fireOrigin.position;
            float targetDistance = Vector3.Distance(origin, targetPoint);
            Vector3 hitPoint = nearestHit.collider != null ? nearestHit.point : origin + dirToTarget * targetDistance;
            Vector3 dirToHit = (hitPoint - origin).sqrMagnitude > 0.0001f ? (hitPoint - origin).normalized : dirToTarget;
            float hitAngleError = Vector3.Angle(barrelForward, dirToHit);
            float targetVsHitAngle = Vector3.Angle(dirToTarget, dirToHit);

            Debug.Log(
                $"{AIConstants.DebugTagWeapon} Geometry debug: origin={origin}, targetPoint={targetPoint}, hitPoint={hitPoint}, " +
                $"targetDistance={targetDistance:F2}, hitDistance={(nearestHit.collider != null ? nearestHit.distance : targetDistance):F2}, " +
                $"barrelToTargetAngle={angleError:F2}, barrelToHitAngle={hitAngleError:F2}, targetVsHitAngle={targetVsHitAngle:F2}, rayCheck={shotReason}");
        }
    }
}
