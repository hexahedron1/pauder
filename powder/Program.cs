global using powder;
using System.Diagnostics.Contracts;
using System.Reflection;
using SDL3;

namespace powder;

public static class Program {
    const int width = 120;
    const int height = 80;
    const int scale = 6;
    const int winWidth = 1080;
    const int winHeight = 720;
    private static IntPtr window;
    private static IntPtr renderer;
    public static List<Pixel> pixels = [];
    public static List<(int, int)> updates = [];
    public static List<Material> materials = [];
    public static bool[,] collision = new bool[width, height];
    private static int cx = 0;
    private static int cy = 0;
    private static Font RegularFont;
    private static bool picker = false;
    public static int selected = 0;
    private static void ExtractResource(string name, string path) {
        Log($"Extracting {name} to {path}", "Resources");
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        if (resource is null) {
            Log($"Could not find resource: {name}", "Resources", LogSeverity.Warning);
            return;
        }
        using var file = new FileStream(path, FileMode.Create, FileAccess.Write);
        resource.CopyTo(file);
    }

    public enum LogSeverity {
        Info,
        Debug,
        Warning,
        Error,
        Fatal
    }

    private static bool _firstLog = true;
    public static bool SaveLog { get; set; } = true;
    public static void Main(string[] args) {
        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events)) {
            SDL.LogError(SDL.LogCategory.System, $"Failed to init SDL: {SDL.GetError()}");
            Log($"Init failed: {SDL.GetError()}", "SDL", LogSeverity.Fatal);
            SDL.Quit();
            return;
        }
        Log("Initialized", "SDL");
        string configFolder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "pauder");
        foreach (string[] path in new string[][] {
                     [],
                     ["fonts"],
                     ["icons"]
                 }) {
            string finalPath = path.Aggregate(configFolder, Path.Join);
            if (!Directory.Exists(finalPath)) Directory.CreateDirectory(finalPath);
            Log($"Restored {finalPath}", "Resources");
        }
        #region Materials
        materials = [
            new("Sand", new NoiseBrush(0xBFA68B,
                0xC3AF91,
                0xB79073,
                0xAB947D,
                0xA38B6F,
                0xB8966D,
                0xC4A98C,
                0xC9B6A1,
                0xA38460,
                0xA57E5D,
                0xBBA285,
                0xAF916A,
                0xB9A388,
                0xB9A287,
                0xB79A71
            ))
        ];
        #endregion
        Log("Loading regular font", "Resources");
        ExtractResource("powder.Resources.vga-font.png", Path.Join(configFolder, "fonts", "vga.png"));
        IntPtr regularSurface = Image.Load(Path.Join(configFolder, "fonts", "vga.png"));
        RegularFont = new(regularSurface, 16);
        for (int i = 0; i < 0xDF; i++) {
            char c = char.ConvertFromUtf32(i + 0x20)[0];
            RegularFont.Glyphs.Add(c, new SDL.FRect {
                X = i % 16 * 8,
                Y = (int)Math.Floor(i / 16.0) * 16,
                W = 8,
                H = 16
            });
        }
        window = SDL.CreateWindow("Pauder", winWidth, winHeight, 0);
        renderer = SDL.CreateRenderer(window, null);
        float xo = (winWidth - width * scale) / 2f;
        float yo = (winHeight - height * scale) / 2f;
        DateTime lastFrame = DateTime.Now;
        bool lmb = false;
        bool rmb = false;
        while (true) {
            Thread.Sleep(1000/60);
            while (SDL.PollEvent(out var e)) {
                switch ((SDL.EventType)e.Type) {
                    case SDL.EventType.WindowCloseRequested:
                    case SDL.EventType.Quit:
                        SDL.DestroyRenderer(renderer);
                        SDL.DestroyWindow(window);
                        return;
                    case SDL.EventType.MouseMotion:
                        SDL.GetMouseState(out float mx, out float my);
                        cx = (int)((mx - xo)/scale);
                        cy = (int)((my - yo)/scale);
                        break;
                    case SDL.EventType.MouseButtonDown:
                        var a = SDL.GetMouseState(out _, out _);
                        if ((a & SDL.MouseButtonFlags.Left) != 0) lmb = true;
                        if ((a & SDL.MouseButtonFlags.Right) != 0) rmb = true;
                        break;
                    case SDL.EventType.MouseButtonUp:
                        a = SDL.GetMouseState(out _, out _);
                        if ((a & SDL.MouseButtonFlags.Left) == 0) lmb = false;
                        if ((a & SDL.MouseButtonFlags.Right) == 0) rmb = false;
                        break;
                }
            }

            #region Simulation

            if (cx is >= 0 and < width && cy is >= 0 and < height) {
                if (lmb && !collision[cx, cy]) {
                    Pixel piksel = new(materials[selected], cx, cy);
                    pixels.Add(piksel);
                    collision[cx, cy] = true;
                }
            }

            #endregion

            #region Rendering
            SDL.SetRenderDrawColor(renderer, 0, 0, 0, 0);
            SDL.RenderClear(renderer);
            SDL.FRect rect = new SDL.FRect {
                X = xo - 1,
                Y = yo - 1,
                W = width*scale + 2, H = height*scale + 2
            };
            SDL.SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.RenderRect(renderer,  rect);
            rect = new SDL.FRect {
                X = xo,
                Y = yo,
                W = width*scale, H = height*scale
            };
            SDL.SetRenderDrawColor(renderer, 18, 20, 34, 255);
            SDL.RenderFillRect(renderer,  rect);
            foreach (var piggsel in pixels) {
                var (r, g, b) = UnpackColor(piggsel.Material.Brush.GetColor(piggsel));
                SDL.SetRenderDrawColor(renderer, r, g, b, 255);
                rect = new SDL.FRect {
                    X = xo + piggsel.X*scale,
                    Y = yo + piggsel.Y*scale,
                    W = scale, H = scale
                };
                SDL.RenderFillRect(renderer,  rect);
            }
            if (cx is >= 0 and < width && cy is >= 0 and < height) {
                SDL.HideCursor();
                rect = new SDL.FRect {
                    X = xo + cx*scale,
                    Y = yo + cy*scale,
                    W = scale, H = scale
                };
                SDL.SetRenderDrawColor(renderer, 255, 255, 255, 255);
                SDL.RenderRect(renderer,  rect);
            } else {
                SDL.ShowCursor();
            }
            int fps = 1000/Math.Max((DateTime.Now - lastFrame).Milliseconds, 1);
            RegularFont.DrawText(renderer, $"{fps} FPS", 8, 8, 0xFFFFFF);
            RegularFont.DrawText(renderer, $"{materials[selected].Name}", 8, 20, 0x00FFFF);
            RegularFont.DrawText(renderer, $"{pixels.Count} particles", 8, 32, 0x00FFFF);
            SDL.RenderPresent(renderer);
            lastFrame = DateTime.Now;
            #endregion
        }
    }
    public static void Log(string message, string? source = null, LogSeverity severity = LogSeverity.Info,
        Exception? e = null) {
        List<string> logLines = [];
        Console.WriteLine($"\e[0;{
            severity switch {
                LogSeverity.Warning => "93mWARN ",
                LogSeverity.Error => "91mERROR ",
                LogSeverity.Fatal => "95mFATAL ",
                LogSeverity.Debug => "94mDEBUG ",
                _ => "0m"
            },-10}\e[0m \e[1m{source?.Ellipsis(16),16}\e[0m: {message}");
        logLines.Add($"{
            severity switch {
                LogSeverity.Warning => "WARN",
                LogSeverity.Error => "ERROR",
                LogSeverity.Fatal => "FATAL",
                LogSeverity.Debug => "DEBUG",
                _ => ""
            },-10} {source?.Ellipsis(16),16}: {message}");
        if (e != null) {
            Console.WriteLine($" \e[91m*\e[0m {e.GetType().Name}: {e.Message}");
            string? trace = e.StackTrace;
            if (trace != null) {
                var traceLines = trace.Split('\n');
                Console.WriteLine(traceLines.Length <= 32 ? trace : "Stack trace too long to show");
                logLines.AddRange(traceLines);
            }
        }
        if (!SaveLog) return;
        string folder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "COIL");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        if (_firstLog) {
            File.Delete(Path.Join(folder, "latest-log.txt"));
            _firstLog = false;
        }
        File.AppendAllLines(Path.Join(folder, "latest-log.txt"), logLines);
    }
    [Pure]
    public static (byte, byte, byte) UnpackColor(int color) {
        return ((byte)(color >> 16 & 0xff), (byte)(color >> 8 & 0xff), (byte)(color & 0xff));
    }
    private static string Ellipsis(this string text, int maxLength) {
        return text.Length <= maxLength
            ? text
            : text[..(maxLength - 3)] + "...";
    }
}