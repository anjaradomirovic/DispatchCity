using System.Numerics;
using DispatchCity.Core;
using DispatchCity.Pathfinding;

var graph = new CityGraph();

Node a = graph.AddNode(new Vector2(0, 0));
Node b = graph.AddNode(new Vector2(100, 0));
Node c = graph.AddNode(new Vector2(100, 100));
Node d = graph.AddNode(new Vector2(200, 100));

// Brzine su u metrima po sekundi.
graph.AddTwoWay(a, b, RoadType.Local, 8f);      // sporedna, 100 m
graph.AddTwoWay(b, d, RoadType.Local, 8f);      // sporedna, ~141 m
graph.AddTwoWay(a, c, RoadType.Avenue, 20f);    // bulevar, ~141 m
graph.AddTwoWay(c, d, RoadType.Avenue, 20f);    // bulevar, 100 m

var astar = new AStarPathfinder(20f);
var dijkstra = new DijkstraPathfinder();

Console.WriteLine("Put od A do D:");
PrintPath(astar, a, d);
PrintPath(dijkstra, a, d);

// Zatvaramo bulevar i gledamo da li put menja tok.
a.Outgoing[1].IsClosed = true;   // druga ulica koja izlazi iz A je bulevar ka C
Console.WriteLine();
Console.WriteLine("Posle zatvaranja bulevara:");
PrintPath(astar, a, d);

static void PrintPath(IPathfinder pathfinder, Node start, Node goal)
{
    IReadOnlyList<Edge>? path = pathfinder.FindPath(start, goal);

    if (path == null)
    {
        Console.WriteLine(pathfinder.Name + ": nema puta");
        return;
    }

    float total = 0f;
    string route = start.Id.ToString();
    foreach (Edge edge in path)
    {
        total += edge.TravelTime;
        route += " -> " + edge.To.Id;
    }

    Console.WriteLine(pathfinder.Name + ": " + route + " (" + total.ToString("F1") + " s)");
}
//using OpenTK.Graphics.OpenGL4;
//using OpenTK.Windowing.Common;
//using OpenTK.Windowing.Desktop;

//var native = new NativeWindowSettings
//{
//    ClientSize = (1280, 720),
//    Title = "Dispatch City",
//    APIVersion = new Version(3, 3),
//    Profile = ContextProfile.Core
//};

//using var window = new Game(GameWindowSettings.Default, native);
//window.Run();

//class Game : GameWindow
//{
//    public Game(GameWindowSettings g, NativeWindowSettings n) : base(g, n) { }

//    protected override void OnLoad()
//    {
//        base.OnLoad();
//        GL.ClearColor(0.12f, 0.14f, 0.18f, 1f);
//    }

//    protected override void OnRenderFrame(FrameEventArgs e)
//    {
//        base.OnRenderFrame(e);
//        GL.Clear(ClearBufferMask.ColorBufferBit);
//        SwapBuffers();
//    }

//    protected override void OnResize(ResizeEventArgs e)
//    {
//        base.OnResize(e);
//        GL.Viewport(0, 0, e.Width, e.Height);
//    }
//}