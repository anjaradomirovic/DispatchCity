using DispatchCity.Rendering;
using DispatchCity.Simulation;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace DispatchCity;

// Prozor: spaja unos, CPU (GameWorld) i GPU (Renderer). Sama ne crta i ne računa pravila igre.
public class Game : GameWindow
{
    private readonly GameWorld _world;
    private Renderer? _renderer;

    public Game(GameWindowSettings gameSettings, NativeWindowSettings nativeSettings, GameWorld world)
        : base(gameSettings, nativeSettings)
    {
        _world = world;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        _renderer = new Renderer(_world, ClientSize.X, ClientSize.Y);
        _renderer.Initialize();
    }

    // CPU: logika igre.
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        _world.Update((float)e.Time);
    }

    // GPU: crtanje.
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        _renderer?.Render();
        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        _renderer?.Resize(e.Width, e.Height);
    }

    // Prevlačenje levim tasterom pomera kameru.
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        if (_renderer != null && MouseState.IsButtonDown(MouseButton.Left))
        {
            _renderer.Camera.Pan(new System.Numerics.Vector2(e.Delta.X, e.Delta.Y));
        }
    }

    // Točkić zumira ka kursoru.
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (_renderer != null)
        {
            var cursor = new System.Numerics.Vector2(MousePosition.X, MousePosition.Y);
            _renderer.Camera.ZoomAt(cursor, MathF.Pow(1.1f, e.OffsetY));
        }
    }

    protected override void OnUnload()
    {
        _renderer?.Dispose();
        base.OnUnload();
    }
}