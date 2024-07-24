
using IButtonBinding = Foster.Framework.VirtualButton.IBinding;

namespace Celeste64.TAS;

public record TASButtonBinding(Actions InputType) : IButtonBinding
{
    public bool IsPressed { get {
        return Input.GetInputValue(InputType) > 0 &&
            Input.GetInputValue(InputType, Input.PreviousState) <= 0;
    } }

    public bool IsDown => Input.GetInputValue(InputType) > 0;

    public bool IsReleased { get {
        return Input.GetInputValue(InputType) <= 0 &&
            Input.GetInputValue(InputType, Input.PreviousState) > 0;
    } }

    public float Value => IsDown ? 1 : 0;

    public float ValueNoDeadzone => IsDown ? 1 : 0;

    public VirtualButton.ConditionFn? Enabled { get; set; }
}

public record TASAxisBinding(Actions InputType, int Sign, float Deadzone) : IButtonBinding
{
    public bool IsPressed { get {
        return GetValue(Input.GetInputValue(InputType)) > 0 &&
            GetValue(Input.GetInputValue(InputType, Input.PreviousState)) <= 0;
    } }

    public bool IsDown => GetValue(Input.GetInputValue(InputType)) > 0;

    public bool IsReleased { get {
        return GetValue(Input.GetInputValue(InputType)) <= 0 &&
            GetValue(Input.GetInputValue(InputType, Input.PreviousState)) > 0;
    } }

    public float Value => GetValue(Input.GetInputValue(InputType));

    public float ValueNoDeadzone => Input.GetInputValue(InputType);

    public VirtualButton.ConditionFn? Enabled { get; set; }

    private float GetValue(float value)
        => Calc.ClampedMap(value, Sign * Deadzone, Sign);
}