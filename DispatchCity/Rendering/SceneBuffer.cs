using OpenTK.Graphics.OpenGL4;

namespace DispatchCity.Rendering;

// Framebuffer objekat (FBO): "nevidljivi ekran" u koji se crta mapa, pa se ta slika
// kao tekstura obrađuje i tek onda prikazuje na pravom ekranu.
public class SceneBuffer : IDisposable
{
    private int _framebuffer;
    private int _texture;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Texture
    {
        get { return _texture; }
    }

    public SceneBuffer(int width, int height)
    {
        Width = Math.Max(width, 1);
        Height = Math.Max(height, 1);

        _framebuffer = GL.GenFramebuffer();
        _texture = GL.GenTexture();

        GL.BindTexture(TextureTarget.Texture2D, _texture);
        AllocateTexture();
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, _texture, 0);

        FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            throw new Exception("Framebuffer nije potpun: " + status);
        }
    }

    // Tekstura mora da prati veličinu prozora, inače bi slika bila razvučena.
    public void Resize(int width, int height)
    {
        Width = Math.Max(width, 1);
        Height = Math.Max(height, 1);
        GL.BindTexture(TextureTarget.Texture2D, _texture);
        AllocateTexture();
    }

    private void AllocateTexture()
    {
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, Width, Height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
    }

    // Od ovog trenutka se crta u teksturu, a ne na ekran.
    public void Bind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        GL.Viewport(0, 0, Width, Height);
    }

    public void Unbind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose()
    {
        GL.DeleteFramebuffer(_framebuffer);
        GL.DeleteTexture(_texture);
    }
}