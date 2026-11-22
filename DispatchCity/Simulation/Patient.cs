namespace DispatchCity.Simulation;

public class Patient
{
    private readonly float _survivalSeconds;

    // Zdravlje je razmer: 1 = zdrav, 0 = izgubljen. Tako ne zavisi od izabrane skale.
    public float Health { get; private set; } = 1f;

    // Kad vozilo stigne, zdravlje prestaje da opada.
    public bool IsBeingTreated { get; private set; }

    public bool IsDead
    {
        get { return Health <= 0f; }
    }

    // survivalSeconds: za koliko sekundi bez pomoći zdravlje padne sa 1 na 0.
    public Patient(float survivalSeconds)
    {
        _survivalSeconds = survivalSeconds;
    }

    public void BeginTreatment()
    {
        IsBeingTreated = true;
    }

    public void Update(float deltaSeconds)
    {
        if (IsBeingTreated)
        {
            return;
        }
        Health = Math.Max(0f, Health - deltaSeconds / _survivalSeconds);
    }
}