using GraphQL.Types;

namespace StarWars.Types;

internal sealed class Episodes : EnumerationGraphType
{
    public Episodes()
    {
        Name = "Episode";
        Description = "One of the films in the Star Wars Trilogy.";
        Add("NEWHOPE", 4, "Released in 1977.");
        Add("EMPIRE", 5, "Released in 1980.");
        Add("JEDI", 6, "Released in 1983.");
    }
}
