using System;

namespace Prism.Navigation.Regions;

public sealed class RegionNavigationJournalEntry : IRegionNavigationJournalEntry
{
    public RegionNavigationJournalEntry()
        : this(string.Empty, new Uri("/", UriKind.Relative), new NavigationParameters())
    {
    }

    public RegionNavigationJournalEntry(string regionName, Uri uri, INavigationParameters parameters)
    {
        RegionName = regionName;
        Uri = uri;
        Parameters = parameters;
    }

    public string RegionName { get; }

    public Uri Uri { get; }

    public INavigationParameters Parameters { get; }
}