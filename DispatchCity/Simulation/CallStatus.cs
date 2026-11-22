namespace DispatchCity.Simulation;

public enum CallStatus
{
    Waiting,    // niko još nije krenuo
    Assigned,   // vozilo je na putu
    Attended    // vozilo je stiglo, pacijent se zbrinjava
}