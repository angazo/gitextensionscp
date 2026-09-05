using Avalonia.Headless;
using GitCommands;
using GitExtUtils;
using GitUI;
using NUnit.Framework;

[assembly: LevelOfParallelism(1)]

namespace GitExtensions.Avalonia.Tests;

/// <summary>
/// Starts Avalonia before each test, dispatches lifecycle-dependent assertions on its UI context,
/// and restores global platform delegates before disposing the headless session.
/// </summary>
internal sealed class AvaloniaHeadlessTestContext : IDisposable
{
    private readonly Action<Exception> _exceptionReporter;
    private readonly Action<GitExtensions.Extensibility.IWindow?, string, string?> _showError;
    private readonly Func<GitExtensions.Extensibility.IWindow?, string?, string?> _pickFolder;
    private readonly HeadlessUnitTestSession _session;

    public AvaloniaHeadlessTestContext()
    {
        _exceptionReporter = TaskManager.ExceptionReporter;
        _showError = UserMessageHandler.ShowError;
        _pickFolder = OsShellUtil.PickFolder;
        _session = HeadlessUnitTestSession.StartNew(typeof(Program), AvaloniaTestIsolationLevel.PerTest);
        _session.Dispatch(static () => { }, CancellationToken.None);
    }

    public void Dispose()
    {
        TaskManager.ExceptionReporter = _exceptionReporter;
        UserMessageHandler.ShowError = _showError;
        OsShellUtil.PickFolder = _pickFolder;
        _session.Dispose();
    }

    public Task<T> Dispatch<T>(Func<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return _session.Dispatch(action, CancellationToken.None);
    }
}
