using DispatchCity.Pathfinding;

namespace DispatchCity.Simulation.Dispatch;

// "Najvisi prioritet prvo": prvo se opsluzuju najhitniji pozivi (visok, pa srednji, pa nizak),
// a medju jednakima onaj cijem je pacijentu zdravlje najnize. Slobodno vozilo koje je
// najbrze do poziva ide na njega, pa visok prioritet ne ceka iza lakih slucajeva.
public class HighestPriorityStrategy : OrderedDispatchStrategy
{
    public HighestPriorityStrategy(IPathfinder pathfinder) : base(pathfinder)
    {
    }

    public override string Name
    {
        get { return "Najvisi prioritet"; }
    }

    protected override IEnumerable<EmergencyCall> Order(IReadOnlyList<EmergencyCall> waiting)
    {
        // Priority je enum (Low < Medium < High), pa opadajuce znaci najhitniji prvi.
        return waiting
            .OrderByDescending(call => call.Priority)
            .ThenBy(call => call.Patient.Health);
    }
}