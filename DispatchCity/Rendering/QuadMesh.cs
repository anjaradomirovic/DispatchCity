using OpenTK.Graphics.OpenGL4;

namespace DispatchCity.Rendering;

// Pravougaonik od dva trougla na grafičkoj kartici (VAO + VBO + EBO, kao u terminu 1).
// Pravi se jednom, a Draw ga crta. Zamenjuje isti kod koji se ranije ponavljao u više slojeva.
public class QuadMesh : IDisposable
{
	private readonly int _vao;
	private readonly int _vbo;
	private readonly int _ebo;

	private QuadMesh(float[] vertices, bool hasUV)
	{
		uint[] indices = { 0, 1, 2, 0, 2, 3 };

		_vao = GL.GenVertexArray();
		_vbo = GL.GenBuffer();
		_ebo = GL.GenBuffer();

		GL.BindVertexArray(_vao);

		GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
		GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

		GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
		GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

		// Svako teme je (x, y) ili (x, y, u, v).
		int floatsPerVertex = hasUV ? 4 : 2;
		int stride = floatsPerVertex * sizeof(float);
		GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
		GL.EnableVertexAttribArray(0);

		if (hasUV)
		{
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));
			GL.EnableVertexAttribArray(1);
		}
	}

	// Kvadrat od -0.5 do 0.5 sa UV koordinatama; (0, 0) u slici je gornji levi ugao. Za sprite-ove.
	public static QuadMesh Unit()
	{
		float[] vertices =
		{
			-0.5f, -0.5f, 0f, 0f,
			 0.5f, -0.5f, 1f, 0f,
			 0.5f,  0.5f, 1f, 1f,
			-0.5f,  0.5f, 0f, 1f,
		};
		return new QuadMesh(vertices, true);
	}

	// Pravougaonik samo sa pozicijama (koordinate sveta ili ekrana). Boju računa shader.
	public static QuadMesh Rectangle(float minX, float minY, float maxX, float maxY)
	{
		float[] vertices =
		{
			minX, minY,
			maxX, minY,
			maxX, maxY,
			minX, maxY,
		};
		return new QuadMesh(vertices, false);
	}

	// Ceo ekran u koordinatama isecanja (od -1 do 1), za prolaze preko celog prozora.
	public static QuadMesh FullScreen()
	{
		return Rectangle(-1f, -1f, 1f, 1f);
	}

	public void Draw()
	{
		GL.BindVertexArray(_vao);
		GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
	}

	public void Dispose()
	{
		GL.DeleteBuffer(_vbo);
		GL.DeleteBuffer(_ebo);
		GL.DeleteVertexArray(_vao);
	}
}