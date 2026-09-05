using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Avalonia.Localization;
using GitExtensions.Avalonia.Services;

namespace GitExtensions.Avalonia;

internal sealed partial class RepositoryShellViewModel : ObservableObject
{
    private readonly AvaloniaLocalizationService _localization;
    private CancellationTokenSource? _openingCancellation;
    private string? _lastPath;

    public RepositoryShellViewModel(RepositoryOpeningService openingService, AvaloniaLocalizationService localization, CommitListViewModel commitList)
    {
        ArgumentNullException.ThrowIfNull(openingService);
        ArgumentNullException.ThrowIfNull(localization);
        ArgumentNullException.ThrowIfNull(commitList);

        _openingService = openingService;
        _localization = localization;
        CommitList = commitList;
        _localization.PropertyChanged += LocalizationPropertyChanged;
        StatusMessage = _localization.Resolve(AvaloniaLocalizationKeys.NoRepositoryOpen);
    }

    private readonly RepositoryOpeningService _openingService;

    public AvaloniaLocalizationService Localization => _localization;

    public CommitListViewModel CommitList { get; }

    public string BranchSummary => ActiveRepository is null
        ? string.Empty
        : $"{Localization.Resolve(AvaloniaLocalizationKeys.Branch)}: {ActiveRepository.Branch}";

    public string RemotesSummary => ActiveRepository is null
        ? string.Empty
        : $"{Localization.Resolve(AvaloniaLocalizationKeys.Remotes)}: {ActiveRepository.RemoteSummary}";

    public ObservableCollection<Repository> RecentRepositories { get; } = [];

    [ObservableProperty]
    private RepositoryPresentation? _activeRepository;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isWelcome = true;

    [ObservableProperty]
    private string _statusMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasRepository => ActiveRepository is not null;

    public async Task InitializeAsync()
    {
        RecentRepositories.Clear();
        foreach (Repository repository in await _openingService.LoadRecentHistoryAsync())
        {
            RecentRepositories.Add(repository);
        }
    }

    partial void OnActiveRepositoryChanged(RepositoryPresentation? value)
    {
        OnPropertyChanged(nameof(HasRepository));
        OnPropertyChanged(nameof(BranchSummary));
        OnPropertyChanged(nameof(RemotesSummary));

        // LoadAsync/StopAsync observe their own cancellation and errors, so firing them here is safe.
        _ = value is not null ? CommitList.LoadAsync(value.Path) : CommitList.StopAsync();
    }

    private void LocalizationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(BranchSummary));
        OnPropertyChanged(nameof(RemotesSummary));

        if (IsBusy)
        {
            StatusMessage = _localization.Resolve(AvaloniaLocalizationKeys.OpeningRepository);
        }
        else if (HasError)
        {
            StatusMessage = _localization.Resolve(AvaloniaLocalizationKeys.UnableToOpenRepository);
        }
        else if (ActiveRepository is null)
        {
            StatusMessage = _localization.Resolve(AvaloniaLocalizationKeys.NoRepositoryOpen);
        }
    }

    partial void OnErrorMessageChanged(string? value)
        => OnPropertyChanged(nameof(HasError));

    [RelayCommand]
    private Task OpenFolderAsync()
        => OpenPickedFolderAsync();

    [RelayCommand]
    private Task OpenRecentAsync(Repository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return OpenPathAsync(repository.Path);
    }

    [RelayCommand]
    private Task RetryAsync()
        => _lastPath is null ? OpenPickedFolderAsync() : OpenPathAsync(_lastPath);

    [RelayCommand]
    private void ShowWelcome()
    {
        ActiveRepository = null;
        IsWelcome = true;
        ErrorMessage = null;
        StatusMessage = _localization.Resolve(AvaloniaLocalizationKeys.NoRepositoryOpen);
    }

    [RelayCommand]
    private void ShowRepositoryInformation()
    {
        if (ActiveRepository is null)
        {
            return;
        }

        IsWelcome = false;
        ErrorMessage = null;
        StatusMessage = ActiveRepository.Path;
    }

    private async Task OpenPickedFolderAsync()
    {
        await OpenPathAsync(await _openingService.PickFolderAsync(null, CancellationToken.None));
    }

    private async Task OpenPathAsync(string? path)
    {
        if (path is null)
        {
            return;
        }

        if (_openingCancellation is not null)
        {
            await _openingCancellation.CancelAsync();
        }

        _openingCancellation?.Dispose();
        _openingCancellation = new CancellationTokenSource();
        CancellationToken cancellationToken = _openingCancellation.Token;
        _lastPath = path;
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = _localization.Resolve(AvaloniaLocalizationKeys.OpeningRepository);

        try
        {
            ActiveRepository = await _openingService.OpenAsync(path, cancellationToken);
            IsWelcome = false;
            StatusMessage = ActiveRepository.Path;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusMessage = ActiveRepository is null
                ? _localization.Resolve(AvaloniaLocalizationKeys.NoRepositoryOpen)
                : ActiveRepository.Path;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            StatusMessage = _localization.Resolve(AvaloniaLocalizationKeys.UnableToOpenRepository);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
