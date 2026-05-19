using UnityEngine;

public partial class TankWeaponController : MonoBehaviour
{

    private struct BarrelAvoidanceResult
    {
        public bool HasBlockingCollision;
        public float ResolvedPitch;
        public float MinimumClearance;
        public float NormalizedCorrection;
        public Collider BlockingCollider;
    }


    /// <summary>
    /// 评估避撞速度倍率：根据当前避撞严重程度（0 = 无需避撞，1 = 已完全阻塞）评估一个旋转速度倍率，用于在避撞过程中动态调整炮管旋转速度，提升玩家体验。
    /// </summary>
    private float EvaluateBarrelAvoidanceSpeedMultiplier(float normalizedSeverity)
    {
        if (turretData == null)
        {
            return 1f;
        }

        float resolvedMultiplier = turretData.BarrelAvoidanceSpeedCurve != null
            ? Mathf.Max(0f, turretData.BarrelAvoidanceSpeedCurve.Evaluate(Mathf.Clamp01(normalizedSeverity)))
            : Mathf.Lerp(1.3f, 2.8f, Mathf.Clamp01(normalizedSeverity));

        return float.IsNaN(resolvedMultiplier) || float.IsInfinity(resolvedMultiplier)
            ? 1f
            : resolvedMultiplier;
    }

    private void CalculateTargetPoint()
    {
        if (TankAImUIController.Instance != null)
        {
            _currentAimPoint = TankAImUIController.Instance.WorldAimPoint;
        }

        if (!_isFreeLooking && !_isRecovering)
        {
            RotateHardware();
        }
    }


    // 通用避撞处理：只对本车非装甲碰撞体执行最小上扬修正
    private float ResolveSelfBarrelCollision(float targetPitch, out float rotationSpeedMultiplier)
    {
        rotationSpeedMultiplier = 1f;

        if (float.IsNaN(targetPitch) || float.IsInfinity(targetPitch))
        {
            return GetMinimumBarrelPitch();
        }

        float minBarrelPitch = GetMinimumBarrelPitch();
        float maxBarrelPitch = GetMaximumBarrelPitch();
        float clampedTargetPitch = Mathf.Clamp(targetPitch, minBarrelPitch, maxBarrelPitch);

        if (barrelRoot == null || turretData == null)
        {
            return clampedTargetPitch;
        }

        if (_selfAvoidanceColliders.Count == 0)
        {
            return clampedTargetPitch;
        }

        BarrelAvoidanceResult avoidanceResult = EvaluateSelfBarrelAvoidance(clampedTargetPitch);
        if (!avoidanceResult.HasBlockingCollision)
        {
            return clampedTargetPitch;
        }

        rotationSpeedMultiplier = EvaluateBarrelAvoidanceSpeedMultiplier(avoidanceResult.NormalizedCorrection);
        return Mathf.Clamp(avoidanceResult.ResolvedPitch, minBarrelPitch, maxBarrelPitch);
    }


    //炮管碰撞检测：在炮管当前朝向上发出一个SphereCast，检测是否与本车非装甲碰撞体发生碰撞，如果发生碰撞则返回一个修正后的炮管仰角，确保炮管不会穿透本车
    private bool TryBuildBarrelEnvelopeSamplePoints(float barrelPitch, out Vector3[] samplePoints)
    {
        samplePoints = null;

        if (float.IsNaN(barrelPitch) || float.IsInfinity(barrelPitch))
        {
            return false;
        }

        if (barrelRoot == null || turretData == null)
        {
            return false;
        }

        int sampleCount = Mathf.Max(2, turretData.BarrelEnvelopeSampleCount);
        samplePoints = new Vector3[sampleCount];

        Quaternion barrelRootRotation = GetBarrelRootWorldRotationForPitch(barrelPitch);
        Vector3 barrelForward = barrelRootRotation * Vector3.forward;

        if (barrelForward.sqrMagnitude < 0.0001f || float.IsNaN(barrelForward.x) || float.IsNaN(barrelForward.y) || float.IsNaN(barrelForward.z))
        {
            return false;
        }

        for (int index = 0; index < sampleCount; index++)
        {
            float t = sampleCount == 1 ? 0f : (float)index / (sampleCount - 1);
            float distanceAlongBarrel = Mathf.Lerp(turretData.BarrelEnvelopeStartOffset, turretData.BarrelEnvelopeLength, t);
            samplePoints[index] = barrelRoot.position + barrelForward.normalized * distanceAlongBarrel;
        }

        return true;
    }


