using DispatchCity.Config;
using DispatchCity.Core;
using DispatchCity.Pathfinding;

namespace DispatchCity.Simulation;

// CPU strana igre: stanje sveta i pravila. Ne zna ništa o OpenGL-u,
// crtanje samo čita odavde (Renderer -> IRenderLayer).
public class GameWorld
{
    private readonly List<Ambulance> _ambulances = new List<Ambulance>();
    private readonly IPathfinder _pathfinder;   // Strategy: A* ili Dijkstra, svejedno je svetu
    private readonly Random _random;

    public GameConfig Config { get; }
    public CityGraph Graph { get; }
    public Hospital Hospital { get; }
    public IReadOnlyList<Ambulance> Ambulances => _ambulances;

    public GameWorld(GameConfig config, CityGraph graph, Hospital hospital, IPathfinder pathfinder)
    {
        Config = config;
        Graph = graph;
        Hospital = hospital;
        _pathfinder = pathfinder;
        _random = new Random(config.Seed);
    }

    public Ambulance AddAmbulance()
    {
        var ambulance = new Ambulance(Hospital.Node);
        _ambulances.Add(ambulance);
        return ambulance;
    }

    public void Update(float deltaSeconds)
    {
        foreach (Ambulance ambulance in _ambulances)
        {
            // Privremeno: slobodno vozilo ide ka slučajnoj raskrsnici, dok ne dodamo pozive.
            if (ambulance.IsIdle)
            {
                SendToRandomNode(ambulance);
            }
            ambulance.Update(deltaSeconds);
        }
    }

    private void SendToRandomNode(Ambulance ambulance)
    {
        Node target = Graph.Nodes[_random.Next(Graph.Nodes.Count)];
        if (target == ambulance.CurrentNode)
        {
            return;
        }

        IReadOnlyList<Edge>? path = _pathfinder.FindPath(ambulance.CurrentNode, target);
        if (path != null)
        {
            ambulance.Drive(path);
        }
    }
}