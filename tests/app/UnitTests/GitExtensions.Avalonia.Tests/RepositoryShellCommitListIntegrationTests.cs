using System.Runtime.CompilerServices;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Avalonia.Localization;
using GitExtensions.Avalonia.Services;
using GitExtensions.Extensibility.Git;
using GitUIPluginInterfaces;

namespace GitExtensions.Avalonia.Tests;

public sealed class RepositoryShellCommitListIntegrationTests
{
    [Test]
    public async Task Opening_a_different_repository_should_replace_the_commit_list_with_the_new_repositorys_commits()
    {
        RoutingFakePort port = new(new Dictionary<string, IRevisionLogPort>
        {
            ["/repo-1"] = new ImmediateFakePort([[CreateRevision("repo-1 commit")]]),
            ["/repo-2"] = new ImmediateFakePort([[CreateRevision("repo-2 commit")]])
        });
        CommitListViewModel commitList = new(new CommitLogService(port));
        AvaloniaLocalizationService localization = new(new Dictionary<string, XliffCatalog>(StringComparer.OrdinalIgnoreCase));
        RepositoryOpeningService openingService = new(
            new FakeHistory(),
            new FakePicker(null),
            new FakeReader());
        RepositoryShellViewModel shell = new(openingService, localization, commitList);

        await OpenPathAsync(shell, "/repo-1");
        commitList.Commits.Should().ContainSingle().Which.Subject.Should().Be("repo-1 commit");

        await OpenPathAsync(shell, "/repo-2");

        commitList.Commits.Should().ContainSingle().Which.Subject.Should().Be("repo-2 commit");
    }

    private static async Task OpenPathAsync(RepositoryShellViewModel shell, string path)
    {
        await shell.OpenRecentCommand.ExecuteAsync(new Repository(path));
    }

    private static GitRevision CreateRevision(string subject)
        => new(ObjectId.Random()) { Subject = subject, Author = "Author" };

    private sealed class FakeHistory : IRepositoryHistoryPort
    {
        public Task<IList<Repository>> AddAsMostRecentAsync(string path)
            => Task.FromResult<IList<Repository>>([]);

        public Task<IList<Repository>> LoadRecentHistoryAsync()
            => Task.FromResult<IList<Repository>>([]);
    }

    private sealed class FakePicker(string? path) : IRepositoryFolderPicker
    {
        public Task<string?> PickFolderAsync(string? selectedPath, CancellationToken cancellationToken)
            => Task.FromResult(path);
    }

    private sealed class FakeReader : IRepositoryReader
    {
        public Task<RepositoryPresentation> ReadAsync(string path, CancellationToken cancellationToken)
            => Task.FromResult(new RepositoryPresentation(path, "main", [], 0));
    }

    private sealed class ImmediateFakePort(IReadOnlyList<IReadOnlyList<GitRevision>> batches) : IRevisionLogPort
    {
        public async IAsyncEnumerable<IReadOnlyList<GitRevision>> ReadLogAsync(string repositoryPath, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (IReadOnlyList<GitRevision> batch in batches)
            {
                yield return batch;
            }

            await Task.CompletedTask;
        }
    }

    private sealed class RoutingFakePort(IReadOnlyDictionary<string, IRevisionLogPort> routes) : IRevisionLogPort
    {
        public IAsyncEnumerable<IReadOnlyList<GitRevision>> ReadLogAsync(string repositoryPath, CancellationToken cancellationToken)
            => routes[repositoryPath].ReadLogAsync(repositoryPath, cancellationToken);
    }
}
