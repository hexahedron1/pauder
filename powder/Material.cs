namespace powder;

public class Material(string name, IBrush brush, double melt = 1713, double boil = 2950, double density = 1.6, double heatCapacity = 0.7, double stickyness = 0.01) {
    public IBrush Brush { get; } = brush;
    public string Name { get; } = name;
    public List<Reaction> Reactions { get; } = [];
    public Action? Tick = null;
    public double MeltPoint { get; set; } = melt;
    public double BoilPoint { get; set; } = boil;
    public double Density { get; set; } = density; // g/cm³; since each pixel is 1cm³ internally, this is effectively mass of a pixel
    public double HeatCapacity { get; set; } = heatCapacity; // J/g·°C
    public double Stickyness { get; set; } = stickyness; // how much grip it gains for each second passed not moving
}

