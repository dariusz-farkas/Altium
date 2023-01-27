using System.Diagnostics.CodeAnalysis;

namespace Altium.TestTask.Sorter.Utilities;

/// <summary>
/// Sorted buffer queue 
/// </summary>
/// <remarks>
/// Ensures the buffer is always sorted.
/// </remarks>
internal class SortedBufferQueue<TKey, TOrigin>
{
    sealed record Entry(TKey Key, TOrigin Origin);

    private readonly IComparer<TKey> _comparer;
    private readonly IList<Entry> _internalList = new List<Entry>();

    public SortedBufferQueue(IComparer<TKey> comparer)
    {
        _comparer = comparer;
    }

    public void Add(TKey key, TOrigin origin)
    {
        if (_internalList.Any(x => x.Origin!.Equals(origin)))
        {
            throw new InvalidOperationException("Key with given origin already exists");
        }

        bool isAdded = false;
        for (int i = 0; i < _internalList.Count; i++)
        {
            if (_comparer.Compare(_internalList[i].Key, key) >= 1)
            {
                _internalList.Insert(i, new(key, origin));
                isAdded = true;
                break;
            }
        }

        if (!isAdded)
        {
            _internalList.Insert(_internalList.Count, new(key, origin));
        }
    }

    public int Count => _internalList.Count;

    public bool TryDequeue([NotNullWhen(true)]out TKey? key, [NotNullWhen(true)] out TOrigin? origin)
    {
        if (_internalList.Count == 0)
        {
            key = default;
            origin = default;
            return false;
        }

        var entry = _internalList[0];
        _internalList.RemoveAt(0);
        key = entry.Key!;
        origin = entry.Origin!;
        return true;
    }
}