using SDL3;
using static powder.Program;

namespace powder;

public class Font(IntPtr surface, int height, int spacing = 1) : IDisposable {
    public Dictionary<char, SDL.FRect> Glyphs { get; } = new();
    public int Height { get; } = height;
    public int Spacing { get; } = spacing - 1;
    public SDL.FRect this[char c] => Glyphs.GetValueOrDefault(c, new ());
    private bool _disposed;
    public IntPtr Surface { get; private set; } = surface;
    public void Dispose() {
        if(!_disposed) {
            if (Surface != IntPtr.Zero) {
                SDL.DestroySurface(Surface);
                Surface = IntPtr.Zero;
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
    public SDL.FRect ErrorGlyph { get; set; }

    public int MeasureText(string text) =>
        text.Select(t => this[t]).Select((rect, i) => (int)rect.W + (i == text.Length - 1 ? 0 : Spacing)).Sum();

    public void DrawText(IntPtr renderer, string text, int x, int y, int color) {
        foreach (char c in text) {
            var rect = this[c];
            var drect = new SDL.FRect {
                X = x, Y = y,
                W = rect.W, H = rect.H
            };
            IntPtr tex = SDL.CreateTextureFromSurface(renderer, Surface);
            var col = UnpackColor(color);
            SDL.SetTextureColorMod(tex, col.Item1, col.Item2, col.Item3);
            SDL.RenderTexture(renderer, tex, in rect, in drect);
            x += (int)rect.W + Spacing;
            SDL.DestroyTexture(tex);
        }
    }
}