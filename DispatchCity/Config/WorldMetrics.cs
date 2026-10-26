using DispatchCity.Core;

namespace DispatchCity.Config;

// Sve veličine u igri su razmeri jedne jedinice mere: razmaka između raskrsnica (Spacing).
// Promeni se jedinica, i ulice, bolnica, vozila i brzine se skaliraju zajedno.
// Brojevi ispod su razmeri, a ne pikseli ni svetske jedinice.
public class WorldMetrics
{
    // Širina ulice (razmer prema jedinici).
    private const float LocalWidth = 0.067f;
    private const float MainWidth = 0.107f;
    private const float AvenueWidth = 0.16f;

    // Brzina ulice (jedinica mere u sekundi).
    private const float LocalSpeed = 0.107f;
    private const float MainSpeed = 0.173f;
    private const float AvenueSpeed = 0.267f;

    public float Unit { get; }

    public WorldMetrics(float unit)
    {
        Unit = unit;
    }

    // Pojas okruženja oko mape.
    public float WorldMargin
    {
        get { return 1.6f * Unit; }
    }

    public float HospitalRingRadius
    {
        get { return 0.213f * Unit; }
    }

    public float HospitalFillRadius
    {
        get { return 0.147f * Unit; }
    }

    public float AmbulanceRadius
    {
        get { return 0.107f * Unit; }
    }

    public float AmbulanceCoreRadius
    {
        get { return 0.04f * Unit; }
    }

    public float RoadWidth(RoadType type)
    {
        switch (type)
        {
            case RoadType.Avenue:
                return AvenueWidth * Unit;
            case RoadType.Main:
                return MainWidth * Unit;
            default:
                return LocalWidth * Unit;
        }
    }

    public float SpeedOf(RoadType type)
    {
        switch (type)
        {
            case RoadType.Avenue:
                return AvenueSpeed * Unit;
            case RoadType.Main:
                return MainSpeed * Unit;
            default:
                return LocalSpeed * Unit;
        }
    }
}