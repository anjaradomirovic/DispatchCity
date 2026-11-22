using System.Numerics;
using DispatchCity.Config;
using DispatchCity.Core;
using DispatchCity.Pathfinding;
using DispatchCity.Simulation;
using OpenTK.Graphics.OpenGL4;

namespace DispatchCity.Rendering;

// Ulice su statična geometrija: CPU je jednom pretvori u trouglove i pošalje na GPU.
// Ono što se menja (gužva po ulici, vreme dolaska po raskrsnici) ide u dve male teksture
// koje se osvežavaju svaki frame, pa se geometrija nikad ponovo ne šalje.
// Svaki vrh nosi indeks svoje raskrsnice i svoje ulice, a GPU iz tekstura očita vrednosti.
public class RoadLayer : IRenderLayer
{
    private const int CircleSegments = 10;
    private const int FloatsPerVertex = 7;   // x, y, r, g, b, raskrsnica, ulica

    // Indeks vrha koji ne učestvuje (npr. strelice nemaju svoju ulicu u teksturi gužve).
    private const float None = -1f;

    private readonly Shader _shader;
    private readonly ViewOptions _options;
    private WorldMetrics _metrics = null!;       // postavlja se u Initialize
    private int _vao;
    private int _vbo;
    private int _vertexCount;

    private int _heatTexture;
    private int _congestionTexture;
    private float[] _heatValues = Array.Empty<float>();
    private float[] _congestionValues = Array.Empty<float>();

    public RoadLayer(Shader shader, ViewOptions options)
    {
        _shader = shader;
        _options = options;
    }

    public void Initialize(GameWorld world)
    {
        CityGraph graph = world.Graph;
        _metrics = world.Config.Metrics;
        var data = new List<float>();

        // Svaka ulica dobija indeks (redni broj u listi grana) po kome GPU traži njenu gužvu.
        var edgeIndex = new Dictionary<Edge, int>();
        for (int i = 0; i < graph.Edges.Count; i++)
        {
            edgeIndex[graph.Edges[i]] = i;
        }

        // Redosled crtanja: ono što je kasnije, nalazi se preko prethodnog.
        AddRoadsOfType(graph, RoadType.Local, edgeIndex, data);
        AddRoadsOfType(graph, RoadType.Main, edgeIndex, data);
        AddRoadsOfType(graph, RoadType.Avenue, edgeIndex, data);
        AddOneWayArrows(graph, data);

        float[] array = data.ToArray();
        _vertexCount = array.Length / FloatsPerVertex;

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, array.Length * sizeof(float), array, BufferUsageHint.StaticDraw);

