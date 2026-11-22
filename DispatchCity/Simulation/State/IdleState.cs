namespace DispatchCity.Simulation.State;

// Vozilo stoji u bolnici i čeka zadatak. Zadatak mu daje Ambulance.Assign.
public class IdleState : IAmbulanceState
{
    public string Name
    {
        get { return "Idle"; }
    }

    public void Enter(Ambulance ambulance)
    {
    }

    public void Update(Ambulance ambulance, float deltaSeconds)
    {
    }
}