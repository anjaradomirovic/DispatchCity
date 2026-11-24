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

    private readonly List<Texture2D> _figures = new List<Texture2D>();
    private int _figureColumns;
    private int _figureRows;
    private Shader _background = null!;
    private Shader _sprite = null!;
    private QuadMesh _backgroundQuad = null!;
    private SpriteQuad _spriteQuad = null!;

    public void Initialize()
    {
        _background = new Shader(MenuShaders.Vertex, MenuShaders.Fragment);
        _sprite = new Shader(SpriteShaders.Vertex, SpriteShaders.SpriteFragment);

        // Isti crteži pacijenata kao u igri.
        PatientSet patients = SpriteAssets.LoadPatients();
        _figures.AddRange(patients.Textures);
        _figureColumns = patients.Columns;
        _figureRows = patients.Rows;

        _backgroundQuad = QuadMesh.FullScreen();
        _spriteQuad = new SpriteQuad();
    }

    public void Render(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        float time = RealTime.Seconds;
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

        _backgroundQuad.Draw();
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

            Texture2D texture = _figures[i % _figures.Count];
            texture.Bind(TextureUnit.Texture0);

            float cellAspect = SpriteQuad.AspectOf(texture, _figureColumns, _figureRows);
            _spriteQuad.Draw(_sprite, new Vector2(x, y), size, cellAspect, angle, 0, 0, _figureColumns, _figureRows);
        }

        GL.Disable(EnableCap.Blend);
    }

    public void Dispose()
    {
        foreach (Texture2D texture in _figures)
        {
            texture.Dispose();
        }
        _backgroundQuad.Dispose();
        _spriteQuad.Dispose();
    }
}