    //碰撞规避相关：在旋转炮管时检测是否与自身碰撞体发生碰撞，如果有则自动调整炮管角度以避免碰撞
    private void CacheSelfAvoidanceColliders()
    {
        _selfAvoidanceColliders.Clear();

        Transform collisionRoot = _tankRoot != null ? _tankRoot : transform.root;
        if (collisionRoot == null)
        {
            return;
        }

        Collider[] colliders = collisionRoot.GetComponentsInChildren<Collider>(true);
        for (int index = 0; index < colliders.Length; index++)
        {
            Collider candidate = colliders[index];
            if (ShouldRegisterAvoidanceCollider(candidate))
            {
                _selfAvoidanceColliders.Add(candidate);
            }
        }
    }



    //碰撞规避相关：判断一个碰撞体是否应该注册为避撞碰撞体
    private bool ShouldRegisterAvoidanceCollider(Collider candidate)
    {
        if (candidate == null || !candidate.enabled || candidate.isTrigger)
        {
            return false;
        }

        Transform candidateTransform = candidate.transform;
        if (candidateTransform == null)
        {
            return false;
        }

        if (barrel != null && candidateTransform.IsChildOf(barrel))
        {
            return false;
        }

        if (barrelRoot != null && candidateTransform == barrelRoot)
        {
            return false;
        }

        GeneralBarrelAvoidanceCollider overrideMarker = candidate.GetComponent<GeneralBarrelAvoidanceCollider>();
        if (overrideMarker == null)
        {
            overrideMarker = candidate.GetComponentInParent<GeneralBarrelAvoidanceCollider>();
        }

        if (overrideMarker != null)
        {
            if (overrideMarker.Role == BarrelAvoidanceColliderRole.Ignore)
            {
                return false;
            }

            if (overrideMarker.Role == BarrelAvoidanceColliderRole.Avoid)
            {
                return true;
            }
        }

        return candidate.GetComponentInParent<GeneralHitPosition>() == null;
    }


