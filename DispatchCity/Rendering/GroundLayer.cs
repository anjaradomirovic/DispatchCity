using System.Numerics;
using DispatchCity.Config;
using DispatchCity.Simulation;
using OpenTK.Graphics.OpenGL4;

namespace DispatchCity.Rendering;

// Pozadina ispod ulica: trava, šuma i (u pojasu oko grada) voda.
// CPU samo pošalje jedan pravougaonik i nekoliko brojeva; boju svakog piksela računa GPU iz šuma.
public class GroundLayer : IRenderLayer
{
    // Veličina "mrlje" šuma u jedinicama mere (veće = krupnije šume i jezera).
    private const float FeatureSizeInUnits = 5f;

    // Pragovi šuma (0 do 1). Ispod WaterLevel je voda, iznad ForestLevel je šuma.
    private const float WaterLevel = 0.36f;
    private const float ForestLevel = 0.56f;
    private const float Softness = 0.05f;
    private const float ShoreWidth = 0.03f;

    // Koliko se voda povećava ka spoljnoj ivici pojasa.
    private const float EdgeWaterBias = 0.18f;

    // Seed grada pomera šum, pa svaki grad dobija drugačiji teren.
    private const float SeedStep = 37.1f;

    private Shader _shader = null!;
    private GameWorld _world = null!;
    private int _vao;
    private int _vbo;

    public void Initialize(GameWorld world)
    {
        _world = world;
        _shader = new Shader(EffectShaders.GroundVertex, EffectShaders.GroundFragment);

        float margin = world.Config.WorldMargin;
        float minX = -margin;
        float minY = -margin;
        float maxX = world.Config.WorldWidth + margin;
        float maxY = world.Config.WorldHeight + margin;

        float[] quad =
        {
            minX, minY,  maxX, minY,  maxX, maxY,
            minX, minY,  maxX, maxY,  minX, maxY,
        };

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
    }

    // Teren se ne menja, pa nema šta da se šalje svaki frame.
    public void Update(GameWorld world)
    {
    }

    public void Render(Camera2D camera)
    {
        GameConfig config = _world.Config;
        float unit = config.Metrics.Unit;

        _shader.Use();
        _shader.SetMatrix("uViewProj", camera.ViewProjection);
        _shader.SetVector2("uWorldMin", 0f, 0f);
        _shader.SetVector2("uWorldMax", config.WorldWidth, config.WorldHeight);
        _shader.SetFloat("uMargin", config.WorldMargin);
        _shader.SetFloat("uNoiseScale", 1f / (FeatureSizeInUnits * unit));
        _shader.SetVector2("uSeedOffset", config.Seed * SeedStep % 97f, config.Seed * SeedStep % 53f);
        _shader.SetFloat("uWaterLevel", WaterLevel);
        _shader.SetFloat("uForestLevel", ForestLevel);
        _shader.SetFloat("uSoftness", Softness);
        _shader.SetFloat("uShoreWidth", ShoreWidth);
        _shader.SetFloat("uEdgeWaterBias", EdgeWaterBias);
        _shader.SetVector3("uWater", Palette.Water);
        _shader.SetVector3("uShore", Palette.Shore);
        _shader.SetVector3("uGrass", Palette.Grass);
        _shader.SetVector3("uForest", Palette.Forest);

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
    }
}