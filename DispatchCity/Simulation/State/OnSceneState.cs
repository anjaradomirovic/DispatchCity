using DispatchCity.Simulation.State;

namespace DispatchCity.Simulation.States;

// Vozilo je na licu mesta. Pacijent više ne gubi zdravlje.
// Laka povreda: kratko zadržavanje pa povratak. Teška: utovar pa prevoz u bolnicu.
public class OnSceneState : IAmbulanceState
{
    private float _remainingSeconds;

    public string Name
    {
        get { return "OnScene"; }
    }

    public void Enter(Ambulance ambulance)
    {
        EmergencyCall? call = ambulance.Call;
        if (call == null)
        {
            ambulance.ChangeState(new ReturningState());
            return;
        }

        call.MarkArrived();
        _remainingSeconds = call.Severity == InjurySeverity.Heavy
            ? ambulance.Config.HeavyLoadSeconds
            : ambulance.Config.LightSceneSeconds;
    }

    public void Update(Ambulance ambulance, float deltaSeconds)
    {
        EmergencyCall? call = ambulance.Call;
        if (call == null)
        {
            return;
        }

        _remainingSeconds -= deltaSeconds;
        if (_remainingSeconds > 0f)
        {
            return;
        }

        if (call.Severity == InjurySeverity.Heavy)
        {
            ambulance.ChangeState(new TransportingState());
        }
        else
        {
            ambulance.CompleteCall();
            ambulance.ChangeState(new ReturningState());
        }
    }
}