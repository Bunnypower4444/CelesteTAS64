
namespace Celeste64.TAS;

public static class Input
{
    public static InputState CurrentState { get; private set; }
    public static InputState PreviousState { get; private set; }

    internal static void BindControls()
    {
        BindStick(Controls.Move, StickActions.Move, 0f, 0f);
        BindStick(Controls.Menu, Actions.MenuUp, Actions.MenuDown, Actions.MenuLeft, Actions.MenuRight);
        BindStick(Controls.Camera, StickActions.Camera, 0f, 0f);

        BindButton(Controls.Jump, Actions.Jump, Actions.Jump2);
        BindButton(Controls.Dash, Actions.Dash, Actions.Dash2);
        BindButton(Controls.Climb, Actions.Climb);
        BindButton(Controls.Confirm, Actions.Confirm);
        BindButton(Controls.Cancel, Actions.Cancel);
        BindButton(Controls.Pause, Actions.Pause);
    }

    private static void BindButton(VirtualButton button, params Actions[] inputTypes)
        => button.Add(inputTypes);

    private static void BindStick(VirtualStick stick,
        Actions up, Actions down, Actions left, Actions right)
    {
        stick.Vertical.Negative.Add(up);
        stick.Vertical.Positive.Add(down);
        stick.Horizontal.Negative.Add(left);
        stick.Horizontal.Negative.Add(right);
    }

    private static void BindStick(VirtualStick stick,
        StickActions actions, float deadzoneX = 0, float deadzoneY = 0)
    {
        stick.Horizontal.Negative.Add(-1, deadzoneX, StickAxis.X, actions);
        stick.Horizontal.Positive.Add(1, deadzoneX, StickAxis.X, actions);
        stick.Vertical.Negative.Add(-1, deadzoneY, StickAxis.Y, actions);
        stick.Vertical.Positive.Add(1, deadzoneY, StickAxis.Y, actions);
    }

    internal static void Update(InputState state)
    {
        PreviousState = CurrentState;
        CurrentState = state;
        
        // Update all buttons
        Controls.Move.Update();
        Controls.Camera.Update();
        Controls.Menu.Update();

        Controls.Jump.Update();
        Controls.Dash.Update();
        Controls.Climb.Update();
        Controls.Pause.Update();
        Controls.Confirm.Update();
        Controls.Cancel.Update();
    }

    public static bool GetInputValue(Actions inputType)
        => GetInputValue(inputType, CurrentState);
    public static bool GetInputValue(Actions inputType, InputState state)
    {
        return state.Actions.HasFlag(inputType);
    }

    public static float GetInputValue(StickActions inputType, StickAxis axis)
        => GetInputValue(inputType, axis, CurrentState);
    public static float GetInputValue(StickActions inputType, StickAxis axis, InputState state)
    {
        var vector = state.GetStickInput(inputType);

        if (axis == StickAxis.X)
            return vector.X;
        else
            return vector.Y;
    }
}

public static class InputHelper
{
    public static VirtualButton Add(this VirtualButton button, params Actions[] inputs)
    {
        foreach (var type in inputs)
        {
            button.Bindings.Add(new TASButtonBinding(type));
        }

        return button;
    }

    public static VirtualButton Add(this VirtualButton button,
        VirtualButton.ConditionFn condition, params Actions[] inputs)
    {
        foreach (var type in inputs)
        {
            button.Bindings.Add(new TASButtonBinding(type) { Enabled = condition });
        }

        return button;
    }

    public static VirtualButton Add(this VirtualButton button, int sign, float deadzone, StickAxis axis, params StickActions[] inputs)
    {
        foreach (var type in inputs)
        {
            button.Bindings.Add(new TASAxisBinding(axis, type, sign, deadzone));
        }

        return button;
    }

    public static VirtualButton Add(this VirtualButton button,
        VirtualButton.ConditionFn condition, int sign, float deadzone, StickAxis axis, params StickActions[] inputs)
    {
        foreach (var type in inputs)
        {
            button.Bindings.Add(new TASAxisBinding(axis, type, sign, deadzone) { Enabled = condition });
        }

        return button;
    }

    public static void Update(this VirtualButton button)
    {
        // reflection is kinda funny
        var method = typeof(VirtualButton).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method?.Invoke(button, null);
    }

    public static void Update(this VirtualStick stick)
    {
        stick.Horizontal.Positive.Update();
        stick.Horizontal.Negative.Update();
        stick.Vertical.Positive.Update();
        stick.Vertical.Negative.Update();
    }
}