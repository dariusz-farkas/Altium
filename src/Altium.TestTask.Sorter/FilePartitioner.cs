using System.IO.Abstractions;
using Altium.TestTask.Sorter.Abstractions;
using Altium.TestTask.Sorter.Configuration;
using Altium.TestTask.Sorter.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IFileSystem = Altium.TestTask.Sorter.Abstractions.IFileSystem;

namespace Altium.TestTask.Sorter;

internal class FilePartitioner : IPartitioner
{
    private readonly IOptions<PartitionOptions> _partitionOptions;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<FilePartitioner> _logger;
    private const char NewLine = '\n';

    public FilePartitioner(
        IOptions<PartitionOptions> partitionOptions,
        IFileSystem fileSystem,
        ILogger<FilePartitioner> logger)
    {
        _partitionOptions = partitionOptions;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async Task<PartitionData> Partition(
        TestFile testFile,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[_partitionOptions.Value.BufferSize];
        await using var source = _fileSystem.File.Open(testFile.Path, FileMode.Open, FileAccess.Read);

        int lines = 0;
        int totalBytesWritten = 0;
        var files = new List<FileData>();
        OutputFileProvider fileProvider = OutputFileProvider.New(_fileSystem);

        _logger.LogInformation("Partitioning started...");

        try
        {
            int bytesRead;

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
               int remainingBytesInBuffer = bytesRead;

                var bufferOffset = 0;

                while (remainingBytesInBuffer > 0)
                {
                    fileProvider.EnsureExists();

                    var index = bufferOffset;
                    while (index < bytesRead)
                    {
                        var length = index - bufferOffset + 1;

                        if (buffer[index] == NewLine)
                        {
                            lines++;

                            // check if current file size + position of given line ending exceeds the file size -> if yes then write the buffer,
                            // and break execution in order to get to the parent loop.
                            if (totalBytesWritten + length >= _partitionOptions.Value.FileSize)
                            {
                                await fileProvider.WriteAsync(buffer, bufferOffset, length, cancellationToken);

                                bufferOffset += length;

                                remainingBytesInBuffer -= length;

                                files.Add(new FileData(fileProvider.FileName, lines));
                                _logger.LogInformation("Data splitted to {fileName}. stream position: {sourcePosition} out of {sourceLength}",
                                    fileProvider.FileName, source.Position, source.Length);

                                await fileProvider.Complete();

                                lines = 0;
                                totalBytesWritten = 0;

                                // break execution to ensure new file is present
                                break;
                            }
                        }

                        index++;
                    }

                    // iterated over entire buffer, just write remaining bytes.
                    if (index == bytesRead)
                    {
                        await fileProvider.WriteAsync(buffer, bufferOffset, remainingBytesInBuffer, cancellationToken);
                        totalBytesWritten += remainingBytesInBuffer;
                        break;
                    }
                }
            }
        }
        finally
        {
            if (fileProvider.IsAwaiting())
            {
                files.Add(new FileData(fileProvider.FileName, lines));
                _logger.LogInformation("Data splitted to {fileName}.", fileProvider.FileName);
            }

            await fileProvider.DisposeAsync();
        }

        return new PartitionData(files);
    }

    public sealed class OutputFileProvider : IAsyncDisposable
    {
        private readonly IFileSystem _fileSystem;
        private int _fileIndex;

        private FileSystemStream? _internalStream;

        public static OutputFileProvider New(IFileSystem fileSystem) => new OutputFileProvider(fileSystem);

        public OutputFileProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            FileName = $"unsorted-{_fileIndex++}.data";
            _internalStream = _fileSystem.File.Create(Path.Combine(_fileSystem.TempDir, FileName));
        }

        public string FileName { get; private set; }

        public bool IsAwaiting() => _internalStream != null;

        public async ValueTask DisposeAsync()
        {
            if (_internalStream != null)
            {
                await _internalStream.DisposeAsync();
            }
        }

        public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_internalStream == null)
            {
                throw new InvalidOperationException("Cannot write data on disposed stream.");
            }

            await _internalStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public void EnsureExists()
        {
            if (_internalStream == null)
            {
                FileName = $"unsorted-{_fileIndex++}.data";
                _internalStream = _fileSystem.File.Create(Path.Combine(_fileSystem.TempDir, FileName));
            }
        }

        public async Task Complete()
        {
            if (_internalStream != null)
            {
                await _internalStream.DisposeAsync();
                _internalStream = null;
            }
        }
    }
}