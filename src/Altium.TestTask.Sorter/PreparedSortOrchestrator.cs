using Altium.TestTask.Sorter.Abstractions;
using Altium.TestTask.Sorter.Extensions;
using Altium.TestTask.Sorter.Models;
using Microsoft.Extensions.Logging;

namespace Altium.TestTask.Sorter;

internal class PreparedSortOrchestrator : ISortOrchestrator
{
    private readonly ISortOrchestrator _sortOrchestrator;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<PreparedSortOrchestrator> _logger;

    public PreparedSortOrchestrator(ISortOrchestrator sortOrchestrator, IFileSystem fileSystem, ILogger<PreparedSortOrchestrator> logger)
    {
        _sortOrchestrator = sortOrchestrator;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async Task<string?> Execute(TestFile testFile, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_fileSystem.TempDir);

        try
        {
            return await _sortOrchestrator.Execute(testFile, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred during sorting.");
            return null;
        }
        finally
        {
            Directory.Delete(_fileSystem.TempDir, true);
        }
    }
}