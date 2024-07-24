
using System.Text;

namespace Celeste64.TAS;

[Flags]
public enum Actions
{
    None = 0,
    MoveX = 1 << 0, MoveY = 1 << 1,
    Jump = 1 << 2, Jump2 = 1 << 3,
    Dash = 1 << 4, Dash2 = 1 << 5,
    CameraX = 1 << 6, CameraY = 1 << 7,
    Climb = 1 << 8,
    Pause = 1 << 9, Confirm = 1 << 10, Cancel = 1 << 11,
    MenuUp = 1 << 12, MenuDown = 1 << 13, MenuLeft = 1 << 14, MenuRight = 1 << 15
}

public static class ActionsExt
{
    public static string GetAbbreviation(this Actions actions) => actions switch
    {
        Actions.None => string.Empty,
        Actions.MoveX | Actions.MoveY => "M",
        Actions.MoveX => "M",
        Actions.MoveY => "M",
        Actions.Jump => "J",
        Actions.Jump2 => "K",
        Actions.Dash => "X",
        Actions.Dash2 => "C",
        Actions.CameraX | Actions.CameraY => "E",
        Actions.CameraX => "E",
        Actions.CameraY => "E",
        Actions.Climb => "G",
        Actions.Pause => "P",
        Actions.Confirm => "A",
        Actions.Cancel => "B",
        Actions.MenuUp => "U",
        Actions.MenuDown => "D",
        Actions.MenuLeft => "L",
        Actions.MenuRight => "R",
        _ => throw new NotImplementedException("Must be a single action")
    };
}

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
                if (action == Actions.MoveX || action == Actions.CameraY)
                    continue;
                if (!first)
                    s.Append(',');
                else
                    first = false;
                
                s.Append(action.GetAbbreviation());

                if (action == Actions.MoveY)
                    s.Append($",{Move.X} {Move.Y}");
                if (action == Actions.CameraY)
                    s.Append($",{Camera.X} {Camera.Y}");
            }
        }

        return s.ToString();
    }
}