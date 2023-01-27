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

    public async Task<bool> Verify(string path, CancellationToken cancellationToken)
    {
        using var fileStream = _fileSystem.File.OpenRead(path);
        using var reader = new StreamReader(fileStream);

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