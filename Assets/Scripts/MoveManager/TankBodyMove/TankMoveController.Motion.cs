using UnityEngine;
public partial class TankMoveController : MonoBehaviour
{
    private void SimulatePowerSplit()
    {
        Vector3 groundNormal = _groundContact.HasHit ? _groundContact.Normal : Vector3.up;
        Vector3 forwardAxis = GetGroundProjectedForward(groundNormal);

        float currentForwardSpeed = Vector3.Dot(tankRigidbody.linearVelocity, forwardAxis);
        float speedAbs = Mathf.Abs(currentForwardSpeed);
        MovementHsmState hsmState = ResolveMovementHsmState(currentForwardSpeed, speedAbs);
        _debugForwardInput = hsmState.TravelInputCommand;
        _debugTurnInput = hsmState.YawTurnInput;
        _hasDebugTurnCenterPoint = TryGetTurnCenterPoint(hsmState.TrackTurnInput, out _debugTurnCenterPoint);

        float normalizedSpeed = GetNormalizedSpeed(speedAbs);

        float desiredYawDegreesPerSecond = ResolveDesiredTurnYawSpeedDegrees(hsmState.CurrentSteeringRegime, normalizedSpeed);
        float turnResponseFactor = GetTurnResponseFactor();

        float availablePower = CalculateAvailablePower(normalizedSpeed, hsmState.TravelInputCommand, hsmState.RawSteerInput);
        float steeringResistanceTorque = CalculateSteeringResistanceTorque(currentForwardSpeed, hsmState.YawTurnInput, normalizedSpeed, hsmState.CurrentSteeringRegime);
        float steeringPowerRequirement = CalculateSteeringPowerRequirement(currentForwardSpeed, hsmState.YawTurnInput, normalizedSpeed, hsmState.CurrentSteeringRegime);
        float turnSpeedRetentionFactor = CalculateTurnSpeedRetention(normalizedSpeed, hsmState.YawTurnInput);
        float turnRollFactor = CalculateHighSpeedTurnRollFactor(normalizedSpeed, hsmState.YawTurnInput);
        float steeringPowerConsumption = CalculateSteeringPowerConsumption(steeringPowerRequirement, hsmState.YawTurnInput, turnSpeedRetentionFactor);
        float remainingPower = availablePower - steeringPowerConsumption;

        UpdateEngineAudioTelemetry(hsmState, currentForwardSpeed, normalizedSpeed, availablePower, remainingPower, steeringPowerRequirement);

        ApplyAnisotropicFriction(groundNormal);

        ApplyDynamicSteering(hsmState, desiredYawDegreesPerSecond, turnResponseFactor);

        ApplyTurnRollMoment(hsmState.YawTurnInput, turnRollFactor, steeringResistanceTorque);

        ApplyLongitudinalDynamics(forwardAxis, currentForwardSpeed, hsmState, normalizedSpeed, speedAbs, turnSpeedRetentionFactor, steeringResistanceTorque, availablePower, remainingPower, desiredYawDegreesPerSecond, turnResponseFactor);

        ApplySpeedHardLimits(forwardAxis);

        if (!hsmState.HasTravelCommand && !hsmState.HasSteeringCommand && speedAbs < 0.2f)
        {
            tankRigidbody.linearVelocity = Vector3.MoveTowards(tankRigidbody.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);
            tankRigidbody.angularVelocity = Vector3.MoveTowards(tankRigidbody.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);
        }

        ApplyRuntimeDamping(hsmState, currentForwardSpeed);

        _cachedForwardSpeed = Vector3.Dot(tankRigidbody.linearVelocity, forwardAxis);
    }

