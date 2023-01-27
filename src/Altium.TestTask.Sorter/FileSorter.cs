using System.Buffers;
using Altium.TestTask.Sorter.Abstractions;
using Altium.TestTask.Sorter.Configuration;
using Altium.TestTask.Sorter.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altium.TestTask.Sorter;

internal class FileSorter : ISorter
{
    private readonly IOptions<SortOptions> _options;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<FileSorter> _logger;

    public FileSorter(IFileSystem fileSystem, IOptions<SortOptions> options, ILogger<FileSorter> logger)
    {
        _options = options;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async Task Sort(FileData file, ArrayPool<string> pool, CancellationToken cancellationToken)
    {
        var data = pool.Rent(file.RowCount);

        try
        {
            _logger.LogInformation("Sorting file {filePath}..", file.FileName);
            var index = 0;

            await using var fs = _fileSystem.File.Open(_fileSystem.GetFullPath(file.FileName), FileMode.Open, FileAccess.ReadWrite);
            using (var reader = new StreamReader(fs, bufferSize: _options.Value.InputBufferSize, leaveOpen: true))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync(cancellationToken: cancellationToken);
                    if (line == null)
                    {
                        break;
                    }

                    if (line == string.Empty)
                    {
                        continue;
                    }

                    data[index++] = line;
                }
            }

            Array.Sort(data, 0, index, CustomComparer.Default);
            fs.Position = 0;

            await using var writer = new StreamWriter(fs, bufferSize: _options.Value.OutputBufferSize);
            foreach (var line in data[..index])
            {
                await writer.WriteLineAsync(line);
            }
        }
        finally
        {
            pool.Return(data);
        }
    }
}