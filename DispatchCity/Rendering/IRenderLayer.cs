using DispatchCity.Simulation;

namespace DispatchCity.Rendering;

// Jedan sloj slike (ulice, bolnica, vozila...). Renderer ih crta redom, jedan preko drugog.
public interface IRenderLayer : IDisposable
{
    // Jednom: pravi GPU bafere i šalje podatke koji se ne menjaju.
    void Initialize(GameWorld world);

    // Svaki frame: šalje na GPU samo ono što se promenilo (npr. položaje vozila).
    void Update(GameWorld world);

    // Svaki frame: postavlja kameru i crta.
    void Render(Camera2D camera);
}