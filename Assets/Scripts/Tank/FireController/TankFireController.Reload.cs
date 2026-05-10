using NNewUIFramework;
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

        if (NewUIManager.instance != null && NewUIManager.instance.IsGameplayControlLocked)
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
                Debug.Log("正在重新装填中，无法开火！");
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

    private void AddReloadTimePenalty(float additionalSeconds)
    {
        if (!_isReloading)
        {
            return;
        }

        _pendingReloadPenaltyTime += Mathf.Max(0f, additionalSeconds);
    }


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
