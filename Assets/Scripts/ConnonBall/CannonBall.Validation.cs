using UnityEngine;

public partial class CannonBall : MonoBehaviour
{
    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();
        InitializeTrailRenderer();

        if (_pooledObject == null)
        {
            Debug.LogError($"CannonBall needs a PooledObject on {name}.", this);
        }

        if (isRealisticFlight)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError($"CannonBall realistic flight requires a Rigidbody on {name}.", this);
            }
        }

        if (_hitMask == 0)
        {
            Debug.LogWarning($"CannonBall hit mask is empty on {name}.", this);
        }
    }

    private void InitializeTrailRenderer()
    {
        _trailRenderer = GetComponent<TrailRenderer>();
        if (_trailRenderer == null && !enableSpeedTrail)
        {
            return;
        }

        if (_trailRenderer == null)
        {
            _trailRenderer = gameObject.AddComponent<TrailRenderer>();
        }

        if (_trailRenderer.sharedMaterial == null)
        {
            Shader trailShader = Shader.Find("Sprites/Default");
            if (trailShader != null)
            {
                _runtimeTrailMaterial = new Material(trailShader)
                {
                    name = $"{name}_TrailRuntimeMaterial"
                };
                _trailRenderer.sharedMaterial = _runtimeTrailMaterial;
            }
        }

        ConfigureTrailRenderer();
        ResetTrailState();
    }

    private void ConfigureTrailRenderer()
    {
        if (_trailRenderer == null)
        {
            return;
        }

        _trailRenderer.time = trailTime;
        _trailRenderer.startWidth = trailStartWidth;
        _trailRenderer.endWidth = trailEndWidth;
        _trailRenderer.minVertexDistance = trailMinVertexDistance;
        _trailRenderer.alignment = LineAlignment.View;
        _trailRenderer.textureMode = LineTextureMode.Stretch;
        _trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _trailRenderer.receiveShadows = false;
        _trailRenderer.generateLightingData = false;
        _trailRenderer.widthMultiplier = 1f;
        _trailRenderer.colorGradient = BuildTrailGradient();
    }

    private Gradient BuildTrailGradient()
    {
        if (HasConfiguredTrailGradient())
        {
            return trailColor;
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.92f, 0.55f), 0f),
                new GradientColorKey(new Color(1f, 0.45f, 0.15f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    private bool HasConfiguredTrailGradient()
    {
        return trailColor != null && trailColor.colorKeys.Length > 0;
    }

    private void UpdateTrailState()
    {
        if (_trailRenderer == null)
        {
            return;
        }

        bool shouldEmit = enableSpeedTrail && _velocity.magnitude >= trailSpeedThreshold;
        _trailRenderer.emitting = shouldEmit;
    }

    private void ResetTrailState()
    {
        if (_trailRenderer == null)
        {
            return;
        }

        _trailRenderer.emitting = false;
        _trailRenderer.Clear();
    }

    private void OnEnable()
    {
        ResetTrailState();
    }

    private void OnDisable()
    {
        ResetTrailState();
    }

    private void OnDestroy()
    {
        if (_runtimeTrailMaterial != null)
        {
            Destroy(_runtimeTrailMaterial);
            _runtimeTrailMaterial = null;
        }
    }
}
