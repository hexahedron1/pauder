namespace powder;

public enum State {
    Solid,
    Liquid,
    Gas
}
public class Pixel(Material material, int x, int y) : IEquatable<Pixel> {
    public Material Material { get; } = material;
    public int X { get; set; } = x;
    public int Y { get; set; } = y;
    public State State { get; set; } = State.Solid;
    public double Grip = 0.0;
    public double Energy = 0.0; // J
    public double IncomingEnergy = 0.0; // J
    public double Temperature = 0.0; // °C
    public double LastAwake = 0.0;
    public int MagicNumber { get; } = GlobalRandom.Next(int.MaxValue);
    
    public bool Equals(Pixel? other) {
        return Material.Equals(other?.Material) && X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj) {
        return obj is Pixel other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Material, X, Y);
    }
}