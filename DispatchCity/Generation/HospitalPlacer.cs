using System.Numerics;
using DispatchCity.Core;

namespace DispatchCity.Generation;

public static class HospitalPlacer
{
    // Bolnica ide na raskrsnicu najbližu centru mape.
    // Pošto je graf jako povezan, iz nje se stiže svuda i nazad.
    public static Node FindCentralNode(CityGraph graph, float width, float height)
    {
        var center = new Vector2(width / 2f, height / 2f);
        Node best = graph.Nodes[0];
        float bestDistance = Vector2.DistanceSquared(best.Position, center);

        foreach (Node node in graph.Nodes)
        {
            float distance = Vector2.DistanceSquared(node.Position, center);
            if (distance < bestDistance)
            {
                best = node;
                bestDistance = distance;
            }
        }
        return best;
    }
}