    private void SampleGroundContact()
    {
        // 这里采样的是“地面接触状态”，不是最终的驱动力。
        // 最终用于移动解算的是滚动阻力系数，必须保持在很小的量级。
        Vector3 origin = tanker.transform.position + Vector3.up * Mathf.Max(0f, groundProbeHeight);
        float castDistance = Mathf.Max(0.1f, groundProbeDistance);

        if (Physics.SphereCast(origin, Mathf.Max(0.05f, groundProbeRadius), Vector3.down, out RaycastHit hit, castDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            _groundContact.HasHit = true;
            _groundContact.Normal = hit.normal.sqrMagnitude > 0f ? hit.normal.normalized : Vector3.up;
            _groundContact.SlopeAngle = Vector3.Angle(_groundContact.Normal, Vector3.up);
            _groundContact.FrictionCoefficient = ResolveGroundFrictionCoefficient(hit);
            return;
        }

        _groundContact.HasHit = false;
        _groundContact.Normal = Vector3.up;
        _groundContact.SlopeAngle = 0f;
        _groundContact.FrictionCoefficient = GetDefaultGroundFrictionCoefficient();
    }

    /// <summary>
    /// 各向异性摩擦力求解器。计算侧滑，引发离心力矩平衡，执行瞬态动能损耗。
    /// </summary>
    private void ApplyAnisotropicFriction(Vector3 groundNormal)
    {
        if (!_groundContact.HasHit) return;

        // 将全局速度投影到载具本地坐标系中，分离出 v_x 和 v_y
        Vector3 localVelocity = tanker.transform.InverseTransformDirection(tankRigidbody.linearVelocity);
        float lateralVelocity = localVelocity.x; // 横向速度 v_y

        // 法向力 F_n = m * g * cos(theta)
        float gravity = Mathf.Abs(Physics.gravity.y);
        float fn = tankMoveData.Mass * gravity * Mathf.Cos(_groundContact.SlopeAngle * Mathf.Deg2Rad);

        // 侧向摩擦力上限: \mu_y * F_n
        float mu_y = _groundContact.FrictionCoefficient * lateralFrictionMultiplier;
        float maxLateralFriction = mu_y * fn;

        // 计算消除侧滑所需的力 (隐式欧拉阻尼，防止低速时的数值震荡反转)
        // 物理意义：v_y * k_damping，由冲量定理 I = F*dt = m*dv 推导
        float requiredForceToStop = (lateralVelocity * tankMoveData.Mass) / Time.fixedDeltaTime;

        // 核心公式：F_lat = clamp(v_y * k_damping, ± \mu_y * F_n)
        float appliedLateralForce = -Mathf.Clamp(requiredForceToStop, -maxLateralFriction, maxLateralFriction);

        // 【关键点】：将侧向摩擦力施加在车体底部的“接地中心点”，而不是质心。
        // 这会自然产生围绕质心的翻滚力矩 M_roll = F_lat * h
        Vector3 groundContactPatch = tanker.transform.position - tanker.transform.up * cogHeight;

        tankRigidbody.AddForceAtPosition(tanker.transform.right * appliedLateralForce, groundContactPatch, ForceMode.Force);
    }

    private void ApplyRuntimeDamping(MovementHsmState hsmState, float currentForwardSpeed)
    {
        float inputMagnitude = Mathf.Clamp01(Mathf.Abs(hsmState.TravelInputCommand) + Mathf.Abs(hsmState.RawSteerInput));

        if (inputMagnitude > 0.01f)
        {
            bool isBraking = hsmState.HasTravelCommand
                && !hsmState.HasSteeringCommand
                && Mathf.Sign(hsmState.TravelCommandSign) != Mathf.Sign(currentForwardSpeed)
                && Mathf.Abs(currentForwardSpeed) > 0.5f;

            tankRigidbody.linearDamping = isBraking ? idleLinearDamping : movingLinearDamping;
        }
        else
        {
            tankRigidbody.linearDamping = idleLinearDamping;
        }
    }


    private void ApplyDynamicSteering(MovementHsmState hsmState, float desiredYawDegreesPerSecond, float turnResponseFactor)
    {
        float effectiveTurnInput = hsmState.YawTurnInput;
        Vector3 localAngularVel = tanker.transform.InverseTransformDirection(tankRigidbody.angularVelocity);
        float currentYawRate = localAngularVel.y;
        float yawInertia = tankRigidbody.inertiaTensor.y;

        if (Mathf.Abs(effectiveTurnInput) < 0.0001f)
        {
            float stopTorque = -currentYawRate * yawInertia * 2.0f;
            tankRigidbody.AddRelativeTorque(Vector3.up * stopTorque, ForceMode.Force);
            return;
        }

        if (!hsmState.HasLatchedTravel)
        {
            return;
        }

        float desiredYawRate = desiredYawDegreesPerSecond * Mathf.Deg2Rad * Mathf.Sign(effectiveTurnInput);
        float yawError = desiredYawRate - currentYawRate;
        float responseGain = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(turnResponseFactor));
        float torque = yawError * yawInertia * responseGain;
        tankRigidbody.AddRelativeTorque(Vector3.up * torque, ForceMode.Force);
    }

