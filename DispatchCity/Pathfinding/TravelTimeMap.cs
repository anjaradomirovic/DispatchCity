using DispatchCity.Core;

namespace DispatchCity.Pathfinding;

// Vreme vožnje od jednog čvora (bolnice) do SVAKOG drugog čvora. Jedan Dijkstra prolaz bez cilja:
// ne traži se put do jedne tačke, nego se obiđe ceo graf. Koristi se za toplotnu mapu.
// Ako se gužva ili zatvorene ulice promene, Recompute() ponovo računa vremena.
public class TravelTimeMap
{
    private readonly CityGraph _graph;
    private readonly Node _source;
    private readonly float[] _times;   // po Id-u čvora, beskonačno ako se do njega ne stiže

    // Najduže vreme do nekog dostižnog čvora (za razmeru boja).
    public float MaxTime { get; private set; }

    public TravelTimeMap(CityGraph graph, Node source)
    {
        _graph = graph;
        _source = source;
        _times = new float[graph.Nodes.Count];
        Recompute();
    }

    public float TimeTo(Node node)
    {
        return _times[node.Id];
    }

    public void Recompute()
    {
        Array.Fill(_times, float.PositiveInfinity);
        _times[_source.Id] = 0f;

        var open = new PriorityQueue<Node, float>();
        open.Enqueue(_source, 0f);

        while (open.TryDequeue(out Node? current, out float time))
        {
            // Zastareo unos: do ovog čvora je u međuvremenu nađen brži put.
            if (time > _times[current.Id])
            {
                continue;
            }

            foreach (Edge edge in current.Outgoing)
            {
                float cost = edge.TravelTime;
                if (float.IsPositiveInfinity(cost))
                {
                    continue;   // zatvorena ulica
                }

                float newTime = time + cost;
                if (newTime < _times[edge.To.Id])
                {
                    _times[edge.To.Id] = newTime;
                    open.Enqueue(edge.To, newTime);
                }
            }
        }

        MaxTime = 0f;
        foreach (float time in _times)
        {
            if (!float.IsPositiveInfinity(time) && time > MaxTime)
            {
                MaxTime = time;
            }
        }
    }
}