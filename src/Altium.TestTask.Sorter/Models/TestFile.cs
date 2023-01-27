namespace Altium.TestTask.Sorter.Models;

public sealed record TestFile(string Path)
{
    public bool IsValid() => File.Exists(Path);
}