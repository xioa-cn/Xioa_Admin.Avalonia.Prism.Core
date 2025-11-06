using Prism.Navigation;

namespace Ava.Xioa.Common.Utils;

public static class NavigationParametersHelper
{
    public static NavigationParameters TargetNavigationParameters(
        string targetNavigateView, string regionName)
    {
        var navigationParameters = new NavigationParameters();
        navigationParameters.Add("TargetView", targetNavigateView);
        navigationParameters.Add("RegionName", regionName);

        return navigationParameters;
    }

    public static NavigationParameters TargetNavigationParametersWithHeader(
        string targetNavigateView, string regionName, string header)
    {
        var navigationParameters = new NavigationParameters();
        navigationParameters.Add("TargetView", targetNavigateView);
        navigationParameters.Add("RegionName", regionName);
        navigationParameters.Add("Header", header);

        return navigationParameters;
    }
}