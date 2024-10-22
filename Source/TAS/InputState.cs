
using System.Text;

namespace Celeste64.TAS;

[Flags]
public enum Actions
{
    None = 0,
    Move = 1 << 0,
    Jump = 1 << 1, Jump2 = 1 << 2,
    Dash = 1 << 3, Dash2 = 1 << 4,
    Camera = 1 << 5,
    Climb = 1 << 6,
    Pause = 1 << 7, Confirm = 1 << 8, Cancel = 1 << 9,
    MenuUp = 1 << 10, MenuDown = 1 << 11, MenuLeft = 1 << 12, MenuRight = 1 << 13
}

public static class ActionsHelper
{
    public static Actions ToActions(this StickActions actions) => actions switch
    {
        StickActions.Move => Actions.Move,
        StickActions.Camera => Actions.Camera,
        _ => Actions.None
    };

    public static string GetAbbreviation(this Actions actions) => actions switch
    {
        Actions.None      => string.Empty,
        Actions.Move      => "M",
        Actions.Jump      => "J",
        Actions.Jump2     => "K",
        Actions.Dash      => "X",
        Actions.Dash2     => "C",
        Actions.Camera    => "E",
        Actions.Climb     => "G",
        Actions.Pause     => "P",
        Actions.Confirm   => "O",
        Actions.Cancel    => "I",
        Actions.MenuUp    => "U",
        Actions.MenuDown  => "D",
        Actions.MenuLeft  => "L",
        Actions.MenuRight => "R",
        _ => throw new NotImplementedException("Must be a single action")
    };

    public static Actions GetActionFromChar(string action) => action.ToUpper() switch
    {
        "M" => Actions.Move,
        "J" => Actions.Jump,
        "K" => Actions.Jump2,
        "X" => Actions.Dash,
        "C" => Actions.Dash2,
        "E" => Actions.Camera,
        "G" => Actions.Climb,
        "P" => Actions.Pause,
        "O" => Actions.Confirm,
        "I" => Actions.Cancel,
        "U" => Actions.MenuUp,
        "D" => Actions.MenuDown,
        "L" => Actions.MenuLeft,
        "R" => Actions.MenuRight,
        _ => Actions.None
    };
}

public enum StickActions { Move, Camera }
public enum StickAxis { X, Y };

public record struct InputState(Actions Actions, Vec2 Move, Vec2 Camera)
{
    public readonly Vec2 GetStickInput(StickActions action) => action switch
    {
        StickActions.Move => Move,
        StickActions.Camera => Camera,
        _ => Vec2.Zero
    };

    public override readonly string ToString()
    {
        var s = new StringBuilder();
        var actions = Enum.GetValues<Actions>();
        bool first = true;
        foreach (var action in actions)
        {
            if (Actions.Has(action) && action != Actions.None)
            {
                if (!first)
                    s.Append(',');
                else
                    first = false;
                
                s.Append(action.GetAbbreviation());

                if (action == Actions.Move)
                    s.Append($",{Move.X} {Move.Y}");
                if (action == Actions.Camera)
                    s.Append($",{Camera.X} {Camera.Y}");
            }
        }

        return s.ToString();
    }
}