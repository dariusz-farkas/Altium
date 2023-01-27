using System.Buffers;

namespace Altium.TestTask.Sorter.Abstractions;

public interface ISorter
{
    Task Sort(FileData file, ArrayPool<string> pool, CancellationToken cancellationToken);
}