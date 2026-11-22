using DispatchCity.Rendering;
using DispatchCity.Simulation;
using DispatchCity.UI;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace DispatchCity;

// Prozor: spaja unos, CPU (GameWorld) i GPU (Renderer + panel). Sama ne crta i ne računa pravila igre.
// Prozor je podeljen na mapu (levo) i kontrolni panel (desno, širina je razmer prozora).
public class Game : GameWindow
{
    private readonly GameWorld _world;
    private readonly SidePanel _panel = new SidePanel();
    private Renderer? _renderer;
    private ImGuiController? _imgui;
    private int _referenceHeight;

    public Game(GameWindowSettings gameSettings, NativeWindowSettings nativeSettings, GameWorld world)
        : base(gameSettings, nativeSettings)
    {
        _world = world;
    }

    private int PanelWidth(int windowWidth)
    {
        return (int)(windowWidth * _world.Config.PanelWidthRatio);
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        int mapWidth = ClientSize.X - PanelWidth(ClientSize.X);
        _renderer = new Renderer(_world, mapWidth, ClientSize.Y);
        _renderer.Initialize();

        _imgui = new ImGuiController(ClientSize.X, ClientSize.Y);

        // Početna visina prozora je mera: panel je veći onoliko koliko je prozor veći od nje.
        _referenceHeight = ClientSize.Y;
        ApplyUiScale();
    }

    private void ApplyUiScale()
    {
        // Minimizovan prozor ima visinu 0, pa se tada skala ne menja.
        if (_imgui == null || _referenceHeight <= 0 || ClientSize.Y <= 0)
        {
            return;
        }

        float scale = ClientSize.Y / (float)_referenceHeight;
        _imgui.SetScale(scale);
        UiTheme.Apply(scale);
    }

    // CPU: logika igre.
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        _world.Update((float)e.Time);
    }

    // GPU: prvo mapa, pa panel preko nje.
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        _renderer?.Render();

        if (_imgui != null)
        {
            int panelWidth = PanelWidth(ClientSize.X);
            _imgui.Update(this, (float)e.Time);
            _panel.Draw(_world, ClientSize.X - panelWidth, panelWidth, ClientSize.Y);
            _imgui.Render();
        }

        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        _renderer?.Resize(e.Width - PanelWidth(e.Width), e.Height);
        _imgui?.WindowResized(e.Width, e.Height);
        ApplyUiScale();
    }

    // Prevlačenje levim tasterom pomera kameru (ne i kad je miš iznad panela).
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        if (_renderer != null && _imgui != null && !_imgui.WantsMouse
            && MouseState.IsButtonDown(MouseButton.Left))
        {
            _renderer.Camera.Pan(new System.Numerics.Vector2(e.Delta.X, e.Delta.Y));
        }
    }

    // Točkić zumira ka kursoru (ili pomera listu u panelu, ako je miš iznad njega).
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        _imgui?.AddMouseWheel(e.OffsetY);

        if (_renderer != null && _imgui != null && !_imgui.WantsMouse)
        {
            var cursor = new System.Numerics.Vector2(MousePosition.X, MousePosition.Y);
            _renderer.Camera.ZoomAt(cursor, MathF.Pow(1.1f, e.OffsetY));
        }
    }

    protected override void OnUnload()
    {
        _imgui?.Dispose();
        _renderer?.Dispose();
        base.OnUnload();
    }
}