using Altium.TestTask.Sorter.Abstractions;
using Altium.TestTask.Sorter.Utilities;

namespace Altium.TestTask.Sorter;

public class FileVerifier
{
    private readonly IFileSystem _fileSystem;

    public FileVerifier(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<bool> Verify(string unsortedFile, string sortedFile, CancellationToken cancellationToken)
    {
        var sourceSize = _fileSystem.FileInfo.New(unsortedFile).Length;
        using var fileStream = _fileSystem.File.OpenRead(sortedFile);
        using var reader = new StreamReader(fileStream);

        if (sourceSize != fileStream.Length)
        {
            return false;
        }

        var left = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrEmpty(left))
        {
            return true;
        }

        while (!reader.EndOfStream)
        {
            var right = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrEmpty(right))
            {
                return true;
            }

            if (CustomComparer.Default.Compare(left, right) == 1)
            {
                return false;
            }
        }

        return true;
    }
}