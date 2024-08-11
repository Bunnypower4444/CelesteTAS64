
using System.Runtime.CompilerServices;
using FosterInput = Foster.Framework.Input;

namespace Celeste64.TAS;

public static class Manager
{
    public const string VersionString = "CelesteTAS 64 v0.1.0";

    public static readonly Dictionary<string, TAS> TASes = [];
    public static TAS? CurrentTAS { get; private set; }
    public static bool Running => !menuOpen && CurrentTAS != null && ((CurrentTAS.CurrentFrame > 0 && !CurrentTAS.Finished) || recording);

    private static bool initialized = false;
    private static bool menuOpen = false;
    private static readonly Batcher batch = new();

    private static bool paused = true;
    private static bool recording = false;
    public enum RecordingMode { Overwrite, Insert };
    private static RecordingMode recordingMode = RecordingMode.Insert;
    private static InputState recordingInput;
    // Used as stick inputs for recording mode
    private static readonly VirtualStick recordingMoveStick = new("Move", VirtualAxis.Overlaps.TakeNewer, 0.35f);
    private static readonly VirtualStick recordingCameraStick = new("Camera", VirtualAxis.Overlaps.TakeNewer, 0.35f);
    private static float stickAngle, stickMagnitude;
    private static readonly NumberInput stickAngleInput = new(0,
        () => stickAngle,
        value =>
        {
            if (stickAngle != value)
            {
                stickAngle = value;
                UpdateStickInput();
            }
        }
    ) { Min = -360, Max = 360, Label = "Angle" };
    private static readonly NumberInput stickMagnitudeInput = new(0,
        () => stickMagnitude,
        value =>
        {
            if (stickMagnitude != value)
            {
                stickMagnitude = value;
                UpdateStickInput();
            }
        }
    ) { Min = 0, Max = 1, Label = "Magnitude" };
    private static StickActions? editingStickAction = null;
    private static int stickInputIndex = 0;

    private static void UpdateStickInput()
    {
        switch (editingStickAction)
        {
            case StickActions.Move:
                recordingInput.Move = Calc.AngleToVector(stickAngle * Calc.DegToRad, stickMagnitude);
                break;
            case StickActions.Camera:
                recordingInput.Camera = Calc.AngleToVector(-stickAngle * Calc.DegToRad, stickMagnitude);
                break;
        }
    }

    static Manager() 
    {
        recordingMoveStick.Horizontal.AddPositive(Keys.Right);
        recordingMoveStick.Horizontal.AddNegative(Keys.Left);
        recordingMoveStick.Vertical.AddPositive(Keys.Down);
        recordingMoveStick.Vertical.AddNegative(Keys.Up);

        recordingCameraStick.Horizontal.AddPositive(Keys.D);
        recordingCameraStick.Horizontal.AddNegative(Keys.A);
        recordingCameraStick.Vertical.AddPositive(Keys.S);
        recordingCameraStick.Vertical.AddNegative(Keys.W);
    }

    private static void SetMenuOpen(bool open)
    {
        menuOpen = open;
        if (!menuOpen)
        {
            optionsMenu.CloseSubMenus();
        }
    }

    public static void Start()
    {
        optionsMenu.Title = Loc.Str("TASOptionsTitle");
        optionsMenu.UpSound = Sfx.main_menu_roll_up;
        optionsMenu.DownSound = Sfx.main_menu_roll_down;
        optionsMenu.Add(new Menu.Submenu(Loc.Str("TASSelect"), optionsMenu, tasMenu));

        tasMenu.Title = Loc.Str("TASSelectTitle");
        tasMenu.UpSound = Sfx.main_menu_roll_up;
        tasMenu.DownSound = Sfx.main_menu_roll_down;

        LoadTASes();
        Input.BindControls();
    }

