using DispatchCity.Simulation;
using OpenTK.Graphics.OpenGL4;

namespace DispatchCity.Rendering;

// Template Method: ovde je ceo OpenGL postupak (bafer, instanciranje, crtanje) koji je
// isti za sve slojeve sa krugovima. Konkretan sloj samo kaže KOJE krugove crta (Collect).
public abstract class InstancedCircleLayer : IRenderLayer
{
    private const int Segments = 24;
    private const int FloatsPerInstance = 6;   // x, y, poluprečnik, r, g, b

    private readonly Shader _shader;
    private readonly List<CircleInstance> _instances = new List<CircleInstance>();
    private int _vao;
    private int _meshVbo;       // jedan jedinični krug, isti za sve instance
    private int _instanceVbo;   // podaci po instanci
    private int _count;
    private bool _uploaded;

    protected InstancedCircleLayer(Shader shader)
    {
        _shader = shader;
    }

    // Statični slojevi (bolnica) se šalju na GPU jednom, dinamični (vozila) svaki frame.
    protected abstract bool IsDynamic { get; }

    // Koje krugove sloj trenutno crta. Poziva se na CPU, bez ikakvog OpenGL-a.
    protected abstract void Collect(GameWorld world, List<CircleInstance> into);

    public void Initialize(GameWorld world)
    {
        _vao = GL.GenVertexArray();
        _meshVbo = GL.GenBuffer();
        _instanceVbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);

        // Jedinični krug kao "lepeza" trouglova: centar pa tačke na obodu (poslednja je ista kao prva).
        var mesh = new float[(Segments + 2) * 2];
        mesh[0] = 0f;
        mesh[1] = 0f;
        for (int i = 0; i <= Segments; i++)
        {
            float angle = (float)(i * 2.0 * Math.PI / Segments);
            mesh[2 + i * 2] = MathF.Cos(angle);
            mesh[3 + i * 2] = MathF.Sin(angle);
        }

        GL.BindBuffer(BufferTarget.ArrayBuffer, _meshVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, mesh.Length * sizeof(float), mesh, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        // Podaci po instanci: divisor 1 znači da se atribut menja po instanci, a ne po vrhu.
        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);
        int stride = FloatsPerInstance * sizeof(float);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribDivisor(1, 1);
        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribDivisor(2, 1);

        Upload(world);
    }

    public void Update(GameWorld world)
    {
        if (IsDynamic || !_uploaded)
        {
            Upload(world);
        }
    }

    public void Render(Camera2D camera)
    {
        if (_count == 0)
        {
            return;
        }

        _shader.Use();
        _shader.SetMatrix("uViewProj", camera.ViewProjection);

        GL.BindVertexArray(_vao);
        GL.DrawArraysInstanced(PrimitiveType.TriangleFan, 0, Segments + 2, _count);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_meshVbo);
        GL.DeleteBuffer(_instanceVbo);
        GL.DeleteVertexArray(_vao);
    }

    private void Upload(GameWorld world)
    {
        _instances.Clear();
        Collect(world, _instances);
        _count = _instances.Count;

        var data = new float[_count * FloatsPerInstance];
        for (int i = 0; i < _count; i++)
        {
            CircleInstance c = _instances[i];
            int o = i * FloatsPerInstance;
            data[o] = c.Position.X;
            data[o + 1] = c.Position.Y;
            data[o + 2] = c.Radius;
            data[o + 3] = c.Color.X;
            data[o + 4] = c.Color.Y;
            data[o + 5] = c.Color.Z;
        }

        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data,
            IsDynamic ? BufferUsageHint.DynamicDraw : BufferUsageHint.StaticDraw);
        _uploaded = true;
    }
}