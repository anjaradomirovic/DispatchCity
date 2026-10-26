using System.Numerics;
using DispatchCity.Config;
using DispatchCity.Core;

namespace DispatchCity.Generation;

public class CityBuilder
{
    private float _width = 1000f;
    private float _height = 800f;
    private float _minDistance = 75f;
    private int _seed = 1;
    private int _neighborCount = 6;
    private float _extraEdgeChance = 0.25f;

    private List<Vector2> _points = new List<Vector2>();
    private readonly List<Candidate> _roads = new List<Candidate>();
    private readonly CityGraph _graph = new CityGraph();

    // Korak 3: tipovi ulica (bulevari, glavne, sporedne).
    public CityBuilder WithRoadTypes()
    {
        var classifier = new RoadClassifier(_graph, _seed, new WorldMetrics(_minDistance));
        classifier.Classify();
        return this;
    }

    public CityBuilder WithSize(float width, float height)
    {
        _width = width;
        _height = height;
        return this;
    }

    public CityBuilder WithSeed(int seed)
    {
        _seed = seed;
        return this;
    }

    public CityBuilder WithSpacing(float minDistance)
    {
        _minDistance = minDistance;
        return this;
    }

    public CityBuilder WithExtraEdges(float chance)
    {
        _extraEdgeChance = chance;
        return this;
    }

    // Korak 1: raskrsnice.
    public CityBuilder WithPoints()
    {
        var sampler = new PoissonDiskSampler(_width, _height, _minDistance, _seed);
        _points = sampler.Generate();

        foreach (Vector2 point in _points)
        {
            _graph.AddNode(point);
        }
        return this;
    }

    // Korak 2: ulice. Nikad se ne seku, nema trouglova ni oštrih uglova.
    public CityBuilder WithRoads()
    {
        var candidates = FindCandidates();
        candidates.Sort((a, b) => a.Length.CompareTo(b.Length));

        var unionFind = new UnionFind(_points.Count);
        var rejected = new List<Candidate>();

        // Prolaz 1: razapinjuće stablo (samo ulice koje ne seku druge).
        foreach (Candidate c in candidates)
        {
            if (unionFind.Find(c.A) != unionFind.Find(c.B) && !CrossesExisting(c))
            {
                unionFind.Union(c.A, c.B);
                AddRoad(c);
            }
            else
            {
                rejected.Add(c);
            }
        }

        // Prolaz 2: ako su ostala odvojena ostrva, spajamo ih.
        ConnectIslands(unionFind);

        // Prolaz 3: dodatne ulice (petlje), uz stroga pravila.
        var random = new Random(_seed + 1);
        foreach (Candidate c in rejected)
        {
            if (random.NextDouble() >= _extraEdgeChance)
            {
                continue;
            }
            if (AlreadyConnected(c.A, c.B))
            {
                continue;
            }
            if (_graph.Nodes[c.A].Outgoing.Count >= 4 || _graph.Nodes[c.B].Outgoing.Count >= 4)
            {
                continue;
            }
            if (HasCommonNeighbor(c.A, c.B))
            {
                continue;   // bi napravilo trougao
            }
            if (TooSharpAngle(c.A, c.B) || TooSharpAngle(c.B, c.A))
            {
                continue;
            }
            if (CrossesExisting(c))
            {
                continue;
            }
            AddRoad(c);
        }
        return this;
    }

    // Korak 4: jednosmerne ulice. Samo sporedne (Local) ulice, a svaka promena se
    // zadržava samo ako graf ostaje jako povezan, inače se ulica vraća u dvosmernu.
    public CityBuilder WithOneWayStreets(float fraction)
    {
        var random = new Random(_seed + 3);

        var pairs = new List<Edge>();
        foreach (Edge edge in _graph.Edges)
        {
            if (edge.Type == RoadType.Local && edge.From.Id < edge.To.Id)
            {
                pairs.Add(edge);
            }
        }

        // Mešanje (Fisher-Yates) da jednosmerne ulice nisu grupisane na jednom mestu.
        for (int i = pairs.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (pairs[i], pairs[j]) = (pairs[j], pairs[i]);
        }

        int target = (int)(pairs.Count * fraction);
        int made = 0;

        foreach (Edge forward in pairs)
        {
            if (made >= target)
            {
                break;
            }

            Edge? backward = _graph.FindEdge(forward.To, forward.From);
            if (backward == null)
            {
                continue;
            }

            // Pokušavamo oba smera, nasumičnim redom.
            bool forwardFirst = random.Next(2) == 0;
            Edge first = forwardFirst ? forward : backward;
            Edge second = forwardFirst ? backward : forward;

            if (TryRemove(first) || TryRemove(second))
            {
                made++;
            }
        }
        return this;
    }

    private bool TryRemove(Edge edge)
    {
        _graph.RemoveEdge(edge);
        if (ConnectivityChecker.IsStronglyConnected(_graph))
        {
            return true;
        }
        _graph.RestoreEdge(edge);
        return false;
    }

