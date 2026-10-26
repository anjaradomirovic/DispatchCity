using DispatchCity.Core;

namespace DispatchCity.Generation;

// Jaka povezanost: iz svakog čvora se stiže do svakog drugog.
// Dovoljno je proveriti jedan čvor: do svih se stiže idući napred i do svih idući unazad
// (po obrnutim granama). Tada ne postoji ćorsokak u koji se ulazi, a ne izlazi.
public static class ConnectivityChecker
{
    public static bool IsStronglyConnected(CityGraph graph)
    {
        int count = graph.Nodes.Count;
        if (count <= 1)
        {
            return true;
        }

        // Obilazak napred: koristi odlazne grane.
        if (CountForward(graph, 0) != count)
        {
            return false;
        }

        // Obilazak unazad: koristi obrnute grane.
        var incoming = new List<int>[count];
        for (int i = 0; i < count; i++)
        {
            incoming[i] = new List<int>();
        }
        foreach (Edge edge in graph.Edges)
        {
            incoming[edge.To.Id].Add(edge.From.Id);
        }

        return CountBackward(incoming, 0) == count;
    }

    private static int CountForward(CityGraph graph, int startId)
    {
        var visited = new bool[graph.Nodes.Count];
        var stack = new Stack<int>();
        visited[startId] = true;
        stack.Push(startId);
        int reached = 1;

        while (stack.Count > 0)
        {
            Node current = graph.Nodes[stack.Pop()];
            foreach (Edge edge in current.Outgoing)
            {
                int next = edge.To.Id;
                if (!visited[next])
                {
                    visited[next] = true;
                    reached++;
                    stack.Push(next);
                }
            }
        }
        return reached;
    }

    private static int CountBackward(List<int>[] incoming, int startId)
    {
        var visited = new bool[incoming.Length];
        var stack = new Stack<int>();
        visited[startId] = true;
        stack.Push(startId);
        int reached = 1;

        while (stack.Count > 0)
        {
            int current = stack.Pop();
            foreach (int previous in incoming[current])
            {
                if (!visited[previous])
                {
                    visited[previous] = true;
                    reached++;
                    stack.Push(previous);
                }
            }
        }
        return reached;
    }
}