    /// <summary>
    /// 炮管避撞结果评估器：根据当前请求的炮管俯仰角，评估是否存在与自身碰撞体的阻塞碰撞，并返回一个包含避撞结果的结构体，包括是否有阻塞、最终调整后的俯仰角、最小间隙距离、归一化避撞程度以及阻塞碰撞体引用。
    /// </summary>
    private BarrelAvoidanceResult EvaluateSelfBarrelAvoidance(float requestedPitch)
    {
        BarrelAvoidanceResult result = new BarrelAvoidanceResult
        {
            HasBlockingCollision = false,
            ResolvedPitch = requestedPitch,
            MinimumClearance = float.MaxValue,
            NormalizedCorrection = 0f,
            BlockingCollider = null
        };

        if (turretData == null || _selfAvoidanceColliders.Count == 0)
        {
            return result;
        }

        float minPitch = GetMinimumBarrelPitch();
        float maxPitch = GetMaximumBarrelPitch();
        float clampedRequestedPitch = Mathf.Clamp(requestedPitch, minPitch, maxPitch);
        float requiredClearance = Mathf.Max(0f, turretData.BarrelAvoidanceRequiredClearance);

        if (!HasBlockingCollisionAtPitch(clampedRequestedPitch, requiredClearance, out float requestedClearance, out Collider requestedCollider))
        {
            result.MinimumClearance = requestedClearance;
            result.BlockingCollider = requestedCollider;
            return result;
        }

        result.HasBlockingCollision = true;
        result.MinimumClearance = requestedClearance;
        result.BlockingCollider = requestedCollider;

        float searchStep = Mathf.Max(0.05f, turretData.BarrelAvoidanceSearchStepDegrees);
        float blockedPitch = clampedRequestedPitch;
        float clearPitch = clampedRequestedPitch;
        bool foundClearPitch = false;

        while (clearPitch > minPitch + 0.0001f)
        {
            clearPitch = Mathf.Max(minPitch, clearPitch - searchStep);

            if (!HasBlockingCollisionAtPitch(clearPitch, requiredClearance, out float clearCandidateClearance, out Collider clearCandidateCollider))
            {
                result.MinimumClearance = clearCandidateClearance;
                result.BlockingCollider = clearCandidateCollider;
                foundClearPitch = true;
                break;
            }

            blockedPitch = clearPitch;
            result.MinimumClearance = clearCandidateClearance;
            result.BlockingCollider = clearCandidateCollider;
        }

        if (!foundClearPitch)
        {
            result.ResolvedPitch = minPitch;
            result.NormalizedCorrection = 1f;
            return result;
        }

        int refinementSteps = Mathf.Max(0, turretData.BarrelAvoidanceBinaryRefinementSteps);
        for (int index = 0; index < refinementSteps; index++)
        {
            float midPitch = Mathf.Lerp(clearPitch, blockedPitch, 0.5f);
            if (HasBlockingCollisionAtPitch(midPitch, requiredClearance, out _, out _))
            {
                blockedPitch = midPitch;
            }
            else
            {
                clearPitch = midPitch;
            }
        }

        result.ResolvedPitch = clearPitch;
        HasBlockingCollisionAtPitch(clearPitch, requiredClearance, out float resolvedClearance, out Collider resolvedCollider);
        result.MinimumClearance = resolvedClearance;
        result.BlockingCollider = resolvedCollider;

        float availableLift = Mathf.Max(0.0001f, Mathf.Abs(clampedRequestedPitch - minPitch));
        float appliedLift = Mathf.Abs(clampedRequestedPitch - result.ResolvedPitch);
        result.NormalizedCorrection = Mathf.Clamp01(appliedLift / availableLift);
        return result;
    }


    //炮管避撞检测：在给定炮管俯仰角的情况下，构建炮管包络线上的采样点，并检测这些点是否与本车非装甲碰撞体发生碰撞，如果发生碰撞则返回true，并输出最小间隙距离和阻塞碰撞体引用

    private bool HasBlockingCollisionAtPitch(float pitch, float requiredClearance, out float minimumClearance, out Collider blockingCollider)
    {
        minimumClearance = float.MaxValue;
        blockingCollider = null;

        if (!TryBuildBarrelEnvelopeSamplePoints(pitch, out Vector3[] samplePoints))
        {
            return false;
        }

        bool hasBlockingCollision = false;
        for (int colliderIndex = 0; colliderIndex < _selfAvoidanceColliders.Count; colliderIndex++)
        {
            Collider candidate = _selfAvoidanceColliders[colliderIndex];
            if (candidate == null || !candidate.enabled || !candidate.gameObject.activeInHierarchy)
            {
                continue;
            }

            for (int pointIndex = 0; pointIndex < samplePoints.Length; pointIndex++)
            {
                Vector3 closestPoint = candidate.ClosestPoint(samplePoints[pointIndex]);
                float distance = Vector3.Distance(samplePoints[pointIndex], closestPoint);
                float clearance = distance - turretData.BarrelEnvelopeRadius;

                if (clearance < minimumClearance)
                {
                    minimumClearance = clearance;
                    blockingCollider = candidate;
                }

                if (clearance <= requiredClearance)
                {
                    hasBlockingCollision = true;
                }
            }
        }

        return hasBlockingCollision;
    }


}