namespace Altium.TestTask.Sorter.Abstractions;

public interface IMerger
{
    Task<string?> Merge(IReadOnlyCollection<string> fileNames, CancellationToken cancellationToken);
}