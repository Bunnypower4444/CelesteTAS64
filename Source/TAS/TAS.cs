
using System.Text;
using System.Text.RegularExpressions;

namespace Celeste64.TAS;

public partial class TAS(List<TAS.InputRecord> inputs)
{
    public record struct InputRecord(InputState State, int Frames)
    {
        public override readonly string ToString()
            => State.Actions == Actions.None ?
                Frames.ToString() :
                (Frames + "," + State.ToString());
    };

    private readonly List<InputRecord> inputs = inputs;
    private int currentFrame, inputIndex, framesToNext = inputs.Count > 0 ? inputs[0].Frames : 0;

    public int CurrentFrame => currentFrame;
    public InputRecord CurrentInput => inputIndex >= inputs.Count ? default : inputs[inputIndex];

    public bool Finished => inputIndex >= inputs.Count;

    public void AdvanceFrame()
    {
        if (Finished)
            return;
        
        currentFrame++;
        // Skip over any inputs with 0 frames
        do
        {
            framesToNext--;

            if (framesToNext <= 0) {
                inputIndex++;
                framesToNext = CurrentInput.Frames;
            }
        } while (framesToNext <= 0 && !Finished);
    }

    public void Reset()
    {
        currentFrame = 0;
        inputIndex = 0;
        framesToNext = inputs.Count > 0 ? inputs[0].Frames : 0;
    }

    public void Write(InputState state)
    {
        // if we're at the end, append it to the end
        if (Finished)
        {
            // extend the previous input by 1 frame if they are the same
            //  (and make sure there even is one first)
            if (inputIndex > 0 && state == inputs[inputIndex - 1].State)
            {
                inputs[inputIndex - 1] = inputs[inputIndex - 1] with
                    { Frames = inputs[inputIndex - 1].Frames + 1 };
            }
            else
            {
                inputs.Add(new(state, 1));
                inputIndex++;
            }
        }

        // overwrite previous things
        else
        {
            // if the current input is the same as the new input, don't do anything
            if (CurrentInput.State == state)
                return;

            // before
            if (framesToNext >= CurrentInput.Frames)
            {
                inputs[inputIndex] = CurrentInput with
                    { Frames = CurrentInput.Frames - 1 };

                if (!CombinePrevious(state))
                    inputs.Insert(inputIndex, new(state, 1));
                
                framesToNext = 1;
            }
            // after
            else if (framesToNext <= 1)
            {
                inputs[inputIndex] = CurrentInput with
                    { Frames = CurrentInput.Frames - 1 };
                
                if (!CombineNext(state))
                {
                    inputs.Insert(inputIndex + 1, new(state, 1));
                    framesToNext = 1;
                }

                inputIndex++;
            }
            // middle
            else
            {
                inputs[inputIndex] = CurrentInput with
                    { Frames = CurrentInput.Frames - framesToNext };
                
                inputs.Insert(inputIndex + 1, new(state, 1));
                inputs.Insert(inputIndex + 2, new(CurrentInput.State, framesToNext - 1));
                inputIndex++;
                framesToNext = 1;
            }
        }
    }

    public void Insert(InputState state)
    {
        // handle empty case
        if (inputs.Count <= 0)
        {
            inputs.Add(new(state, 1));
            framesToNext = 1;
            return;
        }

        // find where to insert based on if it is at the start, middle or end of an input
        // start
        if (framesToNext >= CurrentInput.Frames)
        {
            framesToNext = 1;
            if (!CombinePrevious(state))
            {
                inputIndex--;
                if (!CombineNext(state))
                    inputs.Insert(++inputIndex, new(state, 1));
                else
                    inputIndex++;
            }
        }
        // end
        else if (framesToNext == 1)
        {
            framesToNext = 1;
            inputIndex++;
            if (!CombinePrevious(state))
            {
                inputIndex--;
                if (!CombineNext(state))
                    inputs.Insert(++inputIndex, new(state, 1));
                else
                    inputIndex++;
            }
        }
        // middle
        else
        {
            if (state == CurrentInput.State)
            {
                inputs[inputIndex] = CurrentInput with
                    { Frames = CurrentInput.Frames + 1 };
                framesToNext++;
                return;
            }
            
            inputs[inputIndex] = CurrentInput with
                { Frames = CurrentInput.Frames - framesToNext };
            
            inputs.Insert(inputIndex + 1, new(state, 1));
            inputs.Insert(inputIndex + 2, new(CurrentInput.State, framesToNext));
            inputIndex++;
            framesToNext = 1;
        }
    }

