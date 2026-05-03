using UnityEngine;

public partial class TankFireController : MonoBehaviour
{
    // 炮口参考方向相关 - 用于解决自由观察回归中断时的炮弹反向问题。
    private Vector3 _referenceBarrelDirection;
    private bool _isInFreeLookMode = false;

    private void InitializeDirectionState()
    {
        if (TankWeaponController.Instance != null)
        {
            _referenceBarrelDirection = TankWeaponController.Instance.GetBarrelForward();
        }
        else if (_firePoint != null)
        {
            _referenceBarrelDirection = _firePoint.forward;
        }
        else
        {
            _referenceBarrelDirection = transform.forward;
        }
    }

    private void UpdateReferenceBarrelDirection()
    {
        if (TankWeaponController.Instance == null)
        {
            return;
        }

        bool currentFreeLookState = TankWeaponController.Instance.IsFreeLooking;
        bool isRecovering = TankWeaponController.Instance.IsRecovering;

        if (currentFreeLookState && !_isInFreeLookMode)
        {
            _isInFreeLookMode = true;
            _referenceBarrelDirection = TankWeaponController.Instance.GetBarrelForward();
        }
        else if (!currentFreeLookState && !isRecovering && _isInFreeLookMode)
        {
            _isInFreeLookMode = false;
            _referenceBarrelDirection = TankWeaponController.Instance.GetBarrelForward();
        }
        else if (!currentFreeLookState && !isRecovering && !_isInFreeLookMode)
        {
            _referenceBarrelDirection = TankWeaponController.Instance.GetBarrelForward();
        }
    }

    private Vector3 ResolveFireDirection(Transform firePoint)
    {
        if (TankWeaponController.Instance == null)
        {
            return firePoint != null ? firePoint.forward : transform.forward;
        }

        if (TankWeaponController.Instance.IsFreeLooking || TankWeaponController.Instance.IsRecovering)
        {
            return _referenceBarrelDirection;
        }

        Vector3 fireDirection = TankWeaponController.Instance.GetBarrelForward();
        if (fireDirection == Vector3.zero)
        {
            fireDirection = firePoint != null ? firePoint.forward : transform.forward;
            Debug.LogWarning("TankFireController: 获取炮管前向失败，使用备用方向发射！");
        }

        return fireDirection;
    }
}