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
        var pendingFiles = fileNames.ToList();

        string? mergedFilePath = null;
        int level = 0;
        int totalChunks = 0;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunks = pendingFiles.Chunk(_mergeOptions.ChunkSize).ToList();
            pendingFiles.Clear();

            foreach (var chunk in chunks)
            {
                var mergedFileName = $"{mergePrefix}-{Guid.NewGuid()}.data";

                _logger.LogInformation("[Level: {level}][Chunk {totalChunks}]\t Merging {count} files into {file}",
                    level, totalChunks, chunks.Count, mergedFileName);

                mergedFilePath = _fileSystem.GetFullPath(mergedFileName);

                if (chunk.Length == 1)
                {
                    var fileName = chunk.Single();
                    if (fileName.StartsWith(mergePrefix))
                    {
                        pendingFiles.Add(fileName);
                        continue;
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

                totalChunks++;
            }

            level++;
        } while (pendingFiles.Count > 1);

        return mergedFilePath;
    }

    private async Task MergeFiles(string[] chunk, string fullPath, CancellationToken cancellationToken)
    {
        await using var streamWriter = new StreamWriter(_fileSystem.File.OpenWrite(fullPath));

        // initialize
        var streamDictionary = chunk.ToDictionary(x => x, x => new StreamReader(_fileSystem.File.OpenRead(x)));
        var sortedList = new SortedList<string, string>(chunk.Length, new CustomComparer());

        await PreFillSortedList(streamDictionary, sortedList, cancellationToken);

        // we always have a copy of sorted list of current values from each of the streams
        while (sortedList.Count > 0)
        {
            var (min, path) = sortedList.First();
            sortedList.Remove(min);

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

            sortedList.Add(line, path);
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
        SortedList<string, string> sortedList,
        CancellationToken cancellationToken)
    {
        var streamsToRemove = new List<string>();
        foreach (var (key, stream) in streamDictionary)
        {
            var line = await stream.ReadLineAsync(cancellationToken);
            if (line != null)
            {
                sortedList.Add(line, key);
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