namespace DispatchCity.Core;

public class Edge
{
    public Node From { get; }
    public Node To { get; }
    public RoadType Type { get; }
    public float Length { get; }
    public float SpeedLimit { get; }

    public bool IsClosed { get; set; }
    public float Congestion { get; set; }   // 0 = prazno, 1 = zagušeno

    public Edge(Node from, Node to, RoadType type, float speedLimit)
    {
        From = from;
        To = to;
        Type = type;
        SpeedLimit = speedLimit;
        Length = System.Numerics.Vector2.Distance(from.Position, to.Position);
    }

    // Vreme prolaska u sekundama. A* i Dijkstra koriste ovo kao težinu grane.
    public float TravelTime
    {
        get
        {
            if (IsClosed)
            {
                return float.PositiveInfinity;
            }

            float speed = SpeedLimit * (1f - 0.7f * Congestion);
            return Length / speed;
        }
    }
}