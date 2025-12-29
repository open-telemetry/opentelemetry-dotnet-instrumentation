namespace StarWars.Types;

public abstract class StarWarsCharacter
{
    public string Id { get; set; } = "<default>";

    public string? Name { get; set; }

    public IReadOnlyList<string> Friends { get; set; } = new List<string>();

    public int[]? AppearsIn { get; set; }

    public string? Cursor { get; set; }
}

public class Human : StarWarsCharacter
{
    public string? HomePlanet { get; set; }
}

public class Droid : StarWarsCharacter
{
    public string? PrimaryFunction { get; set; }
}
