using Altium.TestTask.Sorter.Configuration;
using Altium.TestTask.Sorter.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Altium.TestTask.ConsoleApp;

internal static class ConsoleHost
{
    public static (ServiceProvider serviceProvider, CancellationToken token) Build()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var services = new ServiceCollection()
            .AddLogging(configure: c => c.AddConsole())
            .AddSortComponents()
            .Configure<SortOptions>(config.GetSection(SortOptions.Sort))
            .Configure<MergeOptions>(config.GetSection(MergeOptions.Merge))
            .Configure<PartitionOptions>(config.GetSection(PartitionOptions.Partition))
            .Configure<FileSystemOptions>(config.GetSection(FileSystemOptions.FileSystem));

        var sp = services.BuildServiceProvider();

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            Console.WriteLine("Canceling...");
            cts.Cancel();
            e.Cancel = true;
        };

        return (sp, cts.Token);
    }

    public static async Task<int> Run(Func<Task<int>> action)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message);
            return 0;
        }
        finally
        {
            stopwatch.Stop();
            Console.WriteLine("Elapsed time is {0}", stopwatch.Elapsed);
        }
    }
}