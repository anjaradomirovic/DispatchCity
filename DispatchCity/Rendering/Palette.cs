using System.Numerics;
using DispatchCity.Core;
using DispatchCity.Simulation;

namespace DispatchCity.Rendering;

// Sve boje na jednom mestu. Menjaš ovde, promeni se u celoj igri.
public static class Palette
{
    public static Vector3 Background
    {
        get { return FromHex(0xF6F3EA); }
    }

    public static Vector3 RoadColor(RoadType type)
    {
        switch (type)
        {
            case RoadType.Avenue:
                return FromHex(0xF6CF8E);   // topla svetlonarandžasta
            case RoadType.Main:
                return FromHex(0xF1E2B8);   // svetložuta
            default:
                return FromHex(0xDDD8CB);   // svetlosiva
        }
    }

    // Strelica na jednosmernoj ulici.
    public static Vector3 OneWayArrow
    {
        get { return FromHex(0xA8A18E); }
    }

    public static Vector3 HospitalRing
    {
        get { return FromHex(0xF08A7E); }   // koral
    }

    // Poziv: boja ispune ide od bledožute (zdrav) do narandžaste (kritičan), bez oštrih crvenih tonova.
    public static Vector3 HealthColor(float health)
    {
        Vector3 critical = FromHex(0xF08A3C);
        Vector3 healthy = FromHex(0xF4E3A1);
        return Vector3.Lerp(critical, healthy, Math.Clamp(health, 0f, 1f));
    }

    // Prsten poziva: laka povreda je neutralna, teška je koral.
    public static Vector3 CallRing(InjurySeverity severity)
    {
        return severity == InjurySeverity.Heavy ? FromHex(0xE5634F) : FromHex(0xB9B29F);
    }

    public static Vector3 Ambulance
    {
        get { return FromHex(0xE5634F); }   // tamniji koral
    }

    public static Vector3 HospitalFill
    {
        get { return FromHex(0xFFFFFF); }
    }

    private static Vector3 FromHex(int rgb)
    {
        float r = ((rgb >> 16) & 0xFF) / 255f;
        float g = ((rgb >> 8) & 0xFF) / 255f;
        float b = (rgb & 0xFF) / 255f;
        return new Vector3(r, g, b);
    }
}