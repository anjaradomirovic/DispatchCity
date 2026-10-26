using System.Numerics;

namespace DispatchCity.Core;

public class CityGraph
{
    private readonly List<Node> _nodes = new();
    private readonly List<Edge> _edges = new();

    public IReadOnlyList<Node> Nodes => _nodes;
    public IReadOnlyList<Edge> Edges => _edges;

    public Node AddNode(Vector2 position)
    {
        var node = new Node(_nodes.Count, position);
        _nodes.Add(node);
        return node;
    }

    public Edge AddEdge(Node from, Node to, RoadType type, float speedLimit)
    {
        var edge = new Edge(from, to, type, speedLimit);
        from.Outgoing.Add(edge);
        _edges.Add(edge);
        return edge;
    }

    // Vraća granu from -> to, ili null ako takva ulica ne postoji.
    public Edge? FindEdge(Node from, Node to)
    {
        foreach (Edge edge in from.Outgoing)
        {
            if (edge.To == to)
            {
                return edge;
            }
        }
        return null;
    }

    // Skida granu iz grafa (za jednosmerne ulice).
    public void RemoveEdge(Edge edge)
    {
        edge.From.Outgoing.Remove(edge);
        _edges.Remove(edge);
    }

    // Vraća granu koja je prethodno skinuta sa RemoveEdge.
    public void RestoreEdge(Edge edge)
    {
        edge.From.Outgoing.Add(edge);
        _edges.Add(edge);
    }

    // Dvosmerna ulica = dve usmerene grane.
    public void AddTwoWay(Node a, Node b, RoadType type, float speedLimit)
    {
        AddEdge(a, b, type, speedLimit);
        AddEdge(b, a, type, speedLimit);
    }
}