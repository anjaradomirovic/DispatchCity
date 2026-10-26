using System.Numerics;

namespace DispatchCity.Core;

public class Node
{
    public int Id { get; }
    public Vector2 Position { get; }
    public List<Edge> Outgoing { get; } = new();

    public Node(int id, Vector2 position)
    {
        Id = id;
        Position = position;
    }
}