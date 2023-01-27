using System.IO.Abstractions;
using Altium.TestTask.Sorter.Configuration;
using Microsoft.Extensions.Options;

namespace Altium.TestTask.Sorter.Utilities;

internal class AltiumFileSystem : FileSystem, Abstractions.IFileSystem
{
    private readonly FileSystemOptions _options;
    
    public AltiumFileSystem(IOptions<FileSystemOptions> options)
    {
        _options = options.Value;
    }

    public string TempDir => _options.TempDir;
    public string GetFullPath(string fileName) => Path.Combine(_options.TempDir, fileName);
}