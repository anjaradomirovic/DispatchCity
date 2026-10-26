using DispatchCity;
using DispatchCity.Config;
using DispatchCity.Core;
using DispatchCity.Generation;
using DispatchCity.Pathfinding;
using DispatchCity.Simulation;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

// Test generatora: dotnet run -- --verify 1000
// Pravi grad za svaki seed i proverava da prolazi sve provere (jaka povezanost, bez preseka).
if (args.Length > 0 && args[0] == "--verify")
{
    int total = args.Length > 1 ? int.Parse(args[1]) : 1000;
    int failed = 0;

    for (int seed = 1; seed <= total; seed++)
    {
        GameConfig testConfig = GameConfig.Level1();
        testConfig.Seed = seed;

        CityGraph testGraph = CityFactory.Create(testConfig);
        if (!CityFactory.Validate(testGraph, out string error))
        {
            failed++;
            Console.WriteLine("Seed " + seed + ": " + error);
        }
    }

    Console.WriteLine((total - failed) + " / " + total + " gradova je ispravno.");
    return failed == 0 ? 0 : 1;
}

GameConfig config = GameConfig.Level1();
CityGraph graph = CityFactory.Create(config);

if (!CityFactory.Validate(graph, out string problem))
{
    Console.WriteLine("Mapa nije ispravna: " + problem);
    return 1;
}

var hospital = new Hospital(
    "Gradska bolnica",
    HospitalPlacer.FindCentralNode(graph, config.WorldWidth, config.WorldHeight));

var nativeSettings = new NativeWindowSettings();
nativeSettings.ClientSize = new Vector2i(1000, 800);
nativeSettings.Title = "Dispatch City";
nativeSettings.APIVersion = new Version(3, 3);
nativeSettings.Profile = ContextProfile.Core;

var pathfinder = new AStarPathfinder(graph.MaxSpeed());
var world = new GameWorld(config, graph, hospital, pathfinder);
world.AddAmbulance();

using (var game = new Game(GameWindowSettings.Default, nativeSettings, world))
{
    game.Run();
}

return 0;