    private void ApplyLongitudinalDynamics(Vector3 forwardAxis, float currentForwardSpeed, MovementHsmState hsmState, float normalizedSpeed, float speedAbs, float turnSpeedRetentionFactor, float steeringResistanceTorque, float availablePower, float remainingPower, float desiredYawDegreesPerSecond, float turnResponseFactor)
    {
        if (ShouldApplyDirectionBrake(hsmState, currentForwardSpeed, speedAbs))
        {
            float brakeAcceleration = hsmState.CurrentTravelState == TravelState.Reverse ? tankMoveData.BackMoveAcceleration : tankMoveData.MoveAcceleration;
            float brakeForce = tankMoveData.Mass * brakeAcceleration * 1.5f;
            tankRigidbody.AddForce(-forwardAxis * brakeForce * Mathf.Sign(currentForwardSpeed), ForceMode.Force);
            ApplyRollingResistance(forwardAxis, currentForwardSpeed);
            return;
        }

        if (!hsmState.HasTravelCommand && !hsmState.HasSteeringCommand)
        {
            ApplyRollingResistance(forwardAxis, currentForwardSpeed);
            return;
        }

        float speedLimit = ResolveTurnSpeedLimit(hsmState.CurrentSteeringRegime, speedAbs);

        float accelSetting = hsmState.CurrentTravelState == TravelState.Reverse ? tankMoveData.BackMoveAcceleration : tankMoveData.MoveAcceleration;
        float inputMagnitude = Mathf.Clamp01(Mathf.Max(Mathf.Abs(hsmState.TravelInputCommand), Mathf.Abs(hsmState.RawSteerInput)));
        float tractionLimitForce = tankMoveData.Mass * accelSetting * inputMagnitude;

        float speedFactor = Mathf.InverseLerp(speedLimit, speedLimit * 0.9f, speedAbs);
        speedFactor = Mathf.SmoothStep(0f, 1f, speedFactor);

        if (hsmState.HasSteeringCommand)
        {
            speedFactor = Mathf.Max(speedFactor, turnSpeedRetentionFactor);
        }

        float effectivePowerBudget = Mathf.Max(0f, remainingPower);
        float speedForPowerCalc = Mathf.Max(speedAbs, GetMinimumPowerSpeed());
        float powerLimitForce = Mathf.Max(0f, (effectivePowerBudget * speedFactor) / speedForPowerCalc);
        float driveForceMagnitude = Mathf.Min(tractionLimitForce, powerLimitForce);
        ApplySplitTrackDrive(forwardAxis, driveForceMagnitude, currentForwardSpeed, hsmState, speedAbs, turnSpeedRetentionFactor, steeringResistanceTorque, desiredYawDegreesPerSecond, turnResponseFactor);
        ApplyRollingResistance(forwardAxis, currentForwardSpeed);
    }

    private bool ShouldApplyDirectionBrake(MovementHsmState hsmState, float currentForwardSpeed, float speedAbs)
    {
        if (!hsmState.HasTravelCommand || speedAbs <= 0.5f)
        {
            return false;
        }

        float desiredTravelSign = hsmState.TravelCommandSign;
        if (Mathf.Abs(desiredTravelSign) < 0.001f)
        {
            return false;
        }

        if (Mathf.Sign(currentForwardSpeed) == desiredTravelSign)
        {
            return false;
        }

        if (hsmState.HasSteeringCommand && speedAbs <= Mathf.Max(0.75f, GetBrakeTurnMaxSpeed()))
        {
            return false;
        }

        return true;
    }

