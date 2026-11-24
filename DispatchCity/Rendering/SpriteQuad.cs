using System.Numerics;

namespace DispatchCity.Rendering;

// Crtanje jedne slike (ili sličice iz atlasa) kvadratom. Položaj, veličina, ugao i sličica
// šalju se kao uniforme, kao u mini igri iz termina 5. Koriste ga igra i uvodni meni.
public class SpriteQuad : IDisposable
{
    private readonly QuadMesh _mesh = QuadMesh.Unit();

    // column i row biraju sličicu u atlasu od columns x rows sličica (obična slika je 1 x 1).
    public void Draw(Shader shader, Vector2 position, float size, float aspect, float angle,
        int column, int row, int columns, int rows)
    {
        shader.SetVector2("uPosition", position.X, position.Y);
        shader.SetFloat("uSize", size);
        shader.SetFloat("uAspect", aspect);
        shader.SetFloat("uAngle", angle);
        shader.SetVector2("uCell", column, row);
        shader.SetVector2("uGrid", columns, rows);
        _mesh.Draw();
    }

    // Širina / visina jedne sličice u pikselima. Tako slika bilo kog oblika ne ispadne razvučena.
    public static float AspectOf(Texture2D texture, int columns, int rows)
    {
        float cellWidth = texture.Width / (float)columns;
        float cellHeight = texture.Height / (float)rows;
        return cellWidth / cellHeight;
    }

    public void Dispose()
    {
        _mesh.Dispose();
    }
}