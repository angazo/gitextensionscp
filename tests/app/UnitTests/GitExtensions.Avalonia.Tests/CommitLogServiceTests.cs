using System.Runtime.CompilerServices;
using GitExtensions.Avalonia.Services;
using GitExtensions.Extensibility.Git;
using GitUIPluginInterfaces;

namespace GitExtensions.Avalonia.Tests;

public sealed class CommitLogServiceTests
{
    [Test]
    public async Task LoadAsync_should_forward_batches_in_order()
    {
        GitRevision first = CreateRevision("first");
        GitRevision second = CreateRevision("second");
        ImmediateFakePort port = new([[first], [second]]);
        CommitLogService service = new(port);
        List<CommitPresentation> received = [];

        await service.LoadAsync("/repo", batch =>
        {
            received.AddRange(batch);
            return Task.CompletedTask;
        }, CancellationToken.None);

        received.Select(c => c.Subject).Should().Equal("first", "second");
        port.RequestedPaths.Should().ContainSingle().Which.Should().Be("/repo");
    }

    [Test]
    public async Task LoadAsync_should_cancel_the_previous_stream_when_a_new_one_starts()
    {
        GatedFakePort firstPort = new(CreateRevision("first"));
        ImmediateFakePort secondPort = new([[CreateRevision("second")]]);
        RoutingFakePort port = new(new Dictionary<string, IRevisionLogPort>
        {
            ["/repo-1"] = firstPort,
            ["/repo-2"] = secondPort
        });
        CommitLogService service = new(port);
        List<CommitPresentation> firstBatches = [];
        List<CommitPresentation> secondBatches = [];

        Task firstLoad = service.LoadAsync("/repo-1", batch =>
        {
            firstBatches.AddRange(batch);
            return Task.CompletedTask;
        }, CancellationToken.None);
        await firstPort.Started;

        await service.LoadAsync("/repo-2", batch =>
        {
            secondBatches.AddRange(batch);
            return Task.CompletedTask;
        }, CancellationToken.None);

        // VSTHRD003: firstLoad was already started above; awaiting its outcome here is intentional, not foreign.
#pragma warning disable VSTHRD003
        Func<Task> awaitFirstLoad = () => firstLoad;
        await awaitFirstLoad.Should().ThrowAsync<OperationCanceledException>();
#pragma warning restore VSTHRD003
        firstBatches.Should().ContainSingle().Which.Subject.Should().Be("first");
        secondBatches.Should().ContainSingle().Which.Subject.Should().Be("second");
    }

    [Test]
    public async Task StopAsync_should_cancel_an_active_stream_without_starting_a_new_one()
    {
        GatedFakePort port = new(CreateRevision("first"));
        CommitLogService service = new(port);
        List<CommitPresentation> received = [];

        Task load = service.LoadAsync("/repo", batch =>
        {
            received.AddRange(batch);
            return Task.CompletedTask;
        }, CancellationToken.None);
        await port.Started;

        await service.StopAsync();

        // VSTHRD003: load was already started above; awaiting its outcome here is intentional, not foreign.
#pragma warning disable VSTHRD003
        Func<Task> awaitLoad = () => load;
        await awaitLoad.Should().ThrowAsync<OperationCanceledException>();
#pragma warning restore VSTHRD003
    }

    private static GitRevision CreateRevision(string subject)
        => new(ObjectId.Random()) { Subject = subject, Author = "Author" };

    private sealed class ImmediateFakePort(IReadOnlyList<IReadOnlyList<GitRevision>> batches) : IRevisionLogPort
    {
        public List<string> RequestedPaths { get; } = [];

        public async IAsyncEnumerable<IReadOnlyList<GitRevision>> ReadLogAsync(string repositoryPath, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            RequestedPaths.Add(repositoryPath);
            foreach (IReadOnlyList<GitRevision> batch in batches)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return batch;
            }

            await Task.CompletedTask;
        }
    }

    private sealed class GatedFakePort(GitRevision firstRevision) : IRevisionLogPort
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public async IAsyncEnumerable<IReadOnlyList<GitRevision>> ReadLogAsync(string repositoryPath, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return [firstRevision];
            _started.TrySetResult();

            using CancellationTokenRegistration registration = cancellationToken.Register(() => _gate.TrySetCanceled(cancellationToken));

            // VSTHRD003: this TaskCompletionSource is a test-only gate awaited by design, not a foreign task.
#pragma warning disable VSTHRD003
            await _gate.Task;
#pragma warning restore VSTHRD003
        }
    }

    private sealed class RoutingFakePort(IReadOnlyDictionary<string, IRevisionLogPort> routes) : IRevisionLogPort
    {
        public IAsyncEnumerable<IReadOnlyList<GitRevision>> ReadLogAsync(string repositoryPath, CancellationToken cancellationToken)
            => routes[repositoryPath].ReadLogAsync(repositoryPath, cancellationToken);
    }
}
