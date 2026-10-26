using System.Numerics;
using DispatchCity.Core;

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