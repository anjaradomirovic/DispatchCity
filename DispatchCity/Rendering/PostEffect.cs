using System.Numerics;
using DispatchCity.Simulation;

namespace DispatchCity.Rendering;

// CPU strana završne obrade: pamti koliko je koji efekat jak (0 do 1) i kako slabi.
// Čuje događaje igre (Observer), a GPU (PostProcessPass) samo čita ove brojeve.
// Efekti teku u pravom vremenu, pa rade i kad je igra pauzirana.
public class PostEffects : IDisposable
{
    // Koliko traje prelaz u sivo i nazad (sekunde).
    private const float GrayFadeSeconds = 0.4f;

    // Koliko traje crvena vinjeta posle izgubljenog pacijenta i sjaj posle spasenog (sekunde).
    private const float VignetteFadeSeconds = 1.2f;
    private const float GlowFadeSeconds = 1.0f;

    // Vinjeta koja ostaje na kraju nivoa ako je izgubljeno više nego spaseno.
    private const float DefeatVignette = 0.6f;

    // Poluprečnik sjaja u jedinicama mere.
    private const float GlowRadiusInUnits = 3f;

    private readonly GameWorld _world;
    private float _vignetteImpulse;

    public float Gray { get; private set; }
    public float Vignette { get; private set; }
    public float Glow { get; private set; }
    public Vector2 GlowPosition { get; private set; }
    public float GlowRadius { get; }

    public PostEffects(GameWorld world)
    {
        _world = world;
        GlowRadius = GlowRadiusInUnits * world.Config.Metrics.Unit;
        _world.PatientDied += OnPatientDied;
        _world.PatientSaved += OnPatientSaved;
    }

    private void OnPatientDied(EmergencyCall call)
    {
        _vignetteImpulse = 1f;
    }

    // Sjaj se pali kod bolnice, jer je tu pacijent spasen.
    private void OnPatientSaved(EmergencyCall call)
    {
        Glow = 1f;
        GlowPosition = _world.Hospital.Node.Position;
    }

    public void Update()
    {
        float delta = RealTime.Delta;

        float grayTarget = _world.IsPaused ? 1f : 0f;
        Gray = MoveToward(Gray, grayTarget, delta / GrayFadeSeconds);

        _vignetteImpulse = MathF.Max(0f, _vignetteImpulse - delta / VignetteFadeSeconds);
        bool defeat = _world.IsFinished && _world.Score.Lost > _world.Score.Saved;
        Vignette = MathF.Max(_vignetteImpulse, defeat ? DefeatVignette : 0f);

        Glow = MathF.Max(0f, Glow - delta / GlowFadeSeconds);
    }

    private static float MoveToward(float value, float target, float maxStep)
    {
        if (value < target)
        {
            return MathF.Min(value + maxStep, target);
        }
        return MathF.Max(value - maxStep, target);
    }

    public void Dispose()
    {
        _world.PatientDied -= OnPatientDied;
        _world.PatientSaved -= OnPatientSaved;
    }
}