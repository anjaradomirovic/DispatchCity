using DispatchCity.Core;

namespace DispatchCity.Simulation;

public class EmergencyCall
{
    public int Id { get; }
    public Node Location { get; }
    public Priority Priority { get; }
    public InjurySeverity Severity { get; }
    public Patient Patient { get; }

    public CallStatus Status { get; private set; } = CallStatus.Waiting;
    public Ambulance? AssignedAmbulance { get; private set; }

    // Koliko je poziv čekao do dolaska vozila. Posle dolaska se ne povećava.
    public float WaitedSeconds { get; private set; }

    // Vreme odziva (isto što i WaitedSeconds, zabeleženo u trenutku dolaska). Ulazi u bodovanje.
    public float ResponseSeconds { get; private set; }

    public EmergencyCall(int id, Node location, Priority priority, InjurySeverity severity, Patient patient)
    {
        Id = id;
        Location = location;
        Priority = priority;
        Severity = severity;
        Patient = patient;
    }

    public void AssignTo(Ambulance ambulance)
    {
        AssignedAmbulance = ambulance;
        Status = CallStatus.Assigned;
    }

    public void MarkArrived()
    {
        Status = CallStatus.Attended;
        ResponseSeconds = WaitedSeconds;
        Patient.BeginTreatment();
    }

    public void Update(float deltaSeconds)
    {
        if (Status != CallStatus.Attended)
        {
            WaitedSeconds += deltaSeconds;
        }
        Patient.Update(deltaSeconds);
    }
}