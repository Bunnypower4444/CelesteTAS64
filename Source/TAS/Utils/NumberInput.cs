
using System.Text;
using FosterInput = Foster.Framework.Input;

namespace Celeste64.TAS;

public class NumberInput(float value, Func<float> get, Action<float> set)
{
    public StringBuilder Text = new(value.ToString());
    public float Value { get => get(); set => set(value); }
    public float Min = float.NegativeInfinity, Max = float.PositiveInfinity;
    public bool IntegersOnly;
    public bool IsValid { get; private set; } = true;
    public int CursorIndex = value.ToString().Length;
    public string Label = string.Empty;
    public bool Focused => focused;
    private bool focused = false;

    public void Update()
    {
        if (!Focused)
            return;

        if (FosterInput.Keyboard.PressedOrRepeated(Keys.Left) && CursorIndex > 0)
            CursorIndex--;
        if (FosterInput.Keyboard.PressedOrRepeated(Keys.Right) && CursorIndex < Text.Length)
            CursorIndex++;
        if (FosterInput.Keyboard.PressedOrRepeated(Keys.Backspace) && CursorIndex > 0)
            Text.Remove(--CursorIndex, 1);

        var hasDecimal = Text.ToString()[..CursorIndex].Contains('.');
        var newText = string.Concat(FosterInput.Keyboard.Text.ToString().Where((ch, index) => 
        {
            // only allow negative at the beginning
            if (ch == '-' && index == 0 && CursorIndex == 0)
                return true;
            
            // only allow one decimal
            else if (ch == '.' && !IntegersOnly && !hasDecimal)
            {
                hasDecimal = true;
                return true;
            }

            else if (char.IsNumber(ch))
                return true;

            return false;
        }));

        Text.Insert(CursorIndex, newText);
        CursorIndex += newText.Length;
        if (float.TryParse(Text.ToString(), out var result) &&
            (!IntegersOnly || result % 1 == 0))
        {;
            if (result < Min || result > Max)
            {
                Value = Calc.Clamp(result, Min, Max);
                RefreshText();
            }
            else
                Value = result;
            IsValid = true;
        }
        else
            IsValid = false;
    }

    public void Focus(bool force = false)
    {
        if (focused && !force)
            return;

        focused = true;
        CursorIndex = Text.Length;
    }

    public void UnFocus()
    {
        if (!focused)
            return;

        focused = false;
        // Set the text to Value.ToString() to make sure it is valid and to remove stuff like
        //  extra zeros
        RefreshText();
        CursorIndex = Text.Length;
        IsValid = true;
    }

    public void RefreshText()
    {
        Text.Clear();
        Text.Append(Value);
        CursorIndex = Math.Min(CursorIndex, Text.Length);
    }

    public void Draw(Batcher batch, Vec2 at, Vec2 justify)
    {
        var text = !string.IsNullOrEmpty(Label) ? Label + ": " + Text.ToString() : Text.ToString();
        Color col = focused ? (Time.BetweenInterval(0.1f) ? 0x84FF54 : 0xFCFF59) : Color.White;
        UI.Text(batch, text, at, justify, IsValid ? col : 0xff8080);

        if (focused && Time.BetweenInterval(0.4))
        {
            int cursorIndex = !string.IsNullOrEmpty(Label) ? Label.Length + 2 + CursorIndex : CursorIndex;
            Vec2 size = Language.Current.SpriteFont.SizeOf(text);
            Vec2 cursorPos = at - justify * size + Language.Current.SpriteFont.WidthOf(text.AsSpan()[..cursorIndex]) * Vec2.UnitX;
            batch.Line(cursorPos, cursorPos + Language.Current.SpriteFont.LineHeight * Vec2.UnitY, 2, Color.Black);
        }
    }
}