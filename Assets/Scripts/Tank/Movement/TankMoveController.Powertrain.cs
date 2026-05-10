using NNewUIFramework;
using System;
using UnityEngine;

public partial class TankMoveController : MonoBehaviour
{
    public enum PowerDomain
    {
        Propulsion,
        Electrical
    }

    [Header("动力系统")]
    [SerializeField] private bool startWithEngineOn = false;
    [SerializeField] private bool startWithElectricalPower = true;
    [SerializeField] private bool linkElectricalPowerToEngine = true;
    [SerializeField, Min(0f)] private float engineStartupInputLockSeconds = 3.25f;

    public event Action<bool> OnEngineStateChanged;
    public event Action<PowerDomain, bool> OnPowerDomainStateChanged;

    private bool _isEngineOn;
    private bool _hasElectricalPower;
    private float _engineInputLockRemaining;

    public bool IsEngineOn => _isEngineOn;
    public bool HasPropulsionPower => _isEngineOn;
    public bool HasElectricalPower => _hasElectricalPower;
    public bool CanAcceptPropulsionInput => HasPropulsionPower && _engineInputLockRemaining <= 0f && (NewUIManager.instance == null || !NewUIManager.instance.IsGameplayControlLocked);

    private void Update()
    {
        PollPowerInput();
        UpdateEngineInputLock();
    }

    private void InitializePowerState()
    {
        _isEngineOn = startWithEngineOn;
        _hasElectricalPower = linkElectricalPowerToEngine ? _isEngineOn : startWithElectricalPower;
        _engineInputLockRemaining = 0f;
    }

    private void PollPowerInput()
    {
        MIddleInputingController inputController = MIddleInputingController.Instance;
        if (inputController == null)
        {
            return;
        }

        if (NewUIManager.instance.IsGameplayControlLocked)
        {
            return;
        }

        if (inputController.IsToggleEnginePressed())
        {
            SetEngineEnabled(!_isEngineOn);
        }
    }

    public bool IsPowerDomainEnabled(PowerDomain domain)
    {
        return domain switch
        {
            PowerDomain.Propulsion => HasPropulsionPower,
            PowerDomain.Electrical => HasElectricalPower,
            _ => false
        };
    }

    public void SetPowerDomainEnabled(PowerDomain domain, bool enabled)
    {
        switch (domain)
        {
            case PowerDomain.Propulsion:
                SetEngineEnabled(enabled);
                break;
            case PowerDomain.Electrical:
                SetElectricalPowerEnabled(enabled);
                break;
        }
    }

    public void SetEngineEnabled(bool enabled)
    {
        if (_isEngineOn == enabled)
        {
            return;
        }

        _isEngineOn = enabled;
        _engineInputLockRemaining = enabled ? ResolveEngineStartupInputLockSeconds() : 0f;
        OnEngineStateChanged?.Invoke(_isEngineOn);
        OnPowerDomainStateChanged?.Invoke(PowerDomain.Propulsion, _isEngineOn);

        if (linkElectricalPowerToEngine)
        {
            SetElectricalPowerInternal(_isEngineOn);
        }

        Debug.Log($"TankMoveController: 引擎已{(_isEngineOn ? "启动" : "关闭")}");
    }

    public void SetElectricalPowerEnabled(bool enabled)
    {
        if (linkElectricalPowerToEngine && !_isEngineOn && enabled)
        {
            Debug.LogWarning("TankMoveController: 当前电力与引擎联动，发动机关闭时无法单独开启电力。");
            return;
        }

        SetElectricalPowerInternal(enabled);
    }

    private void SetElectricalPowerInternal(bool enabled)
    {
        if (_hasElectricalPower == enabled)
        {
            return;
        }

        _hasElectricalPower = enabled;
        OnPowerDomainStateChanged?.Invoke(PowerDomain.Electrical, _hasElectricalPower);
    }

    private void UpdateEngineInputLock()
    {
        if (_engineInputLockRemaining <= 0f)
        {
            return;
        }

        _engineInputLockRemaining = Mathf.Max(0f, _engineInputLockRemaining - Time.deltaTime);
    }

    private float ResolveEngineStartupInputLockSeconds()
    {
        float startupDuration = GetStartupDuration();

        if (startupDuration > 0f)
        {
            return startupDuration;
        }

        return Mathf.Max(0f, engineStartupInputLockSeconds);
    }
}