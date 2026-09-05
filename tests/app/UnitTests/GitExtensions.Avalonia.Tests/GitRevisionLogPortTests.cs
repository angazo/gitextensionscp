using System.IO.Abstractions;
using CommonTestUtils;
using GitCommands;
using GitCommands.Git;
using GitExtensions.Avalonia.Services;
using GitExtensions.Extensibility.Git;
using GitUIPluginInterfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GitExtensions.Avalonia.Tests;

public sealed class GitRevisionLogPortTests
{
    [Test]
    public async Task ReadLogAsync_should_yield_the_commits_of_a_real_repository()
    {
        using AvaloniaHeadlessTestContext context = new();
        using ReferenceRepository repository = await context.Dispatch(static () => new ReferenceRepository(createCommit: true));
        GitRevisionLogPort port = new(CreateExecutorProvider());
        List<GitRevision> revisions = [];

        await foreach (IReadOnlyList<GitRevision> batch in port.ReadLogAsync(repository.Module.WorkingDir, CancellationToken.None))
        {
            revisions.AddRange(batch);
        }

        revisions.Should().ContainSingle(revision => revision.ObjectId.ToString() == repository.CommitHash);
    }

    [Test]
    public async Task ReadLogAsync_should_throw_operation_cancelled_when_the_token_is_already_cancelled()
    {
        GitRevisionLogPort port = new(CreateExecutorProvider());
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        Func<Task> readLog = async () =>
        {
            await foreach (IReadOnlyList<GitRevision> batch in port.ReadLogAsync("/nonexistent", cts.Token))
            {
                _ = batch;
            }
        };

        await readLog.Should().ThrowAsync<OperationCanceledException>();
    }

    private static IGitExecutorProvider CreateExecutorProvider()
    {
        ServiceCollection services = new();
        services.AddSingleton<IGitDirectoryResolver>(new GitDirectoryResolver(new FileSystem()));
        services.AddGitCommands();
        return services.BuildServiceProvider().GetRequiredService<IGitExecutorProvider>();
    }
}
