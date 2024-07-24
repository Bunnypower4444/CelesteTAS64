
using FosterInput = Foster.Framework.Input;

namespace Celeste64.TAS;

public static class Manager
{
    public const string VersionString = "CelesteTAS 64 v0.2.0";

    public static readonly Dictionary<string, TAS> TASes = [];
    public static TAS? CurrentTAS { get; private set; }

    private static bool initialized = false;
    private static bool menuOpen = false;
    private static readonly Batcher batch = new();

    private static bool paused = true;
    private static bool recording = false;

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

        if (FosterInput.Keyboard.Pressed(Keys.F5))
            SetMenuOpen(!menuOpen);

        if (CurrentTAS != null)
        {
            if (FosterInput.Keyboard.Pressed(Keys.Space))
                paused = !paused;
            if (FosterInput.Keyboard.Pressed(Keys.Backspace))
                CurrentTAS.Reset();
        }

        if (menuOpen)
            optionsMenu.Update();
        // Update the tas
        // This actually doesn't do anything this frame, its for the next frame
        // Foster updates inputs before updating the Game
        else if (CurrentTAS != null && !CurrentTAS.Finished)
        {
            // Left bracket = frame advance
            if (!paused || FosterInput.Keyboard.Pressed(Keys.LeftBracket))
            {
                Input.Update(CurrentTAS.CurrentInput.State);
                CurrentTAS.AdvanceFrame();
            }
            // Only freeze game if it hasn't started yet
            else if (CurrentTAS.CurrentFrame > 0)
                return false;
        }
        else
            Input.Update(default);

        return true;
    }

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

    public static void Render(Target target)
    {
        batch.SetSampler(new TextureSampler(TextureFilter.Linear, TextureWrap.ClampToEdge, TextureWrap.ClampToEdge));
        var bounds = new Rect(0, 0, target.Width, target.Height);

        // pause menu
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
}