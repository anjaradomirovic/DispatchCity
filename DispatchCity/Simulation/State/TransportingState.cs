using DispatchCity.Simulation.State;

namespace DispatchCity.Simulation.States;

// Vozilo vozi pacijenta u bolnicu. Kad stigne, poziv je završen i vozilo je slobodno.
public class TransportingState : IAmbulanceState
{
    private bool _driving;

    public string Name
    {
        get { return "Transporting"; }
    }

    public void Enter(Ambulance ambulance)
    {
        _driving = ambulance.DriveTo(ambulance.Hospital.Node);
    }

    public void Update(Ambulance ambulance, float deltaSeconds)
    {
        if (!_driving)
        {
            _driving = ambulance.DriveTo(ambulance.Hospital.Node);
            return;
        }

        if (ambulance.HasArrived)
        {
            ambulance.CompleteCall();
            ambulance.ChangeState(new IdleState());
        }
    }
}