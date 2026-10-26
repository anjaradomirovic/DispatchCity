using DispatchCity.Core;

namespace DispatchCity.Pathfinding;

public class AStarPathfinder : IPathfinder
{
    private readonly float _maxSpeed;

    public string Name
    {
        get { return "A*"; }
    }

    // Koliko je čvorova obrađeno u poslednjem traženju (za poređenje sa Dijkstrom u izveštaju).
    public int LastVisitedCount { get; private set; }

    // maxSpeed je najveća brzina ulice u gradu, koristi se u heuristici.
    public AStarPathfinder(float maxSpeed)
    {
        _maxSpeed = maxSpeed;
    }

    public IReadOnlyList<Edge>? FindPath(Node start, Node goal)
    {
        var bestTime = new Dictionary<Node, float>();
        var cameFrom = new Dictionary<Node, Edge>();
        var closed = new HashSet<Node>();
        var open = new PriorityQueue<Node, float>();

        LastVisitedCount = 0;
        bestTime[start] = 0f;
        open.Enqueue(start, Heuristic(start, goal));

        while (open.Count > 0)
        {
            Node current = open.Dequeue();

            if (current == goal)
            {
                return BuildPath(cameFrom, start, goal);
            }

            // Čvor može biti u redu više puta, obrađujemo ga samo jednom.
            if (!closed.Add(current))
            {
                continue;
            }
            LastVisitedCount++;

            foreach (Edge edge in current.Outgoing)
            {
                float cost = edge.TravelTime;
                if (float.IsPositiveInfinity(cost))
                {
                    continue;   // zatvorena ulica
                }

                Node next = edge.To;
                if (closed.Contains(next))
                {
                    continue;
                }

                float newTime = bestTime[current] + cost;
                float oldTime;
                bool known = bestTime.TryGetValue(next, out oldTime);

                if (!known || newTime < oldTime)
                {
                    bestTime[next] = newTime;
                    cameFrom[next] = edge;
                    open.Enqueue(next, newTime + Heuristic(next, goal));
                }
            }
        }

        return null;   // put ne postoji
    }

    // Procena preostalog vremena: vazdušna linija podeljena sa najvećom brzinom.
    private float Heuristic(Node a, Node b)
    {
        float distance = System.Numerics.Vector2.Distance(a.Position, b.Position);
        return distance / _maxSpeed;
    }

    // Ide unazad od cilja do starta i okreće redosled.
    private IReadOnlyList<Edge> BuildPath(Dictionary<Node, Edge> cameFrom, Node start, Node goal)
    {
        var path = new List<Edge>();
        Node current = goal;

        while (current != start)
        {
            Edge edge = cameFrom[current];
            path.Add(edge);
            current = edge.From;
        }

        path.Reverse();
        return path;
    }
}