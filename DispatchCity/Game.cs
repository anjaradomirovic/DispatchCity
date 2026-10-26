using DispatchCity.Core;
using DispatchCity.Rendering;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace DispatchCity;

public class Game : GameWindow
{
    private const string VertexSource =
    "#version 330 core\n" +
    "layout(location = 0) in vec2 aPos;\n" +
    "layout(location = 1) in vec3 aColor;\n" +
    "uniform vec2 uWorldSize;\n" +
    "out vec3 vColor;\n" +
    "void main()\n" +
    "{\n" +
    "    vec2 n = aPos / uWorldSize;\n" +
    "    gl_Position = vec4(n.x * 2.0 - 1.0, 1.0 - n.y * 2.0, 0.0, 1.0);\n" +
    "    vColor = aColor;\n" +
    "}\n";

    private const string FragmentSource =
        "#version 330 core\n" +
        "in vec3 vColor;\n" +
        "out vec4 FragColor;\n" +
        "void main()\n" +
        "{\n" +
        "    FragColor = vec4(vColor, 1.0);\n" +
        "}\n";
    private readonly CityGraph _graph;
    private readonly float _worldWidth;
    private readonly float _worldHeight;

    private Shader? _shader;
    private RoadLayer? _roadLayer;

    public Game(GameWindowSettings gameSettings, NativeWindowSettings nativeSettings,
                CityGraph graph, float worldWidth, float worldHeight)
        : base(gameSettings, nativeSettings)
    {
        _graph = graph;
        _worldWidth = worldWidth;
        _worldHeight = worldHeight;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0.965f, 0.953f, 0.918f, 1f);

        _shader = new Shader(VertexSource, FragmentSource);
        _roadLayer = new RoadLayer();
        _roadLayer.Load(_graph);
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        if (_shader != null && _roadLayer != null)
        {
            _shader.Use();
            _shader.SetVector2("uWorldSize", _worldWidth, _worldHeight);
            _roadLayer.Render();
        }

        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }
}