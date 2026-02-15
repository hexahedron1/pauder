using System.Diagnostics.Contracts;
using System.Reflection;
using SDL3;

namespace powder;

public static class Program {
    const int width = 120;
    const int height = 80;
    const int scale = 6;
    const int xbezel = 128;
    const int ybezel = 256;
    const int winWidth = width * scale + xbezel;
    const int winHeight = height * scale + ybezel;
    private static IntPtr window;
    private static IntPtr renderer;
    public static List<Pixel> pixels = [];
    public static List<(int, int)> updates = [];
    public static List<Material> materials = [];
    public static bool[,] collision = new bool[width, height];
    private static int cx;
    private static int cy;
    private static Font? RegularFont;
    private static bool picker = false;
    public static int selected = 1;
    public static Random GlobalRandom = new();
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
    public static bool SaveLog { get; set; } = false;
    public static double t;

    public static void WakeUp(int x, int y) {
        for (int dx = x - 1; dx <= x + 1; dx++) {
            for (int dy = y - 1; dy <= y + 1; dy++) {
                if (!IsOutOfBounds(dx, dy) && collision[dx, dy] && !updates.Contains((dx, dy)))
                    updates.Add((dx, dy)); 
            }
        }
    }

    public static Pixel? FindPixel(int x, int y) => pixels.FirstOrDefault(pixel => pixel.X == x && pixel.Y == y);

    public static (int, int)[] Neighbors(int x, int y) {
        List<(int, int)> neighbors = [];
        for (int dx = x - 1; dx <= x + 1; dx++) {
            for (int dy = y - 1; dy <= y + 1; dy++) {
                if (!(dx == x && dy == y) && Occupied(dx, dy))
                    neighbors.Add((dx, dy));
            }
        }
        return neighbors.ToArray();
    }
    public static bool DebugDraw;

    public static bool Occupied(int x, int y) {
        if (IsOutOfBounds(x, y)) return true;
        return collision[x, y];
    }

