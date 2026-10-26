using DispatchCity.Config;
using DispatchCity.Simulation;

namespace DispatchCity.Rendering;

public class AmbulanceLayer : InstancedCircleLayer
{
    public AmbulanceLayer(Shader shader) : base(shader)
    {
    }

    protected override bool IsDynamic
    {
        get { return true; }
    }

    // CPU svaki frame samo pročita položaje vozila, a krugove crta GPU.
    protected override void Collect(GameWorld world, List<CircleInstance> into)
    {
        WorldMetrics metrics = world.Config.Metrics;
        foreach (Ambulance ambulance in world.Ambulances)
        {
            into.Add(new CircleInstance(ambulance.Position, metrics.AmbulanceRadius, Palette.Ambulance));
            into.Add(new CircleInstance(ambulance.Position, metrics.AmbulanceCoreRadius, Palette.HospitalFill));
        }
    }
}