namespace Altium.TestTask.Sorter.Configuration
{
    public class SortOptions
    {
        public const string Sort = "Sort";
        public int InputBufferSize { get; init; } = 4096;
        public int OutputBufferSize { get; init; } = 4096;
        public int MaxParallelism { get; init; } = 1;
    }
}
