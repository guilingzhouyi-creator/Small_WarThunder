using NGameData.NAIConfigs;
using UnityEngine;

/// <summary>
/// AI 坦克自有物理驱动层
/// 地面探测(SampleGroundContact)、各向异性摩擦、履带驱动力拆分(直行/枢转/弧形/制动)、
/// 速度硬限制、滚动阻力。借鉴自 TankMoveController.Motion，不引用 Tank 文件夹。
/// </summary>
namespace NAI
{
    [RequireComponent(typeof(Rigidbody))]
    public class AI_MotionDriver : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private AI_Controller _aiController;

        [Header("启动稳定")]
        [SerializeField, Min(0f)] private float _driveEnableDelay = 0.35f;

        [Header("运行时阻尼")]
        [SerializeField] private float _movingLinearDamping = 0.03f;
        [SerializeField] private float _idleLinearDamping = 1f;

        [Header("地面探测配置")]
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private float _groundProbeLength = 2f;
        [SerializeField] private float _groundProbeRadius = 0.3f;
        [SerializeField] private float _groundStickForce = 5000f;

        [Header("履带驱动配置")]
        [SerializeField] private float _maxMotorForce = 8000f;
        [SerializeField] private float _maxBrakeForce = 12000f;
        [SerializeField] private float _maxForwardSpeed = 25f;
        [SerializeField] private float _maxReverseSpeed = 8f;
        [SerializeField] private float _maxTurnSpeed = 60f;

        [Header("各向异性摩擦")]
        [SerializeField] private float _forwardFriction = 1.5f;
        [SerializeField] private float _sideFriction = 0.6f;
        [SerializeField] private float _frictionBlendSharpness = 3f;

        [Header("转向模型")]
        [SerializeField] private float _pivotTurnBlendSharpness = 5f;
        [SerializeField] private float _trackWidth = 3f;
        [SerializeField] private float _trackWheelBase = 4.5f;

        [Header("滚动阻力")]
        [SerializeField] private float _fallbackGroundFrictionCoefficient = 1f;
        [SerializeField] private float _rollingResistanceScale = 0.03f;
        [SerializeField] private float _maxRollingResistanceCoefficient = 0.05f;
        [SerializeField] private float _rollingResistanceCoeff = 0.05f;
        [SerializeField] private float _staticFrictionThreshold = 0.5f;

        [Header("载具 DNA")]
        [SerializeField] private float _cogHeight = 0.65f;
        [SerializeField] private float _lateralFrictionMultiplier = 3.5f;

        [Header("转向响应")]
        [SerializeField] private float _turnResponseTime = 0.15f;

        [Header("安全护栏")]
        [SerializeField] private float _maxLateralSpeedRatio = 0.3f;
        [SerializeField] private float _maxTotalSpeedMultiplier = 1.5f;
        [SerializeField] private float _maxAngularSpeedMultiplier = 1.5f;
        [SerializeField] private float _teleportDistanceThreshold = 25f;
        [SerializeField] private float _teleportGuardWarmupSeconds = 1f;

        [Header("斜坡补偿")]
        [SerializeField] private float _slopeCompensationFactor = 0.3f;
        [SerializeField] private float _slopeRecoveryForce = 2000f;

        private Rigidbody _rigidbody;
        private readonly RaycastHit[] _groundProbeHits = new RaycastHit[8];
        private RaycastHit _groundHit;
        private bool _isGrounded;
        private float _forwardInput;
        private float _turnInput;
        private Vector3 _groundNormal = Vector3.up;
        private Vector3 _groundPoint = Vector3.zero;
        private float _driveEnableTime;
        private float _groundFrictionCoefficient = 1f;
        private Vector3 _lastStablePosition;
        private Quaternion _lastStableRotation = Quaternion.identity;
        private bool _hasStablePose;
        private float _teleportGuardEnableTime;

