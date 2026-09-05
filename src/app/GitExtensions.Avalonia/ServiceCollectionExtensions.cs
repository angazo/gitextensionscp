using System.IO.Abstractions;
using GitCommands;
using GitCommands.Git;
using GitExtensions.Avalonia.Localization;
using GitExtensions.Avalonia.Services;
using GitExtUtils;
using Microsoft.Extensions.DependencyInjection;

namespace GitExtensions.Avalonia;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAvaloniaServices(this IServiceCollection services)
    {
        services.AddGitExtUtils();

        FileSystem fileSystem = new();
        GitDirectoryResolver gitDirectoryResolver = new(fileSystem);

        services.AddSingleton<IFileSystem>(fileSystem);
        services.AddSingleton<IGitDirectoryResolver>(gitDirectoryResolver);
        services.AddSingleton(_ => AvaloniaLocalizationService.FromAssembly(
            typeof(AvaloniaLocalizationService).Assembly,
            Environment.GetEnvironmentVariable(AvaloniaLocalizationService.CultureEnvironmentVariable)));

        services.AddGitCommands();
        services.AddSingleton<IRepositoryHistoryPort, ProductionRepositoryHistoryPort>();
        services.AddSingleton<IRepositoryReader, GitRepositoryReader>();
        services.AddSingleton<IRepositoryFolderPicker>(sp =>
            new ProductionRepositoryFolderPicker(() => App.MainWindow));
        services.AddSingleton<RepositoryOpeningService>();
        services.AddSingleton<IRevisionLogPort, GitRevisionLogPort>();
        services.AddSingleton<CommitLogService>();
        services.AddSingleton<CommitListViewModel>();
        services.AddSingleton<RepositoryShellViewModel>();

        return services;
    }
}
