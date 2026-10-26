using DispatchCity.Core;

namespace DispatchCity.Pathfinding;

public class DijkstraPathfinder : IPathfinder
{
    public string Name
    {
        get { return "Dijkstra"; }
    }

    // Koliko je čvorova obrađeno u poslednjem traženju.
    public int LastVisitedCount { get; private set; }

    public IReadOnlyList<Edge>? FindPath(Node start, Node goal)
    {
        var bestTime = new Dictionary<Node, float>();
        var cameFrom = new Dictionary<Node, Edge>();
        var closed = new HashSet<Node>();
        var open = new PriorityQueue<Node, float>();

        LastVisitedCount = 0;
        bestTime[start] = 0f;
        open.Enqueue(start, 0f);

        while (open.Count > 0)
        {
            Node current = open.Dequeue();

            if (current == goal)
            {
                return BuildPath(cameFrom, start, goal);
            }

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
                    open.Enqueue(next, newTime);
                }
            }
        }

        return null;
    }

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