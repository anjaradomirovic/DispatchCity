using System.Numerics;

namespace DispatchCity.Rendering;

// Podaci jedne instance kruga. Ovo je ono što CPU šalje GPU-u (6 brojeva po krugu).
public readonly record struct CircleInstance(Vector2 Position, float Radius, Vector3 Color);