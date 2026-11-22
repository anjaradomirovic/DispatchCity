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
    private readonly List<IDispatchStrategy> _strategies = new List<IDispatchStrategy>();
    private readonly CallFactory _callFactory;
    private readonly Random _random;
    private float _timeToNextCall;
    private float _timeToHeatRefresh;

    // Observer: UI i ostali se pretplate, a svet ne zna ko sluša.
    public event Action<EmergencyCall>? CallCreated;
    public event Action<EmergencyCall>? PatientDied;
    public event Action<EmergencyCall>? PatientSaved;
    public event Action? LevelFinished;

    public GameConfig Config { get; }
    public CityGraph Graph { get; }
    public Hospital Hospital { get; }
    public IReadOnlyList<Ambulance> Ambulances => _ambulances;
    public IReadOnlyList<EmergencyCall> Calls => _calls;
    public ScoreBoard Score { get; }

    // Vreme vožnje od bolnice do svake raskrsnice (za toplotnu mapu).
    public TravelTimeMap TravelTimes { get; }

    // Gužve: menjaju Edge.Congestion, pa se menjaju i putevi, vremena i boje ulica.
    public TrafficModel Traffic { get; }

    // Koliko je poziva do sada stiglo (od CallsPerLevel) i da li je nivo gotov.
    public int CallsCreated { get; private set; }
    public bool IsFinished { get; private set; }

    // Vreme igre: pauza i brzina (1 = normalno). Panel i taster Space ih menjaju.
    public bool IsPaused { get; set; }
    public float SpeedMultiplier { get; set; } = 1f;

    // Automatsko raspoređivanje. Null znači ručno upravljanje (igrač šalje naredbe).
    public IDispatchStrategy? AutoDispatch { get; set; }

    // Strategije koje igrač može da izabere (panel ih prikazuje).
    public IReadOnlyList<IDispatchStrategy> Strategies => _strategies;

    public GameWorld(GameConfig config, CityGraph graph, Hospital hospital, IPathfinder pathfinder)
    {
        Config = config;
        Graph = graph;
        Hospital = hospital;
        _pathfinder = pathfinder;
        Score = new ScoreBoard(config);
        TravelTimes = new TravelTimeMap(graph, hospital.Node);
        Traffic = new TrafficModel(config, graph);
        _callFactory = new CallFactory(config, graph);
        _random = new Random(config.Seed + 5);
        _timeToNextCall = config.FirstCallDelaySeconds;
    }

    public void AddStrategy(IDispatchStrategy strategy)
    {
        _strategies.Add(strategy);
    }

    // Jedino mesto gde se naredba izvršava, bilo da je pravi igrač (klik) ili strategija.
    public bool Execute(ICommand command)
    {
        if (!command.CanExecute())
        {
            return false;
        }

        command.Execute();
        return true;
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
        // Pauza zaustavlja ceo svet, a brzina ubrzava sve (pozive, pacijente i vozila) isto.
        if (IsPaused)
        {
            return;
        }
        deltaSeconds *= SpeedMultiplier;

        // Kad je nivo gotov, pozivi i raspodela staju, a vozila dovrse povratak u bolnicu.
        if (!IsFinished)
        {
            UpdateTraffic(deltaSeconds);
            UpdateCalls(deltaSeconds);
            RunAutoDispatch();
            CheckLevelEnd();
        }

        foreach (Ambulance ambulance in _ambulances)
        {
            ambulance.Update(deltaSeconds);
        }
    }

    // Gužva se menja stalno, a toplotna mapa (Dijkstra kroz ceo graf) se ne računa svaki frame
    // nego u razmaku HeatMapRefreshSeconds.
    private void UpdateTraffic(float deltaSeconds)
    {
        Traffic.Update(deltaSeconds);

        _timeToHeatRefresh -= deltaSeconds;
        if (_timeToHeatRefresh <= 0f)
        {
            TravelTimes.Recompute();
            _timeToHeatRefresh = Config.HeatMapRefreshSeconds;
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
            Execute(command);
        }
    }

    private void UpdateCalls(float deltaSeconds)
    {
        // Novi pozivi stižu povremeno, najviše MaxActiveCalls odjednom.
        _timeToNextCall -= deltaSeconds;
        if (_timeToNextCall <= 0f
            && _calls.Count < Config.MaxActiveCalls
            && CallsCreated < Config.CallsPerLevel)
        {
            EmergencyCall? call = _callFactory.Create(_calls, Hospital.Node);
            if (call != null)
            {
                _calls.Add(call);
                CallsCreated++;
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
                Score.RecordLost();
                PatientDied?.Invoke(call);
            }
        }
    }

    private void OnCallCompleted(EmergencyCall call)
    {
        _calls.Remove(call);
        Score.RecordSaved(call);
        PatientSaved?.Invoke(call);
    }

    // Nivo se zavrsava kad su stigli svi pozivi i nijedan vise ne ceka.
    private void CheckLevelEnd()
    {
        if (CallsCreated >= Config.CallsPerLevel && _calls.Count == 0)
        {
            IsFinished = true;
            LevelFinished?.Invoke();
        }
    }
}