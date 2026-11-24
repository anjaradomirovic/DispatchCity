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

    // Gužva boji ulicu od njene boje ka bledožutoj (srednja), pa narandžastoj (jaka). Bez oštrih crvenih tonova.
    public static Vector3 JamMid
    {
        get { return FromHex(0xF6D77A); }
    }

    public static Vector3 JamHigh
    {
        get { return FromHex(0xF08A3C); }
    }

    // Rampa toplotne mape: blizu bolnice je bledo, daleko je tamnije. Bez oštrih crvenih tonova.
    public static Vector3 HeatNear
    {
        get { return FromHex(0xF6EBB5); }
    }

    public static Vector3 HeatMid
    {
        get { return FromHex(0xF4B66E); }
    }

    public static Vector3 HeatFar
    {
        get { return FromHex(0xD9695F); }
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

    // Zatvorena ulica.
    public static Vector3 ClosedRoad
    {
        get { return FromHex(0x9E3B33); }
    }

    // Uvodni ekran.
    public static Vector3 MenuSkyTop { get { return FromHex(0x6FB1E6); } }
    public static Vector3 MenuSkyBottom { get { return FromHex(0xD6EEFA); } }
    public static Vector3 MenuGrassFar { get { return FromHex(0x9ED36B); } }
    public static Vector3 MenuGrassNear { get { return FromHex(0x5FA844); } }
    public static Vector3 MenuCloud { get { return FromHex(0xFFFFFF); } }
    public static Vector3 MenuTrack { get { return FromHex(0x8A5A3C); } }
    public static Vector3 MenuTrackEdge { get { return FromHex(0x4A2E1C); } }
    public static Vector3 MenuPlate { get { return FromHex(0x6D1A2E); } }

    // Tamna ivica oko ulica, da se jasno vide na pozadini.
    public static Vector3 RoadBorder
    {
        get { return FromHex(0x3B3733); }
    }

    // Pozadina (teren ispod ulica).
    public static Vector3 Grass
    {
        get { return FromHex(0xDCE8C4); }
    }

    public static Vector3 Forest
    {
        get { return FromHex(0xC3D9A8); }
    }

    public static Vector3 Water
    {
        get { return FromHex(0xB5D6E3); }
    }

    public static Vector3 Shore
    {
        get { return FromHex(0xEFE6C8); }
    }

    // Završna obrada: vinjeta pri porazu i sjaj pri spašenom pacijentu.
    public static Vector3 VignetteRed
    {
        get { return FromHex(0xC8402F); }
    }

    public static Vector3 GlowWarm
    {
        get { return FromHex(0x5A4A22); }
    }

    private static Vector3 FromHex(int rgb)
    {
        float r = ((rgb >> 16) & 0xFF) / 255f;
        float g = ((rgb >> 8) & 0xFF) / 255f;
        float b = (rgb & 0xFF) / 255f;
        return new Vector3(r, g, b);
    }
}