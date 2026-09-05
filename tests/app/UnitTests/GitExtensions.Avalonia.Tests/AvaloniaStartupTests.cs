using System.Globalization;
using System.IO.Abstractions;
using GitCommands;
using GitExtensions.Avalonia;
using GitExtensions.Avalonia.Localization;
using GitExtUtils;
using GitUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Threading;

namespace GitExtensions.Avalonia.Tests;

[NonParallelizable]
public sealed class AvaloniaStartupTests
{
    private AvaloniaHeadlessTestContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        _context = new AvaloniaHeadlessTestContext();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public void OnFrameworkInitializationCompleted_should_register_portable_services()
    {
        App.ServiceProvider.GetRequiredService<IFileSystem>().Should().NotBeNull();

        AvaloniaLocalizationService localization = App.ServiceProvider.GetRequiredService<AvaloniaLocalizationService>();
        localization["OpenRepository"].Should().Be("Open repository");
        localization.ActiveCulture = CultureInfo.GetCultureInfo("en");

        int notificationCount = 0;
        localization.PropertyChanged += (_, _) => notificationCount++;
        localization.SetCulture("es").Should().BeTrue();
        notificationCount.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task OnFrameworkInitializationCompleted_should_initialize_joinable_task_context()
    {
        (bool HasContext, bool IsOnMainThread) result = await _context.Dispatch(
            static () => (ThreadHelper.HasJoinableTaskContext, ThreadHelper.JoinableTaskContext.IsOnMainThread));

        result.HasContext.Should().BeTrue();
        result.IsOnMainThread.Should().BeTrue();
    }

    [Test]
    public void OnFrameworkInitializationCompleted_should_install_headless_safe_delegates()
    {
        Action action = () => TaskManager.ExceptionReporter(new InvalidOperationException("test"));

        action.Should().NotThrow();
        action = () => UserMessageHandler.ShowError(null, "text", "caption");
        action.Should().NotThrow();
        OsShellUtil.PickFolder(null, null).Should().BeNull();
    }
}
