using DispatchCity.Rendering;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.CompilerServices;

namespace DispatchCity.UI;

// Most između ImGui-ja i OpenGL-a: prosleđuje miša u ImGui i crta ono što ImGui vrati.
// ImGui sam ne crta ništa. Daje listu trouglova (CPU), a ovde ih šaljemo GPU-u.
public class ImGuiController : IDisposable
{
    private const string VertexSource =
        "#version 330 core\n" +
        "uniform mat4 projection_matrix;\n" +
        "layout(location = 0) in vec2 in_position;\n" +
        "layout(location = 1) in vec2 in_texCoord;\n" +
        "layout(location = 2) in vec4 in_color;\n" +
        "out vec4 color;\n" +
        "out vec2 texCoord;\n" +
        "void main()\n" +
        "{\n" +
        "    gl_Position = projection_matrix * vec4(in_position, 0, 1);\n" +
        "    color = in_color;\n" +
        "    texCoord = in_texCoord;\n" +
        "}\n";

    private const string FragmentSource =
        "#version 330 core\n" +
        "uniform sampler2D in_fontTexture;\n" +
        "in vec4 color;\n" +
        "in vec2 texCoord;\n" +
        "out vec4 outputColor;\n" +
        "void main()\n" +
        "{\n" +
        "    outputColor = color * texture(in_fontTexture, texCoord);\n" +
        "}\n";

    private int _vertexArray;
    private int _vertexBuffer;
    private int _indexBuffer;
    private int _fontTexture;
    private Shader? _shader;
    private int _projectionLocation;
    private int _textureLocation;

    private int _windowWidth;
    private int _windowHeight;
    private bool _frameBegun;

    // Kad je miš iznad panela, mapa ne sme da reaguje (prevlačenje, zum).
    public bool WantsMouse
    {
        get { return ImGui.GetIO().WantCaptureMouse; }
    }

    public ImGuiController(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;

        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.AddFontDefault();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        CreateDeviceResources();
    }

    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    // Skalira ceo tekst panela (1 = početna veličina). Razmaci se skaliraju u UiTheme.Apply.
    public void SetScale(float scale)
    {
        ImGui.GetIO().FontGlobalScale = scale;
    }

    public void AddMouseWheel(float offsetY)
    {
        ImGui.GetIO().AddMouseWheelEvent(0f, offsetY);
    }

    // Početak frame-a: stanje miša ide u ImGui, posle ovoga se crta panel (ImGui.Begin...).
    public void Update(GameWindow window, float deltaSeconds)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(_windowWidth, _windowHeight);
        io.DisplayFramebufferScale = System.Numerics.Vector2.One;
        io.DeltaTime = deltaSeconds > 0f ? deltaSeconds : 1f / 60f;

        MouseState mouse = window.MouseState;
        io.AddMousePosEvent(mouse.X, mouse.Y);
        io.AddMouseButtonEvent(0, mouse.IsButtonDown(MouseButton.Left));
        io.AddMouseButtonEvent(1, mouse.IsButtonDown(MouseButton.Right));
        io.AddMouseButtonEvent(2, mouse.IsButtonDown(MouseButton.Middle));

        ImGui.NewFrame();
        _frameBegun = true;
    }

    // Kraj frame-a: ImGui pravi trouglove, a mi ih crtamo.
    public void Render()
    {
        if (!_frameBegun)
        {
            return;
        }
        _frameBegun = false;

        ImGui.Render();
        RenderDrawData(ImGui.GetDrawData());
    }

    public void Dispose()
    {
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);
        GL.DeleteTexture(_fontTexture);
        GL.DeleteProgram(_shader?.Handle ?? 0);
        ImGui.DestroyContext();
    }

    private void CreateDeviceResources()
    {
        _vertexArray = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArray);

        _vertexBuffer = GL.GenBuffer();
        _indexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);

        // ImDrawVert: pozicija (2 float), uv (2 float), boja (4 bajta).
        int stride = Unsafe.SizeOf<ImDrawVert>();
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);
        GL.EnableVertexAttribArray(2);

        _shader = new Shader(VertexSource, FragmentSource);
        _projectionLocation = GL.GetUniformLocation(_shader.Handle, "projection_matrix");
        _textureLocation = GL.GetUniformLocation(_shader.Handle, "in_fontTexture");

        CreateFontTexture();
    }

    // Slova su slika (atlas) koju ImGui napravi na CPU-u, a mi je šaljemo GPU-u kao teksturu.
    private void CreateFontTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int _);

        _fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        io.Fonts.SetTexID((IntPtr)_fontTexture);
        io.Fonts.ClearTexData();
    }

    private void RenderDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.CmdListsCount == 0 || _shader == null)
        {
            return;
        }

        // Panel se crta preko celog prozora, a ne samo preko dela sa mapom.
        GL.Viewport(0, 0, _windowWidth, _windowHeight);

        ImGuiIOPtr io = ImGui.GetIO();
        Matrix4 projection = Matrix4.CreateOrthographicOffCenter(
            0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, -1f, 1f);

        GL.UseProgram(_shader.Handle);
        GL.UniformMatrix4(_projectionLocation, false, ref projection);
        GL.Uniform1(_textureLocation, 0);

        GL.BindVertexArray(_vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);

        // Providnost (alfa blending) za zaobljene ivice i senke, bez dubine i odsecanja stranica.
        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.ScissorTest);

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr commandList = drawData.CmdLists[n];

            GL.BufferData(BufferTarget.ArrayBuffer,
                commandList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(),
                commandList.VtxBuffer.Data, BufferUsageHint.StreamDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                commandList.IdxBuffer.Size * sizeof(ushort),
                commandList.IdxBuffer.Data, BufferUsageHint.StreamDraw);

            for (int i = 0; i < commandList.CmdBuffer.Size; i++)
            {
                ImDrawCmdPtr command = commandList.CmdBuffer[i];

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, (int)command.GetTexID());

                // ImGui šalje pravougaonik van koga se ne crta (npr. da se tekst ne prelije iz panela).
                System.Numerics.Vector4 clip = command.ClipRect;
                GL.Scissor((int)clip.X, _windowHeight - (int)clip.W,
                    (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));

                GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)command.ElemCount,
                    DrawElementsType.UnsignedShort,
                    (IntPtr)(command.IdxOffset * sizeof(ushort)),
                    unchecked((int)command.VtxOffset));
            }
        }

        GL.Disable(EnableCap.ScissorTest);
        GL.Disable(EnableCap.Blend);
    }
}