using NNewUIFramework;
using UnityEngine;


/// <summary>
/// 炮塔旋转控制器：负责根据当前瞄准点计算炮塔和炮管的旋转，并应用旋转到对应的 Transform 上。
/// </summary>
public partial class TankWeaponController : MonoBehaviour
{





    private void RotateHardware()
    {
        if (turret == null || barrel == null || turretData == null) return;

        if (NewUIManager.instance.IsGameplayControlLocked)
        {
            return;
        }

        bool isAimMode = NewUIManager.instance.IsAimMode;
        if (isAimMode)
        {
            RotateHardwareByMouseDelta();
            return;
        }

        float currentYawSpeed = turretData.TankRotationSpeed;

        // --- B. 炮塔水平旋转 (Yaw) ---
        Vector3 targetDirection = (_currentAimPoint - turret.position).normalized;
        Vector3 localTargetDirection = transform.InverseTransformDirection(targetDirection);
        localTargetDirection.y = 0; // 锁定在底盘水平面

        Quaternion targetTurretRotation = Quaternion.LookRotation(localTargetDirection, Vector3.up);
        turret.localRotation = Quaternion.RotateTowards(turret.localRotation, targetTurretRotation, currentYawSpeed * Time.deltaTime);

        // --- C. 炮管垂直俯仰 (Pitch) ---
        Vector3 targetInTurretSpace = turret.InverseTransformPoint(_currentAimPoint);
        float horizontalDistance = new Vector2(targetInTurretSpace.x, targetInTurretSpace.z).magnitude;

        // 原始物理计算角度 (Unity 中抬头通常为负)
        float rawAngle = -Mathf.Atan2(targetInTurretSpace.y, horizontalDistance) * Mathf.Rad2Deg;

        float realMaxElevation = GetMinimumBarrelPitch();
        float realMaxDepression = GetMaximumBarrelPitch();

        float range = realMaxDepression - realMaxElevation;
        if (Mathf.Abs(range) < 0.0001f)
        {
            range = 0.0001f;
        }

        float normalizedAngle = Mathf.Clamp01((rawAngle - realMaxElevation) / range);
        if (float.IsNaN(normalizedAngle) || float.IsInfinity(normalizedAngle))
        {
            normalizedAngle = 0f;
        }

        // 应用曲线映射
        float pitchCurveValue = turretData.pitchSensitivityCurve.Evaluate(normalizedAngle);
        if (float.IsNaN(pitchCurveValue) || float.IsInfinity(pitchCurveValue))
        {
            pitchCurveValue = 0f;
        }

        float finalTargetPitch = realMaxElevation + (pitchCurveValue * range);
        if (float.IsNaN(finalTargetPitch) || float.IsInfinity(finalTargetPitch))
        {
            finalTargetPitch = realMaxElevation;
        }

        float resolvedTargetPitch = ResolveSelfBarrelCollision(finalTargetPitch, out float rotationSpeedMultiplier);
        if (float.IsNaN(resolvedTargetPitch) || float.IsInfinity(resolvedTargetPitch))
        {
            resolvedTargetPitch = realMaxElevation;
        }
        if (float.IsNaN(rotationSpeedMultiplier) || float.IsInfinity(rotationSpeedMultiplier) || rotationSpeedMultiplier < 0f)
        {
            rotationSpeedMultiplier = 1f;
        }

        // 应用旋转 (限制在硬性物理范围内)
        Quaternion targetBarrelRotation = Quaternion.Euler(resolvedTargetPitch, 0, 0);// 注意：这里是局部旋转，所以只修改 X 轴
        barrel.localRotation = Quaternion.RotateTowards(barrel.localRotation, targetBarrelRotation, turretData.TankerGunRotationSpeed * rotationSpeedMultiplier * Time.deltaTime);
    }

    private void RotateHardwareByMouseDelta()
    {
        MIddleInputingController inputController = MIddleInputingController.Instance;
        if (inputController == null)
        {
            return;
        }

        Vector2 mouseDelta = inputController.GetMouseDelta();

        float zoomModifier = Camera.main.fieldOfView / 60f; // 根据当前FOV调整灵敏度

        float currentYaw = turret.localEulerAngles.y;
        float targetYaw = currentYaw + (mouseDelta.x * aimYawMouseSensitivity * zoomModifier);
        Quaternion targetTurretRotation = Quaternion.Euler(0f, targetYaw, 0f);
        turret.localRotation = Quaternion.RotateTowards(
            turret.localRotation,
            targetTurretRotation,
            turretData.TankRotationSpeed * Time.deltaTime
        );

        float currentPitch = NormalizeSignedAngle(barrel.localEulerAngles.x);
        float targetPitch = currentPitch - (mouseDelta.y * aimPitchMouseSensitivity * zoomModifier);
        targetPitch = Mathf.Clamp(targetPitch, GetMinimumBarrelPitch(), GetMaximumBarrelPitch());

        float resolvedTargetPitch = ResolveSelfBarrelCollision(targetPitch, out float rotationSpeedMultiplier);
        Quaternion targetBarrelRotation = Quaternion.Euler(resolvedTargetPitch, 0f, 0f);
        barrel.localRotation = Quaternion.RotateTowards(
            barrel.localRotation,
            targetBarrelRotation,
            turretData.TankerGunRotationSpeed * rotationSpeedMultiplier * Time.deltaTime
        );
    }

    private static float NormalizeSignedAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }


    //返回炮管最小仰角，给炮管旋转控制器使用
    private float GetMinimumBarrelPitch()
    {
        return turretData != null ? -turretData.TankMaxElevationAngle : 0f;
    }

    //返回炮管最大仰角，给炮管旋转控制器使用
    private float GetMaximumBarrelPitch()
    {
        return turretData != null ? turretData.TankMaxDepressionAngle : 0f;
    }
}