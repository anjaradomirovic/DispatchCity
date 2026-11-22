namespace DispatchCity.Simulation;

public class AssignAmbulanceCommand : ICommand
{
    public Ambulance Ambulance { get; }
    public EmergencyCall Call { get; }

    public AssignAmbulanceCommand(Ambulance ambulance, EmergencyCall call)
    {
        Ambulance = ambulance;
        Call = call;
    }

    // Vozilo mora biti slobodno, a poziv još bez vozila.
    public bool CanExecute()
    {
        return Ambulance.IsAvailable && Call.Status == CallStatus.Waiting;
    }

    public void Execute()
    {
        if (CanExecute())
        {
            Ambulance.Assign(Call);
        }
    }
}