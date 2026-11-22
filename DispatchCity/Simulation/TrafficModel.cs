using System.Numerics;
using DispatchCity.Config;
using DispatchCity.Core;

namespace DispatchCity.Simulation;

// Gužve u gradu (CPU). Povremeno nastane "zagušenje": oblast oko slučajne raskrsnice u kojoj ulice
// usporavaju, najviše u centru a manje ka ivici. Posle nekog vremena nestane. Gužva se menja
// glatko, ne skokovito, pa se najbrži put ne menja naglo nego postepeno kako gužva raste i opada.
// Model samo upisuje Edge.Congestion; sve ostalo (A*, vozila, toplotna mapa, boja) čita odatle.
public class TrafficModel
{
    private class Jam
    {
        public Vector2 Center;
        public float Radius;
        public float Strength;     // 0 do 1, koliko je gužva jaka u centru
        public float Remaining;    // sekunde do nestanka
    }

    private readonly GameConfig _config;
    private readonly CityGraph _graph;
    private readonly Random _random;
    private readonly List<Jam> _jams = new List<Jam>();
    private float _timeToNextJam;

    public TrafficModel(GameConfig config, CityGraph graph)
    {
        _config = config;
        _graph = graph;
        _random = new Random(config.Seed + 6);
        _timeToNextJam = config.FirstJamDelaySeconds;
    }

    public void Update(float deltaSeconds)
    {
        if (!_config.JamsEnabled)
        {
            return;
        }

        _timeToNextJam -= deltaSeconds;
        if (_timeToNextJam <= 0f && _jams.Count < _config.MaxActiveJams)
        {
            StartJam();
            _timeToNextJam = _config.JamIntervalSeconds * (0.5f + (float)_random.NextDouble());
        }

        for (int i = _jams.Count - 1; i >= 0; i--)
        {
            _jams[i].Remaining -= deltaSeconds;
            if (_jams[i].Remaining <= 0f)
            {
                _jams.RemoveAt(i);
            }
        }

        // Svaka ulica se približava svom cilju (gužva koju trenutno "traže" aktivna zagušenja).
        float ease = Math.Min(1f, deltaSeconds / _config.CongestionEaseSeconds);
        foreach (Edge edge in _graph.Edges)
        {
            float target = TargetFor(edge);
            edge.Congestion += (target - edge.Congestion) * ease;

            if (edge.Congestion < 0.005f && target == 0f)
            {
                edge.Congestion = 0f;
            }
        }
    }

    private void StartJam()
    {
        WorldMetrics metrics = _config.Metrics;
        Node center = _graph.Nodes[_random.Next(_graph.Nodes.Count)];

        float radius = metrics.JamRadiusMin
                       + (float)_random.NextDouble() * (metrics.JamRadiusMax - metrics.JamRadiusMin);

        _jams.Add(new Jam
        {
            Center = center.Position,
            Radius = radius,
            Strength = 0.6f + 0.4f * (float)_random.NextDouble(),
            Remaining = _config.JamDurationSeconds,
        });
    }

    // Gužva koju ulica "želi": najjača od svih zagušenja, opada linearno sa rastojanjem od centra.
    // Računa se po sredini ulice, pa su oba smera iste ulice uvek jednako zagušena.
    private float TargetFor(Edge edge)
    {
        Vector2 middle = (edge.From.Position + edge.To.Position) / 2f;
        float target = 0f;

        foreach (Jam jam in _jams)
        {
            float distance = Vector2.Distance(middle, jam.Center);
            float falloff = 1f - distance / jam.Radius;
            if (falloff > 0f)
            {
                target = Math.Max(target, jam.Strength * falloff);
            }
        }
        return target;
    }
}