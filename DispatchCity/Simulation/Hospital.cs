using DispatchCity.Core;

// Namespace je Simulation, a ne Game, jer bi se sudario sa klasom DispatchCity.Game.
namespace DispatchCity.Simulation;

public class Hospital
{
    public string Name { get; }
    public Node Node { get; }

    public Hospital(string name, Node node)
    {
        Name = name;
        Node = node;
    }
}