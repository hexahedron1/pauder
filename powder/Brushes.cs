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
        return colors[new Random(pixel.MagicNumber).Next(0, colors.Length)];
    }
}

public readonly struct PulseNoiseBrush(double sweep, params int[] colors) : IBrush {
    public int GetColor(Pixel pixel) {
        Random rand = new Random(pixel.MagicNumber);
        var (r, g, b) = UnpackColor(colors[rand.Next(0, colors.Length)]);
        double sin = Math.Sin(t + rand.Next(0, 628)/100.0)/2*sweep+0.5;
        r = (byte)(r * sin);
        g = (byte)(g * sin);
        b = (byte)(b * sin);
        return PackColor(r, g, b);
    }
}