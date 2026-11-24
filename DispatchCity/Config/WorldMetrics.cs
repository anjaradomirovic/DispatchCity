using DispatchCity.Core;
using DispatchCity.Simulation;

namespace DispatchCity.Config;

// Sve veličine u igri su razmeri jedne jedinice mere: razmaka između raskrsnica (Spacing).
// Promeni se jedinica, i ulice, bolnica, vozila i brzine se skaliraju zajedno.
// Brojevi ispod su razmeri, a ne pikseli ni svetske jedinice.
public class WorldMetrics
{
    // Širina ulice (razmer prema jedinici).
    private const float LocalWidth = 0.09f;
    private const float MainWidth = 0.14f;
    private const float AvenueWidth = 0.22f;

    // Tamna ivica sa svake strane ulice, i položaj trake na avenijama (razmer širine ulice od sredine).
    private const float BorderWidth = 0.02f;
    private const float LaneCenterRatio = 0.25f;

    // Brzina ulice (jedinica mere u sekundi).
    private const float LocalSpeed = 0.107f;
    private const float MainSpeed = 0.173f;
    private const float AvenueSpeed = 0.267f;

    // Veličina poziva po prioritetu (razmer prema CallBaseRadius).
    private const float LowCallScale = 0.8f;
    private const float MediumCallScale = 1.0f;
    private const float HighCallScale = 1.3f;

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

    // Poluprečnik poziva srednjeg prioriteta. Ostali se skaliraju razmerom (vidi CallRadius).
    public float CallBaseRadius
    {
        get { return 0.13f * Unit; }
    }

    // Poluprečnik poziva za dati prioritet. Koriste ga i krug i slika, da uvek budu iste veličine.
    public float CallRadius(Priority priority)
    {
        switch (priority)
        {
            case Priority.High:
                return CallBaseRadius * HighCallScale;
            case Priority.Low:
                return CallBaseRadius * LowCallScale;
            default:
                return CallBaseRadius * MediumCallScale;
        }
    }

    // Poluprečnik oblasti zahvaćene gužvom (u jedinicama mere).
    public float JamRadiusMin
    {
        get { return 1.5f * Unit; }
    }

    public float JamRadiusMax
    {
        get { return 3f * Unit; }
    }

    public float RoadBorder
    {
        get { return BorderWidth * Unit; }
    }

    // Koliko je sredina trake pomerena od sredine ulice (avenija ima dve trake).
    public float LaneOffset(RoadType type)
    {
        return RoadWidth(type) * LaneCenterRatio;
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