using System.Numerics;
using DispatchCity.Core;

namespace DispatchCity.Simulation;

// Vozilo ne pamti slobodne x, y koordinate, nego (ulica, pređeni put na njoj).
// Zbog toga se kasnije lako dodaje zabrana preklapanja vozila.
public class Ambulance
{
    // Koliko puta je simulacija brža od "stvarnog" vremena prolaska grane.
    private const float SimulationSpeed = 4f;

    private readonly List<Edge> _path = new List<Edge>();
    private int _pathIndex;
    private float _distanceOnEdge;

    // Čvor u kome je vozilo poslednji put bilo (ili je krenulo).
    public Node CurrentNode { get; private set; }

    public bool IsIdle
    {
        get { return _pathIndex >= _path.Count; }
    }

    // Tačka na mapi, računa se iz ulice i pređenog puta.
    public Vector2 Position
    {
        get
        {
            if (IsIdle)
            {
                return CurrentNode.Position;
            }

            Edge edge = _path[_pathIndex];
            float t = _distanceOnEdge / edge.Length;
            return Vector2.Lerp(edge.From.Position, edge.To.Position, t);
        }
    }

    public Ambulance(Node start)
    {
        CurrentNode = start;
    }

    // Kreće po zadatoj putanji (lista ulica od trenutnog čvora).
    public void Drive(IReadOnlyList<Edge> path)
    {
        _path.Clear();
        _path.AddRange(path);
        _pathIndex = 0;
        _distanceOnEdge = 0f;
    }

    public void Update(float deltaSeconds)
    {
        float timeLeft = deltaSeconds * SimulationSpeed;

        while (!IsIdle && timeLeft > 0f)
        {
            Edge edge = _path[_pathIndex];
            float travelTime = edge.TravelTime;

            // Zatvorena ulica: za sada samo čekamo. Preusmeravanje dolazi u kasnijem koraku.
            if (float.IsPositiveInfinity(travelTime))
            {
                return;
            }

            // Brzina na ovoj ulici (uključuje gužvu), a vreme do kraja ulice iz nje.
            float speed = edge.Length / travelTime;
            float timeToEnd = (edge.Length - _distanceOnEdge) / speed;

            if (timeLeft >= timeToEnd)
            {
                // Stiglo do kraja ulice, ostatak vremena ide na sledeću.
                timeLeft -= timeToEnd;
                CurrentNode = edge.To;
                _pathIndex++;
                _distanceOnEdge = 0f;
            }
            else
            {
                _distanceOnEdge += speed * timeLeft;
                timeLeft = 0f;
            }
        }
    }
}