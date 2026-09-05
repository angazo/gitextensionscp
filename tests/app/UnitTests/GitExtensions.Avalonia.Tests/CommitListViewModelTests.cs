using System.Runtime.CompilerServices;
using GitExtensions.Avalonia.Services;
using GitExtensions.Extensibility.Git;
using GitUIPluginInterfaces;

namespace GitExtensions.Avalonia.Tests;

public sealed class CommitListViewModelTests
{
    [Test]
    public async Task LoadAsync_should_report_loaded_state_and_commits_after_batches_arrive()
    {
        GitRevision revision = CreateRevision("initial commit");
        CommitLogService service = new(new ImmediateFakePort([[revision]]));
        CommitListViewModel viewModel = new(service);

        await viewModel.LoadAsync("/repo");

        viewModel.State.Should().Be(CommitListState.Loaded);
        viewModel.IsLoaded.Should().BeTrue();
        viewModel.Commits.Should().ContainSingle().Which.Subject.Should().Be("initial commit");
    }

    [Test]
    public async Task LoadAsync_should_report_empty_state_when_the_repository_has_no_commits()
    {
        CommitLogService service = new(new ImmediateFakePort([]));
        CommitListViewModel viewModel = new(service);

        await viewModel.LoadAsync("/repo");

        viewModel.State.Should().Be(CommitListState.Empty);
        viewModel.IsEmpty.Should().BeTrue();
        viewModel.Commits.Should().BeEmpty();
    }

    [Test]
    public async Task LoadAsync_should_report_error_state_when_reading_fails()
    {
        CommitLogService service = new(new ThrowingFakePort());
        CommitListViewModel viewModel = new(service);

        await viewModel.LoadAsync("/repo");

        viewModel.State.Should().Be(CommitListState.Error);
        viewModel.HasCommitError.Should().BeTrue();
        viewModel.ErrorMessage.Should().Be("read failed");
    }

    [Test]
    public async Task RetryCommand_should_reload_the_last_repository_and_recover_from_error()
    {
        FlakyFakePort port = new();
        CommitLogService service = new(port);
        CommitListViewModel viewModel = new(service);

        await viewModel.LoadAsync("/repo");
        viewModel.State.Should().Be(CommitListState.Error);

        await viewModel.RetryCommand.ExecuteAsync(null);

        viewModel.State.Should().Be(CommitListState.Loaded);
        viewModel.Commits.Should().ContainSingle().Which.Subject.Should().Be("recovered");
    }

    private static GitRevision CreateRevision(string subject)
        => new(ObjectId.Random()) { Subject = subject, Author = "Author" };

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

    private sealed class ThrowingFakePort : IRevisionLogPort
    {
        public IAsyncEnumerable<IReadOnlyList<GitRevision>> ReadLogAsync(string repositoryPath, CancellationToken cancellationToken)
            => throw new InvalidOperationException("read failed");
    }

    private sealed class FlakyFakePort : IRevisionLogPort
    {
        private int _attempt;

        public IAsyncEnumerable<IReadOnlyList<GitRevision>> ReadLogAsync(string repositoryPath, CancellationToken cancellationToken)
        {
            _attempt++;
            return _attempt == 1
                ? throw new InvalidOperationException("read failed")
                : ReadRecoveredAsync();
        }

        private static async IAsyncEnumerable<IReadOnlyList<GitRevision>> ReadRecoveredAsync()
        {
            yield return [CreateRevision("recovered")];
            await Task.CompletedTask;
        }
    }
}
