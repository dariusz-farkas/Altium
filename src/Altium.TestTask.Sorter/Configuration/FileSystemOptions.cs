namespace Altium.TestTask.Sorter.Configuration;

public class FileSystemOptions
{
    public const string FileSystem = "FileSystem";

    public string TempDir { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
}