    private float ResolveTurnSpeedLimit(SteeringRegime steeringRegime, float speedAbs)
    {
        if (steeringRegime == SteeringRegime.Straight)
        {
            return Mathf.Max(0.01f, tankMoveData.MoveMaxSpeed);
        }

        if (steeringRegime == SteeringRegime.PivotTurn)
        {
            return Mathf.Max(0.01f, tankMoveData.UseDualStreamTransmission ? tankMoveData.LocalTwoTurnMaxSpeed : tankMoveData.LocalOneTurnMaxSpeed);
        }

        if (steeringRegime == SteeringRegime.BrakeTurn)
        {
            return Mathf.Max(0.01f, tankMoveData.LocalOneTurnMaxSpeed);
        }

        float arcBlend = GetMovingArcBlend(speedAbs);
        return Mathf.Max(0.01f, Mathf.Lerp(tankMoveData.LocalOneTurnMaxSpeed, tankMoveData.MoveMaxSpeed, arcBlend));
    }

    private float ResolveDesiredTurnYawSpeedDegrees(SteeringRegime steeringRegime, float normalizedSpeed)
    {
        if (tankMoveData == null)
        {
            return 0f;
        }

        switch (steeringRegime)
        {
            case SteeringRegime.PivotTurn:
                return tankMoveData.UseDualStreamTransmission ? tankMoveData.LocalTwoTurnMaxSpeed : tankMoveData.LocalOneTurnMaxSpeed;
            case SteeringRegime.BrakeTurn:
                return tankMoveData.LocalOneTurnMaxSpeed;
            case SteeringRegime.MovingTurn:
                return Mathf.Lerp(tankMoveData.MovingTurnYawSpeed * 0.85f, tankMoveData.MovingTurnYawSpeed, normalizedSpeed);
            default:
                return 0f;
        }
    }

    private float GetTurnResponseFactor()
    {
        if (tankMoveData == null)
        {
            return 1f;
        }

        float responseTime = Mathf.Max(0.01f, tankMoveData.TurnAccelerationTime);
        return Mathf.Clamp01(1f - Mathf.Exp(-Time.fixedDeltaTime / responseTime));
    }

    private void ApplySpeedHardLimits(Vector3 forwardAxis)
    {
        float forwardSpeed = Vector3.Dot(tankRigidbody.linearVelocity, forwardAxis);
        float forwardLimit = GetForwardSpeedLimit();
        float backwardLimit = GetBackwardSpeedLimit();

        float clampedForwardSpeed = forwardSpeed;

        if (forwardSpeed > forwardLimit)
        {
            clampedForwardSpeed = forwardLimit;
        }
        else if (forwardSpeed < -backwardLimit)
        {
            clampedForwardSpeed = -backwardLimit;
        }

        if (!Mathf.Approximately(clampedForwardSpeed, forwardSpeed))
        {
            tankRigidbody.linearVelocity += forwardAxis * (clampedForwardSpeed - forwardSpeed);
        }
    }

    private void ApplySplitTrackDrive(Vector3 forwardAxis, float driveForceMagnitude, float currentForwardSpeed, MovementHsmState hsmState, float speedAbs, float turnSpeedRetentionFactor, float steeringResistanceTorque, float desiredYawDegreesPerSecond, float turnResponseFactor)
    {
        if (driveForceMagnitude <= 0f)
        {
            return;
        }

        if (leftTrackDrivePoint == null || rightTrackDrivePoint == null)
        {
            CacheTrackDrivePoints();
        }

        if (leftTrackDrivePoint == null || rightTrackDrivePoint == null)
        {
            return;
        }

        if (hsmState.CurrentSteeringRegime == SteeringRegime.PivotTurn)
        {
            ApplyPivotTurnDrive(forwardAxis, driveForceMagnitude, hsmState.TrackTurnInput);
            return;
        }

        if (hsmState.CurrentSteeringRegime == SteeringRegime.MovingTurn)
        {
            ApplyArcTurnDrive(forwardAxis, driveForceMagnitude, hsmState, speedAbs, turnSpeedRetentionFactor, steeringResistanceTorque, desiredYawDegreesPerSecond, turnResponseFactor);
            return;
        }

        if (hsmState.CurrentSteeringRegime == SteeringRegime.BrakeTurn)
        {
            ApplyLowSpeedBrakeTurnDrive(forwardAxis, driveForceMagnitude, currentForwardSpeed, hsmState, steeringResistanceTorque);
            return;
        }

        ApplyStraightTrackDrive(forwardAxis, driveForceMagnitude, hsmState);
    }

