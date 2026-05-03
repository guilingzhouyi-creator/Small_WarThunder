using UnityEngine;


/// <summary>
/// 自由视角控制器：负责在玩家按下自由视角键时，允许玩家通过鼠标移动来调整炮塔和炮管的朝向，同时保持当前瞄准点不变。
/// </summary>
public partial class TankWeaponController : MonoBehaviour
{
    private void OnFreeLook()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsGameplayControlLocked)
        {
            _isFreeLooking = false;
            _isRecovering = false;
            _freeLookOrbitalFollow = null;
            _recoverElapsed = 0f;
            return;
        }

        if (UIManager.Instance != null && UIManager.Instance.IsAimMode)
        {
            _isFreeLooking = false;
            _isRecovering = false;
            _freeLookOrbitalFollow = null;
            _recoverElapsed = 0f;
            return;
        }

        MIddleInputingController inputController = MIddleInputingController.Instance;
        if (inputController == null)
        {
            return;
        }

        bool isCKeyPressed = inputController.IsFreeLookPressed();

        if (isCKeyPressed && !_isFreeLooking)
        {
            _freeLookOrbitalFollow = ResolveActiveOrbitalFollow();
            _isFreeLooking = true;
            _isRecovering = false;
            _recoverElapsed = 0f;
            _horizontalVelocity = 0f;
            _verticalVelocity = 0f;
            if (_freeLookOrbitalFollow != null)
            {
                _savedHorizontalAxis = _freeLookOrbitalFollow.HorizontalAxis.Value;
                _savedVerticalAxis = _freeLookOrbitalFollow.VerticalAxis.Value;
            }
        }

        if (!isCKeyPressed && _isFreeLooking)
        {
            _isFreeLooking = false;
            _isRecovering = _freeLookOrbitalFollow != null;
            _recoverElapsed = 0f;
            _horizontalVelocity = 0f;
            _verticalVelocity = 0f;
        }

        if (_freeLookOrbitalFollow != null && _isRecovering)
        {
            _recoverElapsed += Time.deltaTime;
            float currentHorizontal = _freeLookOrbitalFollow.HorizontalAxis.Value;
            float currentVertical = _freeLookOrbitalFollow.VerticalAxis.Value;

            // 使用 SmoothDamp 平滑逼近记录点
            _freeLookOrbitalFollow.HorizontalAxis.Value = Mathf.SmoothDampAngle(currentHorizontal, _savedHorizontalAxis, ref _horizontalVelocity, snapSmoothTime);
            _freeLookOrbitalFollow.VerticalAxis.Value = Mathf.SmoothDamp(currentVertical, _savedVerticalAxis, ref _verticalVelocity, snapSmoothTime);

            float horizontalDelta = Mathf.Abs(Mathf.DeltaAngle(currentHorizontal, _savedHorizontalAxis));
            float verticalDelta = Mathf.Abs(currentVertical - _savedVerticalAxis);

            // 如果距离已经非常接近，结束回归状态
            if (horizontalDelta < 0.1f && verticalDelta < 0.1f)
            {
                _isRecovering = false;
                _freeLookOrbitalFollow = null;
                _recoverElapsed = 0f;
            }
            else if (_recoverElapsed >= maxRecoverDuration)
            {
                // 兜底：避免因输入/相机系统干扰导致回归状态卡住。
                _isRecovering = false;
                _freeLookOrbitalFollow = null;
                _recoverElapsed = 0f;
            }
        }
        else if (_isRecovering)
        {
            _isRecovering = false;
            _recoverElapsed = 0f;
        }
    }
}