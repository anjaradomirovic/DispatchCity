using System.Numerics;
using OpenTK.Graphics.OpenGL4;

namespace DispatchCity.Rendering;

// Završni prolaz: kadar iz FBO-a se crta preko celog pravougaonika mape, a fragment shader
// primenjuje efekte (sivo pri pauzi, crvena vinjeta, sjaj). CPU šalje samo nekoliko brojeva.
public class PostProcessPass : IDisposable
{
    private readonly SceneBuffer _scene;
    private readonly Shader _shader;
    private readonly int _vao;
    private readonly int _vbo;

    public PostProcessPass(int width, int height)
    {
        _scene = new SceneBuffer(width, height);
        _shader = new Shader(EffectShaders.PostVertex, EffectShaders.PostFragment);

        float[] quad =
        {
            -1f, -1f,  1f, -1f,  1f, 1f,
            -1f, -1f,  1f, 1f,  -1f, 1f,
        };

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
    }

    public void Resize(int width, int height)
    {
        _scene.Resize(width, height);
    }

    // Sve što se nacrta posle ovoga ide u teksturu.
    public void Begin()
    {
        _scene.Bind();
        GL.Clear(ClearBufferMask.ColorBufferBit);
    }

    // Vraća se na pravi ekran i crta obrađenu sliku.
    public void End(Camera2D camera, PostEffects effects)
    {
        _scene.Unbind();
        GL.Viewport(0, 0, _scene.Width, _scene.Height);
        GL.Disable(EnableCap.Blend);

        _shader.Use();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _scene.Texture);
        _shader.SetInt("uScene", 0);
        _shader.SetFloat("uAspect", _scene.Width / (float)_scene.Height);
        _shader.SetFloat("uGray", effects.Gray);
        _shader.SetFloat("uVignette", effects.Vignette);
        _shader.SetVector3("uVignetteColor", Palette.VignetteRed);
        _shader.SetFloat("uGlow", effects.Glow);
        _shader.SetVector3("uGlowColor", Palette.GlowWarm);

        // Mesto sjaja iz sveta u koordinate teksture (0 do 1): ista matrica kao za ulice.
        Matrix4x4 viewProjection = camera.ViewProjection;
        Vector4 center = Vector4.Transform(new Vector4(effects.GlowPosition, 0f, 1f), viewProjection);
        Vector4 edge = Vector4.Transform(
            new Vector4(effects.GlowPosition + new Vector2(effects.GlowRadius, 0f), 0f, 1f), viewProjection);
        _shader.SetVector2("uGlowCenter", center.X * 0.5f + 0.5f, center.Y * 0.5f + 0.5f);
        _shader.SetFloat("uGlowRadius", MathF.Abs(edge.X - center.X) * 0.5f);

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    public void Dispose()
    {
        _scene.Dispose();
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        GL.DeleteProgram(_shader.Handle);
    }
}