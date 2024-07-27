
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

public static class ActionsExt
{
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
        Actions.Confirm   => "A",
        Actions.Cancel    => "B",
        Actions.MenuUp    => "U",
        Actions.MenuDown  => "D",
        Actions.MenuLeft  => "L",
        Actions.MenuRight => "R",
        _ => throw new NotImplementedException("Must be a single action")
    };
}

public enum StickActions { Move, Camera }
public enum StickAxis { X, Y };

public record struct InputState(Actions Actions, Vec2 Move, Vec2 Camera)
{
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