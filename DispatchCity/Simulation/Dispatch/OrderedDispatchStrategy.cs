using DispatchCity.Core;
using DispatchCity.Pathfinding;

namespace DispatchCity.Simulation.Dispatch;

// Template Method: svaka strategija ide kroz pozive koji cekaju i svakom daje slobodno vozilo
// koje do njega stize najbrze (po stvarnim ulicama). Strategije se razlikuju samo po tome
// KOJIM REDOM gledaju pozive (Order), jer prvi poziv bira vozilo prvi.
public abstract class OrderedDispatchStrategy : IDispatchStrategy
{
    private readonly IPathfinder _pathfinder;

    protected OrderedDispatchStrategy(IPathfinder pathfinder)
    {
        _pathfinder = pathfinder;
    }

    public abstract string Name { get; }

    // Redosled u kome se pozivi opsluzuju.
    protected abstract IEnumerable<EmergencyCall> Order(IReadOnlyList<EmergencyCall> waiting);

    public IReadOnlyList<AssignAmbulanceCommand> Decide(GameWorld world)
    {
        var commands = new List<AssignAmbulanceCommand>();
        var used = new HashSet<Ambulance>();

        var waiting = new List<EmergencyCall>();
        foreach (EmergencyCall call in world.Calls)
        {
            if (call.Status == CallStatus.Waiting)
            {
                waiting.Add(call);
            }
        }

        foreach (EmergencyCall call in Order(waiting))
        {
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