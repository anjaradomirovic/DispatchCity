using System.Diagnostics;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;

namespace DispatchCity.Rendering;

// Uvodni ekran: pola nebo, pola trava, dva oblaka, a pacijenti prolaze s desna na levo po traci
// (kao figure u satu sa kukavicom). Sve mere su razmeri visine prozora, pa se skalira sa prozorom.
public class MenuScene : IDisposable
{
    // Horizont je na sredini ekrana, a traka je malo ispod njega (razmer visine).
    private const float Horizon = 0.5f;
    private const float TrackBelowHorizon = 0.3f;
    private const float TrackHalfHeight = 0.012f;

    // Figure: visina, razmak (razmer visine ekrana) i brzina (visina ekrana u sekundi).
    private const float FigureHeight = 0.2f;
    private const float FigureSpacing = 0.42f;
    private const float FigureSpeed = 0.12f;

    // Ljuljanje figura dok idu: nagib u radijanima, poskakivanje kao razmer visine figure.
    private const float TiltAmount = 0.07f;
    private const float BobAmount = 0.05f;
    private const float BobFrequency = 5f;

    // Oblaci se krecu sporije od figura, a letvice trake ih prate.
    private const float CloudSpeed = 0.012f;
    private const float SlatsPerHeight = 14f;

    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly List<Texture2D> _figures = new List<Texture2D>();
    private readonly List<(int Columns, int Rows)> _grids = new List<(int, int)>();
    private Shader _background = null!;
    private Shader _sprite = null!;
    private int _backgroundVao;
    private int _backgroundVbo;
    private int _spriteVao;
    private int _spriteVbo;
    private int _spriteEbo;

    public void Initialize()
    {
        _background = new Shader(MenuShaders.Vertex, MenuShaders.Fragment);
        _sprite = new Shader(SpriteShaders.Vertex, SpriteShaders.SpriteFragment);

        // Isti crtezi pacijenata kao u igri (assets/patient_1.png...), ili privremeni iz koda.
        string folder = Path.Combine(AppContext.BaseDirectory, "assets");
        for (int i = 1; ; i++)
        {
            Texture2D? texture = Texture2D.FromFile(Path.Combine(folder, "patient_" + i + ".png"));
            if (texture == null)
            {
                break;
            }
            _figures.Add(texture);
            _grids.Add((1, 1));
        }

        if (_figures.Count == 0)
        {
            _figures.Add(PlaceholderSprites.CreateGrandmaAtlas());
            _grids.Add((4, 2));
        }

        CreateBackgroundQuad();
        CreateSpriteQuad();
    }

    public void Render(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        float time = (float)_clock.Elapsed.TotalSeconds;
        float aspect = width / (float)height;

        GL.Viewport(0, 0, width, height);
        GL.Disable(EnableCap.Blend);

        DrawBackground(aspect, time);
        DrawFigures(width, height, time);
    }

