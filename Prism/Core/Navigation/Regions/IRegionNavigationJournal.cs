namespace Prism.Navigation.Regions;

public interface IRegionNavigationJournal
{
    event EventHandler? CurrentEntryChanged;

    int MaxEntryCount { get; set; }

    IRegionNavigationJournalEntry? CurrentEntry { get; }

    bool CanGoBack { get; }

    bool CanGoForward { get; }

    void Clear();

    void GoBack();

    void GoForward();

    Task<NavigationResult> GoBackAsync();

    Task<NavigationResult> GoBackAsync(INavigationParameters navigationParameters);

    Task<NavigationResult> GoForwardAsync();

    Task<NavigationResult> GoForwardAsync(INavigationParameters navigationParameters);
}