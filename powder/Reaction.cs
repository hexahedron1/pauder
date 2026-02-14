namespace powder;

public struct Reaction(string[] reagents, string[] products, double energy = 0.0) {
    public string[] Reagents { get; set; } = reagents;
    public string[] Products { get; set; } = products;
    public double Energy { get; set; } = energy;
}