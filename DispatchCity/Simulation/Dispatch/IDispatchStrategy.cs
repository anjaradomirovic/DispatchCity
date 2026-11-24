namespace DispatchCity.Simulation.Dispatch;

// Strategy obrazac: pravilo po kome se biraju vozila za pozive. Strategija samo predlaže
// naredbe, a izvršava ih svet.
public interface IDispatchStrategy
{
    string Name { get; }

    IReadOnlyList<AssignAmbulanceCommand> Decide(GameWorld world);
}