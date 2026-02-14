namespace powder;

public class Reaction(Material[] reagents, Material[] products, int rate = 60) {
    public Material[] Reagents { get; set; } = reagents;
    public Material[] Products { get; set; } = products;
    public int Rate { get; set; } = rate;
}