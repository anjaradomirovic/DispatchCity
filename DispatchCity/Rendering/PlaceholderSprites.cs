namespace DispatchCity.Rendering;

// Privremeni crteži, napravljeni u kodu (CPU crtanje u niz piksela, kao u terminu 2).
// Koriste se dok ne postoje tvoji PNG-ovi u assets/. Isti format kao tvoji crteži:
//   bakica: 512 x 256, 4 kolone x 2 reda sličica od 128 x 128 (red 0 = stabilna, red 1 = kritična)
//   ambulanta: 128 x 128, okrenuta udesno, pogled odozgo
public static class PlaceholderSprites
{
    private const int Cell = 128;

    private static readonly (byte R, byte G, byte B) Skin = (244, 204, 170);
    private static readonly (byte R, byte G, byte B) Hair = (200, 200, 205);
    private static readonly (byte R, byte G, byte B) Dress = (150, 120, 190);
    private static readonly (byte R, byte G, byte B) Dark = (60, 50, 60);
    private static readonly (byte R, byte G, byte B) Lips = (225, 110, 120);

    public static Texture2D CreateGrandmaAtlas()
    {
        int width = Cell * 4;
        int height = Cell * 2;
        byte[] pixels = new byte[width * height * 4];   // sve nule = potpuno providno

        for (int row = 0; row < 2; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                DrawGrandma(pixels, width, column * Cell, row * Cell, column, row);
            }
        }

        return Texture2D.FromPixels(width, height, pixels);
    }

    public static Texture2D CreateAmbulance()
    {
        byte[] pixels = new byte[Cell * Cell * 4];

        FillRect(pixels, Cell, 16, 34, 112, 94, (150, 150, 160));    // senka/ivica
        FillRect(pixels, Cell, 20, 38, 108, 90, (250, 250, 250));    // karoserija
        FillRect(pixels, Cell, 86, 42, 102, 86, (70, 100, 150));     // vetrobran (napred = desno)
        FillRect(pixels, Cell, 38, 59, 66, 69, (225, 60, 60));       // crveni krst, vodoravno
        FillRect(pixels, Cell, 47, 50, 57, 78, (225, 60, 60));       // crveni krst, uspravno

        return Texture2D.FromPixels(Cell, Cell, pixels);
    }

    // frame = kolona (0 do 3), row = 0 stabilna / 1 kritična.
    private static void DrawGrandma(byte[] pixels, int width, int originX, int originY, int frame, int row)
    {
        // Kolona menja položaj ruke (mahanje) ili drhtanje, pa niz sličica izgleda kao animacija.
        float wave = MathF.Sin(frame * MathF.PI / 2f);
        int shake = row == 1 ? (frame % 2 == 0 ? -4 : 4) : 0;
        int x = originX + 64 + shake;

        FillCircle(pixels, width, x, originY + 96, 28, Dress);              // telo
        FillCircle(pixels, width, x, originY + 16, 9, Hair);                // ćuska
        FillCircle(pixels, width, x, originY + 34, 22, Hair);               // kosa
        FillCircle(pixels, width, x, originY + 50, 20, Skin);               // lice
        FillCircle(pixels, width, x - 7, originY + 47, 3, Dark);            // oči
        FillCircle(pixels, width, x + 7, originY + 47, 3, Dark);

        if (row == 0)
        {
            FillCircle(pixels, width, x, originY + 58, 3, Lips);            // osmeh
            FillCircle(pixels, width, x + 34, originY + 78 + (int)(wave * 14f), 7, Skin);   // maše
        }
        else
        {
            FillCircle(pixels, width, x, originY + 59, 5, Dark);            // otvorena usta
            FillCircle(pixels, width, x + 8, originY + 88, 7, Skin);        // ruka na grudima
        }
    }

    // Mešanje nove boje preko postojeće ("over"): ivice kruga su glatke.
    private static void BlendPixel(byte[] pixels, int width, int x, int y, (byte R, byte G, byte B) color, float alpha)
    {
        if (x < 0 || y < 0 || x >= width || y * width * 4 >= pixels.Length || alpha <= 0f)
        {
            return;
        }

        int i = (y * width + x) * 4;
        float destAlpha = pixels[i + 3] / 255f;
        float outAlpha = alpha + destAlpha * (1f - alpha);

        pixels[i] = (byte)((color.R * alpha + pixels[i] * destAlpha * (1f - alpha)) / outAlpha);
        pixels[i + 1] = (byte)((color.G * alpha + pixels[i + 1] * destAlpha * (1f - alpha)) / outAlpha);
        pixels[i + 2] = (byte)((color.B * alpha + pixels[i + 2] * destAlpha * (1f - alpha)) / outAlpha);
        pixels[i + 3] = (byte)(outAlpha * 255f);
    }

    private static void FillCircle(byte[] pixels, int width, int centerX, int centerY, int radius, (byte R, byte G, byte B) color)
    {
        for (int y = centerY - radius - 1; y <= centerY + radius + 1; y++)
        {
            for (int x = centerX - radius - 1; x <= centerX + radius + 1; x++)
            {
                float distance = MathF.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                float alpha = Math.Clamp(radius - distance + 0.5f, 0f, 1f);
                BlendPixel(pixels, width, x, y, color, alpha);
            }
        }
    }

    private static void FillRect(byte[] pixels, int width, int left, int top, int right, int bottom, (byte R, byte G, byte B) color)
    {
        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                BlendPixel(pixels, width, x, y, color, 1f);
            }
        }
    }
}