    private bool CombinePrevious(InputState state)
    {
        if (inputIndex <= 0 || inputs[inputIndex - 1].State != state)
            return false;

        inputIndex--;
        inputs[inputIndex] = CurrentInput with
            { Frames = CurrentInput.Frames + 1 };
        return true;
    }

    private bool CombineNext(InputState state)
    {
        if (inputIndex >= inputs.Count - 1 || inputs[inputIndex + 1].State != state)
            return false;

        var nextIndex = inputIndex + 1;
        inputs[nextIndex] = inputs[nextIndex] with
            { Frames = inputs[nextIndex].Frames + 1 };

        framesToNext = inputs[nextIndex].Frames;
        return true;
    }

    public static TAS LoadFile(string path)
    {
        if (!File.Exists(path))
            throw new FileLoadException("File does not exist: " + path);

        List<InputRecord> inputs = [];
        int lineNumber = 1;
        foreach (var line in File.ReadAllLines(path))
        {
            try {
                var record = ParseLine(line);
                if (record.Frames > 0)
                    inputs.Add(record);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"Error on line {lineNumber} of {Path.GetFileName(path)}: " + e.Message);
            }
            lineNumber++;
        }

        return new TAS(inputs);
    }

    public static InputRecord ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return new(default, -1);

        // comment
        // TODO: add labels
        if (line.TrimStart().StartsWith('#'))
            return new(default, -1);

        // Make it so multiple whitespaces in a row turn into one space
        var reduceWhitespaces = ExtraWhitespace().Replace(line.Trim(), " ");
        var tokens = TokenSeparator().Split(reduceWhitespaces);
        // If set, the next token will be the vector input for this action
        Actions vectorInputAction = Actions.None;
        int numFrames = -1;
        Vec2 move = Vec2.Zero, camera = Vec2.Zero;
        Actions actions = Actions.None;
        foreach (var token in tokens)
        {
            // first token is num frames
            if (numFrames == -1)
            {
                if (int.TryParse(token, out var result)) {
                    numFrames = result;
                    continue;
                }
                else
                    throw new ArgumentException("Frame count is not an integer");
            }

            // Parse a vector: angle or x_component [space] y_component
            if (vectorInputAction != Actions.None)
            {
                float x, y;
                var components = token.Split(' ');
                if (components.Length <= 0)
                    throw new ArgumentException("Expected a vector for action " + vectorInputAction.GetAbbreviation());
                
                // angle
                if (components.Length == 1)
                {
                    if (float.TryParse(components[0], out var result))
                    {
                        var vec = Calc.AngleToVector(result * Calc.DegToRad);
                        x = vec.X;
                        y = vec.Y;
                    }
                    else
                        throw new ArgumentException("Vector angle is not a valid number");
                }
                // x [space] y
                else
                {
                    if (float.TryParse(components[0], out var result))
                        x = Calc.Clamp(result, -1, 1);
                    else
                        throw new ArgumentException("Vector X-component is not a valid number");

                    if (float.TryParse(components[1], out result))
                        y = Calc.Clamp(result, -1, 1);
                    else
                        throw new ArgumentException("Vector Y-component is not a valid number");
                }

                if (vectorInputAction.Has(Actions.Move))
                    move = new(x, y);
                if (vectorInputAction.Has(Actions.Camera))
                    camera = new(x, y);

                vectorInputAction = Actions.None;
            }
            // 
            else
            {
                var newActions = ActionsHelper.GetActionFromChar(token);
                actions |= newActions;

                if (newActions == Actions.None)
                    Log.Warning("Character is not a valid action: " + token);

                if (newActions.Has(Actions.Move | Actions.Camera))
                    vectorInputAction = newActions;
            }
        }

        return new InputRecord(new InputState(actions, move, camera), numFrames);
    }


    [GeneratedRegex("\\s\\s+")]
    private static partial Regex ExtraWhitespace();

    [GeneratedRegex("\\s?,\\s?")]
    private static partial Regex TokenSeparator();

    public override string ToString()
    {
        StringBuilder s = new();
        bool first = true;
        foreach (var input in inputs)
        {
            if (!first)
                s.Append('\n');
            else
                first = false;
            s.Append(input.ToString());
        }
        return s.ToString();
    }
}