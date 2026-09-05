using GitUIPluginInterfaces;

namespace GitExtensions.Avalonia.Services;

internal sealed record CommitPresentation(string ShortId, string Subject, string Author, DateTime Date)
{
    public static CommitPresentation FromRevision(GitRevision revision)
    {
        ArgumentNullException.ThrowIfNull(revision);

        return new CommitPresentation(
            revision.ObjectId.ToShortString(),
            revision.Subject,
            revision.Author ?? string.Empty,
            revision.AuthorDate);
    }
}

internal enum CommitListState
{
    Loading,
    Empty,
    Loaded,
    Error
}

internal interface IRevisionLogPort
{
    IAsyncEnumerable<IReadOnlyList<GitRevision>> ReadLogAsync(string repositoryPath, CancellationToken cancellationToken);
}
