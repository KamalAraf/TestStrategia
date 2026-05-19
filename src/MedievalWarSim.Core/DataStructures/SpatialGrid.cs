using System.Runtime.InteropServices;

namespace MedievalWarSim.Core.DataStructures;

public class SpatialGrid
{
    private readonly Dictionary<long, List<int>> _cells = new();
    private readonly List<long> _activeKeys = new();
    private const float CellSize = 200f;

    public void Clear()
    {
        foreach (var key in _activeKeys)
            _cells[key].Clear();
        _activeKeys.Clear();
    }

    public void Insert(int id, float x, float y)
    {
        int cx = (int)MathF.Floor(x / CellSize);
        int cy = (int)MathF.Floor(y / CellSize);
        long key = (long)cx << 32 | (uint)cy;
        ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_cells, key, out bool exists);
        if (!exists)
            list = new List<int>();

        if (list!.Count == 0)
            _activeKeys.Add(key);

        list.Add(id);
    }

    public void Query(float x, float y, float radius, List<int> result)
    {
        int minCx = (int)MathF.Floor((x - radius) / CellSize);
        int maxCx = (int)MathF.Floor((x + radius) / CellSize);
        int minCy = (int)MathF.Floor((y - radius) / CellSize);
        int maxCy = (int)MathF.Floor((y + radius) / CellSize);

        for (int cx = minCx; cx <= maxCx; cx++)
        {
            for (int cy = minCy; cy <= maxCy; cy++)
            {
                long key = (long)cx << 32 | (uint)cy;
                if (_cells.TryGetValue(key, out var list))
                {
                    result.AddRange(list);
                }
            }
        }
    }
}
