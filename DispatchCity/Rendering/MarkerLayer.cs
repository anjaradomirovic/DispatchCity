using System.Numerics;
using OpenTK.Graphics.OpenGL4;

namespace DispatchCity.Rendering;

// Obojeni krugovi na mapi (za sada bolnica). Isti format vrhova kao RoadLayer: x, y, r, g, b.
public class MarkerLayer
{
    private const int CircleSegments = 24;

    private int _vao;
    private int _vbo;
    private int _vertexCount;

    public void Load(IEnumerable<(Vector2 Position, float Radius, Vector3 Color)> markers)
    {
        var data = new List<float>();

        // Redosled je bitan: kasniji krug se crta preko ranijeg.
        foreach (var marker in markers)
        {
            AddCircle(marker.Position, marker.Radius, marker.Color, data);
        }

        float[] array = data.ToArray();
        _vertexCount = array.Length / 5;

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

    public void Render()
    {
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);
    }

    private static void AddCircle(Vector2 center, float radius, Vector3 color, List<float> data)
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

    private static void AddVertex(Vector2 position, Vector3 color, List<float> data)
    {
        data.Add(position.X);
        data.Add(position.Y);
        data.Add(color.X);
        data.Add(color.Y);
        data.Add(color.Z);
    }
}