    private void DrawBackground(float aspect, float time)
    {
        float trackY = Horizon - TrackBelowHorizon;

        _background.Use();
        _background.SetFloat("uAspect", aspect);
        _background.SetFloat("uTime", time);
        _background.SetFloat("uHorizon", Horizon);
        _background.SetFloat("uTrackY", trackY);
        _background.SetFloat("uTrackHalfHeight", TrackHalfHeight);

        // Letvice klize ulevo istom brzinom kao figure (brzina je u visinama ekrana, letvice u sirinama).
        _background.SetFloat("uTrackScroll", time * FigureSpeed * SlatsPerHeight);
        _background.SetFloat("uCloudDrift", time * CloudSpeed);
        _background.SetVector3("uSkyTop", Palette.MenuSkyTop);
        _background.SetVector3("uSkyBottom", Palette.MenuSkyBottom);
        _background.SetVector3("uGrassNear", Palette.MenuGrassNear);
        _background.SetVector3("uGrassFar", Palette.MenuGrassFar);
        _background.SetVector3("uTrackColor", Palette.MenuTrack);
        _background.SetVector3("uTrackEdge", Palette.MenuTrackEdge);
        _background.SetVector3("uCloudColor", Palette.MenuCloud);

        GL.BindVertexArray(_backgroundVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    // Figure stoje na traci i klize s desna na levo. Kad izadju levo, vracaju se desno (petlja).
    private void DrawFigures(int width, int height, float time)
    {
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // Koordinate su pikseli, Y ide nadole: isto kao u mapi, pa se slike ne obrcu.
        Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(0f, width, height, 0f, 0f, 1f);
        _sprite.Use();
        _sprite.SetMatrix("uViewProj", projection);
        _sprite.SetInt("uTexture", 0);
        GL.BindVertexArray(_spriteVao);

        float size = FigureHeight * height;
        float spacing = FigureSpacing * height;
        float trackTop = (1f - (Horizon - TrackBelowHorizon + TrackHalfHeight)) * height;

        // Traka je dovoljno duga da stane ceo ekran plus po jedna figura sa obe strane.
        int count = (int)MathF.Ceiling((width + 2f * size) / spacing) + 1;
        float loopLength = count * spacing;
        float offset = time * FigureSpeed * height;

        for (int i = 0; i < count; i++)
        {
            float travelled = (i * spacing + offset) % loopLength;
            float x = width + size - travelled;

            // Ljuljanje i poskakivanje (sinus), svaka figura krece u drugoj fazi.
            float phase = time * BobFrequency + i * 1.7f;
            float angle = MathF.Sin(phase) * TiltAmount;
            float lift = MathF.Abs(MathF.Sin(phase)) * size * BobAmount;
            float y = trackTop - size / 2f - lift;

            int index = i % _figures.Count;
            Texture2D texture = _figures[index];
            (int columns, int rows) = _grids[index];
            texture.Bind(TextureUnit.Texture0);

            float cellAspect = (texture.Width / (float)columns) / (texture.Height / (float)rows);
            _sprite.SetVector2("uPosition", x, y);
            _sprite.SetFloat("uSize", size);
            _sprite.SetFloat("uAspect", cellAspect);
            _sprite.SetFloat("uAngle", angle);
            _sprite.SetVector2("uCell", 0, 0);
            _sprite.SetVector2("uGrid", columns, rows);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        }

        GL.Disable(EnableCap.Blend);
    }

    private void CreateBackgroundQuad()
    {
        float[] quad =
        {
            -1f, -1f,  1f, -1f,  1f, 1f,
            -1f, -1f,  1f, 1f,  -1f, 1f,
        };

        _backgroundVao = GL.GenVertexArray();
        _backgroundVbo = GL.GenBuffer();
        GL.BindVertexArray(_backgroundVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _backgroundVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
    }

    // Isti kvadrat kao u SpriteLayer: x, y, u, v.
    private void CreateSpriteQuad()
    {
        float[] vertices =
        {
            -0.5f, -0.5f, 0f, 0f,
             0.5f, -0.5f, 1f, 0f,
             0.5f,  0.5f, 1f, 1f,
            -0.5f,  0.5f, 0f, 1f,
        };
        uint[] indices = { 0, 1, 2, 0, 2, 3 };

        _spriteVao = GL.GenVertexArray();
        _spriteVbo = GL.GenBuffer();
        _spriteEbo = GL.GenBuffer();
        GL.BindVertexArray(_spriteVao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _spriteVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _spriteEbo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        int stride = 4 * sizeof(float);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);
    }

    public void Dispose()
    {
        foreach (Texture2D texture in _figures)
        {
            texture.Dispose();
        }
        GL.DeleteBuffer(_backgroundVbo);
        GL.DeleteVertexArray(_backgroundVao);
        GL.DeleteBuffer(_spriteVbo);
        GL.DeleteBuffer(_spriteEbo);
        GL.DeleteVertexArray(_spriteVao);
    }
}