    // If the return value is false, freeze the game
    public static bool Update()
    {
        if (!initialized)
        {
            Start();
            initialized = true;
        }

        if (KeyPressed(Keys.F5))
            SetMenuOpen(!menuOpen);
        if (KeyPressed(Keys.F3))
            debugDraw = !debugDraw;

        if (menuOpen)
        {
            optionsMenu.Update();
            return false;
        }
        
        // Update the tas
        else if (CurrentTAS != null)
        {
            if (KeyPressed(Keys.Space))
                paused = !paused;
            if (KeyPressed(Keys.Tab))
                {recording = !recording;Log.Info(CurrentTAS.ToString());}
            if (KeyPressed(Keys.Backspace))
                CurrentTAS.Reset();
            
            // record inputs
            if (recording)
            {
                UpdateRecordingInput();
                if (!paused || KeyPressed(Keys.LeftBracket))
                {
                    Input.Update(recordingInput);
                    WriteToTAS(recordingInput);
                    CurrentTAS.AdvanceFrame();
                }
                else
                    return false;
            }

            // normal playback
            else if (!CurrentTAS.Finished)
            {
                // Left bracket = frame advance
                if (!paused || KeyPressed(Keys.LeftBracket))
                {
                    Input.Update(CurrentTAS.CurrentInput.State);
                    CurrentTAS.AdvanceFrame();
                }
                else
                {
                    // Only freeze game if it hasn't started yet
                    if (CurrentTAS.CurrentFrame > 0)
                        return false;
                    
                    Input.Update(default);
                }
            }

            // the TAS is done and we aren't recording, input nothing
            else
            {
                Input.Update(default);
            }
        }
        else
            Input.Update(default);

        return true;
    }

    private static void UpdateRecordingInput()
    {
        // normal recording
        if (!paused)
        {
            // TODO: be able to bind keys to these
            recordingInput = new();

            if (KeyDown(Keys.J))
                recordingInput.Actions |= Actions.Jump;
            if (KeyDown(Keys.K))
                recordingInput.Actions |= Actions.Jump2;
            if (KeyDown(Keys.X))
                recordingInput.Actions |= Actions.Dash;
            if (KeyDown(Keys.C))
                recordingInput.Actions |= Actions.Dash2;
            if (KeyDown(Keys.G))
                recordingInput.Actions |= Actions.Climb;
            if (KeyDown(Keys.P))
                recordingInput.Actions |= Actions.Pause;
            if (KeyDown(Keys.O))
                recordingInput.Actions |= Actions.Confirm;
            if (KeyDown(Keys.I))
                recordingInput.Actions |= Actions.Cancel;
            if (KeyDown(Keys.U))
                recordingInput.Actions |= Actions.MenuUp;
            if (KeyDown(Keys.D))
                recordingInput.Actions |= Actions.MenuDown;
            if (KeyDown(Keys.L))
                recordingInput.Actions |= Actions.MenuLeft;
            if (KeyDown(Keys.R))
                recordingInput.Actions |= Actions.MenuRight;

            // recordingInput.Move = GetMoveInput(recordingMoveStick.Value);
            recordingInput.Move = recordingMoveStick.Value;
            
            recordingInput.Camera = recordingCameraStick.Value;

            if (recordingInput.Move != Vec2.Zero)
                recordingInput.Actions |= Actions.Move;
            if (recordingInput.Camera != Vec2.Zero)
                recordingInput.Actions |= Actions.Camera;
        }
        // Frame stepping recording
        else
        {
            if (KeyPressed(Keys.M))
            {
                recordingInput.Actions |= Actions.Move;
                editingStickAction = editingStickAction != StickActions.Move ? StickActions.Move : null;
                if (editingStickAction == null)
                {
                    stickAngleInput.UnFocus();
                    stickMagnitudeInput.UnFocus();
                    
                    if (recordingInput.Move == Vec2.Zero)
                        recordingInput.Actions &= ~Actions.Move;
                }
                else
                {
                    if (recordingInput.Camera == Vec2.Zero)
                        recordingInput.Actions &= ~Actions.Camera;
                    
                    stickAngle = -recordingInput.Move.Angle() * Calc.RadToDeg;
                    if (stickAngle == float.NegativeZero)
                        stickAngle = 0;
                    stickMagnitude = recordingInput.Move.Length();
                    stickInputIndex = 0;
                    stickAngleInput.RefreshText();
                    stickAngleInput.Focus(true);
                    stickMagnitudeInput.RefreshText();
                    stickMagnitudeInput.UnFocus();
                }
            }
            else if (KeyPressed(Keys.E))
            {
                recordingInput.Actions |= Actions.Camera;
                editingStickAction = editingStickAction != StickActions.Camera ? StickActions.Camera : null;
                if (editingStickAction == null)
                {
                    stickAngleInput.UnFocus();
                    stickMagnitudeInput.UnFocus();
                    
                    if (recordingInput.Camera == Vec2.Zero)
                        recordingInput.Actions &= ~Actions.Camera;
                }
                else
                {
                    if (recordingInput.Move == Vec2.Zero)
                        recordingInput.Actions &= ~Actions.Move;
                    
                    stickAngle = -recordingInput.Camera.Angle() * Calc.RadToDeg;
                    if (stickAngle == float.NegativeZero)
                        stickAngle = 0;
                    stickMagnitude = recordingInput.Camera.Length();
                    stickInputIndex = 0;
                    stickAngleInput.RefreshText();
                    stickAngleInput.Focus(true);
                    stickMagnitudeInput.RefreshText();
                    stickMagnitudeInput.UnFocus();
                }
            }

            if (editingStickAction != null)
            {
                int step = 0;
                if (KeyPressed(Keys.Up))
                    step--;
                if (KeyPressed(Keys.Down))
                    step++;
                if (step != 0)
                {
                    stickInputIndex += step;
                    if (stickInputIndex < 0)
                        stickInputIndex = 1;
                    if (stickInputIndex > 1)
                        stickInputIndex = 0;
                    
                    if (stickInputIndex == 0)
                    {
                        stickAngleInput.Focus();
                        stickMagnitudeInput.UnFocus();
                    }
                    else if (stickInputIndex == 1)
                    {
                        stickAngleInput.UnFocus();
                        stickMagnitudeInput.Focus();
                    }
                }

                stickAngleInput.Update();
                stickMagnitudeInput.Update();
                return;
            }
            
            if (KeyPressed(Keys.J))
                recordingInput.Actions ^= Actions.Jump;
            if (KeyPressed(Keys.K))
                recordingInput.Actions ^= Actions.Jump2;
            if (KeyPressed(Keys.X))
                recordingInput.Actions ^= Actions.Dash;
            if (KeyPressed(Keys.C))
                recordingInput.Actions ^= Actions.Dash2;
            if (KeyPressed(Keys.G))
                recordingInput.Actions ^= Actions.Climb;
            if (KeyPressed(Keys.P))
                recordingInput.Actions ^= Actions.Pause;
            if (KeyPressed(Keys.O))
                recordingInput.Actions ^= Actions.Confirm;
            if (KeyPressed(Keys.I))
                recordingInput.Actions ^= Actions.Cancel;
            if (KeyPressed(Keys.U))
                recordingInput.Actions ^= Actions.MenuUp;
            if (KeyPressed(Keys.D))
                recordingInput.Actions ^= Actions.MenuDown;
            if (KeyPressed(Keys.L))
                recordingInput.Actions ^= Actions.MenuLeft;
            if (KeyPressed(Keys.R))
                recordingInput.Actions ^= Actions.MenuRight;
        }
    }

