using Altium.TestTask.Sorter.Abstractions;
using Altium.TestTask.Sorter.Configuration;
using Altium.TestTask.Sorter.Models;
using Microsoft.Extensions.Options;
using System.Buffers;

namespace Altium.TestTask.Sorter;

internal class ExternalMergeSortOrchestrator : ISortOrchestrator
{
    private readonly IPartitioner _partitioner;
    private readonly IMerger _merger;
    private readonly ISorter _sorter;

    private readonly SortOptions _sortOptions;

    public ExternalMergeSortOrchestrator(IPartitioner partitioner,
        ISorter sorter,
        IMerger merger,
        IOptions<SortOptions> sortOptions)
    {
        _partitioner = partitioner;
        _sorter = sorter;
        _merger = merger;
        _sortOptions = sortOptions.Value;
    }

    public async Task<string?> Execute(TestFile testFile, CancellationToken cancellationToken)
    {
        var partitionData = await _partitioner.Partition(testFile, cancellationToken);

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = _sortOptions.MaxParallelism
        };

        var arrayPool = ArrayPool<string>.Create(partitionData.Files.Max(x => x.RowCount), _sortOptions.MaxParallelism);

        await Parallel.ForEachAsync(
            partitionData.Files,
            parallelOptions,
            async (file, ct) => { await _sorter.Sort(file, arrayPool, ct); });

        var sortedFile = await _merger.Merge(partitionData.Files.Select(x => x.FileName).ToList(), cancellationToken);

        if (sortedFile == null)
        {
            return null;
        }

        var dir = Path.GetDirectoryName(testFile.Path);
        var resultFilePath = Path.Combine(dir!, "result.txt");
        File.Move(sortedFile, resultFilePath, true);

        return resultFilePath;
    }
}