using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace DispatchCity.Rendering;

// 2D tekstura: slika (RGBA, 4 bajta po pikselu) koja živi na grafičkoj kartici.
// Isto kao tekstura iz vežbi: niz bajtova -> glTexImage2D -> GPU je čita u shaderu.
public class Texture2D : IDisposable
{
    public int Handle { get; }
    public int Width { get; }
    public int Height { get; }

    private Texture2D(int width, int height, byte[] rgba)
    {
        Width = width;
        Height = height;
        Handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, Handle);
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, rgba);

        // Mipmape: umanjene kopije slike. Velika slika koja se crta mala tako ostaje glatka.
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        // Linear: glatko skaliranje. ClampToEdge: ivica se ne ponavlja.
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
    }

    // Tekstura iz niza piksela napravljenog u kodu (CPU crtanje, kao u terminu 2).
    public static Texture2D FromPixels(int width, int height, byte[] rgba)
    {
        return new Texture2D(width, height, rgba);
    }

    // Tekstura iz PNG fajla. Vraća null ako fajl ne postoji, pa pozivalac može da uzme zamenu.
    public static Texture2D? FromFile(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using (Stream stream = File.OpenRead(path))
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            return new Texture2D(image.Width, image.Height, image.Data);
        }
    }

    public void Bind(TextureUnit unit)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }

    public void Dispose()
    {
        GL.DeleteTexture(Handle);
    }
}