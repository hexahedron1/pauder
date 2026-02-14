namespace powder;

public class Reaction(Material[] reagents, Material[] products, int rate = 60, double temp = double.NegativeInfinity) {
    public Material[] Reagents { get; set; } = reagents;
    public Material[] Products { get; set; } = products;
    public int Rate { get; set; } = rate;
    public double MinTemp { get; set; } = 0;
}