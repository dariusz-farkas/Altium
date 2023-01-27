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
        const string mergePrefix = "merged";
        var pendingFiles = new ConcurrentBag<string>(fileNames);

        string? mergedFilePath = null;
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


            //foreach (var chunk in chunks)
            //{
            await Parallel.ForEachAsync(chunks, parallelOptions, async (chunk, ct) =>
            {
                var mergedFileName = $"{mergePrefix}-{Guid.NewGuid()}.data";

                _logger.LogInformation("[Level: {level}][Chunk {totalChunks}]\t Merging {count} files into {file}",
                    level, totalChunks, chunk.Length, mergedFileName);

                mergedFilePath = _fileSystem.GetFullPath(mergedFileName);

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
                    await MergeFiles(filePaths, mergedFilePath, cancellationToken);
                }

                Interlocked.Increment(ref totalChunks);
                //totalChunks++;
            });

            

            //}

            level++;
        } while (pendingFiles.Count > 1);

        return mergedFilePath;
    }

    private async Task MergeFiles(string[] chunk, string fullPath, CancellationToken cancellationToken)
    {
        await using var streamWriter = new StreamWriter(_fileSystem.File.OpenWrite(fullPath));

        // initialize
        var streamDictionary = chunk.ToDictionary(x => x, x => new StreamReader(_fileSystem.File.OpenRead(x)));
        var sortedList = new SortedSet<(string, string)>(new SetCustomComparer());

        await PreFillSortedList(streamDictionary, sortedList, cancellationToken);

        // we always have a copy of sorted list of current values from each of the streams
        while (sortedList.Count > 0)
        {
            var minElement = sortedList.First();
            var (min, path) = minElement;
            sortedList.Remove(minElement);

            await streamWriter.WriteLineAsync(min.AsMemory(), cancellationToken);

            var affectedStream = streamDictionary[path];

            if (affectedStream.EndOfStream)
            {
                RemoveStream(cancellationToken, affectedStream, streamDictionary, path);
                continue;
            }

            var line = await affectedStream.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line))
            {
                RemoveStream(cancellationToken, affectedStream, streamDictionary, path);
                continue;
            }

            sortedList.Add((line, path));
        }
    }

    private void RemoveStream(CancellationToken cancellationToken, StreamReader affectedStream, Dictionary<string, StreamReader> streamDictionary,
        string path)
    {
        affectedStream.Dispose();
        streamDictionary.Remove(path);
        _ = Task.Run(() => _fileSystem.File.Delete(path), cancellationToken);
    }

    private static async Task PreFillSortedList(
        Dictionary<string, StreamReader> streamDictionary,
        SortedSet<(string, string)> sortedList,
        CancellationToken cancellationToken)
    {
        var streamsToRemove = new List<string>();
        foreach (var (key, stream) in streamDictionary)
        {
            var line = await stream.ReadLineAsync(cancellationToken);
            if (line != null)
            {
                sortedList.Add((line, key));
            }
            else
            {
                stream.Dispose();
                streamsToRemove.Add(key);
            }
        }

        foreach (var key in streamsToRemove)
        {
            streamDictionary.Remove(key);
        }
    }
}