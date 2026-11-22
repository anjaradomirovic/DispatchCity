using System.Diagnostics;
using DispatchCity.Config;
using DispatchCity.Core;
using DispatchCity.Generation;
using DispatchCity.Pathfinding;

namespace DispatchCity.Diagnostics;

// Poredjenje Dijkstre i A*: pokrece se sa  dotnet run -- --compare 100 50
// (100 gradova, 50 slucajnih parova u svakom). Proverava dve stvari:
//   1) tacnost: oba algoritma moraju naci put iste duzine (A* je ispravan samo ako je
//      heuristika dopustiva, a to se ovde proverava na hiljadama parova);
//   2) efikasnost: koliko cvorova svaki obradi i koliko traje jedan upit.
public static class PathfinderComparison
{
    private const float LengthTolerance = 1e-3f;

    public static int Run(int cities, int pairsPerCity)
    {
        var random = new Random(1);

        long pairs = 0;
        int mismatches = 0;
        long dijkstraVisited = 0;
        long astarVisited = 0;
        long dijkstraTicks = 0;
        long astarTicks = 0;

        for (int seed = 1; seed <= cities; seed++)
        {
            GameConfig config = GameConfig.Level1();
            config.Seed = seed;
            CityGraph graph = CityFactory.Create(config);

            var dijkstra = new DijkstraPathfinder();
            var astar = new AStarPathfinder(graph.MaxSpeed());

            for (int i = 0; i < pairsPerCity; i++)
            {
                Node start = graph.Nodes[random.Next(graph.Nodes.Count)];
                Node goal = graph.Nodes[random.Next(graph.Nodes.Count)];
                if (start == goal)
                {
                    continue;
                }

                long t0 = Stopwatch.GetTimestamp();
                IReadOnlyList<Edge>? dijkstraPath = dijkstra.FindPath(start, goal);
                dijkstraTicks += Stopwatch.GetTimestamp() - t0;

                long t1 = Stopwatch.GetTimestamp();
                IReadOnlyList<Edge>? astarPath = astar.FindPath(start, goal);
                astarTicks += Stopwatch.GetTimestamp() - t1;

                pairs++;
                dijkstraVisited += dijkstra.LastVisitedCount;
                astarVisited += astar.LastVisitedCount;

                if (dijkstraPath == null || astarPath == null)
                {
                    if (dijkstraPath != null || astarPath != null)
                    {
                        mismatches++;
                    }
                    continue;
                }

                float dijkstraTime = TotalTime(dijkstraPath);
                float astarTime = TotalTime(astarPath);
                if (Math.Abs(dijkstraTime - astarTime) > LengthTolerance * Math.Max(1f, dijkstraTime))
                {
                    mismatches++;
                }
            }
        }

        if (pairs == 0)
        {
            Console.WriteLine("Nema parova za poredjenje.");
            return 1;
        }

        double microsPerTick = 1_000_000.0 / Stopwatch.Frequency;

        Console.WriteLine("Poredjeno " + pairs + " parova u " + cities + " gradova.");
        Console.WriteLine("Neslaganja u duzini puta: " + mismatches);
        Console.WriteLine();
        Console.WriteLine("Algoritam   Obradjeni cvorovi (prosek)   Vreme po upitu (us)");
        Console.WriteLine("Dijkstra    " + Format(dijkstraVisited / (double)pairs).PadRight(29)
                          + Format(dijkstraTicks * microsPerTick / pairs));
        Console.WriteLine("A*          " + Format(astarVisited / (double)pairs).PadRight(29)
                          + Format(astarTicks * microsPerTick / pairs));
        Console.WriteLine();
        Console.WriteLine("A* obradjuje " + Format(100.0 * astarVisited / dijkstraVisited)
                          + "% cvorova koje obradi Dijkstra.");

        return mismatches == 0 ? 0 : 1;
    }

    private static float TotalTime(IReadOnlyList<Edge> path)
    {
        float total = 0f;
        foreach (Edge edge in path)
        {
            total += edge.TravelTime;
        }
        return total;
    }

    private static string Format(double value)
    {
        return value.ToString("0.0");
    }
}