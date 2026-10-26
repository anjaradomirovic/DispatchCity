using DispatchCity.Core;
using DispatchCity.Pathfinding;

namespace DispatchCity.Generation;

// Određuje koje su ulice bulevari, glavne ili sporedne.
// Ideja: bulevar je najbrži put kroz ceo grad, pa se pravi traženjem puteva.
public class RoadClassifier
{
    private const float AvenueSpeed = 20f;
    private const float MainSpeed = 13f;

    private readonly CityGraph _graph;
    private readonly Random _random;
    private readonly DijkstraPathfinder _pathfinder = new DijkstraPathfinder();

    public RoadClassifier(CityGraph graph, int seed)
    {
        _graph = graph;
        _random = new Random(seed + 2);
    }

    public void Classify()
    {
        if (_graph.Nodes.Count < 2)
        {
            return;
        }

        // Dva bulevara: s leva na desno i odozgo nadole.
        Node left = _graph.Nodes[0];
        Node right = _graph.Nodes[0];
        Node top = _graph.Nodes[0];
        Node bottom = _graph.Nodes[0];

        foreach (Node node in _graph.Nodes)
        {
            if (node.Position.X < left.Position.X) { left = node; }
            if (node.Position.X > right.Position.X) { right = node; }
            if (node.Position.Y < top.Position.Y) { top = node; }
            if (node.Position.Y > bottom.Position.Y) { bottom = node; }
        }

        Upgrade(_pathfinder.FindPath(left, right), RoadType.Avenue, AvenueSpeed);
        Upgrade(_pathfinder.FindPath(top, bottom), RoadType.Avenue, AvenueSpeed);

        // Glavne ulice: nekoliko puteva između slučajnih raskrsnica.
        // Pošto su bulevari brži, glavne ulice se prirodno hvataju za njih.
        for (int i = 0; i < 6; i++)
        {
            Node a = _graph.Nodes[_random.Next(_graph.Nodes.Count)];
            Node b = _graph.Nodes[_random.Next(_graph.Nodes.Count)];
            if (a != b)
            {
                Upgrade(_pathfinder.FindPath(a, b), RoadType.Main, MainSpeed);
            }
        }
    }

    private void Upgrade(IReadOnlyList<Edge>? path, RoadType type, float speed)
    {
        if (path == null)
        {
            return;
        }

        foreach (Edge edge in path)
        {
            // Bulevar se nikad ne vraća na nižu kategoriju.
            if (edge.Type == RoadType.Avenue)
            {
                continue;
            }

            SetType(edge, type, speed);

            Edge? reverse = FindReverse(edge);
            if (reverse != null)
            {
                SetType(reverse, type, speed);
            }
        }
    }

    private void SetType(Edge edge, RoadType type, float speed)
    {
        edge.Type = type;
        edge.SpeedLimit = speed;
    }

    // Ulica u suprotnom smeru (ista dva čvora, obrnuto).
    private Edge? FindReverse(Edge edge)
    {
        foreach (Edge other in edge.To.Outgoing)
        {
            if (other.To == edge.From)
            {
                return other;
            }
        }
        return null;
    }
}