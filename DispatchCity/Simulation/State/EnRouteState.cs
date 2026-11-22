using DispatchCity.Simulation.State;

namespace DispatchCity.Simulation.States;

// Vozilo vozi ka pozivu najbržim putem (A*). Kad stigne, prelazi na lice mesta.
public class EnRouteState : IAmbulanceState
{
    private bool _driving;

    public string Name
    {
        get { return "EnRoute"; }
    }

    public void Enter(Ambulance ambulance)
    {
        TryStart(ambulance);
    }

    public void Update(Ambulance ambulance, float deltaSeconds)
    {
        // Ako put nije nađen, čekamo i pokušavamo ponovo umesto da se vozilo zaglavi.
        if (!_driving)
        {
            TryStart(ambulance);
            return;
        }

        if (ambulance.HasArrived)
        {
            ambulance.ChangeState(new OnSceneState());
        }
    }

    private void TryStart(Ambulance ambulance)
    {
        if (ambulance.Call == null)
        {
            ambulance.ChangeState(new ReturningState());
            return;
        }
        _driving = ambulance.DriveTo(ambulance.Call.Location);
    }
}