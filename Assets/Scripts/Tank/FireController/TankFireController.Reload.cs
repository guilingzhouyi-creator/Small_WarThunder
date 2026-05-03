using System.Collections;
using UnityEngine;

public partial class TankFireController : MonoBehaviour
{
    private bool _isReloading = false;
    private float _currentReloadTime = 0f;
    private float _lastReloadWarningTime = 0f;
    private float _pendingReloadPenaltyTime = 0f;

    private void InitializeReloadState()
    {
        _isReloading = false;
        _currentReloadTime = 0f;
        _lastReloadWarningTime = 0f;
        _pendingReloadPenaltyTime = 0f;
        OnReloadStatusChanged?.Invoke(0f);
    }

    private void HandleFireInput()
    {
        if (MIddleInputingController.Instance == null || _MainGunWeaponData == null)
        {
            return;
        }

        if (UIManager.Instance != null && UIManager.Instance.IsGameplayControlLocked)
        {
            return;
        }

        if (MIddleInputingController.Instance.IsFirePressed() && !IsReloading)
        {
            if (SpawnCannonBall())
            {
                StartCoroutine(ReloadCannon());
            }
        }
        else if (MIddleInputingController.Instance.IsFirePressed() && IsReloading)
        {
            if (Time.time - _lastReloadWarningTime >= 1f)
            {
                _lastReloadWarningTime = Time.time;
                Debug.Log("正在装填中, 无法发射！");
            }
        }
    }

    private IEnumerator ReloadCannon()
    {
        _isReloading = true;
        _pendingReloadPenaltyTime = 0f;
        PlayReloadAudio();

        float baseTime = _MainGunWeaponData != null ? _MainGunWeaponData.ReloadTime : 5f;
        bool useReadyRack = _tank != null && _tank.HasReadyAmmo();

        float reloadDuration = useReadyRack ? baseTime : baseTime * 2.0f;

        float maxReloadDuration = GetMaxReloadDuration();
        float timer = 0f;

        while (timer < reloadDuration)
        {
            if (_pendingReloadPenaltyTime > 0f)
            {
                // 切换下一发弹药只影响“下一次准备装填”的种类，但在装填中切换会额外增加装填时间。
                // 这个加时会被最大装填时间上限限制，防止玩家无限拖长装填。
                reloadDuration = Mathf.Min(reloadDuration + _pendingReloadPenaltyTime, maxReloadDuration);
                _pendingReloadPenaltyTime = 0f;
            }

            timer += Time.deltaTime;
            _currentReloadTime = Mathf.Max(0, reloadDuration - timer);
            OnReloadStatusChanged?.Invoke(_currentReloadTime);
            yield return null;
        }

        if (_tank != null)
        {
            _tank.CommitNextAmmoTypeToCurrent();
        }

        _isReloading = false;
        _currentReloadTime = 0f;
        OnReloadStatusChanged?.Invoke(0f);
    }

    /// <summary>
    /// 仅在装填过程中调用：给当前这次装填叠加延迟。
    /// 每切换一次弹药，增加 1 秒，但不会超过武器数据里配置的最大装填上限。
    /// </summary>
    private void AddReloadTimePenalty(float additionalSeconds)
    {
        if (!_isReloading)
        {
            return;
        }

        _pendingReloadPenaltyTime += Mathf.Max(0f, additionalSeconds);
    }

    /// <summary>
    /// 返回本武器允许的最大装填时长。
    /// 如果没有单独配置 MaxReloadTime，则使用 ReloadTime 作为上限。
    /// </summary>
    private float GetMaxReloadDuration()
    {
        if (_MainGunWeaponData == null)
        {
            return 0f;
        }

        float reloadTime = Mathf.Max(0f, _MainGunWeaponData.ReloadTime);
        float maxReloadTime = Mathf.Max(0f, _MainGunWeaponData.MaxReloadTime);

        return maxReloadTime > 0f ? Mathf.Max(reloadTime, maxReloadTime) : reloadTime;
    }
}