        public bool IsGrounded => _isGrounded;
        public Vector3 GroundNormal => _groundNormal;
        public Vector3 GroundPoint => _groundPoint;
        public float ForwardInput => _forwardInput;
        public float TurnInput => _turnInput;

        public void ApplyConfig(AIMotionConfig config)
        {
            if (config == null)
            {
                return;
            }

            ApplyRigidbodySettings(config);
            _groundMask = config.groundMask;
            _groundProbeLength = config.groundProbeLength;
            _groundProbeRadius = config.groundProbeRadius;
            _groundStickForce = config.groundStickForce;
            _maxMotorForce = config.maxMotorForce;
            _maxBrakeForce = config.maxBrakeForce;
            _maxForwardSpeed = config.maxForwardSpeed;
            _maxReverseSpeed = config.maxReverseSpeed;
            _maxTurnSpeed = config.maxTurnSpeed;
            _trackWidth = config.trackWidth;
            _trackWheelBase = config.trackWheelBase;
            _movingLinearDamping = config.movingLinearDamping;
            _idleLinearDamping = config.idleLinearDamping;
            _forwardFriction = config.forwardFriction;
            _sideFriction = config.sideFriction;
            _frictionBlendSharpness = config.frictionBlendSharpness;
            _pivotTurnBlendSharpness = config.pivotTurnBlendSharpness;
            _fallbackGroundFrictionCoefficient = config.fallbackGroundFrictionCoefficient;
            _rollingResistanceScale = config.rollingResistanceScale;
            _maxRollingResistanceCoefficient = config.maxRollingResistanceCoefficient;
            _rollingResistanceCoeff = config.rollingResistanceCoeff;
            _staticFrictionThreshold = config.staticFrictionThreshold;
            _cogHeight = config.cogHeight;
            _lateralFrictionMultiplier = config.lateralFrictionMultiplier;
            _turnResponseTime = config.turnResponseTime;
            _maxLateralSpeedRatio = config.maxLateralSpeedRatio;
            _maxTotalSpeedMultiplier = config.maxTotalSpeedMultiplier;
            _maxAngularSpeedMultiplier = config.maxAngularSpeedMultiplier;
            _slopeCompensationFactor = config.slopeCompensationFactor;
            _slopeRecoveryForce = config.slopeRecoveryForce;
        }

        private void ApplyRigidbodySettings(AIMotionConfig config)
        {
            if (_rigidbody == null || config == null)
            {
                return;
            }

            _rigidbody.mass = Mathf.Max(1f, config.vehicleMass);
            _rigidbody.linearDamping = Mathf.Max(0f, config.linearDamping);
            _rigidbody.angularDamping = Mathf.Max(0f, config.angularDamping);
            _rigidbody.centerOfMass = new Vector3(0f, config.cogHeight, 0f);

            Vector3 inertia = _rigidbody.inertiaTensor;
            inertia.y *= 1.5f;
            inertia.z *= 1.2f;
            _rigidbody.inertiaTensor = inertia;
        }

        public float GetDifferentialTrackSpeed(bool isLeftTrack)
        {
            if (_rigidbody == null)
            {
                float baseSpeed = _forwardInput >= 0f
                    ? _forwardInput * _maxForwardSpeed
                    : _forwardInput * _maxReverseSpeed;
                float yawSpeed = _turnInput * Mathf.Deg2Rad * _maxTurnSpeed * (_trackWidth * 0.5f);
                return baseSpeed + (isLeftTrack ? -yawSpeed : yawSpeed);
            }

            Vector3 localVelocity = transform.InverseTransformDirection(_rigidbody.linearVelocity);
            Vector3 localAngularVelocity = transform.InverseTransformDirection(_rigidbody.angularVelocity);
            float yawLinearSpeed = localAngularVelocity.y * (_trackWidth * 0.5f);
            return localVelocity.z + (isLeftTrack ? -yawLinearSpeed : yawLinearSpeed);
        }

