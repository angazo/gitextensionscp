namespace GitExtensions.Avalonia.Localization;

internal static class AvaloniaLocalizationKeys
{
    public const string BrowseForFolder = "Browse for folder";
    public const string Branch = "Branch";
    public const string ChooseAnotherFolder = "Choose another folder";
    public const string ChooseRecentRepository = "Choose a recent repository or browse for a folder.";
    public const string Close = "Close";
    public const string Error = "Error";
    public const string LoadingCommits = "Loading commits...";
    public const string MaximizeOrRestore = "Maximize or restore";
    public const string Minimize = "Minimize";
    public const string NoCommits = "No commits";
    public const string Ok = "OK";
    public const string OpenRepository = "Open repository";
    public const string RecentRepositories = "Recent repositories";
    public const string Repository = "Repository";
    public const string RepositoryInformation = "Repository information";
    public const string Remotes = "Remotes";
    public const string SelectFolder = "Select folder";
    public const string NoRepositoryOpen = "No repository open";
    public const string OpeningRepository = "Opening repository...";
    public const string Retry = "Retry";
    public const string UnableToOpenRepository = "Unable to open repository";
    public const string Welcome = "Welcome";

    public static IReadOnlyDictionary<string, string> SourceKeys { get; } = new Dictionary<string, string>
    {
        ["MaximizeOrRestore"] = MaximizeOrRestore,
        ["OpenRepository"] = OpenRepository,
        ["ChooseRecentRepository"] = ChooseRecentRepository,
        ["BrowseForFolder"] = BrowseForFolder,
        ["RecentRepositories"] = RecentRepositories,
        ["OpeningRepository"] = OpeningRepository,
        ["UnableToOpenRepository"] = UnableToOpenRepository,
        ["ChooseAnotherFolder"] = ChooseAnotherFolder,
        ["Error"] = Error,
        ["LoadingCommits"] = LoadingCommits,
        ["NoCommits"] = NoCommits,
        ["Ok"] = Ok,
        ["RepositoryInformation"] = RepositoryInformation,
        ["SelectFolder"] = SelectFolder,
    };

    public static IReadOnlyDictionary<string, string> Defaults { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [BrowseForFolder] = BrowseForFolder,
        [Branch] = Branch,
        [ChooseAnotherFolder] = ChooseAnotherFolder,
        [ChooseRecentRepository] = ChooseRecentRepository,
        [Close] = Close,
        [Error] = Error,
        [LoadingCommits] = LoadingCommits,
        [MaximizeOrRestore] = MaximizeOrRestore,
        [Minimize] = Minimize,
        [NoCommits] = NoCommits,
        [Ok] = Ok,
        [OpenRepository] = OpenRepository,
        [RecentRepositories] = RecentRepositories,
        [Repository] = Repository,
        [NoRepositoryOpen] = NoRepositoryOpen,
        [OpeningRepository] = OpeningRepository,
        [Retry] = Retry,
        [UnableToOpenRepository] = UnableToOpenRepository,
        [RepositoryInformation] = RepositoryInformation,
        [Remotes] = Remotes,
        [SelectFolder] = SelectFolder,
        [Welcome] = Welcome
    };
}
