using System.Collections.Concurrent;
using Altium.TestTask.Sorter.Abstractions;
using Altium.TestTask.Sorter.Configuration;
using Altium.TestTask.Sorter.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altium.TestTask.Sorter;

internal class FileMerger : IMerger
{
    private readonly MergeOptions _mergeOptions;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<FileMerger> _logger;

    public FileMerger(IOptions<MergeOptions> mergeOptions, IFileSystem fileSystem, ILogger<FileMerger> logger)
    {
        _mergeOptions = mergeOptions.Value;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async Task<string?> Merge(IReadOnlyCollection<string> fileNames, CancellationToken cancellationToken)
    {
        if (_mergeOptions.ChunkSize < 2)
        {
            throw new InvalidOperationException("Cannot merge files with chink size lower than 2");
        }

        const string mergePrefix = "merged";
        var pendingFiles = new ConcurrentBag<string>(fileNames);

        int level = 0;
        int totalChunks = 0;

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = _mergeOptions.MaxParallelism
        };

        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunks = pendingFiles.Chunk(_mergeOptions.ChunkSize).ToList();
            pendingFiles.Clear();

            await Parallel.ForEachAsync(chunks, parallelOptions, async (chunk, ct) =>
            {
                var mergedFileName = $"{mergePrefix}-{Guid.NewGuid()}.data";

                _logger.LogInformation("[Level: {level}][Chunk {totalChunks}]\t Merging {count} files into {file}",
                    level, totalChunks, chunk.Length, mergedFileName);

                var mergedFilePath = _fileSystem.GetFullPath(mergedFileName);

                if (chunk.Length == 1)
                {
                    var fileName = chunk.Single();
                    if (fileName.StartsWith(mergePrefix))
                    {
                        pendingFiles.Add(fileName);
                        return;
                    }

                    pendingFiles.Add(mergedFileName);
                    _fileSystem.File.Move(_fileSystem.GetFullPath(fileName), mergedFilePath);
                }
                else
                {
                    pendingFiles.Add(mergedFileName);
                    var filePaths = chunk.Select(_fileSystem.GetFullPath).ToArray();
                    await MergeFiles(filePaths, mergedFilePath, ct);
                }

                Interlocked.Increment(ref totalChunks);
            });

            level++;
        } while (pendingFiles.Count > 1);

        return _fileSystem.GetFullPath(pendingFiles.Single());
    }

    private async Task MergeFiles(string[] chunk, string fullPath, CancellationToken cancellationToken)
    {
        await using var streamWriter = new StreamWriter(_fileSystem.File.OpenWrite(fullPath));

        // initialize
        var streamDictionary = chunk.ToDictionary(x => x, x => new StreamReader(_fileSystem.File.OpenRead(x)));
        var sortedQueue = new SortedBufferQueue<string, string>(new CustomComparer());

        await PreFillSortedList(streamDictionary, sortedQueue, cancellationToken);

        // we always have a copy of sorted list of current values from each of the streams
        while (sortedQueue.TryDequeue(out var min, out var path))
        {
            await streamWriter.WriteLineAsync(min.AsMemory(), cancellationToken);

            var affectedStream = streamDictionary[path];

            if (affectedStream.EndOfStream)
            {
                RemoveStream(path, affectedStream, streamDictionary, cancellationToken);
                continue;
            }

            var line = await affectedStream.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line))
            {
                RemoveStream(path, affectedStream, streamDictionary, cancellationToken);
                continue;
            }

            sortedQueue.Add(line, path);
        }
    }

    private void RemoveStream(string path,
        StreamReader affectedStream,
        Dictionary<string, StreamReader> streamDictionary,
        CancellationToken cancellationToken)
    {
        affectedStream.Dispose();
        streamDictionary.Remove(path);
        var temporaryPath = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), $"{Guid.NewGuid()}-remove.data");
        _fileSystem.File.Move(path, temporaryPath);
        _ = Task.Run(() => _fileSystem.File.Delete(temporaryPath), cancellationToken);
    }

    private static async Task PreFillSortedList(
        Dictionary<string, StreamReader> streamDictionary,
        SortedBufferQueue<string, string> sortedBufferQueue,
        CancellationToken cancellationToken)
    {
        var streamsToRemove = new List<string>();
        foreach (var (filePath, stream) in streamDictionary)
        {
            var line = await stream.ReadLineAsync(cancellationToken);
            if (line != null)
            {
                sortedBufferQueue.Add(line, filePath);
            }
            else
            {
                stream.Dispose();
                streamsToRemove.Add(filePath);
            }
        }

        foreach (var key in streamsToRemove)
        {
            streamDictionary.Remove(key);
        }
    }
}