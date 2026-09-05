using System.Threading.Channels;
using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility.Git;
using GitUIPluginInterfaces;

namespace GitExtensions.Avalonia.Services;

internal sealed class GitRevisionLogPort(IGitExecutorProvider executorProvider) : IRevisionLogPort
{
    public IAsyncEnumerable<IReadOnlyList<GitRevision>> ReadLogAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);

        Channel<IReadOnlyList<GitRevision>> channel = Channel.CreateUnbounded<IReadOnlyList<GitRevision>>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

        _ = Task.Run(
            () =>
            {
                try
                {
                    GitModule module = new(executorProvider, repositoryPath);
                    RevisionReader reader = new(module);
                    ChannelRevisionObserver observer = new(channel.Writer);

                    // No revision filter: logs the currently checked out branch, matching plain `git log`.
                    reader.GetLog(observer, revisionFilter: "", pathFilter: "", hasNotes: false, autostashLabel: "", cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    channel.Writer.TryComplete();
                }
                catch (Exception exception)
                {
                    channel.Writer.TryComplete(exception);
                }
            },
            cancellationToken);

        return channel.Reader.ReadAllAsync(cancellationToken);
    }

    private sealed class ChannelRevisionObserver(ChannelWriter<IReadOnlyList<GitRevision>> writer) : IObserver<IReadOnlyList<GitRevision>>
    {
        public void OnCompleted()
            => writer.TryComplete();

        public void OnError(Exception error)
            => writer.TryComplete(error);

        public void OnNext(IReadOnlyList<GitRevision> value)
        {
            if (value.Count > 0)
            {
                writer.TryWrite(value);
            }
        }
    }
}
