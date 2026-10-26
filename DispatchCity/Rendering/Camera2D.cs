using System.Numerics;

namespace DispatchCity.Rendering;

// Kamera je samo matrica. CPU je računa jednom po frame-u, a GPU je u vertex shaderu
// množi sa svakim vrhom (svetske koordinate -> clip prostor). Zato pomeranje i zumiranje
// ne dira bafere sa geometrijom.
//
// Kamera ne može da izađe iz "granica" (grad + pojas oko njega). Pri zumu 1 se vide cele granice,
// a kad se zumira, pogled se zaustavlja na ivici granica.
public class Camera2D
{
    private const float MaxZoom = 8f;

    private readonly Vector2 _boundsMin;
    private readonly Vector2 _boundsMax;
    private float _viewportWidth;
    private float _viewportHeight;

    public Vector2 Center { get; private set; }
    public float Zoom { get; private set; } = 1f;

    public Camera2D(Vector2 boundsMin, Vector2 boundsMax, float viewportWidth, float viewportHeight)
    {
        _boundsMin = boundsMin;
        _boundsMax = boundsMax;
        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;
        Center = (boundsMin + boundsMax) / 2f;
    }

    private float Aspect
    {
        get { return _viewportWidth / _viewportHeight; }
    }

    // Visina pogleda pri zumu 1: najmanja pri kojoj se granice vide cele, bez obzira na oblik prozora.
    private float FitHeight
    {
        get
        {
            float boundsWidth = _boundsMax.X - _boundsMin.X;
            float boundsHeight = _boundsMax.Y - _boundsMin.Y;
            return Math.Max(boundsHeight, boundsWidth / Aspect);
        }
    }

    private float ViewHeight
    {
        get { return FitHeight / Zoom; }
    }

    private float ViewWidth
    {
        get { return ViewHeight * Aspect; }
    }

    // Ortografska projekcija. Y osa ide nadole (kao u mapi), zato su bottom i top zamenjeni.
    public Matrix4x4 ViewProjection
    {
        get
        {
            float halfWidth = ViewWidth / 2f;
            float halfHeight = ViewHeight / 2f;
            return Matrix4x4.CreateOrthographicOffCenter(
                Center.X - halfWidth, Center.X + halfWidth,
                Center.Y + halfHeight, Center.Y - halfHeight,
                0f, 1f);
        }
    }

    public void SetViewport(float width, float height)
    {
        if (width > 0f && height > 0f)
        {
            _viewportWidth = width;
            _viewportHeight = height;
            ClampCenter();
        }
    }

    // Piksel na ekranu -> tačka u svetu.
    public Vector2 ScreenToWorld(Vector2 screen)
    {
        float nx = screen.X / _viewportWidth - 0.5f;
        float ny = screen.Y / _viewportHeight - 0.5f;
        return Center + new Vector2(nx * ViewWidth, ny * ViewHeight);
    }

    // Prevlačenje mišem: svet se pomera za onoliko koliko je miš prešao.
    public void Pan(Vector2 screenDelta)
    {
        float worldPerPixel = ViewHeight / _viewportHeight;
        Center -= screenDelta * worldPerPixel;
        ClampCenter();
    }

    // Zum ka tački ispod miša: ta tačka sveta ostaje tačno ispod kursora (ako granice dozvoljavaju).
    public void ZoomAt(Vector2 screenPoint, float factor)
    {
        Vector2 before = ScreenToWorld(screenPoint);
        Zoom = Math.Clamp(Zoom * factor, 1f, MaxZoom);
        Vector2 after = ScreenToWorld(screenPoint);
        Center += before - after;
        ClampCenter();
    }

    // Pogled ne sme da izađe iz granica. Ako je pogled širi od granica na nekoj osi, centrira se.
    private void ClampCenter()
    {
        Center = new Vector2(
            ClampAxis(Center.X, ViewWidth, _boundsMin.X, _boundsMax.X),
            ClampAxis(Center.Y, ViewHeight, _boundsMin.Y, _boundsMax.Y));
    }

    private static float ClampAxis(float center, float viewSize, float min, float max)
    {
        if (viewSize >= max - min)
        {
            return (min + max) / 2f;
        }

        float half = viewSize / 2f;
        return Math.Clamp(center, min + half, max - half);
    }
}