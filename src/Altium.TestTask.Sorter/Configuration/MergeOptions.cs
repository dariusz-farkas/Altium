namespace Altium.TestTask.Sorter.Configuration;
public class MergeOptions
{
    public const string Merge = "Merge";

    public int ChunkSize { get; init; } = 10;

    public int MaxParallelism { get; init; } = 1;
}