using Altium.TestTask.Sorter.Models;

namespace Altium.TestTask.Sorter.Abstractions;

public sealed record FileData(string FileName, int RowCount);
public sealed record PartitionData(IReadOnlyCollection<FileData> Files);

public interface IPartitioner
{
    Task<PartitionData> Partition(TestFile file, CancellationToken cancellationToken);
}