    private static Vec2 GetMoveInput(Vec2 stickInput)
    {
        if (Game.Instance.Scene is World world)
        {
            Vec2 forward, side;

            var cameraForward = (world.Camera.LookAt - world.CameraDestPos).Normalized().XY();
            if (cameraForward.X == 0 && cameraForward.Y == 0)
                forward = world.Get<Player>()?.Facing ?? new(0, 1);
            else
                forward = cameraForward.Normalized();
            side = Vec2.Transform(forward, Matrix3x2.CreateRotation(MathF.PI / 2));

            return forward * -stickInput.Y + side * -stickInput.X;
        }
        else
            return stickInput;
    }

    private static void WriteToTAS(InputState input)
    {
        if (CurrentTAS == null)
            return;
        
        switch (recordingMode)
        {
            case RecordingMode.Overwrite:
                CurrentTAS.Write(input);
                break;
            
            case RecordingMode.Insert:
                CurrentTAS.Insert(input);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool KeyPressed(Keys key)
        => FosterInput.Keyboard.Pressed(key);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool KeyDown(Keys key)
        => FosterInput.Keyboard.Down(key);

    public static void LoadTASes()
    {
        static string GetResourceName(string contentFolder, string path)
        {
            var fullname = Path.Join(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
            var relative = Path.GetRelativePath(contentFolder, fullname);
            var normalized = relative.Replace("\\", "/");
            return normalized;
        }

        List<Task> tasks = [];
		var tasPath = Path.Join(Assets.ContentPath, "..");
		foreach (var file in Directory.EnumerateFiles(tasPath, "*.tas", SearchOption.AllDirectories))
		{
			var name = GetResourceName(tasPath, file);
            tasks.Add(Task.Run(() =>
            {
                TASes.Add(name, TAS.LoadFile(file));
            }));
		}

        foreach (var task in tasks)
        {
            task.Wait();
        }

        foreach (var keyValuePair in TASes)
        {
            tasMenu.Add(new Menu.Option(keyValuePair.Key, () =>
            {
                SelectTAS(keyValuePair.Value);
                SetMenuOpen(false);
            }));
        }
    }

    public static void SelectTAS(TAS tas)
    {
        CurrentTAS = tas;
        paused = true;
        recording = false;
    }

    private static readonly Menu optionsMenu = new();

    private static readonly Menu tasMenu = new();
    private static bool debugDraw = false;

    public static void Render(Target target)
    {
        batch.SetSampler(new TextureSampler(TextureFilter.Linear, TextureWrap.ClampToEdge, TextureWrap.ClampToEdge));
        var bounds = new Rect(0, 0, target.Width, target.Height);

        if (paused && recording)
        {
            RenderRecordingUI(target);
            if (editingStickAction != null)
            {
                var height = 2.75f * Language.Current.SpriteFont.LineHeight;
                var pos = bounds.Center - height * 0.5f * Vec2.UnitY;

                batch.PushMatrix(Matrix3x2.CreateScale(0.75f) * Matrix3x2.CreateTranslation(pos));
                UI.Text(batch, editingStickAction.ToString() ?? "Stick", Vec2.Zero, new(0.5f, 0f), Color.Gray);
                batch.PopMatrix();

                pos.Y += 0.75f * Language.Current.SpriteFont.LineHeight;
                stickAngleInput.Draw(batch, pos, new(0.5f, 0));
                stickMagnitudeInput.Draw(batch, pos + Language.Current.SpriteFont.LineHeight * Vec2.UnitY, new(0.5f, 0));

                /* var textOffset = 0.375f * Language.Current.SpriteFont.LineHeight * Vec2.UnitY;
                batch.PushMatrix(Matrix3x2.CreateScale(0.75f) * Matrix3x2.CreateTranslation(
                    bounds.Center - textOffset
                ));
                UI.Text(batch, editingStickAction.ToString() ?? "Stick", Vec2.Zero, new(0.5f, 1f), Color.Gray);
                batch.PopMatrix();

                var pos = bounds.Center + textOffset;
                stickAngleInput.Draw(batch, pos - 2 * Game.RelativeScale * Vec2.UnitY, new(0.5f, 1));
                stickMagnitudeInput.Draw(batch, pos + 2 * Game.RelativeScale * Vec2.UnitY, new(0.5f, 0)); */
            }
        }

        if (debugDraw)
            RenderDebug(target);

        // options menu
        if (menuOpen)
        {
            batch.Rect(bounds, Color.Black * 0.70f);
            optionsMenu.Render(batch, bounds.Center);

            // Version number
            UI.Text(batch, VersionString, bounds.BottomRight + new Vec2(-4, -4) * Game.RelativeScale, new Vec2(1, 1), Color.White * 0.25f);
        }
        batch.Render(target);
        batch.Clear();
    }

    private static void RenderDebug(Target target)
    {
        if (Game.Instance.Scene is World world && world.Get<Player>() is {} player)
        {
            var at = new Vec2(target.Bounds.X + target.Bounds.Width * 0.6f, target.Bounds.Top) + new Vec2(-4, 0) * Game.RelativeScale;
            var lineHeight = Language.Current.SpriteFont.LineHeight;

            UI.Text(batch, $"X: {player.Position.X}", at, new(0, 0), 0xffa0a0);
            UI.Text(batch, $"Y: {player.Position.Y}", at + new Vec2(0, lineHeight), new(0, 0), 0xa0a0ff);
            UI.Text(batch, $"Z: {player.Position.Z}", at + new Vec2(0, lineHeight * 2), new(0, 0), 0xa0ffa0);
            UI.Text(batch, $"VX: {player.Velocity.X}", at + new Vec2(0, lineHeight * 3), new(0, 0), 0xffa0a0);
            UI.Text(batch, $"VY: {player.Velocity.Y}", at + new Vec2(0, lineHeight * 4), new(0, 0), 0xa0a0ff);
            UI.Text(batch, $"VZ: {player.Velocity.Z}", at + new Vec2(0, lineHeight * 5), new(0, 0), 0xa0ffa0);
            UI.Text(batch, $"Facing: {player.Facing.Angle() * Calc.RadToDeg}", at + new Vec2(0, lineHeight * 6), new(0, 0), Color.White);
        }
    }

    // renders the UI for recording inputs during frame stepping
    private static void RenderRecordingUI(Target target)
    {
        // Render a column of buttons
        static void RenderButtons(Vec2 pos, params Actions[] actions)
        {
            float height = 1.75f * Language.Current.SpriteFont.LineHeight + 2 * Game.RelativeScale + 4 * Game.RelativeScale;
            pos.Y -= (actions.Length - 1) * height * 0.5f;

            foreach (var action in actions)
            {
                Color color = recordingInput.Actions.HasFlag(action) ? 0x84FF54 : Color.LightGray;
                UI.Text(batch, action.ToString(), pos - 1 * Game.RelativeScale * Vec2.UnitY, new(0.5f, 1), color);

                batch.PushMatrix(Matrix3x2.CreateScale(0.75f) * Matrix3x2.CreateTranslation(pos + 1 * Game.RelativeScale * Vec2.UnitY));
                UI.Text(batch, action.GetAbbreviation(), Vec2.Zero, new(0.5f, 0), Color.White);
                batch.PopMatrix();

                pos.Y += height;
            }
        }

        // Render a column of sticks
        static void RenderStick(Vec2 pos, float stickSize, params StickActions[] actions)
        {
            // height from the text
            float textHeight = 1.75f * Language.Current.SpriteFont.LineHeight + 2 * Game.RelativeScale + 4 * Game.RelativeScale;
            // height from the stick graphic
            float stickHeight = stickSize + 4 * Game.RelativeScale;
            pos.Y -= actions.Length * (textHeight + stickHeight) * 0.5f;
            
            foreach (var action in actions)
            {
                var circleCenter = new Vec2(pos.X, pos.Y + stickSize * 0.5f + Game.RelativeScale);
                // Draw the stick graphic
                batch.PushMatrix(Matrix3x2.CreateScale(1.1f, 1.1f, circleCenter));
                batch.Circle(new Circle(circleCenter, stickSize * 0.5f), 12, 0x202020);
                batch.PopMatrix();
                batch.Circle(new Circle(circleCenter, stickSize * 0.5f), 12, 0x404040);

                batch.Circle(new Circle(circleCenter, 4 * Game.RelativeScale), 6, 0x202020);

                var input = recordingInput.GetStickInput(action);
                var inputPosition = circleCenter + input * stickSize * 0.5f;
                batch.Circle(new Circle(inputPosition, 4 * Game.RelativeScale), 6, Color.Red);

                RenderButtons(new(pos.X, pos.Y + stickHeight + textHeight * 0.5f), action.ToActions());
                pos.Y += textHeight + stickHeight;
            }
        }

        Vec2 pos = new(0.5f * target.Width / 8, target.Height * 0.75f);
        RenderStick(pos, 36 * Game.RelativeScale, StickActions.Move, StickActions.Camera);
        pos.X += target.Width / 8;
        RenderButtons(pos, Actions.Jump, Actions.Jump2);
        pos.X += target.Width / 8;
        RenderButtons(pos, Actions.Dash, Actions.Dash2);
        pos.X += target.Width / 8;
        RenderButtons(pos, Actions.Climb);
        pos.X += target.Width / 8;
        RenderButtons(pos, Actions.Pause, Actions.Confirm, Actions.Cancel);
        pos.X += target.Width / 8;
        RenderButtons(pos, Actions.MenuLeft);
        pos.X += target.Width / 8;
        RenderButtons(pos, Actions.MenuUp, Actions.MenuDown);
        pos.X += target.Width / 8;
        RenderButtons(pos, Actions.MenuRight);
    }
}