using DispatchCity.Config;

namespace DispatchCity.Simulation;

// Rezultat igre: spaseni, izgubljeni, prosecno vreme odziva i bodovi.
// Cista logika (CPU), bez ikakvog crtanja: panel ga samo cita.
public class ScoreBoard
{
    private readonly GameConfig _config;
    private float _totalResponseSeconds;

    public int Saved { get; private set; }
    public int Lost { get; private set; }
    public int Points { get; private set; }

    public float AverageResponseSeconds
    {
        get { return Saved == 0 ? 0f : _totalResponseSeconds / Saved; }
    }

    public ScoreBoard(GameConfig config)
    {
        _config = config;
    }

    public void RecordSaved(EmergencyCall call)
    {
        Saved++;
        _totalResponseSeconds += call.ResponseSeconds;
        Points += PointsFor(call.Priority);
    }

    public void RecordLost()
    {
        Lost++;
    }

    // Visok prioritet vredi vise (bonus za spasavanje onih kojima je najhitnije).
    private int PointsFor(Priority priority)
    {
        switch (priority)
        {
            case Priority.High:
                return _config.PointsHigh;
            case Priority.Medium:
                return _config.PointsMedium;
            default:
                return _config.PointsLow;
        }
    }
}