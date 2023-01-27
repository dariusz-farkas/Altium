namespace Altium.TestTask.Sorter.Configuration;

public class PartitionOptions
{
    public const string Partition = "Partition";

    public int FileSize { get; init; } = 256 * 1024 * 1024;
    public int BufferSize { get; init; } = 1024 * 64;
}