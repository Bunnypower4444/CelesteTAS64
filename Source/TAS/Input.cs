
namespace Celeste64.TAS;

public static class Input
{
    public static InputState CurrentState { get; private set; }
    public static InputState PreviousState { get; private set; }

    internal static void BindControls()
    {
        BindStick(Controls.Move, Actions.MoveX, Actions.MoveY, 0f, 0f);
        BindStick(Controls.Menu, Actions.MenuUp, Actions.MenuDown, Actions.MenuLeft, Actions.MenuRight);
        BindStick(Controls.Camera, Actions.CameraX, Actions.CameraY, 0f, 0f);

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
        Actions x, Actions y, float deadzoneX = 0, float deadzoneY = 0)
    {
        stick.Horizontal.Negative.Add(-1, deadzoneX, x);
        stick.Horizontal.Positive.Add(1, deadzoneX, x);
        stick.Vertical.Negative.Add(-1, deadzoneY, y);
        stick.Vertical.Positive.Add(1, deadzoneY, y);
    }

    internal static void Update(InputState state)
    {
        PreviousState = CurrentState;
        CurrentState = state;
    }

    public static float GetInputValue(Actions inputType)
        => GetInputValue(inputType, CurrentState);
    public static float GetInputValue(Actions inputType, InputState state)
    {
        if (state.Actions.HasFlag(inputType))
        {
            return inputType switch
            {
                // Idk, there probably is a better implementation for sticks...
                Actions.MoveX => state.Move.X,
                Actions.MoveY => state.Move.Y,
                Actions.CameraX => state.Camera.X,
                Actions.CameraY => state.Camera.Y,
                _ => 1
            };
        }
        return 0;
    }

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

    public static VirtualButton Add(this VirtualButton button, int sign, float deadzone, params Actions[] inputs)
    {
        foreach (var type in inputs)
        {
            button.Bindings.Add(new TASAxisBinding(type, sign, deadzone));
        }

        return button;
    }

    public static VirtualButton Add(this VirtualButton button,
        VirtualButton.ConditionFn condition, int sign, float deadzone, params Actions[] inputs)
    {
        foreach (var type in inputs)
        {
            button.Bindings.Add(new TASAxisBinding(type, sign, deadzone) { Enabled = condition });
        }

        return button;
    }
}