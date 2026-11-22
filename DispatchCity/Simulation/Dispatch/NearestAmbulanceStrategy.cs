using DispatchCity.Pathfinding;

namespace DispatchCity.Simulation.Dispatch;

// "Najblize vozilo": pozivi se opsluzuju redom kojim su stigli, a svaki dobija vozilo
// koje do njega stize najbrze. Ne gleda koliko je poziv hitan.
public class NearestAmbulanceStrategy : OrderedDispatchStrategy
{
    public NearestAmbulanceStrategy(IPathfinder pathfinder) : base(pathfinder)
    {
    }

    // Bez dijakritika jer se ime prikazuje u panelu (osnovni ImGui font ih nema).
    public override string Name
    {
        get { return "Najblize vozilo"; }
    }

    protected override IEnumerable<EmergencyCall> Order(IReadOnlyList<EmergencyCall> waiting)
    {
        return waiting;
    }
}