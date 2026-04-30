using UnityEngine;

public partial class TankMoveController : MonoBehaviour
{
    private const float EngineAudioTelemetrySpeedSmoothing = 28f;
    private const float EngineAudioTelemetryLoadSmoothing = 2.4f;
    private const float EngineAudioTelemetryRpmSmoothing = 1350f;
    private const float EngineAudioIdleRpmFloor = 650f;
    private const float EngineAudioMaxRpmCeiling = 2800f;

    private float _engineAudioRpm;
    private float _engineAudioLoadNormalized;
    private float _engineAudioSpeedKmh;
    private float _engineAudioSpeedSampleKmh;

    private float GetMinimumPowerSpeed()
    {
        if (tankMoveData != null && tankMoveData.MinimumPowerSpeed > 0f)
        {
            return tankMoveData.MinimumPowerSpeed;
        }

        return 0.5f;
    }

    private float GetNormalizedSpeed(float speedAbs)
    {
        if (tankMoveData == null || tankMoveData.MoveMaxSpeed <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(speedAbs / tankMoveData.MoveMaxSpeed);
    }

    private float CalculateAvailablePower(float normalizedSpeed, float forwardInput, float turnInput)
    {
        if (!HasPropulsionPower)
        {
            return 0f;
        }

        float enginePowerWatts = Mathf.Max(0f, tankMoveData.EnginePowerKw) * 1000f;
        float powerCurve = EvaluateCurve(tankMoveData.EnginePowerCurve, normalizedSpeed);
        float efficiency = Mathf.Clamp01(tankMoveData.TransmissionEfficiency);
        float demandFactor = Mathf.Clamp01(Mathf.Abs(forwardInput) + Mathf.Abs(turnInput) * Mathf.Max(0f, tankMoveData.PivotTurnPowerFactor));

        return enginePowerWatts * efficiency * powerCurve * demandFactor;
    }

    private void UpdateEngineAudioTelemetry(MovementHsmState hsmState, float currentForwardSpeed, float normalizedSpeed, float availablePower, float remainingPower, float steeringPowerRequirement)
    {
        if (!HasPropulsionPower || tankMoveData == null)
        {
            ResetEngineAudioTelemetry();
            return;
        }

        float speedAbs = Mathf.Abs(currentForwardSpeed);
        float speedTargetKmh = speedAbs * 3.6f;
        float maxSpeedKmh = Mathf.Max(1f, GetForwardSpeedLimit() * 3.6f);
        float speedRatio = Mathf.Clamp01(speedTargetKmh / maxSpeedKmh);
        _engineAudioSpeedSampleKmh = speedTargetKmh;

        float efficiency = Mathf.Clamp01(tankMoveData.TransmissionEfficiency);
        float maxPowerBudget = Mathf.Max(1f, Mathf.Max(0f, tankMoveData.EnginePowerKw) * 1000f * efficiency);
        float demandPowerRatio = Mathf.Clamp01((Mathf.Max(0f, availablePower) + Mathf.Max(0f, steeringPowerRequirement)) / maxPowerBudget);
        float deliveredPowerRatio = Mathf.Clamp01(Mathf.Max(0f, remainingPower) / maxPowerBudget);
        float inputDemandRatio = Mathf.Clamp01(Mathf.Abs(hsmState.TravelInputCommand) + Mathf.Abs(hsmState.RawSteerInput) * 0.5f);

        float loadTarget = Mathf.Clamp01(Mathf.Max(demandPowerRatio, deliveredPowerRatio * 0.85f, inputDemandRatio * 0.8f));
        float rpmDriveRatio = Mathf.Clamp01(speedRatio * 0.72f + demandPowerRatio * 0.18f + deliveredPowerRatio * 0.1f);
        float rpmTarget = Mathf.Lerp(EngineAudioIdleRpmFloor, EngineAudioMaxRpmCeiling, rpmDriveRatio);

        if (!hsmState.HasTravelCommand && !hsmState.HasSteeringCommand && speedAbs < 0.2f)
        {
            loadTarget *= 0.35f;
            rpmTarget = Mathf.Max(EngineAudioIdleRpmFloor, Mathf.Lerp(EngineAudioIdleRpmFloor, rpmTarget, 0.28f));
        }

        _engineAudioSpeedKmh = Mathf.MoveTowards(_engineAudioSpeedKmh, speedTargetKmh, EngineAudioTelemetrySpeedSmoothing * Time.fixedDeltaTime);
        _engineAudioLoadNormalized = Mathf.MoveTowards(_engineAudioLoadNormalized, loadTarget, EngineAudioTelemetryLoadSmoothing * Time.fixedDeltaTime);
        _engineAudioRpm = Mathf.MoveTowards(_engineAudioRpm, rpmTarget, EngineAudioTelemetryRpmSmoothing * Time.fixedDeltaTime);
    }

    private void ResetEngineAudioTelemetry()
    {
        _engineAudioRpm = 0f;
        _engineAudioLoadNormalized = 0f;
        _engineAudioSpeedKmh = 0f;
        _engineAudioSpeedSampleKmh = 0f;
    }

    // private float CalculateSteeringPowerRequirement(float currentForwardSpeed, float turnInput, float normalizedSpeed)
    // {
    //     if (Mathf.Abs(turnInput) < 0.0001f)
    //     {
    //         return 0f;
    //     }

    //     float steeringTorque = CalculateSteeringResistanceTorque(currentForwardSpeed, turnInput, normalizedSpeed);
    //     float targetYawDegreesPerSecond = Mathf.Lerp(tankMoveData.LocalTurnMaxSpeed, tankMoveData.MoveTurnMaxSpeed, normalizedSpeed) * Mathf.Abs(turnInput);
    //     float targetYawRadiansPerSecond = targetYawDegreesPerSecond * Mathf.Deg2Rad;

    //     return steeringTorque * targetYawRadiansPerSecond;
    // }

    private float CalculateSteeringPowerRequirement(float currentForwardSpeed, float turnInput, float normalizedSpeed, SteeringRegime steeringRegime)
    {
        if (Mathf.Abs(turnInput) < 0.0001f)
        {
            return 0f;
        }

        float speedAbs = Mathf.Abs(currentForwardSpeed);
        float mass = Mathf.Max(0.01f, tankMoveData.Mass);
        float trackContactLength = Mathf.Max(0.01f, tankMoveData.TrackContactLength);
        // 注意：原代码此处调用可能有点问题，我假设你之前使用的是正确的履带间距
        float trackCenterDistance = Mathf.Max(0.01f, GetTrackCenterDistance());

        float mu = Mathf.Max(0.01f, _groundContact.HasHit ? _groundContact.FrictionCoefficient : GetDefaultGroundFrictionCoefficient());
        float gravity = Mathf.Abs(Physics.gravity.y);

        // ================= 核心修正 1：物理极限角速度钳制 =================
        // 1. 获取配置中的基础期望角速度 (转为弧度)
        float desiredYawDegrees = ResolveDesiredTurnYawSpeedDegrees(steeringRegime, normalizedSpeed);
        float desiredYawRadians = desiredYawDegrees * Mathf.Deg2Rad;

        // 2. 计算物理允许的极限角速度 (omega = mu * g / v)
        // 速度极小时不限制，避免除以0；速度越大，允许的角速度越小（大回转半径）
        float maxPhysicalYawRadians = (mu * gravity) / Mathf.Max(speedAbs, 1f);

        // 3. 最终的实际角速度：取配置与物理极限的较小值
        float targetYawRadiansPerSecond = Mathf.Min(desiredYawRadians, maxPhysicalYawRadians) * Mathf.Abs(turnInput);
        // =================================================================

        if (targetYawRadiansPerSecond < 0.001f) return 0f;

        // 计算转向半径与尼基金公式参数
        float turnRadius = Mathf.Max(trackCenterDistance * 0.5f, speedAbs / targetYawRadiansPerSecond);
        float rho = turnRadius / trackCenterDistance;
        float phi = Mathf.Max(0.1f, EvaluateCurve(tankMoveData.SteeringResistanceCurve, rho));

        // 计算阻力矩 M_c
        float steeringTorque = mu * mass * gravity * trackContactLength * 0.25f * phi;

        // 计算理论所需转向功率: P = M_c * omega
        float rawSteeringPower = steeringTorque * targetYawRadiansPerSecond;

        // ================= 核心修正 2：工程化功率限制 (防止抽干前驱力) =================
        // 现代坦克的双流传动/液压差速系统会保证一定的前进动力。
        // 转向最多只能吃掉发动机总输出的某个百分比（例如 60%~75%），剩下的必须留给履带克服滚阻保持前进。
        float totalAvailablePower = CalculateAvailablePower(normalizedSpeed, 1f, turnInput);
        float maxAllowedSteeringPower = totalAvailablePower * 0.65f; // 65%是一个较好的工程经验值，可提取为SO参数

        return Mathf.Min(rawSteeringPower, maxAllowedSteeringPower);
    }

    private float CalculateSteeringPowerConsumption(float steeringPowerRequirement, float turnInput, float turnSpeedRetentionFactor)
    {
        if (Mathf.Abs(turnInput) < 0.0001f || steeringPowerRequirement <= 0f)
        {
            return 0f;
        }

        float retentionFactor = Mathf.Clamp01(turnSpeedRetentionFactor);
        float steeringPowerScale = 1f - retentionFactor;

        return steeringPowerRequirement * steeringPowerScale;
    }

    private float CalculateTurnSpeedRetention(float normalizedSpeed, float turnInput)
    {
        if (Mathf.Abs(turnInput) < 0.0001f)
        {
            return 1f;
        }

        float curveValue = Mathf.Clamp01(EvaluateCurve(tankMoveData.TurnSpeedRetentionCurve, normalizedSpeed));
        float turnWeight = Mathf.Clamp01(Mathf.Abs(turnInput));
        return Mathf.Lerp(1f, curveValue, turnWeight);
    }

    private float CalculateHighSpeedTurnRollFactor(float normalizedSpeed, float turnInput)
    {
        if (Mathf.Abs(turnInput) < 0.0001f)
        {
            return 0f;
        }

        float curveFactor = Mathf.Clamp01(EvaluateCurve(tankMoveData.HighSpeedTurnRollCurve, normalizedSpeed));
        float turnWeight = Mathf.Clamp01(Mathf.Abs(turnInput));
        return curveFactor * turnWeight * Mathf.Clamp(tankMoveData.HighSpeedTurnRollMultiplier, 0f, 3f);
    }

    private float CalculateSteeringResistanceTorque(float currentForwardSpeed, float turnInput, float normalizedSpeed, SteeringRegime steeringRegime)
    {
        if (Mathf.Abs(turnInput) < 0.0001f)
        {
            return 0f;
        }

        // 尼基金转向阻力主要用于估算“转向会吃掉多少功率预算”，不是直接作为速度值使用。
        float mass = Mathf.Max(0.01f, tankMoveData.Mass);
        float trackContactLength = Mathf.Max(0.01f, tankMoveData.TrackContactLength);
        float trackCenterDistance = Mathf.Max(0.01f, GetTrackCenterDistance());
        float targetYawDegreesPerSecond = ResolveDesiredTurnYawSpeedDegrees(steeringRegime, normalizedSpeed) * Mathf.Abs(turnInput);
        float targetYawRadiansPerSecond = targetYawDegreesPerSecond * Mathf.Deg2Rad;
        float turnRadius = Mathf.Max(trackCenterDistance * 0.5f, Mathf.Abs(currentForwardSpeed) / Mathf.Max(targetYawRadiansPerSecond, 0.01f));
        float rho = turnRadius / trackCenterDistance;
        float phi = Mathf.Max(0.1f, EvaluateCurve(tankMoveData.SteeringResistanceCurve, rho));
        float mu = Mathf.Max(0.01f, _groundContact.HasHit ? _groundContact.FrictionCoefficient : GetDefaultGroundFrictionCoefficient());
        float gravity = Mathf.Abs(Physics.gravity.y);
        return mu * mass * gravity * trackContactLength * 0.25f * phi;
    }

    private float EvaluateCurve(AnimationCurve curve, float input)
    {
        if (curve == null)
        {
            return 1f;
        }

        return Mathf.Max(0f, curve.Evaluate(input));
    }


}