using GitUIPluginInterfaces;

namespace GitExtensions.Avalonia.Services;

internal sealed class CommitLogService(IRevisionLogPort port)
{
    private CancellationTokenSource? _cts;

    public async Task LoadAsync(string repositoryPath, Func<IReadOnlyList<CommitPresentation>, Task> onBatch, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);
        ArgumentNullException.ThrowIfNull(onBatch);

        await StopAsync();

        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cts = cts;

        await foreach (IReadOnlyList<GitRevision> batch in port.ReadLogAsync(repositoryPath, cts.Token))
        {
            await onBatch(MapBatch(batch));
        }
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? previous = _cts;
        _cts = null;

        if (previous is null)
        {
            return;
        }

        await previous.CancelAsync();
        previous.Dispose();
    }

    private static IReadOnlyList<CommitPresentation> MapBatch(IReadOnlyList<GitRevision> batch)
    {
        CommitPresentation[] result = new CommitPresentation[batch.Count];
        for (int i = 0; i < batch.Count; i++)
        {
            result[i] = CommitPresentation.FromRevision(batch[i]);
        }

        return result;
    }
}
