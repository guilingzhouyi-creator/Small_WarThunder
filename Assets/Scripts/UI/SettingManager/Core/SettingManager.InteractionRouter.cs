using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

internal sealed class SettingInteractionRouter : IDisposable
{
    private readonly InputAction _nextTabAction;
    private readonly InputAction _previousTabAction;
    private readonly List<ButtonBinding> _tabButtonBindings = new List<ButtonBinding>();
    private readonly List<ButtonBinding> _applyButtonBindings = new List<ButtonBinding>();
    private readonly List<ButtonBinding> _cancelButtonBindings = new List<ButtonBinding>();
    private readonly Action _onNextTabRequested;
    private readonly Action _onPreviousTabRequested;
    private bool _inputEnabled;

    public SettingInteractionRouter(Action onNextTabRequested, Action onPreviousTabRequested)
    {
        _onNextTabRequested = onNextTabRequested;
        _onPreviousTabRequested = onPreviousTabRequested;

        _nextTabAction = new InputAction("SettingsNextTab", InputActionType.Button);
        _nextTabAction.AddBinding("<Keyboard>/tab");
        _nextTabAction.AddBinding("<Keyboard>/rightArrow");
        _nextTabAction.performed += OnNextTabPerformed;

        _previousTabAction = new InputAction("SettingsPreviousTab", InputActionType.Button);
        _previousTabAction.AddBinding("<Keyboard>/leftArrow");
        _previousTabAction.performed += OnPreviousTabPerformed;
    }

    public void BindTabButtons(IReadOnlyList<Button> buttons, Action<int> onTabSelected)
    {
        UnbindButtons(_tabButtonBindings);

        if (buttons == null || onTabSelected == null)
        {
            return;
        }

        for (int index = 0; index < buttons.Count; index++)
        {
            Button button = buttons[index];
            if (button == null)
            {
                continue;
            }

            int capturedIndex = index;
            UnityAction action = () => onTabSelected(capturedIndex);
            button.onClick.AddListener(action);
            _tabButtonBindings.Add(new ButtonBinding(button, action));
        }
    }

    public void BindApplyButtons(IReadOnlyList<Button> buttons, UnityAction onApplyPressed)
    {
        RebindButtons(_applyButtonBindings, buttons, onApplyPressed);
    }

    public void BindCancelButtons(IReadOnlyList<Button> buttons, UnityAction onCancelPressed)
    {
        RebindButtons(_cancelButtonBindings, buttons, onCancelPressed);
    }

    public void EnableInput()
    {
        if (_inputEnabled)
        {
            return;
        }

        _nextTabAction.Enable();
        _previousTabAction.Enable();
        _inputEnabled = true;
    }

    public void DisableInput()
    {
        if (!_inputEnabled)
        {
            return;
        }

        _nextTabAction.Disable();
        _previousTabAction.Disable();
        _inputEnabled = false;
    }

    public void Dispose()
    {
        DisableInput();
        UnbindButtons(_tabButtonBindings);
        UnbindButtons(_applyButtonBindings);
        UnbindButtons(_cancelButtonBindings);

        _nextTabAction.performed -= OnNextTabPerformed;
        _previousTabAction.performed -= OnPreviousTabPerformed;
        _nextTabAction.Dispose();
        _previousTabAction.Dispose();
    }

    private void OnNextTabPerformed(InputAction.CallbackContext context)
    {
        _onNextTabRequested?.Invoke();
    }

    private void OnPreviousTabPerformed(InputAction.CallbackContext context)
    {
        _onPreviousTabRequested?.Invoke();
    }

    private static void RebindButtons(List<ButtonBinding> bindings, IReadOnlyList<Button> buttons, UnityAction action)
    {
        UnbindButtons(bindings);

        if (buttons == null || action == null)
        {
            return;
        }

        for (int index = 0; index < buttons.Count; index++)
        {
            Button button = buttons[index];
            if (button == null)
            {
                continue;
            }

            button.onClick.AddListener(action);
            bindings.Add(new ButtonBinding(button, action));
        }
    }

    private static void UnbindButtons(List<ButtonBinding> bindings)
    {
        for (int index = 0; index < bindings.Count; index++)
        {
            ButtonBinding binding = bindings[index];
            if (binding.Button != null)
            {
                binding.Button.onClick.RemoveListener(binding.Action);
            }
        }

        bindings.Clear();
    }

    private readonly struct ButtonBinding
    {
        public ButtonBinding(Button button, UnityAction action)
        {
            Button = button;
            Action = action;
        }

        public Button Button { get; }

        public UnityAction Action { get; }
    }
}