    public static bool IsOutOfBounds(int x, int y) {
        return x < 0 || x >= width || y < 0 || y >= height;
    }
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
            new("Sand", new NoiseBrush( 
                0xBFA68B,
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
            )),
            new("Gravel", new NoiseBrush(
                0xAAAAAA,
                0x919290,
                0x7B7E76,
                0x8F8C90,
                0xB2B4B1,
                0x727668,
                0x757774,
                0x898A85,
                0x94969F,
                0x525252,
                0x838581
            ), density: 1.8)
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
        DateTime lastTick = DateTime.Now;
        bool lmb = false;
        bool rmb = false;
        bool paused = false;
        bool step = false;
        double dt = 0;
        while (true) {
            Thread.Sleep(16);
            while (SDL.PollEvent(out var e)) {
                switch ((SDL.EventType)e.Type) {
                    case SDL.EventType.WindowCloseRequested:
                    case SDL.EventType.Quit:
                        SDL.DestroyRenderer(renderer);
                        SDL.DestroyWindow(window);
                        return;
                    case SDL.EventType.MouseMotion:
                        SDL.GetMouseState(out float mx, out float my);
                        cx = (int)((mx - xo) / scale);
                        cy = (int)((my - yo) / scale);
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
                    case SDL.EventType.MouseWheel:
                        selected = (int)Math.Min(Math.Max(selected + e.Wheel.Y, 0), materials.Count - 1);
                        Log($"{e.Wheel.Y}", severity: LogSeverity.Debug);
                        break;
                    case SDL.EventType.KeyDown:
                        if (e.Key.Key == SDL.Keycode.Space) paused = !paused;
                        if (e.Key.Key == SDL.Keycode.Z) step = true;
                        if (e.Key.Key == SDL.Keycode.D) DebugDraw = !DebugDraw;
                        if (e.Key.Key == SDL.Keycode.C) {
                            foreach (var p in pixels.ToArray()) {
                                collision[p.X, p.Y] = false;
                                pixels.Remove(p);
                            }
                        }

                        if (e.Key.Key == SDL.Keycode.LAlt) picker = true;
                        break;
                    case SDL.EventType.KeyUp:
                        if (e.Key.Key == SDL.Keycode.LAlt) picker = false;
                        break;
                }
            }

            #region Simulation

            if (cx is >= 0 and < width && cy is >= 0 and < height) {
                if (lmb && !collision[cx, cy]) {
                    Pixel piksel = new(materials[selected], cx, cy);
                    pixels.Add(piksel);
                    collision[cx, cy] = true;
                    WakeUp(cx, cy);
                }
                if (rmb && collision[cx, cy]) {
                    Pixel? picksell = FindPixel(cx, cy);
                    if (picksell != null) {
                        pixels.Remove(picksell);
                        collision[cx, cy] = false;
                        WakeUp(cx, cy);
                    }
                }
            }

            if (!paused || step) {
                foreach (var (ux, uy) in updates.ToArray()) {
                    var piskel = FindPixel(ux, uy);
                    var neighbors = Neighbors(ux, uy);
                    if (piskel is { } pigsel) {
                        if (neighbors.All(x =>
                                (IsOutOfBounds(x.Item1, x.Item1) ||
                                 FindPixel(x.Item1, x.Item2)?.Material == piskel.Material) && neighbors.Length == 8))
                            updates.Remove((ux, uy));
                        if (!collision[pigsel.X, pigsel.Y]) collision[pigsel.X, pigsel.Y] = true;
                        if (pigsel.State == State.Solid) {
                            if (!Occupied(ux - 1, uy + 1) || !Occupied(ux, uy + 1) || !Occupied(ux + 1, uy + 1)) {
                                WakeUp(ux, uy);
                                if (pigsel.Grip > 0 && neighbors.Length > 0) pigsel.Grip -= dt;
                                else {
                                    if ((!Occupied(ux, uy + 1) || FindPixel(ux, uy + 1)?.Moving != false) && !IsOutOfBounds(ux, uy + 1)) {
                                        pigsel.Y += 1;
                                        collision[ux, uy] = false;
                                        collision[ux, uy + 1] = true;
                                    }
                                    else if ((!Occupied(ux - 1, uy + 1) || FindPixel(ux - 1, uy + 1)?.Moving != false) && !IsOutOfBounds(ux - 1, uy + 1)) {
                                        pigsel.Y += 1;
                                        pigsel.X -= 1;
                                        collision[ux, uy] = false;
                                        collision[ux - 1, uy + 1] = true;
                                    }
                                    else if ((!Occupied(ux + 1, uy + 1) || FindPixel(ux + 1, uy + 1)?.Moving != false) && !IsOutOfBounds(ux + 1, uy + 1)) {
                                        pigsel.Y += 1;
                                        pigsel.X += 1;
                                        collision[ux, uy] = false;
                                        collision[ux + 1, uy + 1] = true;
                                    }

                                    WakeUp(pigsel.X, pigsel.Y);
                                }
                            } else if (Occupied(ux, uy + 1) && !IsOutOfBounds(ux, uy + 1) &&
                                       FindPixel(ux, uy + 1)!.Material.Density < pigsel.Material.Density) {
                                Pixel below = FindPixel(ux, uy + 1)!;
                                below.Y -= 1;
                                pigsel.Y += 1;
                                WakeUp(ux, uy);
                                WakeUp(ux, uy + 1);
                            }
                            pigsel.Moving = pigsel.X != ux || pigsel.Y != uy;
                        }
                    }
                    else updates.Remove((ux, uy));
                }

                step = false;
            }

            #endregion

            int ms = (DateTime.Now - lastTick).Milliseconds;
            dt = ms / 1000.0;
            t += dt;
            #region Rendering
            SDL.SetRenderDrawColor(renderer, 0, 0, 0, 0);
            SDL.RenderClear(renderer);
            SDL.FRect rect = new SDL.FRect {
                X = xo - 1,
                Y = yo - 1,
                W = width * scale + 2, H = height * scale + 2
            };
            SDL.SetRenderDrawColor(renderer, 255, 255, 255, 255);
            SDL.RenderRect(renderer, rect);
            rect = new SDL.FRect {
                X = xo,
                Y = yo,
                W = width * scale, H = height * scale
            };
            SDL.SetRenderDrawColor(renderer, 18, 20, 34, 255);
            SDL.RenderFillRect(renderer, rect);
            foreach (var piggsel in pixels) {
                var (r, g, b) = UnpackColor(piggsel.Material.Brush.GetColor(piggsel));
                SDL.SetRenderDrawColor(renderer, r, g, b, 255);
                rect = new SDL.FRect {
                    X = xo + piggsel.X * scale,
                    Y = yo + piggsel.Y * scale,
                    W = scale, H = scale
                };
                SDL.RenderFillRect(renderer, rect);
            }

            if (DebugDraw) {
                foreach (var (ux, uy) in updates) {
                    SDL.SetRenderDrawColor(renderer, 40, 125, 204, 96);
                    rect = new SDL.FRect {
                        X = xo + ux * scale,
                        Y = yo + uy * scale,
                        W = scale, H = scale
                    };
                    SDL.RenderRect(renderer, rect);
                }
            }

            if (cx is >= 0 and < width && cy is >= 0 and < height) {
                SDL.HideCursor();
                rect = new SDL.FRect {
                    X = xo + cx * scale,
                    Y = yo + cy * scale,
                    W = scale, H = scale
                };
                SDL.SetRenderDrawColor(renderer, 255, 255, 255, 255);
                SDL.RenderRect(renderer, rect);
            }
            else {
                SDL.ShowCursor();
            }

            int fps = 1000 / Math.Max(ms, 1);
            RegularFont.DrawText(renderer, $"{fps} FPS", 8, 8, 0xFFFFFF);
            RegularFont.DrawText(renderer, $"{materials[selected].Name}", 8, 20, 0x00FFFF);
            RegularFont.DrawText(renderer, $"{pixels.Count} particles", 8, 32, 0xFFFFFF);
            RegularFont.DrawText(renderer, $"{updates.Count} updates", 8, 44, 0xFFFFFF);
            Pixel? hover = FindPixel(cx, cy);
            if (hover != null) {
                var neigh = Neighbors(cx, cy);
                RegularFont.DrawText(renderer, $"{hover.Grip}, {neigh.Length}, {hover.Moving}", 8, 56, 0xFFFFFF);
            }

            if (paused) RegularFont.DrawText(renderer, "Paused", 8, 68, 0xFFFF00);
            SDL.RenderPresent(renderer);
            lastTick = DateTime.Now;
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
    public static int PackColor(byte r, byte g, byte b) {
        return r << 16 |  g << 8 | b;
    }
    [Pure]
    public static int PackColor((byte, byte, byte) color) {
        var (r, g, b) = color;
        return PackColor(r, g, b);
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