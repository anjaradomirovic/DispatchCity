namespace DispatchCity.Generation;

public class UnionFind
{
    private readonly int[] _parent;

    public UnionFind(int size)
    {
        _parent = new int[size];
        for (int i = 0; i < size; i++)
        {
            _parent[i] = i;
        }
    }

    // Vraća "predstavnika" grupe kojoj element pripada.
    public int Find(int x)
    {
        while (_parent[x] != x)
        {
            _parent[x] = _parent[_parent[x]];   // skraćuje put, ubrzava sledeće pozive
            x = _parent[x];
        }
        return x;
    }

    // Spaja dve grupe. Vraća false ako su već bile ista grupa.
    public bool Union(int a, int b)
    {
        int rootA = Find(a);
        int rootB = Find(b);

        if (rootA == rootB)
        {
            return false;
        }

        _parent[rootA] = rootB;
        return true;
    }
}