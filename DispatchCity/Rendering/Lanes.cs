using System.Numerics;
using DispatchCity.Core;
using DispatchCity.Simulation;

namespace DispatchCity.Rendering;

// Trake su samo stvar crtanja: simulacija vozilo vodi po sredini ulice, a ovde se
// pri crtanju pomeri u svoju traku (samo na avenijama, i samo kad su dvosmerne).
public static class Lanes
{
    public static Vector2 PositionOf(Ambulance ambulance, GameWorld world)
    {
        return ambulance.Position + OffsetOf(ambulance, world);
    }

    private static Vector2 OffsetOf(Ambulance ambulance, GameWorld world)
    {
        Edge? edge = ambulance.CurrentEdge;
        if (edge == null || edge.Type != RoadType.Avenue)
        {
            return Vector2.Zero;
        }

        // Jednosmerna avenija ima jednu traku, pa vozilo ostaje na sredini.
        if (world.Graph.FindEdge(edge.To, edge.From) == null)
        {
            return Vector2.Zero;
        }

        // Ekran ima Y osu nadole: desna strana pravca vožnje je (-y, x), leva je (y, -x).
        Vector2 direction = Vector2.Normalize(edge.To.Position - edge.From.Position);
        Vector2 normal = world.Config.DriveOnLeft
            ? new Vector2(direction.Y, -direction.X)
            : new Vector2(-direction.Y, direction.X);

        return normal * world.Config.Metrics.LaneOffset(RoadType.Avenue);
    }
}