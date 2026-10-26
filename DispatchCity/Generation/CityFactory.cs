using DispatchCity.Config;
using DispatchCity.Core;

namespace DispatchCity.Generation;

// Jedno mesto gde se od konfiguracije pravi grad (koristi ga i igra i test seed-ova).
public static class CityFactory
{
    public static CityGraph Create(GameConfig config)
    {
        return new CityBuilder()
            .WithSize(config.WorldWidth, config.WorldHeight)
            .WithSeed(config.Seed)
            .WithSpacing(config.Spacing)
            .WithExtraEdges(config.ExtraEdgeChance)
            .WithPoints()
            .WithRoads()
            .WithRoadTypes()
            .WithOneWayStreets(config.OneWayFraction)
            .Build();
    }

    // Pravila koja mapa mora da ispuni da bi bila prihvaćena.
    public static bool Validate(CityGraph graph, out string error)
    {
        if (graph.Nodes.Count < 2)
        {
            error = "premalo raskrsnica";
            return false;
        }

        if (!ConnectivityChecker.IsStronglyConnected(graph))
        {
            error = "graf nije jako povezan";
            return false;
        }

        if (CityBuilder.HasCrossingRoads(graph))
        {
            error = "ulice se seku";
            return false;
        }

        error = "";
        return true;
    }
}