using UnityEngine;

[RequireComponent(typeof(PooledObject))]
[RequireComponent(typeof(Collider))]
public partial class CannonBall : MonoBehaviour
{
    [SerializeField] private LayerMask _hitMask = ~0;

    [Header("Visual Trail")]
    [SerializeField] private bool enableSpeedTrail = true;
    [SerializeField] private float trailSpeedThreshold = 120f;
    [SerializeField] private float trailTime = 0.08f;
    [SerializeField] private float trailStartWidth = 0.06f;
    [SerializeField] private float trailEndWidth = 0.01f;
    [SerializeField] private float trailMinVertexDistance = 0.05f;
    [SerializeField] private Gradient trailColor;

    [SerializeField] private bool isRealisticFlight = false;

    private ProjectileData _projectileData;
    private PooledObject _pooledObject;
    private Vector3 _velocity;
    private Vector3 _previousPosition;
    private float _currentFlightTime;
    private TrailRenderer _trailRenderer;
    private Material _runtimeTrailMaterial;

    private void FixedUpdate()
    {
        UpdateTrailState();

        if (_currentFlightTime <= 0f)
        {
            CononBallReturnToPool();
            return;
        }

        _currentFlightTime -= Time.fixedDeltaTime;

        if (isRealisticFlight && _projectileData != null)
        {
            _velocity += Physics.gravity * _projectileData.GravityScale * Time.fixedDeltaTime;
            _velocity *= 1f - _projectileData.AirResistance * Time.fixedDeltaTime;
        }

        Vector3 startPosition = _previousPosition;
        Vector3 displacement = _velocity * Time.fixedDeltaTime;
        Vector3 endPosition = startPosition + displacement;

        if (TryRayStepHit(startPosition, endPosition, out RaycastHit hit))
        {
            transform.position = hit.point;
            ResolveHit(hit.collider);
            return;
        }

        transform.position = endPosition;
        _previousPosition = endPosition;
    }

    public void Shoot(Vector3 fireDirection, ProjectileData projectileData)
    {
        _projectileData = projectileData;
        _velocity = fireDirection.normalized * projectileData.InitialSpeed;
        _currentFlightTime = projectileData.MaxLifetime;
        _previousPosition = transform.position;

        ResetTrailState();
        UpdateTrailState();
    }

    public float GetDamageAmount() => _projectileData != null ? _projectileData.DamageValue : 0f;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.1f);

        if (_projectileData != null && _velocity != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + _velocity.normalized * _projectileData.InitialSpeed);
        }
    }
#endif
}