        int stride = FloatsPerVertex * sizeof(float);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stride, 5 * sizeof(float));
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
        GL.EnableVertexAttribArray(3);

        _heatValues = new float[graph.Nodes.Count];
        _congestionValues = new float[graph.Edges.Count];
        _heatTexture = CreateTexture(_heatValues.Length);
        _congestionTexture = CreateTexture(_congestionValues.Length);

        Update(world);
    }

    // Svaki frame: CPU prepiše trenutne vrednosti u dva niza i pošalje ih u teksture
    // (nekoliko stotina brojeva), a GPU ih očita pri crtanju.
    public void Update(GameWorld world)
    {
        CityGraph graph = world.Graph;

        TravelTimeMap times = world.TravelTimes;
        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            _heatValues[i] = HeatOf(times, graph.Nodes[i]);
        }

        for (int i = 0; i < graph.Edges.Count; i++)
        {
            _congestionValues[i] = graph.Edges[i].Congestion;
        }

        Upload(_heatTexture, _heatValues);
        Upload(_congestionTexture, _congestionValues);
    }

    public void Render(Camera2D camera)
    {
        _shader.Use();
        _shader.SetMatrix("uViewProj", camera.ViewProjection);

        // Samo uniforme: koji su prikazi uključeni i boje njihovih rampi.
        _shader.SetFloat("uHeatMode", _options.ShowHeatMap ? 1f : 0f);
        _shader.SetFloat("uTrafficMode", _options.ShowTraffic ? 1f : 0f);
        _shader.SetVector3("uNear", Palette.HeatNear);
        _shader.SetVector3("uMid", Palette.HeatMid);
        _shader.SetVector3("uFar", Palette.HeatFar);
        _shader.SetVector3("uJamMid", Palette.JamMid);
        _shader.SetVector3("uJamHigh", Palette.JamHigh);

        // Teksture: 0 = vrelina po raskrsnici, 1 = gužva po ulici.
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture1D, _heatTexture);
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture1D, _congestionTexture);
        _shader.SetInt("uHeatTex", 0);
        _shader.SetInt("uCongestionTex", 1);

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);

        GL.ActiveTexture(TextureUnit.Texture0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        GL.DeleteTexture(_heatTexture);
        GL.DeleteTexture(_congestionTexture);
    }

    // Tekstura sa jednim float-om po elementu (R32F). Služi kao tabela koju GPU čita.
    private static int CreateTexture(int size)
    {
        int texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture1D, texture);
        GL.TexImage1D(TextureTarget.Texture1D, 0, PixelInternalFormat.R32f, size, 0,
            PixelFormat.Red, PixelType.Float, IntPtr.Zero);

        // Bez filtriranja: svaka vrednost je tačno jedan element, ne meša se sa susednim.
        GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        return texture;
    }

    private static void Upload(int texture, float[] values)
    {
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture1D, texture);
        GL.TexSubImage1D(TextureTarget.Texture1D, 0, 0, values.Length,
            PixelFormat.Red, PixelType.Float, values);
    }

    // Širina je razmer jedinice mere (WorldMetrics), pa se skalira sa gustinom grada.
    private float WidthOf(RoadType type)
    {
        return _metrics.RoadWidth(type);
    }

    // Vreme dolaska do čvora kao razmer najdužeg vremena (0 do 1).
    private static float HeatOf(TravelTimeMap times, Node node)
    {
        float max = times.MaxTime;
        float time = times.TimeTo(node);
        if (max <= 0f || float.IsPositiveInfinity(time))
        {
            return 1f;
        }
        return time / max;
    }

    private void AddRoadsOfType(CityGraph graph, RoadType type, Dictionary<Edge, int> edgeIndex, List<float> data)
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

            float index = edgeIndex[edge];

            AddQuad(edge, width, color, index, data);
            AddCircle(edge.From, width / 2f, color, index, data);
            AddCircle(edge.To, width / 2f, color, index, data);
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

            AddVertex(middle + direction * size, color, None, None, data);
            AddVertex(middle - direction * size * 0.6f + normal * size * 0.7f, color, None, None, data);
            AddVertex(middle - direction * size * 0.6f - normal * size * 0.7f, color, None, None, data);
        }
    }

    // Vrhovi na početku ulice nose raskrsnicu početka, a na kraju raskrsnicu kraja.
    // GPU između njih glatko prelazi, pa se vidi kako vreme raste duž ulice.
    private void AddQuad(Edge edge, float width, Vector3 color, float edgeIndex, List<float> data)
    {
        Vector2 from = edge.From.Position;
        Vector2 to = edge.To.Position;
        Vector2 direction = Vector2.Normalize(to - from);
        Vector2 normal = new Vector2(-direction.Y, direction.X) * (width / 2f);

        Vector2 a = from + normal;
        Vector2 b = from - normal;
        Vector2 c = to - normal;
        Vector2 d = to + normal;

        float nodeFrom = edge.From.Id;
        float nodeTo = edge.To.Id;

        AddVertex(a, color, nodeFrom, edgeIndex, data);
        AddVertex(b, color, nodeFrom, edgeIndex, data);
        AddVertex(c, color, nodeTo, edgeIndex, data);

        AddVertex(a, color, nodeFrom, edgeIndex, data);
        AddVertex(c, color, nodeTo, edgeIndex, data);
        AddVertex(d, color, nodeTo, edgeIndex, data);
    }

    private void AddCircle(Node node, float radius, Vector3 color, float edgeIndex, List<float> data)
    {
        Vector2 center = node.Position;
        float nodeIndex = node.Id;

        for (int i = 0; i < CircleSegments; i++)
        {
            float angle1 = (float)(i * 2.0 * Math.PI / CircleSegments);
            float angle2 = (float)((i + 1) * 2.0 * Math.PI / CircleSegments);

            Vector2 p1 = center + new Vector2(MathF.Cos(angle1), MathF.Sin(angle1)) * radius;
            Vector2 p2 = center + new Vector2(MathF.Cos(angle2), MathF.Sin(angle2)) * radius;

            AddVertex(center, color, nodeIndex, edgeIndex, data);
            AddVertex(p1, color, nodeIndex, edgeIndex, data);
            AddVertex(p2, color, nodeIndex, edgeIndex, data);
        }
    }

    private void AddVertex(Vector2 position, Vector3 color, float node, float edge, List<float> data)
    {
        data.Add(position.X);
        data.Add(position.Y);
        data.Add(color.X);
        data.Add(color.Y);
        data.Add(color.Z);
        data.Add(node);
        data.Add(edge);
    }
}