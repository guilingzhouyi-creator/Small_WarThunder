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
        [SerializeField] private float _rollingResistanceCoeff = 0.05f;
        [SerializeField] private float _staticFrictionThreshold = 0.5f;

        [Header("斜坡补偿")]
        [SerializeField] private float _slopeCompensationFactor = 0.3f;
        [SerializeField] private float _slopeRecoveryForce = 2000f;

        private Rigidbody _rigidbody;
        private RaycastHit _groundHit;
        private bool _isGrounded;
        private float _forwardInput;
        private float _turnInput;
        private Vector3 _groundNormal = Vector3.up;
        private Vector3 _groundPoint = Vector3.zero;

        public bool IsGrounded => _isGrounded;
        public Vector3 GroundNormal => _groundNormal;
        public Vector3 GroundPoint => _groundPoint;
        public float ForwardInput => _forwardInput;
        public float TurnInput => _turnInput;

        public void SetMoveInput(float forward, float turn)
        {
            _forwardInput = Mathf.Clamp(forward, -1f, 1f);
            _turnInput = Mathf.Clamp(turn, -1f, 1f);
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            Debug.Log($"[AI_MotionDriver] {name} 初始化完毕");
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null) return;
            SampleGroundContact();
            ApplyGravityAssist();
            ApplyTrackForce();
            ApplyAnisotropicFriction();
            ClampVelocity();
            ApplySlopeCompensation();
        }

        private void SampleGroundContact()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.SphereCast(origin, _groundProbeRadius, Vector3.down, out _groundHit, _groundProbeLength, _groundMask))
            {
                _isGrounded = true;
                _groundNormal = _groundHit.normal;
                _groundPoint = _groundHit.point;
            }
            else
            {
                _isGrounded = false;
                _groundNormal = Vector3.up;
                _groundPoint = origin + Vector3.down * _groundProbeLength;
            }
        }

        private void ApplyGravityAssist()
        {
            if (_isGrounded) return;
            _rigidbody.AddForce(Vector3.down * _groundStickForce, ForceMode.Force);
        }

        private void ApplyTrackForce()
        {
            if (!_isGrounded) return;

            float absForward = Mathf.Abs(_forwardInput);
            float absTurn = Mathf.Abs(_turnInput);

            float leftForce, rightForce;

            if (absForward > 0.1f)
            {
                float baseForce = _forwardInput * _maxMotorForce;
                float turnForce = _turnInput * _maxMotorForce * 0.5f;
                leftForce = baseForce - turnForce;
                rightForce = baseForce + turnForce;
            }
            else if (absTurn > 0.1f)
            {
                float force = _turnInput * _maxMotorForce * 0.5f;
                leftForce = -force;
                rightForce = force;
            }
            else
            {
                ApplyBrakingForce();
                return;
            }

            Vector3 forwardDir = transform.forward;
            Vector3 leftPos = transform.position - transform.right * _trackWidth * 0.5f;
            Vector3 rightPos = transform.position + transform.right * _trackWidth * 0.5f;

            forwardDir = Vector3.ProjectOnPlane(forwardDir, _groundNormal).normalized;

            _rigidbody.AddForceAtPosition(forwardDir * leftForce, leftPos, ForceMode.Force);
            _rigidbody.AddForceAtPosition(forwardDir * rightForce, rightPos, ForceMode.Force);
        }

        private void ApplyBrakingForce()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(_rigidbody.linearVelocity);
            float forwardBrake = -localVelocity.z * _maxBrakeForce * 0.5f;
            _rigidbody.AddForce(transform.forward * forwardBrake, ForceMode.Force);
            float sideBrake = -localVelocity.x * _maxBrakeForce * 0.8f;
            _rigidbody.AddForce(transform.right * sideBrake, ForceMode.Force);
            _rigidbody.AddTorque(-_rigidbody.angularVelocity * _maxBrakeForce * 0.1f, ForceMode.Force);
        }

        private void ApplyAnisotropicFriction()
        {
            if (!_isGrounded) return;

            Vector3 localVelocity = transform.InverseTransformDirection(_rigidbody.linearVelocity);
            float forwardSpeed = localVelocity.z;
            float sideSpeed = localVelocity.x;

            float absForward = Mathf.Abs(forwardSpeed);
            float absSide = Mathf.Abs(sideSpeed);
            float blend = absForward / (absForward + absSide + 0.001f);

            float effectiveFwdFric = Mathf.Lerp(_sideFriction, _forwardFriction, blend);
            float effectiveSideFric = Mathf.Lerp(_forwardFriction, _sideFriction, blend);

            Vector3 fwdFrictionForce = transform.forward * (-forwardSpeed * effectiveFwdFric * _rigidbody.mass * 0.5f);
            Vector3 sideFrictionForce = transform.right * (-sideSpeed * effectiveSideFric * _rigidbody.mass * 0.5f);

            _rigidbody.AddForce(fwdFrictionForce, ForceMode.Force);
            _rigidbody.AddForce(sideFrictionForce, ForceMode.Force);

            if (absForward > _staticFrictionThreshold)
            {
                Vector3 rollResistance = -transform.forward * Mathf.Sign(forwardSpeed) * _rollingResistanceCoeff * _rigidbody.mass;
                _rigidbody.AddForce(rollResistance, ForceMode.Force);
            }
        }

        private void ClampVelocity()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(_rigidbody.linearVelocity);
            float clampedZ = Mathf.Clamp(localVelocity.z, -_maxReverseSpeed, _maxForwardSpeed);
            float clampedX = Mathf.Clamp(localVelocity.x, -_maxForwardSpeed * 0.3f, _maxForwardSpeed * 0.3f);

            Vector3 localAngVel = transform.InverseTransformDirection(_rigidbody.angularVelocity);
            localAngVel.y = Mathf.Clamp(localAngVel.y, -_maxTurnSpeed * Mathf.Deg2Rad, _maxTurnSpeed * Mathf.Deg2Rad);

            Vector3 worldVelocity = transform.TransformDirection(new Vector3(clampedX, localVelocity.y, clampedZ));
            Vector3 worldAngVel = transform.TransformDirection(localAngVel);

            _rigidbody.linearVelocity = worldVelocity;
            _rigidbody.angularVelocity = worldAngVel;
        }

        private void ApplySlopeCompensation()
        {
            if (!_isGrounded || Mathf.Abs(_forwardInput) < 0.1f) return;

            float slopeAngle = Vector3.Angle(transform.forward, _groundNormal) - 90f;

            if (slopeAngle > 5f)
            {
                Vector3 upSlopeForce = transform.forward * slopeAngle * _slopeCompensationFactor * _rigidbody.mass;
                _rigidbody.AddForce(upSlopeForce, ForceMode.Force);
            }
            else if (slopeAngle > 15f)
            {
                Vector3 recoveryForce = transform.forward * _slopeRecoveryForce;
                _rigidbody.AddForce(recoveryForce, ForceMode.Force);
            }
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