        public void SetMoveInput(float forward, float turn)
        {
            _forwardInput = Mathf.Clamp(forward, -1f, 1f);
            _turnInput = Mathf.Clamp(turn, -1f, 1f);
        }

        private void Awake()
        {
            if (_aiController == null)
            {
                _aiController = GetComponent<AI_Controller>();
            }

            _rigidbody = GetComponent<Rigidbody>();
            _driveEnableTime = Time.time + Mathf.Max(0f, _driveEnableDelay);
            _teleportGuardEnableTime = Time.time + Mathf.Max(0f, _teleportGuardWarmupSeconds);
            _lastStablePosition = transform.position;
            _lastStableRotation = transform.rotation;
            _hasStablePose = true;
            Debug.Log($"[AI_MotionDriver] {name} 初始化完毕");
        }

        private void Start()
        {
            if (_aiController != null)
            {
                ApplyConfig(_aiController.ResolvedMotionConfig);
            }
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null) return;

            RecoverFromAbnormalTeleportIfNeeded();
            SampleGroundContact();
            ApplyGravityAssist();
            SimulatePlayerStyleDrive();
            ApplySlopeCompensation();
            CacheStablePose();
        }

        private void SimulatePlayerStyleDrive()
        {
            Vector3 forwardAxis = GetGroundProjectedForward(_groundNormal);
            float currentForwardSpeed = Vector3.Dot(_rigidbody.linearVelocity, forwardAxis);

            ApplyPlayerStyleAnisotropicFriction(forwardAxis);
            ApplyPlayerStyleSteering();
            ApplyPlayerStyleLongitudinalDrive(forwardAxis, currentForwardSpeed);
            ApplyRollingResistance(forwardAxis, currentForwardSpeed);
            ApplySpeedHardLimits(forwardAxis);
            ApplyIdleSettling();
            ApplySafetyGuards(forwardAxis);
            ApplyRuntimeDamping();
        }

        private void SampleGroundContact()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;

            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                Mathf.Max(0.05f, _groundProbeRadius),
                Vector3.down,
                _groundProbeHits,
                Mathf.Max(0.1f, _groundProbeLength),
                _groundMask,
                QueryTriggerInteraction.Ignore);

            float bestDistance = float.MaxValue;
            RaycastHit bestHit = default;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _groundProbeHits[i];
                if (!IsValidGroundHit(hit))
                {
                    continue;
                }

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestHit = hit;
                }
            }

            if (bestDistance < float.MaxValue)
            {
                _groundHit = bestHit;
                _isGrounded = true;
                _groundNormal = bestHit.normal;
                _groundPoint = bestHit.point;
                _groundFrictionCoefficient = ResolveGroundFrictionCoefficient(bestHit);
            }
            else
            {
                _isGrounded = false;
                _groundNormal = Vector3.up;
                _groundPoint = origin + Vector3.down * _groundProbeLength;
                _groundFrictionCoefficient = GetDefaultGroundFrictionCoefficient();
            }
        }

        private void ApplyGravityAssist()
        {
            if (_isGrounded) return;
            _rigidbody.AddForce(Vector3.down * _groundStickForce, ForceMode.Force);
        }

        private void ApplyPlayerStyleLongitudinalDrive(Vector3 forwardAxis, float currentForwardSpeed)
        {
            if (!_isGrounded || !CanApplyDriveForces())
            {
                return;
            }

            bool hasTravelCommand = Mathf.Abs(_forwardInput) > 0.01f;
            bool hasSteeringCommand = Mathf.Abs(_turnInput) > 0.01f;

            if (!hasTravelCommand)
            {
                return;
            }

            float speedLimit = _forwardInput >= 0f ? _maxForwardSpeed : _maxReverseSpeed;
            float currentSpeedAlongForward = Mathf.Sign(_forwardInput) * currentForwardSpeed;
            if (currentSpeedAlongForward >= speedLimit)
            {
                return;
            }

            Vector3 contactPatch = transform.position - transform.up * _cogHeight;
            float driveForce = _maxMotorForce * Mathf.Abs(_forwardInput);
            _rigidbody.AddForceAtPosition(forwardAxis * driveForce * Mathf.Sign(_forwardInput), contactPatch, ForceMode.Force);
        }

        private void ApplyPlayerStyleAnisotropicFriction(Vector3 forwardAxis)
        {
            if (!_isGrounded) return;

            Vector3 localVelocity = transform.InverseTransformDirection(_rigidbody.linearVelocity);
            float forwardSpeed = localVelocity.z;
            float sideSpeed = localVelocity.x;

            float gravity = Mathf.Abs(Physics.gravity.y);
            float normalForce = _rigidbody.mass * gravity * Mathf.Cos(Vector3.Angle(_groundNormal, Vector3.up) * Mathf.Deg2Rad);
            float lateralMu = Mathf.Max(0.01f, _groundFrictionCoefficient * _lateralFrictionMultiplier);
            float maxLateralFriction = lateralMu * normalForce;
            float requiredForceToStop = (sideSpeed * _rigidbody.mass) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            float appliedLateralForce = -Mathf.Clamp(requiredForceToStop, -maxLateralFriction, maxLateralFriction);

            Vector3 contactPatch = transform.position - transform.up * _cogHeight;
            _rigidbody.AddForceAtPosition(transform.right * appliedLateralForce, contactPatch, ForceMode.Force);

            if (Mathf.Abs(_forwardInput) < 0.01f)
            {
                float longitudinalMu = Mathf.Max(0.01f, _groundFrictionCoefficient * 0.3f);
                float maxLongitudinalFriction = longitudinalMu * normalForce;
                float reqForwardToStop = (forwardSpeed * _rigidbody.mass) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
                float appliedForwardForce = -Mathf.Clamp(reqForwardToStop, -maxLongitudinalFriction, maxLongitudinalFriction);
                _rigidbody.AddForceAtPosition(forwardAxis * appliedForwardForce, contactPatch, ForceMode.Force);
            }
        }

        private void ApplySpeedHardLimits(Vector3 forwardAxis)
        {
            float forwardSpeed = Vector3.Dot(_rigidbody.linearVelocity, forwardAxis);
            float clampedForwardSpeed = Mathf.Clamp(forwardSpeed, -_maxReverseSpeed, _maxForwardSpeed);
            if (!Mathf.Approximately(clampedForwardSpeed, forwardSpeed))
            {
                _rigidbody.linearVelocity += forwardAxis * (clampedForwardSpeed - forwardSpeed);
            }
        }

        private void ApplySafetyGuards(Vector3 forwardAxis)
        {
            Vector3 velocity = _rigidbody.linearVelocity;
            Vector3 angularVelocity = _rigidbody.angularVelocity;

            if (!IsFiniteVector(velocity))
            {
                _rigidbody.linearVelocity = Vector3.zero;
                velocity = Vector3.zero;
            }

            if (!IsFiniteVector(angularVelocity))
            {
                _rigidbody.angularVelocity = Vector3.zero;
                angularVelocity = Vector3.zero;
            }

            Vector3 lateralVelocity = Vector3.ProjectOnPlane(velocity, forwardAxis);
            float maxLateralSpeed = Mathf.Max(1f, _maxForwardSpeed * Mathf.Max(0.05f, _maxLateralSpeedRatio));
            if (lateralVelocity.magnitude > maxLateralSpeed)
            {
                Vector3 clampedLateral = lateralVelocity.normalized * maxLateralSpeed;
                float forwardSpeed = Vector3.Dot(velocity, forwardAxis);
                _rigidbody.linearVelocity = clampedLateral + forwardAxis * forwardSpeed;
                velocity = _rigidbody.linearVelocity;
            }

            float maxTotalSpeed = Mathf.Max(_maxForwardSpeed, _maxForwardSpeed * Mathf.Max(1f, _maxTotalSpeedMultiplier));
            if (velocity.magnitude > maxTotalSpeed)
            {
                _rigidbody.linearVelocity = velocity.normalized * maxTotalSpeed;
            }

            float maxAngularSpeed = Mathf.Deg2Rad * _maxTurnSpeed * Mathf.Max(1f, _maxAngularSpeedMultiplier);
            if (_rigidbody.angularVelocity.magnitude > maxAngularSpeed)
            {
                _rigidbody.angularVelocity = _rigidbody.angularVelocity.normalized * maxAngularSpeed;
            }
        }

        private void ApplyPlayerStyleSteering()
        {
            if (!_isGrounded || !CanApplyDriveForces())
            {
                return;
            }

            Vector3 localAngularVelocity = transform.InverseTransformDirection(_rigidbody.angularVelocity);
            float currentYawRate = localAngularVelocity.y;
            float yawInertia = _rigidbody.inertiaTensor.y;

            if (Mathf.Abs(_turnInput) < 0.001f)
            {
                float stopTorque = -currentYawRate * yawInertia * 2f;
                _rigidbody.AddRelativeTorque(Vector3.up * stopTorque, ForceMode.Force);
                return;
            }

            float desiredYawRate = _turnInput * _maxTurnSpeed * Mathf.Deg2Rad;
            float yawError = desiredYawRate - currentYawRate;
            float responseFactor = Mathf.Clamp01(1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(0.01f, _turnResponseTime)));
            float torque = yawError * yawInertia * Mathf.Lerp(0.35f, 1f, responseFactor);
            _rigidbody.AddRelativeTorque(Vector3.up * torque, ForceMode.Force);
        }

        private void ApplyRollingResistance(Vector3 forwardAxis, float currentForwardSpeed)
        {
            float speedAbs = Mathf.Abs(currentForwardSpeed);
            float rollingCoeff = Mathf.Min(_groundFrictionCoefficient * _rollingResistanceScale, _maxRollingResistanceCoefficient);
            float resistanceMag = rollingCoeff * _rigidbody.mass * Mathf.Abs(Physics.gravity.y);

            if (speedAbs < _staticFrictionThreshold)
            {
                resistanceMag *= speedAbs / Mathf.Max(_staticFrictionThreshold, 0.0001f);
            }

            Vector3 rollingForce = -forwardAxis * resistanceMag * Mathf.Sign(currentForwardSpeed);
            _rigidbody.AddForce(rollingForce, ForceMode.Force);
        }

        private void ApplyRuntimeDamping()
        {
            float inputMagnitude = Mathf.Clamp01(Mathf.Abs(_forwardInput) + Mathf.Abs(_turnInput));
            _rigidbody.linearDamping = inputMagnitude > 0.01f ? _movingLinearDamping : _idleLinearDamping;
        }

        private void ApplyIdleSettling()
        {
            bool hasTravelCommand = Mathf.Abs(_forwardInput) > 0.01f;
            bool hasSteeringCommand = Mathf.Abs(_turnInput) > 0.01f;
            if (hasTravelCommand || hasSteeringCommand)
            {
                return;
            }

            if (_rigidbody.linearVelocity.magnitude < 0.2f)
            {
                _rigidbody.linearVelocity = Vector3.MoveTowards(_rigidbody.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);
            }

            if (_rigidbody.angularVelocity.magnitude < 0.2f)
            {
                _rigidbody.angularVelocity = Vector3.MoveTowards(_rigidbody.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);
            }
        }

        private void ApplySlopeCompensation()
        {
            if (!_isGrounded || !CanApplyDriveForces() || Mathf.Abs(_forwardInput) < 0.1f) return;

            Vector3 forwardAxis = GetGroundProjectedForward(_groundNormal);
            float slopeAngle = Vector3.Angle(forwardAxis, _groundNormal) - 90f;

            if (slopeAngle > 5f)
            {
                Vector3 upSlopeForce = forwardAxis * slopeAngle * _slopeCompensationFactor * _rigidbody.mass;
                _rigidbody.AddForce(upSlopeForce, ForceMode.Force);
            }
            else if (slopeAngle > 15f)
            {
                Vector3 recoveryForce = forwardAxis * _slopeRecoveryForce;
                _rigidbody.AddForce(recoveryForce, ForceMode.Force);
            }
        }

        private Vector3 GetGroundProjectedForward(Vector3 groundNormal)
        {
            Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, groundNormal);
            if (projectedForward.sqrMagnitude < 0.0001f)
            {
                projectedForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            }

            if (projectedForward.sqrMagnitude < 0.0001f)
            {
                projectedForward = transform.forward;
            }

            return projectedForward.normalized;
        }

        private float ResolveGroundFrictionCoefficient(RaycastHit hit)
        {
            float coefficient = GetDefaultGroundFrictionCoefficient();
            if (hit.collider != null && hit.collider.sharedMaterial != null)
            {
                PhysicsMaterial material = hit.collider.sharedMaterial;
                coefficient = Mathf.Max(coefficient, (material.staticFriction + material.dynamicFriction) * 0.5f);
            }

            return Mathf.Max(0.01f, coefficient);
        }

        private float GetDefaultGroundFrictionCoefficient()
        {
            return Mathf.Max(0.01f, _fallbackGroundFrictionCoefficient);
        }

        private bool IsValidGroundHit(RaycastHit hit)
        {
            if (hit.collider == null)
            {
                return false;
            }

            Transform root = _rigidbody != null ? _rigidbody.transform : transform.root;
            if (hit.rigidbody != null && hit.rigidbody == _rigidbody)
            {
                return false;
            }

            return !hit.collider.transform.IsChildOf(root);
        }

        private bool CanApplyDriveForces()
        {
            return !Application.isPlaying || Time.time >= _driveEnableTime;
        }

        private void RecoverFromAbnormalTeleportIfNeeded()
        {
            if (!_hasStablePose || !Application.isPlaying || Time.time < _teleportGuardEnableTime)
            {
                return;
            }

            float distanceFromLastStable = Vector3.Distance(transform.position, _lastStablePosition);
            if (distanceFromLastStable <= Mathf.Max(1f, _teleportDistanceThreshold))
            {
                return;
            }

            string state = _aiController != null ? _aiController.CurrentState : "<null>";
            Debug.LogWarning(
                $"[AI_MotionDriver] Teleport guard triggered on {name}. state={state}, distance={distanceFromLastStable:F2}, " +
                $"pos={transform.position}, lastPos={_lastStablePosition}, vel={_rigidbody.linearVelocity}, angVel={_rigidbody.angularVelocity}, grounded={_isGrounded}, groundNormal={_groundNormal}");

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.position = _lastStablePosition;
            _rigidbody.rotation = _lastStableRotation;
            transform.SetPositionAndRotation(_lastStablePosition, _lastStableRotation);
        }

        private void CacheStablePose()
        {
            if (!Application.isPlaying)
            {
                _lastStablePosition = transform.position;
                _lastStableRotation = transform.rotation;
                _hasStablePose = true;
                return;
            }

            if (Time.time < _teleportGuardEnableTime)
            {
                _lastStablePosition = transform.position;
                _lastStableRotation = transform.rotation;
                _hasStablePose = true;
                return;
            }

            float speed = _rigidbody.linearVelocity.magnitude;
            if (speed <= Mathf.Max(_maxForwardSpeed, _maxReverseSpeed) * Mathf.Max(1f, _maxTotalSpeedMultiplier))
            {
                _lastStablePosition = transform.position;
                _lastStableRotation = transform.rotation;
                _hasStablePose = true;
            }
        }

        private static bool IsFiniteVector(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, _groundPoint);
            Gizmos.DrawWireSphere(_groundPoint, 0.2f);

            if (_isGrounded)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(_groundPoint, _groundPoint + _groundNormal * 0.5f);
            }
        }
#endif
    }
}
