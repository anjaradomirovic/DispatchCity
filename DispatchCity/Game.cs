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
        HandleCameraKeys((float)e.Time);
        _world.Update((float)e.Time);
    }

    // GPU: prvo mapa, pa panel preko nje.
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        _renderer?.Render();

        if (_imgui != null && _renderer != null)
        {
            int panelWidth = PanelWidth(ClientSize.X);
            _imgui.Update(this, (float)e.Time);
            _panel.Draw(_world, _renderer.Options, ClientSize.X - panelWidth, panelWidth, ClientSize.Y);
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

    // Space pauzira i nastavlja igru.
    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Keys.Space:
                _world.IsPaused = !_world.IsPaused;
                break;
            case Keys.H:
                if (_renderer != null) _renderer.Options.ShowHeatMap = !_renderer.Options.ShowHeatMap;
                break;
            case Keys.G:
                if (_renderer != null) _renderer.Options.ShowTraffic = !_renderer.Options.ShowTraffic;
                break;
            case Keys.D1:
                _world.SpeedMultiplier = 1f;
                break;
            case Keys.D2:
                _world.SpeedMultiplier = 2f;
                break;
            case Keys.D3:
                _world.SpeedMultiplier = 4f;
                break;
            case Keys.Tab:
                CycleDispatchMode();
                break;
            case Keys.Enter:
                DispatchNow();
                break;
            case Keys.Escape:
                Close();
                break;
        }
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
    private const float CameraKeySpeed = 500f;   // pikseli u sekundi

    // Kamera se pomera dok je taster pritisnut: pomeraj = brzina * dt (isto kao kretanje u terminu 4).
    private void HandleCameraKeys(float deltaSeconds)
    {
        if (_renderer == null)
        {
            return;
        }

        float dx = 0f;
        float dy = 0f;
        if (KeyboardState.IsKeyDown(Keys.A) || KeyboardState.IsKeyDown(Keys.Left)) dx -= 1f;
        if (KeyboardState.IsKeyDown(Keys.D) || KeyboardState.IsKeyDown(Keys.Right)) dx += 1f;
        if (KeyboardState.IsKeyDown(Keys.W) || KeyboardState.IsKeyDown(Keys.Up)) dy -= 1f;
        if (KeyboardState.IsKeyDown(Keys.S) || KeyboardState.IsKeyDown(Keys.Down)) dy += 1f;

        if (dx != 0f || dy != 0f)
        {
            // Pan pomera svet suprotno od pomeraja, pa se znak okreće.
            _renderer.Camera.Pan(new System.Numerics.Vector2(-dx, -dy) * CameraKeySpeed * deltaSeconds);
        }

        float zoom = 0f;
        if (KeyboardState.IsKeyDown(Keys.E)) zoom += 1f;
        if (KeyboardState.IsKeyDown(Keys.Q)) zoom -= 1f;

        if (zoom != 0f)
        {
            var mapCenter = new System.Numerics.Vector2((ClientSize.X - PanelWidth(ClientSize.X)) / 2f, ClientSize.Y / 2f);
            _renderer.Camera.ZoomAt(mapCenter, MathF.Pow(2f, zoom * deltaSeconds));
        }
    }

    // Ručno -> prva strategija -> druga strategija -> ručno.
    private void CycleDispatchMode()
    {
        var strategies = _world.Strategies;
        int current = -1;
        for (int i = 0; i < strategies.Count; i++)
        {
            if (strategies[i] == _world.AutoDispatch)
            {
                current = i;
            }
        }

        _world.AutoDispatch = current + 1 < strategies.Count ? strategies[current + 1] : null;
    }

    // Jedan potez strategije odmah, i kad je izabrano ručno.
    private void DispatchNow()
    {
        if (_world.Strategies.Count == 0)
        {
            return;
        }

        var strategy = _world.AutoDispatch ?? _world.Strategies[0];
        foreach (AssignAmbulanceCommand command in strategy.Decide(_world))
        {
            _world.Execute(command);
        }
    }
}