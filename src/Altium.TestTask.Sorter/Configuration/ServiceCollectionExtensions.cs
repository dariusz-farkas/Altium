using Altium.TestTask.Sorter.Abstractions;
using Altium.TestTask.Sorter.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Altium.TestTask.Sorter.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSortComponents(this IServiceCollection services)
    {
        return services.AddTransient<IPartitioner, FilePartitioner>()
            .AddSingleton<ISorter, FileSorter>()
            .AddSingleton<IMerger, FileMerger>()
            .AddSingleton<ISortOrchestrator, ExternalMergeSortOrchestrator>()
            .Decorate<ISortOrchestrator, PreparedSortOrchestrator>()
            .AddSingleton<IFileSystem, AltiumFileSystem>()
            .AddSingleton<FileGenerator>();
    }
}