using Altium.TestTask.Sorter.Models;

namespace Altium.TestTask.Sorter.Abstractions;

public interface ISortOrchestrator
{
    Task<string?> Execute(TestFile testFile, CancellationToken cancellationToken);
}