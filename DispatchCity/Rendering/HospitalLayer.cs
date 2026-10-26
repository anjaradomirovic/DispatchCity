using DispatchCity.Config;
using DispatchCity.Simulation;

namespace DispatchCity.Rendering;

public class HospitalLayer : InstancedCircleLayer
{
    public HospitalLayer(Shader shader) : base(shader)
    {
    }

    protected override bool IsDynamic
    {
        get { return false; }
    }

    // Koral prsten sa belom ispunom. Kasnija instanca se crta preko ranije.
    protected override void Collect(GameWorld world, List<CircleInstance> into)
    {
        WorldMetrics metrics = world.Config.Metrics;
        var position = world.Hospital.Node.Position;
        into.Add(new CircleInstance(position, metrics.HospitalRingRadius, Palette.HospitalRing));
        into.Add(new CircleInstance(position, metrics.HospitalFillRadius, Palette.HospitalFill));
    }
}