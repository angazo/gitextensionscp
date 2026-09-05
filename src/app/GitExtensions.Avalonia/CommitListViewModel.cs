using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Avalonia.Services;

namespace GitExtensions.Avalonia;

internal sealed partial class CommitListViewModel : ObservableObject
{
    private readonly CommitLogService _logService;
    private string? _repositoryPath;
    private int _loadVersion;

    public CommitListViewModel(CommitLogService logService)
    {
        ArgumentNullException.ThrowIfNull(logService);
        _logService = logService;
    }

    public ObservableCollection<CommitPresentation> Commits { get; } = [];

    [ObservableProperty]
    private CommitListState _state = CommitListState.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    public bool IsLoading => State == CommitListState.Loading;

    public bool IsEmpty => State == CommitListState.Empty;

    public bool IsLoaded => State == CommitListState.Loaded;

    public bool HasCommitError => State == CommitListState.Error;

    public async Task LoadAsync(string repositoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);

        int loadVersion = Interlocked.Increment(ref _loadVersion);
        _repositoryPath = repositoryPath;
        Commits.Clear();
        ErrorMessage = null;
        State = CommitListState.Loading;

        try
        {
            await _logService.LoadAsync(repositoryPath, batch => AppendBatchAsync(batch, loadVersion), CancellationToken.None);

            if (loadVersion == _loadVersion && State == CommitListState.Loading)
            {
                State = CommitListState.Empty;
            }
        }
        catch (OperationCanceledException)
        {
            // A newer LoadAsync call already superseded this one and owns the current state.
        }
        catch (Exception exception)
        {
            if (loadVersion == _loadVersion)
            {
                ErrorMessage = exception.Message;
                State = CommitListState.Error;
            }
        }
    }

    public Task StopAsync()
    {
        Interlocked.Increment(ref _loadVersion);
        _repositoryPath = null;
        return _logService.StopAsync();
    }

    partial void OnErrorMessageChanged(string? value)
        => OnPropertyChanged(nameof(HasCommitError));

    partial void OnStateChanged(CommitListState value)
    {
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsLoaded));
        OnPropertyChanged(nameof(HasCommitError));
    }

    [RelayCommand]
    private Task RetryAsync()
        => _repositoryPath is null ? Task.CompletedTask : LoadAsync(_repositoryPath);

    private async Task AppendBatchAsync(IReadOnlyList<CommitPresentation> batch, int loadVersion)
    {
        if (batch.Count == 0)
        {
            return;
        }

        await DispatchAsync(
            () =>
            {
                if (loadVersion != _loadVersion)
                {
                    return;
                }

                foreach (CommitPresentation commit in batch)
                {
                    Commits.Add(commit);
                }

                State = CommitListState.Loaded;
            });
    }

    private static async Task DispatchAsync(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(action);
    }
}
