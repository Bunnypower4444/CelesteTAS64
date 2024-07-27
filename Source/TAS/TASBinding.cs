
using IButtonBinding = Foster.Framework.VirtualButton.IBinding;

namespace Celeste64.TAS;

public record TASButtonBinding(Actions InputType) : IButtonBinding
{
    public bool IsPressed { get {
        return Input.GetInputValue(InputType) &&
            !Input.GetInputValue(InputType, Input.PreviousState);
    } }

    public bool IsDown => Input.GetInputValue(InputType);

    public bool IsReleased { get {
        return !Input.GetInputValue(InputType) &&
            Input.GetInputValue(InputType, Input.PreviousState);
    } }

    public float Value => IsDown ? 1 : 0;

    public float ValueNoDeadzone => IsDown ? 1 : 0;

    public VirtualButton.ConditionFn? Enabled { get; set; }
}

public record TASAxisBinding(StickAxis Axis, StickActions InputType, int Sign, float Deadzone) : IButtonBinding
{
    public bool IsPressed { get {
        return GetValue(Input.GetInputValue(InputType, Axis)) > 0 &&
            GetValue(Input.GetInputValue(InputType, Axis, Input.PreviousState)) <= 0;
    } }

    public bool IsDown => GetValue(Input.GetInputValue(InputType, Axis)) > 0;

    public bool IsReleased { get {
        return GetValue(Input.GetInputValue(InputType, Axis)) <= 0 &&
            GetValue(Input.GetInputValue(InputType, Axis, Input.PreviousState)) > 0;
    } }

    public float Value => GetValue(Input.GetInputValue(InputType, Axis));

    public float ValueNoDeadzone => Input.GetInputValue(InputType, Axis);

    public VirtualButton.ConditionFn? Enabled { get; set; }

    private float GetValue(float value)
        => Calc.ClampedMap(value, Sign * Deadzone, Sign);
}