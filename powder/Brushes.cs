using System.Diagnostics.Contracts;
using static powder.Program;
namespace powder;
public interface IBrush {
    [Pure]
    public int GetColor(Pixel pixel);
}

public readonly struct NoiseBrush(params int[] colors) : IBrush {
    [Pure]
    public int GetColor(Pixel pixel) {
        return colors[new Random(pixel.GetHashCode()).Next(0, colors.Length)];
    }
}