using Altium.TestTask.ConsoleApp;
using Altium.TestTask.Sorter;
using Altium.TestTask.Sorter.Abstractions;
using Altium.TestTask.Sorter.Models;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = ConsoleHost.BuildServiceProvider();

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    Console.WriteLine("Canceling...");
    cts.Cancel();
    e.Cancel = true;
};

return await Parser.Default.ParseArguments<SortOption, CreateOption>(args)
    .MapResult(
        (SortOption options) => Sort(options, serviceProvider, cts.Token),
        (CreateOption options) => Create(options, serviceProvider, cts.Token),
        _ => Task.FromResult(0));


async Task<int> Sort(SortOption sortOption, IServiceProvider sp, CancellationToken cancellationTokenSource)
{
    var file = new TestFile(sortOption.File);
    if (!file.IsValid())
    {
        Console.Error.WriteLine("File does not exists.");
        return 1;
    }

    var orchestrator = sp.GetRequiredService<ISortOrchestrator>();
    var output = await orchestrator.Execute(file, cancellationTokenSource);
    if (output is not null)
    {
        Console.WriteLine(output);
        return 1;
    }

    return 0;
}

async Task<int> Create(CreateOption options, IServiceProvider sp, CancellationToken cancellationToken)
{
    var generator = sp.GetRequiredService<FileGenerator>();
    await generator.Generate(options.Size.ValueInKb, options.File, cancellationToken);

    return 0;
}