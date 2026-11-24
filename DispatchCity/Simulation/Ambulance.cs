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
    private float _tripTotalSeconds;
    private Node? _target;

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

    // Za koliko sekundi (pri brzini igre 1x) vozilo stiže do kraja putanje. Panel to prikazuje kao dolazak.
    public float RemainingSeconds
    {
        get
        {
            float total = 0f;
            for (int i = _pathIndex; i < _path.Count; i++)
            {
                Edge edge = _path[i];
                float seconds = edge.TravelTime / SimulationSpeed;
                if (i == _pathIndex)
                {
                    seconds *= 1f - _distanceOnEdge / edge.Length;
                }
                total += seconds;
            }
            return total;
        }
    }

    // Koliko je puta već prešlo (0 do 1), kao razmer ukupnog vremena putovanja.
    public float TripProgress
    {
        get
        {
            if (HasArrived || _tripTotalSeconds <= 0f || float.IsInfinity(_tripTotalSeconds))
            {
                return 1f;
            }
            return Math.Clamp(1f - RemainingSeconds / _tripTotalSeconds, 0f, 1f);
        }
    }

    // Ulica kojom vozilo trenutno vozi (null kad je stiglo na kraj putanje).
    public Edge? CurrentEdge
    {
        get { return HasArrived ? null : _path[_pathIndex]; }
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

        _target = target;
        _path.Clear();
        _path.AddRange(path);
        _pathIndex = 0;
        _distanceOnEdge = 0f;
        _tripTotalSeconds = RemainingSeconds;
        return true;
    }

    // Ostatak puta prolazi kroz zatvorenu ulicu.
    private bool IsPathBlocked()
    {
        for (int i = _pathIndex; i < _path.Count; i++)
        {
            if (_path[i].IsClosed)
            {
                return true;
            }
        }
        return false;
    }

    // Traži novi put od trenutnog čvora do istog cilja. Ako ga nema, vozilo čeka.
    private bool Replan()
    {
        if (_target == null)
        {
            return false;
        }
        return DriveTo(_target);
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
            // Na čvoru (pre kretanja ulicom) proveri da put nije presečen zatvorenom ulicom.
            if (_distanceOnEdge == 0f && IsPathBlocked())
            {
                if (!Replan())
                {
                    return;   // nema obilaska, čeka da se ulica otvori
                }
                continue;
            }

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