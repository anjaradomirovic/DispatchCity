using DispatchCity.Core;

namespace DispatchCity.Pathfinding;

public interface IPathfinder
{
    string Name { get; }

    // Vraća listu ulica od starta do cilja, ili null ako put ne postoji.
    IReadOnlyList<Edge>? FindPath(Node start, Node goal);
}