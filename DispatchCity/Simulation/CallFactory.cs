using DispatchCity.Config;
using DispatchCity.Core;

namespace DispatchCity.Simulation;

// Factory: jedino mesto gde se prave pozivi (lokacija, prioritet, težina, pacijent).
public class CallFactory
{
    private readonly GameConfig _config;
    private readonly CityGraph _graph;
    private readonly Random _random;
    private int _nextId = 1;

    public CallFactory(GameConfig config, CityGraph graph)
    {
        _config = config;
        _graph = graph;
        _random = new Random(config.Seed + 4);
    }

    // Vraća null ako nema slobodne raskrsnice. Poziv ne može nastati u bolnici
    // ni na raskrsnici na kojoj već čeka drugi poziv.
    public EmergencyCall? Create(IReadOnlyList<EmergencyCall> active, Node hospitalNode)
    {
        Node? location = PickFreeNode(active, hospitalNode);
        if (location == null)
        {
            return null;
        }

        Priority priority = PickPriority();
        InjurySeverity severity = _random.NextDouble() < _config.HeavyInjuryChance
            ? InjurySeverity.Heavy
            : InjurySeverity.Light;

        var patient = new Patient(SurvivalSecondsOf(priority));
        return new EmergencyCall(_nextId++, location, priority, severity, patient);
    }

    private Node? PickFreeNode(IReadOnlyList<EmergencyCall> active, Node hospitalNode)
    {
        // Slučajan pokušaj, a ako ne uspe, ne čekamo: poziv se pravi sledeći put.
        for (int attempt = 0; attempt < 30; attempt++)
        {
            Node node = _graph.Nodes[_random.Next(_graph.Nodes.Count)];
            if (node == hospitalNode)
            {
                continue;
            }

            bool taken = false;
            foreach (EmergencyCall call in active)
            {
                if (call.Location == node)
                {
                    taken = true;
                    break;
                }
            }
            if (!taken)
            {
                return node;
            }
        }
        return null;
    }

    // Niski prioriteti su češći od visokih.
    private Priority PickPriority()
    {
        double roll = _random.NextDouble();
        if (roll < _config.HighPriorityChance)
        {
            return Priority.High;
        }
        if (roll < _config.HighPriorityChance + _config.MediumPriorityChance)
        {
            return Priority.Medium;
        }
        return Priority.Low;
    }

    private float SurvivalSecondsOf(Priority priority)
    {
        switch (priority)
        {
            case Priority.High:
                return _config.HighPrioritySurvivalSeconds;
            case Priority.Medium:
                return _config.MediumPrioritySurvivalSeconds;
            default:
                return _config.LowPrioritySurvivalSeconds;
        }
    }
}