using DispatchCity.Config;
using DispatchCity.Simulation;

namespace DispatchCity.Rendering;

// Pozivi na mapi. CPU kaže koliko je pacijent zdrav, a GPU crta krugove.
// Prsten pokazuje težinu povrede, veličina prioritet, a ispuna zdravlje:
// što je pacijent lošije, ispuna je manja i narandžastija.
public class CallLayer : InstancedCircleLayer
{
    // Razmeri prema CallBaseRadius.
    private const float LowScale = 0.8f;
    private const float MediumScale = 1.0f;
    private const float HighScale = 1.3f;

    // Ispuna se smanjuje sa 80% na 30% prstena kako zdravlje pada.
    private const float FullFill = 0.8f;
    private const float EmptyFill = 0.3f;

    public CallLayer(Shader shader) : base(shader)
    {
    }

    protected override bool IsDynamic
    {
        get { return true; }
    }

    protected override void Collect(GameWorld world, List<CircleInstance> into)
    {
        WorldMetrics metrics = world.Config.Metrics;

        foreach (EmergencyCall call in world.Calls)
        {
            float ringRadius = metrics.CallBaseRadius * ScaleOf(call.Priority);
            float health = call.Patient.Health;
            float fillRadius = ringRadius * (EmptyFill + (FullFill - EmptyFill) * health);

            into.Add(new CircleInstance(call.Location.Position, ringRadius, Palette.CallRing(call.Severity)));
            into.Add(new CircleInstance(call.Location.Position, fillRadius, Palette.HealthColor(health)));
        }
    }

    private static float ScaleOf(Priority priority)
    {
        switch (priority)
        {
            case Priority.High:
                return HighScale;
            case Priority.Low:
                return LowScale;
            default:
                return MediumScale;
        }
    }
}