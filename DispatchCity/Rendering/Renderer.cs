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
    private int _mapWidth;
    private int _mapHeight;
    private Shader? _flatShader;
    private Shader? _instancedShader;
    private PostProcessPass? _post;
    private PostEffects? _effects;

    public Camera2D Camera { get; }

    // Podešavanja prikaza (toplotna mapa...) koja menjaju panel i tasteri.
    public ViewOptions Options { get; } = new ViewOptions();

    public Renderer(GameWorld world, int viewportWidth, int viewportHeight)
    {
        _world = world;
        _mapWidth = viewportWidth;
        _mapHeight = viewportHeight;

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

        _flatShader = new Shader(ShaderSources.FlatVertex, ShaderSources.RoadFragment);
        _instancedShader = new Shader(ShaderSources.InstancedVertex, ShaderSources.Fragment);

        // Cela mapa se crta u teksturu (FBO), pa je završni prolaz obrađuje i prikazuje.
        _post = new PostProcessPass(_mapWidth, _mapHeight);
        _effects = new PostEffects(_world);

        // Redosled je redosled crtanja: ono što je kasnije, nalazi se preko prethodnog.
        _layers.Add(new GroundLayer());
        _layers.Add(new RoadLayer(_flatShader, Options));
        _layers.Add(new HospitalLayer(_instancedShader));
        _layers.Add(new CallLayer(_instancedShader));
        _layers.Add(new SpriteLayer());

        foreach (IRenderLayer layer in _layers)
        {
            layer.Initialize(_world);
        }
    }

    public void Render()
    {
        // Mapa zauzima samo svoj deo prozora (ostalo je panel). ImGui posle ovoga
        // postavlja ceo prozor, pa se viewport mora postaviti svaki frame.
        _effects!.Update();
        _post!.Begin();

        foreach (IRenderLayer layer in _layers)
        {
            layer.Update(_world);
            layer.Render(Camera);
        }

        _post.End(Camera, _effects);
    }

    // Veličina dela prozora u kome se crta mapa (bez panela).
    public void Resize(int mapWidth, int mapHeight)
    {
        _mapWidth = mapWidth;
        _mapHeight = mapHeight;
        Camera.SetViewport(mapWidth, mapHeight);
        _post?.Resize(mapWidth, mapHeight);
    }

    public void Dispose()
    {
        foreach (IRenderLayer layer in _layers)
        {
            layer.Dispose();
        }
        _layers.Clear();
        _post?.Dispose();
        _effects?.Dispose();
    }
}