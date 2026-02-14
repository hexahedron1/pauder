namespace powder;

public class Material(string name, IBrush brush) {
    public IBrush Brush { get; set; } = brush;
    public string Name { get; set; } = name;
    public List<Reaction> Reactions { get; set; } = [];
    public Action? Tick = null;
}