    // Da li se bilo koje dve ulice (koje ne dele raskrsnicu) seku. Koristi se u validaciji mape.
    public static bool HasCrossingRoads(CityGraph graph)
    {
        IReadOnlyList<Edge> edges = graph.Edges;
        for (int i = 0; i < edges.Count; i++)
        {
            for (int j = i + 1; j < edges.Count; j++)
            {
                Edge a = edges[i];
                Edge b = edges[j];
                if (a.From == b.From || a.From == b.To || a.To == b.From || a.To == b.To)
                {
                    continue;
                }
                if (SegmentsCross(a.From.Position, a.To.Position, b.From.Position, b.To.Position))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public CityGraph Build()
    {
        return _graph;
    }

    private void AddRoad(Candidate c)
    {
        Node a = _graph.Nodes[c.A];
        Node b = _graph.Nodes[c.B];
        float speed = new WorldMetrics(_minDistance).SpeedOf(RoadType.Local);
        _graph.AddTwoWay(a, b, RoadType.Local, speed);
        _roads.Add(c);
    }

    // Spaja odvojene delove grada najkraćom ulicom koja ništa ne preseca.
    private void ConnectIslands(UnionFind unionFind)
    {
        var all = new List<Candidate>();
        for (int i = 0; i < _points.Count; i++)
        {
            for (int j = i + 1; j < _points.Count; j++)
            {
                all.Add(new Candidate(i, j, Vector2.Distance(_points[i], _points[j])));
            }
        }
        all.Sort((a, b) => a.Length.CompareTo(b.Length));

        foreach (Candidate c in all)
        {
            if (unionFind.Find(c.A) != unionFind.Find(c.B) && !CrossesExisting(c))
            {
                unionFind.Union(c.A, c.B);
                AddRoad(c);
            }
        }
    }

    private bool AlreadyConnected(int a, int b)
    {
        foreach (Edge edge in _graph.Nodes[a].Outgoing)
        {
            if (edge.To.Id == b)
            {
                return true;
            }
        }
        return false;
    }

    private bool HasCommonNeighbor(int a, int b)
    {
        foreach (Edge ea in _graph.Nodes[a].Outgoing)
        {
            foreach (Edge eb in _graph.Nodes[b].Outgoing)
            {
                if (ea.To.Id == eb.To.Id)
                {
                    return true;
                }
            }
        }
        return false;
    }

    // Da li nova ulica iz "from" ka "to" zaklapa oštar ugao sa nekom postojećom iz "from".
    private bool TooSharpAngle(int from, int to)
    {
        Vector2 newDir = Vector2.Normalize(_points[to] - _points[from]);

        foreach (Edge edge in _graph.Nodes[from].Outgoing)
        {
            Vector2 oldDir = Vector2.Normalize(_points[edge.To.Id] - _points[from]);
            if (Vector2.Dot(newDir, oldDir) > 0.5f)   // 0.5 = ugao manji od 60 stepeni
            {
                return true;
            }
        }
        return false;
    }

    private bool CrossesExisting(Candidate c)
    {
        Vector2 p1 = _points[c.A];
        Vector2 p2 = _points[c.B];

        foreach (Candidate other in _roads)
        {
            // Ulice koje dele raskrsnicu se ne računaju kao presek.
            if (other.A == c.A || other.A == c.B || other.B == c.A || other.B == c.B)
            {
                continue;
            }

            if (SegmentsCross(p1, p2, _points[other.A], _points[other.B]))
            {
                return true;
            }
        }
        return false;
    }

    internal static bool SegmentsCross(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        float d1 = Cross(p3, p4, p1);
        float d2 = Cross(p3, p4, p2);
        float d3 = Cross(p1, p2, p3);
        float d4 = Cross(p1, p2, p4);

        bool firstSplits = (d1 > 0f && d2 < 0f) || (d1 < 0f && d2 > 0f);
        bool secondSplits = (d3 > 0f && d4 < 0f) || (d3 < 0f && d4 > 0f);
        return firstSplits && secondSplits;
    }

    // Na kojoj strani prave o-a se nalazi tačka b.
    private static float Cross(Vector2 o, Vector2 a, Vector2 b)
    {
        return (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
    }

    private List<Candidate> FindCandidates()
    {
        var result = new List<Candidate>();
        var seen = new HashSet<long>();

        for (int i = 0; i < _points.Count; i++)
        {
            var distances = new List<(int Index, float Distance)>();
            for (int j = 0; j < _points.Count; j++)
            {
                if (i != j)
                {
                    distances.Add((j, Vector2.Distance(_points[i], _points[j])));
                }
            }

            distances.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            int count = Math.Min(_neighborCount, distances.Count);
            for (int k = 0; k < count; k++)
            {
                int j = distances[k].Index;
                int low = Math.Min(i, j);
                int high = Math.Max(i, j);
                long key = (long)low * _points.Count + high;

                if (seen.Add(key))
                {
                    result.Add(new Candidate(low, high, distances[k].Distance));
                }
            }
        }
        return result;
    }

    private struct Candidate
    {
        public int A;
        public int B;
        public float Length;

        public Candidate(int a, int b, float length)
        {
            A = a;
            B = b;
            Length = length;
        }
    }
}