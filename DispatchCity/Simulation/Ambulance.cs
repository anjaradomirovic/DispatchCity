using DispatchCity.Config;
using DispatchCity.Core;
using DispatchCity.Pathfinding;
using DispatchCity.Simulation.State;
using DispatchCity.Simulation.States;
using System.Numerics;

namespace DispatchCity.Simulation;

// Vozilo ne pamti slobodne x, y koordinate, nego (ulica, pređeni put na njoj).
// Zbog toga se kasnije lako dodaje zabrana preklapanja vozila.
// Šta vozilo radi određuje njegovo stanje (State obrazac, vidi States/).
public class Ambulance
{
    // Koliko puta je simulacija brža od "stvarnog" vremena prolaska grane.
    private const float SimulationSpeed = 4f;

    private readonly IPathfinder _pathfinder;
    private readonly List<Edge> _path = new List<Edge>();
    private int _pathIndex;
    private float _distanceOnEdge;

    public Hospital Hospital { get; }
    public GameConfig Config { get; }

    // Čvor u kome je vozilo poslednji put bilo (ili je krenulo).
    public Node CurrentNode { get; private set; }

    public IAmbulanceState State { get; private set; }

    // Poziv koji vozilo trenutno opslužuje (null kad je slobodno).
    public EmergencyCall? Call { get; private set; }

    // Javlja svetu da je poziv završen (pacijent zbrinut ili isporučen).
    public event Action<EmergencyCall>? CallCompleted;

    public bool IsAvailable
    {
        get { return State is IdleState; }
    }

    // Vozilo je stiglo na kraj trenutne putanje.
    public bool HasArrived
    {
        get { return _pathIndex >= _path.Count; }
    }

    // Tačka na mapi, računa se iz ulice i pređenog puta.
    public Vector2 Position
    {
        get
        {
            if (HasArrived)
            {
                return CurrentNode.Position;
            }

            Edge edge = _path[_pathIndex];
            float t = _distanceOnEdge / edge.Length;
            return Vector2.Lerp(edge.From.Position, edge.To.Position, t);
        }
    }

    public Ambulance(Node start, Hospital hospital, IPathfinder pathfinder, GameConfig config)
    {
        CurrentNode = start;
        Hospital = hospital;
        Config = config;
        _pathfinder = pathfinder;
        State = new IdleState();
    }
    private float _lastHeading;

    // Ugao pravca vožnje u radijanima (atan2 pravca ulice). Kad stoji, pamti poslednji.
    public float Heading
    {
        get
        {
            if (!HasArrived)
            {
                Edge edge = _path[_pathIndex];
                Vector2 direction = edge.To.Position - edge.From.Position;
                _lastHeading = MathF.Atan2(direction.Y, direction.X);
            }
            return _lastHeading;
        }
    }
    public void ChangeState(IAmbulanceState state)
    {
        State = state;
        state.Enter(this);
    }

    // Vozilo preuzima poziv i kreće ka njemu.
    public void Assign(EmergencyCall call)
    {
        Call = call;
        call.AssignTo(this);
        ChangeState(new EnRouteState());
    }

    // Poziv je prestao da važi (npr. pacijent je izgubljen dok je vozilo bilo na putu).
    public void Abort()
    {
        Call = null;
        ChangeState(new ReturningState());
    }

    public void CompleteCall()
    {
        EmergencyCall? finished = Call;
        Call = null;
        if (finished != null)
        {
            CallCompleted?.Invoke(finished);
        }
    }

    // Traži najbrži put (strategija koju svet zada) i kreće njime. Vraća false ako puta nema.
    public bool DriveTo(Node target)
    {
        IReadOnlyList<Edge>? path = _pathfinder.FindPath(CurrentNode, target);
        if (path == null)
        {
            return false;
        }

        _path.Clear();
        _path.AddRange(path);
        _pathIndex = 0;
        _distanceOnEdge = 0f;
        return true;
    }

    public void Update(float deltaSeconds)
    {
        Move(deltaSeconds);
        State.Update(this, deltaSeconds);
    }

    private void Move(float deltaSeconds)
    {
        float timeLeft = deltaSeconds * SimulationSpeed;

        while (!HasArrived && timeLeft > 0f)
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