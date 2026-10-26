using DispatchCity;
using DispatchCity.Core;
using DispatchCity.Generation;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

const float WorldWidth = 1000f;
const float WorldHeight = 800f;

CityGraph graph = new CityBuilder()
    .WithSize(WorldWidth, WorldHeight)
    .WithSeed(12345)
    .WithSpacing(75f)
    .WithExtraEdges(0.35f)
    .WithPoints()
    .WithRoads()
    .WithRoadTypes()
    .Build();

var nativeSettings = new NativeWindowSettings();
nativeSettings.ClientSize = new Vector2i(1000, 800);
nativeSettings.Title = "Dispatch City";
nativeSettings.APIVersion = new Version(3, 3);
nativeSettings.Profile = ContextProfile.Core;

using (var game = new Game(GameWindowSettings.Default, nativeSettings, graph, WorldWidth, WorldHeight))
{
    game.Run();
}