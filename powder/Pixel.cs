namespace powder;

public enum State {
    Solid,
    Liquid,
    Gas
}
public struct Pixel(Material material, int x, int y) : IEquatable<Pixel> {
    public Material Material { get; } = material;
    public int X { get; set; } = x;
    public int Y { get; set; } = y;

    public bool Equals(Pixel other) {
        return Material.Equals(other.Material) && X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj) {
        return obj is Pixel other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Material, X, Y);
    }
}