    private void ApplyTrackCommands(Vector3 forwardAxis, float leftDrive, float leftBrake, float rightDrive, float rightBrake)
    {
        leftTrackDrivePoint.ApplyTrackPhysics(forwardAxis, leftDrive, leftBrake, cogHeight);
        rightTrackDrivePoint.ApplyTrackPhysics(forwardAxis, rightDrive, rightBrake, cogHeight);
    }

    private void ApplyStraightTrackDrive(Vector3 forwardAxis, float driveForceMagnitude, MovementHsmState hsmState)
    {
        float travelSign = ResolveTravelSign(hsmState.CurrentTravelState, 0f);
        if (Mathf.Abs(travelSign) < 0.001f)
        {
            return;
        }

        float drivePerSide = driveForceMagnitude * 0.5f * travelSign;
        ApplyTrackCommands(forwardAxis, drivePerSide, 0f, drivePerSide, 0f);
    }

    private void ApplyPivotTurnDrive(Vector3 forwardAxis, float driveForceMagnitude, float turnInput)
    {
        if (Mathf.Abs(turnInput) < 0.001f)
        {
            return;
        }

        if (tankMoveData != null && tankMoveData.UseDualStreamTransmission)
        {
            float turnWeight = Mathf.Clamp01(Mathf.Abs(turnInput));
            float pivotDrive = driveForceMagnitude * Mathf.Clamp01(tankMoveData.PivotTurnPowerFactor) * Mathf.Clamp01(tankMoveData.PivotTurnEfficiency);
            pivotDrive *= Mathf.Max(0.1f, turnWeight);

            float turnDirection = Mathf.Sign(turnInput);
            float leftDrive = pivotDrive * turnDirection;
            float rightDrive = -pivotDrive * turnDirection;

            ApplyTrackCommands(forwardAxis, leftDrive, 0f, rightDrive, 0f);
            return;
        }

        float turnWeightFallback = Mathf.Clamp01(Mathf.Abs(turnInput));
        float pivotDriveFallback = driveForceMagnitude * Mathf.Lerp(0.45f, 0.8f, turnWeightFallback);
        float pivotBrake = pivotDriveFallback * 0.85f;
        bool leftIsOuterTrack = turnInput > 0f;

        if (leftIsOuterTrack)
        {
            ApplyTrackCommands(forwardAxis, pivotDriveFallback, 0f, 0f, pivotBrake);
            return;
        }

        ApplyTrackCommands(forwardAxis, 0f, pivotBrake, pivotDriveFallback, 0f);
    }

    private void ApplyArcTurnDrive(Vector3 forwardAxis, float driveForceMagnitude, MovementHsmState hsmState, float speedAbs, float turnSpeedRetentionFactor, float steeringResistanceTorque, float desiredYawDegreesPerSecond, float turnResponseFactor)
    {
        float trackTurnInput = hsmState.TrackTurnInput;
        float yawTurnInput = hsmState.YawTurnInput;
        float travelSign = ResolveTravelSign(hsmState.CurrentTravelState, 1f);
        float turnWeight = Mathf.Clamp01(Mathf.Abs(yawTurnInput));
        float arcBlend = GetMovingArcBlend(speedAbs);
        float innerDriveScale = Mathf.Lerp(0.58f, Mathf.Max(0.35f, turnSpeedRetentionFactor * 0.85f), arcBlend);
        float outerDriveScale = Mathf.Lerp(1.08f, 1.45f, arcBlend);
        float baseDrive = driveForceMagnitude * 0.5f * travelSign;

        float outerDrive = baseDrive * outerDriveScale;
        float innerDrive = baseDrive * innerDriveScale;
        bool leftIsOuterTrack = trackTurnInput > 0f;

        float leftDrive = leftIsOuterTrack ? outerDrive : innerDrive;
        float rightDrive = leftIsOuterTrack ? innerDrive : outerDrive;

        ApplyTrackCommands(forwardAxis, leftDrive, 0f, rightDrive, 0f);
        ApplyArcYawAssist(forwardAxis, yawTurnInput, turnWeight, turnSpeedRetentionFactor, arcBlend, steeringResistanceTorque, desiredYawDegreesPerSecond, turnResponseFactor);
    }

