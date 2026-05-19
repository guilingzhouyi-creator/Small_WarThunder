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
        [SerializeField] private Transform _turretBarrel;     // 炮管高低机系统

        [Header("参数")]
        [SerializeField] private float _rotationSpeed = 30f;
        [SerializeField] private float _pitchSpeed = 20f;
        [SerializeField] private float _minPitch = -10f;
        [SerializeField] private float _maxPitch = 30f;
        [SerializeField] private float _debugLogInterval = 0.75f;
        [SerializeField] private bool _enablePitchDebug = true;

        private AI_Blackboard _blackboard;
        private BehaviorConfig _behavior;
        private Quaternion _turretBindLocalRotation = Quaternion.identity;
        private Quaternion _barrelBindLocalRotation = Quaternion.identity;
        private bool _loggedPitchRangeInterpretation;
        private float _lastPitchDebugTime;

        private void Start()
        {
            if (_aiController == null) _aiController = GetComponent<AI_Controller>();
            _blackboard = _aiController?.Blackboard;
            _behavior = _aiController?.BehaviorConfig;
            CacheBindRotations();

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
            Vector3 targetPoint = AI_TargetingUtility.ResolveTargetPoint(target);

            // 水平旋转 (Yaw)
            if (_turretHorizontal != null)
            {
                Transform turretParent = _turretHorizontal.parent;
                Vector3 turretLocalDirection = turretParent != null
                    ? turretParent.InverseTransformDirection(targetPoint - _turretHorizontal.position)
                    : transform.InverseTransformDirection(targetPoint - _turretHorizontal.position);
                turretLocalDirection.y = 0f;

                if (turretLocalDirection.sqrMagnitude > 0.0001f)
                {
                    float targetYaw = GetTargetTurretYaw(turretLocalDirection.normalized);
                    Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, 0f) * _turretBindLocalRotation;
                    _turretHorizontal.localRotation = Quaternion.RotateTowards(
                        _turretHorizontal.localRotation,
                        targetRotation,
                        _rotationSpeed * Time.deltaTime);
                }
            }

            // 垂直旋转 (Pitch)
            if (_turretBarrel != null && _turretHorizontal != null)
            {
                ResolvePitchLimits(out float minPitch, out float maxPitch);
                Vector3 targetInTurretSpace = _turretHorizontal.InverseTransformPoint(targetPoint);
                float horizontalDistance = new Vector2(targetInTurretSpace.x, targetInTurretSpace.z).magnitude;
                float rawPitch = -Mathf.Atan2(targetInTurretSpace.y, Mathf.Max(0.0001f, horizontalDistance)) * Mathf.Rad2Deg;
                float targetPitch = Mathf.Clamp(rawPitch, minPitch, maxPitch);
                float currentPitch = GetCurrentBarrelPitch();

                Quaternion barrelTarget = Quaternion.Euler(targetPitch, 0f, 0f) * _barrelBindLocalRotation;
                _turretBarrel.localRotation = Quaternion.RotateTowards(
                    _turretBarrel.localRotation,
                    barrelTarget,
                    _pitchSpeed * Time.deltaTime);

                if (_enablePitchDebug && Time.time - _lastPitchDebugTime >= Mathf.Max(0.1f, _debugLogInterval))
                {
                    _lastPitchDebugTime = Time.time;
                    Debug.Log(
                        $"{AIConstants.DebugTagTurret} Pitch debug: currentPitch={currentPitch:F2}, rawPitch={rawPitch:F2}, targetPitch={targetPitch:F2}, " +
                        $"minPitch={minPitch:F2}, maxPitch={maxPitch:F2}, targetInTurretSpace={targetInTurretSpace}, horizontalDistance={horizontalDistance:F2}, turretBarrelLocalEuler={_turretBarrel.localEulerAngles}");
                }
            }
        }

        /// <summary>
        /// 无目标时炮塔回正
        /// </summary>
        private void ResetToForward()
        {
            if (_turretHorizontal != null)
            {
                _turretHorizontal.localRotation = Quaternion.RotateTowards(
                    _turretHorizontal.localRotation,
                    _turretBindLocalRotation,
                    _rotationSpeed * 0.5f * Time.deltaTime);
            }
            if (_turretBarrel != null && _turretHorizontal != null)
            {
                _turretBarrel.localRotation = Quaternion.RotateTowards(
                    _turretBarrel.localRotation,
                    _barrelBindLocalRotation,
                    _pitchSpeed * 0.5f * Time.deltaTime
                );
            }
        }

        private void CacheBindRotations()
        {
            _turretBindLocalRotation = _turretHorizontal != null ? _turretHorizontal.localRotation : Quaternion.identity;
            _barrelBindLocalRotation = _turretBarrel != null ? _turretBarrel.localRotation : Quaternion.identity;
        }

        private float GetTargetTurretYaw(Vector3 localTargetDirection)
        {
            Vector3 bindForward = _turretBindLocalRotation * Vector3.forward;
            bindForward.y = 0f;
            if (bindForward.sqrMagnitude < 0.0001f)
            {
                bindForward = Vector3.forward;
            }

            return Vector3.SignedAngle(bindForward.normalized, localTargetDirection.normalized, Vector3.up);
        }

        private float GetCurrentBarrelPitch()
        {
            Quaternion relativeRotation = _turretBarrel.localRotation * Quaternion.Inverse(_barrelBindLocalRotation);
            Vector3 euler = relativeRotation.eulerAngles;
            float pitch = euler.x;
            if (pitch > 180f)
            {
                pitch -= 360f;
            }

            return pitch;
        }

        private void ResolvePitchLimits(out float minPitch, out float maxPitch)
        {
            if (_minPitch >= 0f && _maxPitch >= 0f)
            {
                minPitch = -Mathf.Abs(_minPitch);
                maxPitch = Mathf.Abs(_maxPitch);

                if (!_loggedPitchRangeInterpretation)
                {
                    _loggedPitchRangeInterpretation = true;
                    Debug.Log($"{AIConstants.DebugTagTurret} Pitch range interpreted as signed magnitudes: configuredMin={_minPitch:F2}, configuredMax={_maxPitch:F2}, resolvedMin={minPitch:F2}, resolvedMax={maxPitch:F2}");
                }

                return;
            }

            minPitch = Mathf.Min(_minPitch, _maxPitch);
            maxPitch = Mathf.Max(_minPitch, _maxPitch);
        }
    }
}
