using DispatchCity.Core;
using DispatchCity.Pathfinding;

namespace DispatchCity.Simulation.Dispatch;

// "Najbliže vozilo": za svaki poziv koji čeka bira slobodno vozilo koje do njega stiže
// za najkraće vreme (po stvarnim ulicama, ne vazdušnom linijom).
public class NearestAmbulanceStrategy : IDispatchStrategy
{
    private readonly IPathfinder _pathfinder;

    public NearestAmbulanceStrategy(IPathfinder pathfinder)
    {
        _pathfinder = pathfinder;
    }

    public string Name
    {
        get { return "Najbliže vozilo"; }
    }

    public IReadOnlyList<AssignAmbulanceCommand> Decide(GameWorld world)
    {
        var commands = new List<AssignAmbulanceCommand>();
        var used = new HashSet<Ambulance>();

        foreach (EmergencyCall call in world.Calls)
        {
            if (call.Status != CallStatus.Waiting)
            {
                continue;
            }

            Ambulance? best = null;
            float bestTime = float.PositiveInfinity;

            foreach (Ambulance ambulance in world.Ambulances)
            {
                if (!ambulance.IsAvailable || used.Contains(ambulance))
                {
                    continue;
                }

                float time = TravelTime(ambulance.CurrentNode, call.Location);
                if (time < bestTime)
                {
                    bestTime = time;
                    best = ambulance;
                }
            }

            if (best != null)
            {
                used.Add(best);
                commands.Add(new AssignAmbulanceCommand(best, call));
            }
        }
        return commands;
    }

    private float TravelTime(Node from, Node to)
    {
        IReadOnlyList<Edge>? path = _pathfinder.FindPath(from, to);
        if (path == null)
        {
            return float.PositiveInfinity;
        }

        float total = 0f;
        foreach (Edge edge in path)
        {
            total += edge.TravelTime;
        }
        return total;
    }
}