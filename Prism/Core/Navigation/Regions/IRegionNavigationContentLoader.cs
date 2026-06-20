namespace Prism.Navigation.Regions;

public interface IRegionNavigationContentLoader
{
    object LoadContent(IRegion region, NavigationContext navigationContext);
}
