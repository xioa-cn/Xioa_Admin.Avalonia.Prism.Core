using System.Threading.Tasks;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Infrastructure.Services.Services.SplashServices;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ava.Xioa.InfrastructureModule.Views;

[RegisterForNavigation(navigationName: nameof(SplashView), region: AppRegions.MainRegion, zIndex: 9999)]
public partial class SplashView : UserControl
{
    public SplashView(ISplashServices splashServices)
    {
        this.DataContext = splashServices;
        this.Loaded += Splash_Loaded;
        InitializeComponent();
    }

    private void Splash_Loaded(object? sender, RoutedEventArgs e)
    {
        if (this.DataContext is IInitializedAsyncable initializedAsyncable)
        {
            Task.Run(async () => { await initializedAsyncable.InitializedAsync(); });
        }
    }
}