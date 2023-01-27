namespace Altium.TestTask.Sorter.Abstractions;

public interface IFileSystem : System.IO.Abstractions.IFileSystem
{
    string GetFullPath(string fileName);

    string TempDir { get; }
}