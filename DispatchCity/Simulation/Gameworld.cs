using DispatchCity.Config;
using DispatchCity.Core;
using DispatchCity.Pathfinding;
using DispatchCity.Simulation.Dispatch;

namespace DispatchCity.Simulation;

// CPU strana igre: stanje sveta i pravila. Ne zna ništa o OpenGL-u,
// crtanje samo čita odavde (Renderer -> IRenderLayer).
public class GameWorld
{
    private readonly List<Ambulance> _ambulances = new List<Ambulance>();
    private readonly List<EmergencyCall> _calls = new List<EmergencyCall>();
    private readonly IPathfinder _pathfinder;   // Strategy: A* ili Dijkstra, svejedno je svetu
    private readonly CallFactory _callFactory;
    private readonly Random _random;
    private float _timeToNextCall;

    // Observer: UI i ostali se pretplate, a svet ne zna ko sluša.
    public event Action<EmergencyCall>? CallCreated;
    public event Action<EmergencyCall>? PatientDied;
    public event Action<EmergencyCall>? PatientSaved;

    public GameConfig Config { get; }
    public CityGraph Graph { get; }
    public Hospital Hospital { get; }
    public IReadOnlyList<Ambulance> Ambulances => _ambulances;
    public IReadOnlyList<EmergencyCall> Calls => _calls;

    // Automatsko raspoređivanje. Null znači ručno upravljanje (igrač šalje naredbe).
    public IDispatchStrategy? AutoDispatch { get; set; }

    public GameWorld(GameConfig config, CityGraph graph, Hospital hospital, IPathfinder pathfinder)
    {
        Config = config;
        Graph = graph;
        Hospital = hospital;
        _pathfinder = pathfinder;
        _callFactory = new CallFactory(config, graph);
        _random = new Random(config.Seed + 5);
        _timeToNextCall = config.FirstCallDelaySeconds;
    }

    public Ambulance AddAmbulance()
    {
        var ambulance = new Ambulance(Hospital.Node, Hospital, _pathfinder, Config);
        ambulance.CallCompleted += OnCallCompleted;
        _ambulances.Add(ambulance);
        return ambulance;
    }

    public void Update(float deltaSeconds)
    {
        UpdateCalls(deltaSeconds);
        RunAutoDispatch();

        foreach (Ambulance ambulance in _ambulances)
        {
            ambulance.Update(deltaSeconds);
        }
    }

    private void RunAutoDispatch()
    {
        if (AutoDispatch == null)
        {
            return;
        }

        foreach (AssignAmbulanceCommand command in AutoDispatch.Decide(this))
        {
            if (command.CanExecute())
            {
                command.Execute();
            }
        }
    }

    private void UpdateCalls(float deltaSeconds)
    {
        // Novi pozivi stižu povremeno, najviše MaxActiveCalls odjednom.
        _timeToNextCall -= deltaSeconds;
        if (_timeToNextCall <= 0f && _calls.Count < Config.MaxActiveCalls)
        {
            EmergencyCall? call = _callFactory.Create(_calls, Hospital.Node);
            if (call != null)
            {
                _calls.Add(call);
                CallCreated?.Invoke(call);
            }

            // Razmak između poziva varira (0.5x do 1.5x), da ne dolaze kao po satu.
            _timeToNextCall = Config.CallIntervalSeconds * (0.5f + (float)_random.NextDouble());
        }

        // Unazad, da brisanje ne pomeri elemente koje tek obilazimo.
        for (int i = _calls.Count - 1; i >= 0; i--)
        {
            EmergencyCall call = _calls[i];
            call.Update(deltaSeconds);

            if (call.Patient.IsDead)
            {
                _calls.RemoveAt(i);

                // Vozilo koje je bilo na putu više nema kome da ide.
                call.AssignedAmbulance?.Abort();
                PatientDied?.Invoke(call);
            }
        }
    }

    private void OnCallCompleted(EmergencyCall call)
    {
        _calls.Remove(call);
        PatientSaved?.Invoke(call);
    }
}