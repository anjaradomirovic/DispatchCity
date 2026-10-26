using System.Numerics;
using DispatchCity.Config;
using DispatchCity.Core;
using DispatchCity.Simulation;
using OpenTK.Graphics.OpenGL4;

namespace DispatchCity.Rendering;

// Ulice su statična geometrija: CPU je jednom pretvori u trouglove, GPU ih crta svaki frame.
public class RoadLayer : IRenderLayer
{
    private const int CircleSegments = 10;

    private readonly Shader _shader;
    private WorldMetrics _metrics = null!;   // postavlja se u Initialize
    private int _vao;
    private int _vbo;
    private int _vertexCount;

    public RoadLayer(Shader shader)
    {
        _shader = shader;
    }

    public void Initialize(GameWorld world)
    {
        CityGraph graph = world.Graph;
        _metrics = world.Config.Metrics;
        var data = new List<float>();

        // Redosled crtanja: ono što je kasnije, nalazi se preko prethodnog.
        AddRoadsOfType(graph, RoadType.Local, data);
        AddRoadsOfType(graph, RoadType.Main, data);
        AddRoadsOfType(graph, RoadType.Avenue, data);
        AddOneWayArrows(graph, data);

        float[] array = data.ToArray();
        _vertexCount = array.Length / 5;   // svaki vrh ima x, y, r, g, b

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, array.Length * sizeof(float), array, BufferUsageHint.StaticDraw);

        int stride = 5 * sizeof(float);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);
    }

    // Ulice se ne menjaju, nema šta da se šalje na GPU.
    public void Update(GameWorld world)
    {
    }

    public void Render(Camera2D camera)
    {
        _shader.Use();
        _shader.SetMatrix("uViewProj", camera.ViewProjection);

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
    }

    // Širina je razmer jedinice mere (WorldMetrics), pa se skalira sa gustinom grada.
    private float WidthOf(RoadType type)
    {
        return _metrics.RoadWidth(type);
    }

    private void AddRoadsOfType(CityGraph graph, RoadType type, List<float> data)
    {
        Vector3 color = Palette.RoadColor(type);
        float width = WidthOf(type);

        foreach (Edge edge in graph.Edges)
        {
            if (edge.Type != type)
            {
                continue;
            }

            // Dvosmerna ulica postoji u dva smera, crtamo je samo jednom.
            // Jednosmerna ima samo jednu granu, pa se crta uvek.
            bool oneWay = graph.FindEdge(edge.To, edge.From) == null;
            if (!oneWay && edge.From.Id > edge.To.Id)
            {
                continue;
            }

            AddQuad(edge.From.Position, edge.To.Position, width, color, data);
            AddCircle(edge.From.Position, width / 2f, color, data);
            AddCircle(edge.To.Position, width / 2f, color, data);
        }
    }

    // Mali trougao na sredini jednosmerne ulice, okrenut u smeru vožnje.
    private void AddOneWayArrows(CityGraph graph, List<float> data)
    {
        Vector3 color = Palette.OneWayArrow;

        foreach (Edge edge in graph.Edges)
        {
            if (graph.FindEdge(edge.To, edge.From) != null)
            {
                continue;   // dvosmerna
            }

            float size = WidthOf(edge.Type) * 0.45f;
            Vector2 direction = Vector2.Normalize(edge.To.Position - edge.From.Position);
            Vector2 normal = new Vector2(-direction.Y, direction.X);
            Vector2 middle = (edge.From.Position + edge.To.Position) / 2f;

            AddVertex(middle + direction * size, color, data);
            AddVertex(middle - direction * size * 0.6f + normal * size * 0.7f, color, data);
            AddVertex(middle - direction * size * 0.6f - normal * size * 0.7f, color, data);
        }
    }

    private void AddQuad(Vector2 from, Vector2 to, float width, Vector3 color, List<float> data)
    {
        Vector2 direction = Vector2.Normalize(to - from);
        Vector2 normal = new Vector2(-direction.Y, direction.X) * (width / 2f);

        Vector2 a = from + normal;
        Vector2 b = from - normal;
        Vector2 c = to - normal;
        Vector2 d = to + normal;

        AddVertex(a, color, data);
        AddVertex(b, color, data);
        AddVertex(c, color, data);

        AddVertex(a, color, data);
        AddVertex(c, color, data);
        AddVertex(d, color, data);
    }

    private void AddCircle(Vector2 center, float radius, Vector3 color, List<float> data)
    {
        for (int i = 0; i < CircleSegments; i++)
        {
            float angle1 = (float)(i * 2.0 * Math.PI / CircleSegments);
            float angle2 = (float)((i + 1) * 2.0 * Math.PI / CircleSegments);

            Vector2 p1 = center + new Vector2(MathF.Cos(angle1), MathF.Sin(angle1)) * radius;
            Vector2 p2 = center + new Vector2(MathF.Cos(angle2), MathF.Sin(angle2)) * radius;

            AddVertex(center, color, data);
            AddVertex(p1, color, data);
            AddVertex(p2, color, data);
        }
    }

    private void AddVertex(Vector2 position, Vector3 color, List<float> data)
    {
        data.Add(position.X);
        data.Add(position.Y);
        data.Add(color.X);
        data.Add(color.Y);
        data.Add(color.Z);
    }
}