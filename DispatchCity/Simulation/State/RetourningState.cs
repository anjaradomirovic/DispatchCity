using DispatchCity.Simulation.State;

namespace DispatchCity.Simulation.States;

// Vozilo se vraća u bolnicu bez pacijenta (posle lakog slučaja ili prekinutog poziva).
public class ReturningState : IAmbulanceState
{
    private bool _driving;

    public string Name
    {
        get { return "Returning"; }
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
            ambulance.ChangeState(new IdleState());
        }
    }
}