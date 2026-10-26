using System.Numerics;

namespace DispatchCity.Generation;

public class PoissonDiskSampler
{
    private readonly float _width;
    private readonly float _height;
    private readonly float _minDistance;
    private readonly int _attempts;
    private readonly Random _random;

    // width, height: veličina mape. minDistance: najmanje rastojanje između tačaka.
    // attempts: koliko puta pokušavamo da nađemo novu tačku oko postojeće (30 je standard).
    public PoissonDiskSampler(float width, float height, float minDistance, int seed, int attempts = 30)
    {
        _width = width;
        _height = height;
        _minDistance = minDistance;
        _attempts = attempts;
        _random = new Random(seed);
    }

    public List<Vector2> Generate()
    {
        // Mreža ćelija ubrzava proveru "ima li neko blizu". Ćelija je takva da u nju stane najviše jedna tačka.
        float cellSize = _minDistance / MathF.Sqrt(2f);
        int cols = (int)MathF.Ceiling(_width / cellSize);
        int rows = (int)MathF.Ceiling(_height / cellSize);

        var grid = new int[cols, rows];
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                grid[x, y] = -1;   // -1 znači da je ćelija prazna
            }
        }

        var points = new List<Vector2>();
        var active = new List<int>();   // tačke oko kojih još tražimo nove

        // Prva tačka je slučajna.
        var first = new Vector2(
            (float)_random.NextDouble() * _width,
            (float)_random.NextDouble() * _height);
        AddPoint(first, points, active, grid, cellSize);

        while (active.Count > 0)
        {
            int activeIndex = _random.Next(active.Count);
            Vector2 center = points[active[activeIndex]];
            bool found = false;

            for (int i = 0; i < _attempts; i++)
            {
                // Nova tačka je na slučajnom mestu u prstenu između minDistance i 2 * minDistance.
                float angle = (float)(_random.NextDouble() * Math.PI * 2.0);
                float radius = _minDistance * (1f + (float)_random.NextDouble());
                var candidate = new Vector2(
                    center.X + MathF.Cos(angle) * radius,
                    center.Y + MathF.Sin(angle) * radius);

                if (IsValid(candidate, points, grid, cellSize, cols, rows))
                {
                    AddPoint(candidate, points, active, grid, cellSize);
                    found = true;
                    break;
                }
            }

            // Ako oko tačke nema mesta, više je ne razmatramo.
            if (!found)
            {
                active.RemoveAt(activeIndex);
            }
        }

        return points;
    }

    private void AddPoint(Vector2 point, List<Vector2> points, List<int> active, int[,] grid, float cellSize)
    {
        int index = points.Count;
        points.Add(point);
        active.Add(index);

        int cx = (int)(point.X / cellSize);
        int cy = (int)(point.Y / cellSize);
        grid[cx, cy] = index;
    }

    private bool IsValid(Vector2 candidate, List<Vector2> points, int[,] grid, float cellSize, int cols, int rows)
    {
        // Mora biti unutar mape.
        if (candidate.X < 0f || candidate.X >= _width || candidate.Y < 0f || candidate.Y >= _height)
        {
            return false;
        }

        int cx = (int)(candidate.X / cellSize);
        int cy = (int)(candidate.Y / cellSize);

        // Proveravamo susedne ćelije (2 u svakom smeru).
        int minX = Math.Max(0, cx - 2);
        int maxX = Math.Min(cols - 1, cx + 2);
        int minY = Math.Max(0, cy - 2);
        int maxY = Math.Min(rows - 1, cy + 2);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                int index = grid[x, y];
                if (index != -1)
                {
                    float distance = Vector2.Distance(candidate, points[index]);
                    if (distance < _minDistance)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }
}