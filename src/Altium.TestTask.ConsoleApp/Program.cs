using Altium.TestTask.ConsoleApp;
using Altium.TestTask.Sorter;
using Altium.TestTask.Sorter.Abstractions;
using Altium.TestTask.Sorter.Models;
using Altium.TestTask.Sorter.Utilities;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;

var (serviceProvider, token) = ConsoleHost.Build();

var rs = await ConsoleHost.Run(async () =>
{
    return await Parser.Default.ParseArguments<SortOption, CreateOption, VerifyOption>(args)
        .MapResult(
            (SortOption options) => Sort(options, serviceProvider, token),
            (CreateOption options) => Create(options, serviceProvider, token),
            (VerifyOption options) => Verify(options, serviceProvider, token),
            _ => Task.FromResult(0));
});

async Task<int> Sort(SortOption options, IServiceProvider sp, CancellationToken cancellationTokenSource)
{
    options.Validate();

    var file = new TestFile(options.File);

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
    options.Validate();
    
    var generator = sp.GetRequiredService<FileGenerator>();
    var byteLength = await generator.Generate(options.Size.ByteLength, options.File, cancellationToken);

    Console.WriteLine($"File has been generated with {FileSizeFormatter.FormatSize(byteLength)}");
    return 0;
}
async Task<int> Verify(VerifyOption options, IServiceProvider sp, CancellationToken cancellationToken)
{
    options.Validate();
    var generator = sp.GetRequiredService<FileVerifier>();
    var rs = await generator.Verify(options.UnsortedFile, options.SortedFile, cancellationToken);
    Console.WriteLine($"File {options.SortedFile} is sorted: {rs}");

    return 0;
}