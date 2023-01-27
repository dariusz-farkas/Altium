using Altium.TestTask.Sorter.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altium.TestTask.ConsoleApp;

internal static class ConsoleHost
{
    public static ServiceProvider BuildServiceProvider()
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

        return services.BuildServiceProvider();
    }
}