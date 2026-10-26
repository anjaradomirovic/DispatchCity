using System.Numerics;
using DispatchCity.Simulation;
using OpenTK.Graphics.OpenGL4;

namespace DispatchCity.Rendering;

// Facade: Game poziva samo Initialize / Render / Resize, a ovde se krije sve ostalo
// (shaderi, slojevi, kamera, redosled crtanja).
public class Renderer : IDisposable
{
    private readonly GameWorld _world;
    private readonly List<IRenderLayer> _layers = new List<IRenderLayer>();
    private Shader? _flatShader;
    private Shader? _instancedShader;

    public Camera2D Camera { get; }

    public Renderer(GameWorld world, int viewportWidth, int viewportHeight)
    {
        _world = world;

        // Granice kamere: mapa plus pojas okruženja oko nje.
        float margin = world.Config.WorldMargin;
        var boundsMin = new Vector2(-margin, -margin);
        var boundsMax = new Vector2(world.Config.WorldWidth + margin, world.Config.WorldHeight + margin);
        Camera = new Camera2D(boundsMin, boundsMax, viewportWidth, viewportHeight);
    }

    // Poziva se kad postoji OpenGL kontekst (OnLoad).
    public void Initialize()
    {
        Vector3 background = Palette.Background;
        GL.ClearColor(background.X, background.Y, background.Z, 1f);

        _flatShader = new Shader(ShaderSources.FlatVertex, ShaderSources.Fragment);
        _instancedShader = new Shader(ShaderSources.InstancedVertex, ShaderSources.Fragment);

        // Redosled je redosled crtanja: ono što je kasnije, nalazi se preko prethodnog.
        _layers.Add(new RoadLayer(_flatShader));
        _layers.Add(new HospitalLayer(_instancedShader));
        _layers.Add(new AmbulanceLayer(_instancedShader));

        foreach (IRenderLayer layer in _layers)
        {
            layer.Initialize(_world);
        }
    }

    public void Render()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit);

        foreach (IRenderLayer layer in _layers)
        {
            layer.Update(_world);
            layer.Render(Camera);
        }
    }

    public void Resize(int width, int height)
    {
        GL.Viewport(0, 0, width, height);
        Camera.SetViewport(width, height);
    }

    public void Dispose()
    {
        foreach (IRenderLayer layer in _layers)
        {
            layer.Dispose();
        }
        _layers.Clear();
    }
}