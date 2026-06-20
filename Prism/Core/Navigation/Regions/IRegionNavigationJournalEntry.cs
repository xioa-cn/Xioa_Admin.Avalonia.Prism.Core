using System;

namespace Prism.Navigation.Regions;

public interface IRegionNavigationJournalEntry
{
    string RegionName { get; }

    Uri Uri { get; }

    INavigationParameters Parameters { get; }
}