    private float GetBrakeTurnMaxSpeed()
    {
        if (tankMoveData == null)
        {
            return 0f;
        }

        return Mathf.Max(0.01f, tankMoveData.BrakeTurnMaxSpeed);
    }

    private float GetHighSpeedArcTurnMinSpeed()
    {
        if (tankMoveData == null)
        {
            return 0f;
        }

        return Mathf.Max(GetBrakeTurnMaxSpeed(), tankMoveData.HighSpeedArcTurnMinSpeed);
    }

    private float GetMovingArcBlend(float speedAbs)
    {
        float brakeTurnMaxSpeed = GetBrakeTurnMaxSpeed();
        float highSpeedArcTurnMinSpeed = GetHighSpeedArcTurnMinSpeed();

        if (highSpeedArcTurnMinSpeed <= brakeTurnMaxSpeed + 0.01f)
        {
            return 1f;
        }

        return Mathf.Clamp01(Mathf.InverseLerp(brakeTurnMaxSpeed, highSpeedArcTurnMinSpeed, speedAbs));
    }

    private void ApplyArcYawAssist(Vector3 forwardAxis, float turnInput, float turnWeight, float turnSpeedRetentionFactor, float arcBlend, float steeringResistanceTorque, float desiredYawDegreesPerSecond, float turnResponseFactor)
    {
        if (Mathf.Abs(turnInput) < 0.001f || tankRigidbody == null)
        {
            return;
        }

        Vector3 desiredLateral = tanker.transform.right * turnInput;
        Vector3 yawAxis = Vector3.Cross(forwardAxis, desiredLateral);
        if (yawAxis.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float yawSpeedRatio = Mathf.Clamp01(desiredYawDegreesPerSecond / Mathf.Max(0.01f, tankMoveData.MovingTurnYawSpeed));
        float assistScale = Mathf.Lerp(0.12f, 0.32f, turnWeight) * Mathf.Lerp(0.35f, 1f, turnSpeedRetentionFactor) * Mathf.Lerp(0.85f, 1.15f, arcBlend) * Mathf.Lerp(0.85f, 1.25f, yawSpeedRatio) * Mathf.Lerp(0.75f, 1f, turnResponseFactor);
        float assistTorque = Mathf.Max(0f, steeringResistanceTorque * assistScale);
        tankRigidbody.AddTorque(yawAxis.normalized * assistTorque, ForceMode.Force);
    }

    private void ApplyLowSpeedBrakeTurnDrive(Vector3 forwardAxis, float driveForceMagnitude, float currentForwardSpeed, MovementHsmState hsmState, float steeringResistanceTorque)
    {
        float trackTurnInput = hsmState.TrackTurnInput;
        float yawTurnInput = hsmState.YawTurnInput;
        float turnWeight = Mathf.Clamp01(Mathf.Abs(yawTurnInput));
        float travelSign = ResolveTravelSign(hsmState.CurrentTravelState, currentForwardSpeed);
        float baseDrive = driveForceMagnitude * 0.5f * travelSign;
        float turnBrake = Mathf.Max(driveForceMagnitude * 0.35f, steeringResistanceTorque / Mathf.Max(0.1f, GetTrackCenterDistance()));
        turnBrake *= Mathf.Lerp(1.25f, 0.75f, Mathf.Clamp01(Mathf.Abs(currentForwardSpeed) / Mathf.Max(0.01f, tankMoveData.MoveMaxSpeed)));
        bool solveFromReverseBaseline = travelSign < 0f;
        float reverseInnerDriveScale = Mathf.Lerp(0.18f, 0.04f, turnWeight);
        float reverseRecoveryForce = tankMoveData.Mass * tankMoveData.BackMoveAcceleration * Mathf.Lerp(0.25f, 0.75f, turnWeight);

        float leftDrive = baseDrive;
        float rightDrive = baseDrive;
        float leftBrake = 0f;
        float rightBrake = 0f;

        if (Mathf.Abs(trackTurnInput) > 0.1f)
        {
            float outerDrive = driveForceMagnitude * travelSign;
            float innerDrive = solveFromReverseBaseline ? driveForceMagnitude * travelSign * reverseInnerDriveScale : 0f;

            if (trackTurnInput > 0f)
            {
                leftDrive = outerDrive;
                rightDrive = innerDrive;
                rightBrake = turnBrake * turnWeight;
            }
            else
            {
                rightDrive = outerDrive;
                leftDrive = innerDrive;
                leftBrake = turnBrake * turnWeight;
            }
        }

        if (solveFromReverseBaseline && currentForwardSpeed > 0.01f)
        {
            tankRigidbody.AddForce(-forwardAxis * reverseRecoveryForce, ForceMode.Force);
        }

        ApplyTrackCommands(forwardAxis, leftDrive, leftBrake, rightDrive, rightBrake);
    }

    /// <summary>
    /// 计算前行方向是否正确——应用与驱动力与地面接触点的投影，确保在陡坡上的前行方向始终贴合地面。
    /// </summary>
    private Vector3 GetGroundProjectedForward(Vector3 groundNormal)
    {
        Vector3 forward = tanker != null ? tanker.transform.forward : transform.forward;
        Vector3 projectedForward = Vector3.ProjectOnPlane(forward, groundNormal);

        if (projectedForward.sqrMagnitude < 0.0001f)
        {
            projectedForward = Vector3.ProjectOnPlane(forward, Vector3.up);
        }

        if (projectedForward.sqrMagnitude < 0.0001f)
        {
            projectedForward = forward;
        }

        return projectedForward.normalized;
    }

    /// <summary>
    /// 滚动阻力的逻辑实现
    /// </summary>
    private void ApplyRollingResistance(Vector3 forwardAxis, float currentForwardSpeed)
    {
        float speedAbs = Mathf.Abs(currentForwardSpeed);
        // 1. 如果没动，就不产生阻力（防止在斜坡上因为阻力产生奇怪的位移）
        if (Mathf.Abs(currentForwardSpeed) < 0.01f) return;

        // 2. 计算滚阻系数 (取决于地面材质和SO配置)
        // float rollingCoeff = _groundContact.FrictionCoefficient * rollingResistanceScale;

        float rollingCoeff = (_groundContact.HasHit ? _groundContact.FrictionCoefficient : fallbackGroundFrictionCoefficient) * rollingResistanceScale;
        rollingCoeff = Mathf.Min(rollingCoeff, maxRollingResistanceCoefficient);

        // 3. 计算阻力大小：F = Crr * m * g
        float gravity = Mathf.Abs(Physics.gravity.y);
        float resistanceMag = rollingCoeff * tankMoveData.Mass * gravity;

        // 4. 施加力：方向与当前前进轴相反 (-forwardAxis)，并乘以上速度的方向
        Vector3 rollingForce = -forwardAxis * resistanceMag * Mathf.Sign(currentForwardSpeed);

        tankRigidbody.AddForce(rollingForce, ForceMode.Force);
    }

    private void ApplyTurnRollMoment(float turnInput, float turnRollFactor, float steeringResistanceTorque)
    {
        if (Mathf.Abs(turnInput) < 0.001f || tankRigidbody == null)
        {
            return;
        }

        if (turnRollFactor <= 0f)
        {
            return;
        }

        float speedInfluence = Mathf.Clamp01(Mathf.Abs(_cachedForwardSpeed) / Mathf.Max(1f, tankMoveData.MoveMaxSpeed));
        float rollTorque = steeringResistanceTorque * turnRollFactor * speedInfluence;

        // 负号用于让转向时车体向外侧压倾；若实际朝向相反，可在 Inspector 调整曲线或切换符号。
        tankRigidbody.AddRelativeTorque(Vector3.forward * (-turnInput) * rollTorque, ForceMode.Force);
    }
}